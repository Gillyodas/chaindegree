using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Crypto;
using ChainDegree.Core.Domain.Degrees.Interfaces;
using ChainDegree.Core.Domain.Degrees.ValueObjects;

namespace ChainDegree.Core.Application.Services
{
    public class DegreeHashService : IDegreeHashService
    {
        private readonly IJsonCanonicalizer _canonicalizer;
        private readonly IHashService _hashService;

        public DegreeHashService(IJsonCanonicalizer canonicalizer, IHashService hashService)
        {
            _canonicalizer = canonicalizer;
            _hashService = hashService;
        }

        public Task<CryptoSnapshot> RecalculateAsync(DegreeData data, CancellationToken ct = default)
        {
            var utcIssuedAt = data.IssuedAt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(data.IssuedAt, DateTimeKind.Utc)
                : data.IssuedAt.ToUniversalTime();

            var plainDataObj = new
            {
                classification = data.Classification,
                degreeCode = data.DegreeCode,
                issuedAt = utcIssuedAt.ToString("o"),
                major = data.Major,
                studentId = data.StudentId.ToString()
            };

            var canonResult = _canonicalizer.Canonicalize(plainDataObj);
            if (canonResult.IsFailure)
            {
                throw new InvalidOperationException($"Canonicalization failed: {canonResult.Error.Message}");
            }

            var cryptoResult = CryptoSnapshot.Create(canonResult.Value, _hashService);
            if (cryptoResult.IsFailure)
            {
                throw new InvalidOperationException($"Crypto snapshot creation failed: {cryptoResult.Error.Message}");
            }

            return Task.FromResult(cryptoResult.Value);
        }

        public Task<string> CalculateHashAsync(DegreeData data, string salt, CancellationToken ct = default)
        {
            var utcIssuedAt = data.IssuedAt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(data.IssuedAt, DateTimeKind.Utc)
                : data.IssuedAt.ToUniversalTime();

            var plainDataObj = new
            {
                classification = data.Classification,
                degreeCode = data.DegreeCode,
                issuedAt = utcIssuedAt.ToString("o"),
                major = data.Major,
                studentId = data.StudentId.ToString()
            };

            var canonResult = _canonicalizer.Canonicalize(plainDataObj);
            if (canonResult.IsFailure)
            {
                throw new InvalidOperationException($"Canonicalization failed: {canonResult.Error.Message}");
            }

            var hashResult = _hashService.HashData(canonResult.Value, salt);
            if (hashResult.IsFailure)
            {
                throw new InvalidOperationException($"Hashing failed: {hashResult.Error.Message}");
            }

            return Task.FromResult(hashResult.Value);
        }
    }
}
