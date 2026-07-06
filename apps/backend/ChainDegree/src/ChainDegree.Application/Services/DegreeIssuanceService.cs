using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Abstractions.Policies;
using ChainDegree.Core.Application.Abstractions.Services;
using ChainDegree.Core.Application.Degrees.Commands.IssueDegree;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
using ChainDegree.Core.Domain.Degrees.Interfaces;
using ChainDegree.SharedKernel.Common.Log;
using ChainDegree.SharedKernel.DomainErrors.Degrees.Degree;
using ChainDegree.SharedKernel.Result;
using Microsoft.Extensions.Logging;

namespace ChainDegree.Core.Application.Services
{
    public class DegreeIssuanceService : IDegreeIssuanceService
    {
        private readonly IDegreeRepository _degreeRepository;
        private readonly IDegreeDuplicatePolicy _duplicatePolicy;
        private readonly IJsonCanonicalizer _canonicalizer;
        private readonly IHashService _hashService;
        private readonly ILogger<DegreeIssuanceService> _logger;

        public DegreeIssuanceService(
            IDegreeRepository degreeRepository,
            IDegreeDuplicatePolicy duplicatePolicy,
            IJsonCanonicalizer canonicalizer,
            IHashService hashService,
            ILogger<DegreeIssuanceService> _logger)
        {
            _degreeRepository = degreeRepository;
            _duplicatePolicy = duplicatePolicy;
            _canonicalizer = canonicalizer;
            _hashService = hashService;
            this._logger = _logger;
        }

        public async Task<PartialResult<Degree, IssueDegreeFailureDto>> IssueDegreesAsync(
            Guid institutionId,
            Guid registrarId,
            IReadOnlyList<IssueDegreeItemDto> items,
            CancellationToken ct = default)
        {
            var successes = new List<Degree>();
            var failures = new List<IssueDegreeFailureDto>();

            var totalCount = await _degreeRepository.GetTotalCountAsync(ct);

            foreach (var item in items)
            {
                // 1. Check duplicate via policy
                var isDuplicate = await _duplicatePolicy.IsDuplicateAsync(
                    institutionId,
                    item.StudentId,
                    item.Major,
                    item.IssuedAt.Year,
                    ct);

                if (isDuplicate)
                {
                    _logger.LogWarning("[{LogCode}] Duplicate degree detected for student {StudentId} at institution {InstitutionId} with major {Major} in year {Year}",
                        DegreeLogs.Degree_DuplicateDetected.Code,
                        item.StudentId,
                        institutionId,
                        item.Major,
                        item.IssuedAt.Year);

                    failures.Add(new IssueDegreeFailureDto(item.StudentId, item.Major, DegreeErrors.DuplicateDegree.Message));
                    continue;
                }

                // 2. Build plain data object for canonicalization
                // The canonical JSON must contain: studentId, degreeCode, major, classification, issuedAt (ISO 8601 UTC string)
                var tempIndex = totalCount + successes.Count;
                var generatedCode = $"DEG-{DateTime.UtcNow.Year}-{(tempIndex + 1):D6}";
                
                var plainDataObj = new
                {
                    classification = item.Classification,
                    degreeCode = generatedCode,
                    issuedAt = item.IssuedAt.ToString("o"),
                    major = item.Major,
                    studentId = item.StudentId.ToString()
                };

                // 3. Canonicalize plain data
                var canonResult = _canonicalizer.Canonicalize(plainDataObj);
                if (canonResult.IsFailure)
                {
                    failures.Add(new IssueDegreeFailureDto(item.StudentId, item.Major, canonResult.Error.Message));
                    continue;
                }

                // 4. Create CryptoSnapshot
                var cryptoResult = CryptoSnapshot.Create(canonResult.Value, _hashService);
                if (cryptoResult.IsFailure)
                {
                    failures.Add(new IssueDegreeFailureDto(item.StudentId, item.Major, cryptoResult.Error.Message));
                    continue;
                }

                _logger.LogInformation("[{LogCode}] {Message}. StudentId={StudentId}, Hash={Hash}",
                    DegreeLogs.Degree_CryptoHashGenerated.Code,
                    DegreeLogs.Degree_CryptoHashGenerated.Message,
                    item.StudentId,
                    cryptoResult.Value.DataHashLocal);

                // 5. Create Domain Entity
                var degreeResult = Degree.Create(
                    tempIndex,
                    institutionId,
                    registrarId,
                    item.StudentId,
                    item.Major,
                    item.Classification,
                    cryptoResult.Value);

                if (degreeResult.IsFailure)
                {
                    failures.Add(new IssueDegreeFailureDto(item.StudentId, item.Major, degreeResult.Error.Message));
                    continue;
                }

                successes.Add(degreeResult.Value);
            }

            return PartialResult<Degree, IssueDegreeFailureDto>.Create(successes, failures);
        }
    }
}
