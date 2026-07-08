using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Abstractions.Policies;
using ChainDegree.Core.Application.Abstractions.Services;
using ChainDegree.Core.Application.Abstractions.Crypto;
using ChainDegree.Core.Application.Degrees.Commands.IssueDegree;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
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
        private readonly IDegreeHashService _degreeHashService;
        private readonly ILogger<DegreeIssuanceService> _logger;

        public DegreeIssuanceService(
            IDegreeRepository degreeRepository,
            IDegreeDuplicatePolicy duplicatePolicy,
            IDegreeHashService degreeHashService,
            ILogger<DegreeIssuanceService> logger)
        {
            _degreeRepository = degreeRepository;
            _duplicatePolicy = duplicatePolicy;
            _degreeHashService = degreeHashService;
            _logger = logger;
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

                // 2. Build plain data object for canonicalization and hashing
                var tempIndex = totalCount + successes.Count;
                var generatedCode = $"DEG-{DateTime.UtcNow.Year}-{(tempIndex + 1):D6}";
                
                var degreeData = new DegreeData(
                    generatedCode,
                    item.StudentId,
                    item.Major,
                    item.Classification,
                    item.IssuedAt);

                // 3. Create CryptoSnapshot via IDegreeHashService
                CryptoSnapshot cryptoSnapshot;
                try
                {
                    cryptoSnapshot = await _degreeHashService.RecalculateAsync(degreeData, ct);
                }
                catch (Exception ex)
                {
                    failures.Add(new IssueDegreeFailureDto(item.StudentId, item.Major, ex.Message));
                    continue;
                }

                _logger.LogInformation("[{LogCode}] {Message}. StudentId={StudentId}, Hash={Hash}",
                    DegreeLogs.Degree_CryptoHashGenerated.Code,
                    DegreeLogs.Degree_CryptoHashGenerated.Message,
                    item.StudentId,
                    cryptoSnapshot.DataHashLocal);

                // 4. Create Domain Entity
                var degreeResult = Degree.Create(
                    tempIndex,
                    institutionId,
                    registrarId,
                    item.StudentId,
                    item.Major,
                    item.Classification,
                    cryptoSnapshot);

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
