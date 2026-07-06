using System;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
using ChainDegree.Core.Domain.Degrees.Interfaces;
using ChainDegree.SharedKernel.Common.Error;
using ChainDegree.SharedKernel.DomainErrors.Degrees.Degree;
using ChainDegree.SharedKernel.Result;
using Moq;
using Xunit;

namespace ChainDegree.Domain.Tests.Degrees
{
    public class CryptoSnapshotTests
    {
        private readonly Mock<IHashService> _mockHashService;

        public CryptoSnapshotTests()
        {
            _mockHashService = new Mock<IHashService>();
        }

        [Fact]
        public void Create_GeneratesSaltAndHash_ReturnsSuccess()
        {
            // Arrange
            var plainText = "{\"classification\":\"Gioi\",\"degreeCode\":\"DEG-2026-000001\",\"major\":\"Software Engineering\",\"studentId\":\"550e8400-e29b-41d4-a716-446655440000\"}";
            var salt = "mocked_salt";
            var hash = "mocked_hash";

            _mockHashService.Setup(h => h.GenerateSalt()).Returns(Result<string>.Success(salt));
            _mockHashService.Setup(h => h.HashData(plainText, salt)).Returns(Result<string>.Success(hash));

            // Act
            var result = CryptoSnapshot.Create(plainText, _mockHashService.Object);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(plainText, result.Value.PlainDataJson);
            Assert.Equal(salt, result.Value.Salt);
            Assert.Equal(hash, result.Value.DataHashLocal);
        }

        [Fact]
        public void Create_WithEmptyPlainText_ReturnsFailure()
        {
            // Act
            var result = CryptoSnapshot.Create("", _mockHashService.Object);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(CryptoErrors.EmptyPlainText, result.Error);
        }

        [Fact]
        public void VerifyLocal_WithMatchingHash_ReturnsSuccess()
        {
            // Arrange
            var plainText = "some_data";
            var salt = "some_salt";
            var hash = "some_hash";

            _mockHashService.Setup(h => h.GenerateSalt()).Returns(Result<string>.Success(salt));
            _mockHashService.Setup(h => h.HashData(plainText, salt)).Returns(Result<string>.Success(hash));

            var snapshotResult = CryptoSnapshot.Create(plainText, _mockHashService.Object);
            var snapshot = snapshotResult.Value;

            // Act
            var verificationResult = snapshot.VerifyLocal(hash);

            // Assert
            Assert.True(verificationResult.IsSuccess);
        }

        [Fact]
        public void VerifyLocal_WithMismatchedHash_ReturnsFailure()
        {
            // Arrange
            var plainText = "some_data";
            var salt = "some_salt";
            var hash = "some_hash";

            _mockHashService.Setup(h => h.GenerateSalt()).Returns(Result<string>.Success(salt));
            _mockHashService.Setup(h => h.HashData(plainText, salt)).Returns(Result<string>.Success(hash));

            var snapshotResult = CryptoSnapshot.Create(plainText, _mockHashService.Object);
            var snapshot = snapshotResult.Value;

            // Act
            var verificationResult = snapshot.VerifyLocal("different_hash");

            // Assert
            Assert.True(verificationResult.IsFailure);
            Assert.Equal(DegreeErrors.InvalidCryptoSnapshot, verificationResult.Error);
        }
    }
}
