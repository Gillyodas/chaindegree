using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.Core.Domain.Degrees.Interfaces;
using ChainDegree.SharedKernel.Common.Error;
using ChainDegree.SharedKernel.DomainErrors.Degrees.Degree;
using ChainDegree.SharedKernel.Result;

namespace ChainDegree.Core.Domain.Degrees.ValueObjects
{
    public class CryptoSnapshot
    {
        public string PlainDataJson { get; private set; } = null!;
        public string Salt { get; private set; } = null!;
        public string DataHashLocal { get; private set; } = null!;

        private CryptoSnapshot(string plainDataJson, string salt, string dataHashLocal)
        {
            PlainDataJson = plainDataJson;
            Salt = salt;
            DataHashLocal = dataHashLocal;
        }

        private CryptoSnapshot() { }

        public static Result<CryptoSnapshot> Create(string plainDataJson, IHashService hashService)
        {
            if (string.IsNullOrWhiteSpace(plainDataJson))
                return Result<CryptoSnapshot>.Failure(CryptoErrors.EmptyPlainText);

            var genSaltResult = hashService.GenerateSalt();
            if (genSaltResult.IsFailure) return Result<CryptoSnapshot>.Failure(genSaltResult.Error);
            var salt = genSaltResult.Value;

            var hashResult = hashService.HashData(plainDataJson, salt);
            if (hashResult.IsFailure) return Result<CryptoSnapshot>.Failure(hashResult.Error);
            var dataHashLocal = hashResult.Value;

            var cryptoSnapshot = new CryptoSnapshot(plainDataJson, salt, dataHashLocal);

            return Result<CryptoSnapshot>.Success(cryptoSnapshot);
        }

        public Result VerifyLocal(string calculatedHash)
        {
            if (string.IsNullOrWhiteSpace(calculatedHash)) return Result.Failure(DegreeErrors.EmptyCryptoSnapshot);

            return string.Equals(DataHashLocal, calculatedHash, StringComparison.OrdinalIgnoreCase)
                ? Result.Success()
                : Result.Failure(DegreeErrors.InvalidCryptoSnapshot);
        }
    }
}
