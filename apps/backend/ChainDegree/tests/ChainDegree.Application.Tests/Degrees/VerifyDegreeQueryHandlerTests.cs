using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Blockchain;
using ChainDegree.Core.Application.Abstractions.Crypto;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Degrees.Queries.VerifyDegree;
using ChainDegree.Core.Domain.Degrees.Enums;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
using ChainDegree.Core.Domain.SharedKernel.Enums;
using ChainDegree.SharedKernel.DomainErrors.Degrees.Degree;
using ChainDegree.SharedKernel.Result;
using ChainDegree.SharedKernel.DomainErrors.Blockchain;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChainDegree.Application.Tests.Degrees
{
    public class VerifyDegreeQueryHandlerTests
    {
        private readonly Mock<IDegreeRepository> _mockRepo;
        private readonly Mock<IBlockchainService> _mockBlockchain;
        private readonly Mock<IMerkleTreeService> _mockMerkle;
        private readonly Mock<IDegreeHashService> _mockHash;
        private readonly Mock<IBehaviorLogService> _mockBehaviorLog;
        private readonly Mock<ILogger<VerifyDegreeQueryHandler>> _mockLogger;
        private readonly VerifyDegreeQueryHandler _handler;

        public VerifyDegreeQueryHandlerTests()
        {
            _mockRepo = new Mock<IDegreeRepository>();
            _mockBlockchain = new Mock<IBlockchainService>();
            _mockMerkle = new Mock<IMerkleTreeService>();
            _mockHash = new Mock<IDegreeHashService>();
            _mockBehaviorLog = new Mock<IBehaviorLogService>();
            _mockLogger = new Mock<ILogger<VerifyDegreeQueryHandler>>();

            _handler = new VerifyDegreeQueryHandler(
                _mockRepo.Object,
                _mockBlockchain.Object,
                _mockMerkle.Object,
                _mockHash.Object,
                _mockBehaviorLog.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_DegreeNotFound_ReturnsFailure()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetVerificationSnapshotAsync("DEG-001", null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((VerificationSnapshot?)null);

            var query = new VerifyDegreeQuery("DEG-001");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(DegreeErrors.NotFound, result.Error);
        }

        [Fact]
        public async Task Handle_VersionNotFound_ReturnsFailure()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetVerificationSnapshotAsync("DEG-001", 3, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((VerificationSnapshot?)null);

            var query = new VerifyDegreeQuery("DEG-001", 3);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(DegreeErrors.UnsupportedVersion, result.Error);
        }

        [Fact]
        public async Task Handle_IssuedAtMismatch_ReturnsCryptoHashMismatch()
        {
            // Arrange
            var snapshot = new VerificationSnapshot(
                degreeId: Guid.NewGuid(),
                dataHash: "hash123",
                salt: "salt123",
                plainDataJson: "{}",
                txHash: "0x123",
                merkleProofJson: "{}",
                version: 1,
                status: StatusEnum.Confirmed,
                studentFullName: "Nguyen Van A",
                major: "IT",
                classification: "Gioi",
                studentId: Guid.NewGuid(),
                issuedAt: new DateTime(2026, 7, 1)
            );

            _mockRepo.Setup(r => r.GetVerificationSnapshotAsync("DEG-001", null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(snapshot);

            var query = new VerifyDegreeQuery("DEG-001", null, new DateTime(2026, 7, 2));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(DegreeErrors.CryptoHashMismatch, result.Error);
        }

        [Fact]
        public async Task Handle_RevokedDegree_ReturnsSuccessWithVerifiedFalse()
        {
            // Arrange
            var snapshot = new VerificationSnapshot(
                degreeId: Guid.NewGuid(),
                dataHash: "hash123",
                salt: "salt123",
                plainDataJson: "{}",
                txHash: "0x123",
                merkleProofJson: "{}",
                version: 1,
                status: StatusEnum.Revoked,
                studentFullName: "Nguyen Van A",
                major: "IT",
                classification: "Gioi",
                studentId: Guid.NewGuid(),
                issuedAt: new DateTime(2026, 7, 1)
            );

            _mockRepo.Setup(r => r.GetVerificationSnapshotAsync("DEG-001", null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(snapshot);

            var query = new VerifyDegreeQuery("DEG-001");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.Value.Verified);
            Assert.Equal("Revoked", result.Value.Status);
        }

        [Fact]
        public async Task Handle_CryptoHashMismatch_ReturnsFailure()
        {
            // Arrange
            var snapshot = new VerificationSnapshot(
                degreeId: Guid.NewGuid(),
                dataHash: "hash123",
                salt: "salt123",
                plainDataJson: "{}",
                txHash: "0x123",
                merkleProofJson: "{}",
                version: 1,
                status: StatusEnum.Confirmed,
                studentFullName: "Nguyen Van A",
                major: "IT",
                classification: "Gioi",
                studentId: Guid.NewGuid(),
                issuedAt: new DateTime(2026, 7, 1)
            );

            _mockRepo.Setup(r => r.GetVerificationSnapshotAsync("DEG-001", null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(snapshot);

            _mockHash.Setup(h => h.CalculateHashAsync(It.IsAny<DegreeData>(), "salt123", It.IsAny<CancellationToken>()))
                     .ReturnsAsync("differentHash");

            var query = new VerifyDegreeQuery("DEG-001");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(DegreeErrors.CryptoHashMismatch, result.Error);
        }

        [Fact]
        public async Task Handle_BlockchainRootNotFound_ReturnsBlockchainInvalid()
        {
            // Arrange
            var snapshot = new VerificationSnapshot(
                degreeId: Guid.NewGuid(),
                dataHash: "hash123",
                salt: "salt123",
                plainDataJson: "{}",
                txHash: "0x123",
                merkleProofJson: "{}",
                version: 1,
                status: StatusEnum.Confirmed,
                studentFullName: "Nguyen Van A",
                major: "IT",
                classification: "Gioi",
                studentId: Guid.NewGuid(),
                issuedAt: new DateTime(2026, 7, 1)
            );

            _mockRepo.Setup(r => r.GetVerificationSnapshotAsync("DEG-001", null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(snapshot);

            _mockHash.Setup(h => h.CalculateHashAsync(It.IsAny<DegreeData>(), "salt123", It.IsAny<CancellationToken>()))
                     .ReturnsAsync("hash123");

            var fixedBatchId = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetBatchIdByDegreeIdAsync(snapshot.DegreeId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(fixedBatchId);

            _mockBlockchain.Setup(b => b.GetBatchAsync(fixedBatchId.ToString(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(Result<BatchMetadata>.Success(new BatchMetadata("0x0", 0, "0x0", "Issue", false)));

            var query = new VerifyDegreeQuery("DEG-001");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(DegreeErrors.BlockchainInvalid, result.Error);
        }

        [Fact]
        public async Task Handle_MerkleProofVerificationFails_ReturnsBlockchainInvalid()
        {
            // Arrange
            var snapshot = new VerificationSnapshot(
                degreeId: Guid.NewGuid(),
                dataHash: "hash123",
                salt: "salt123",
                plainDataJson: "{}",
                txHash: "0x123",
                merkleProofJson: "{\"LeafIndex\":0,\"LeafHash\":\"hash123\",\"ProofHashes\":[],\"ProofDirections\":[]}",
                version: 1,
                status: StatusEnum.Confirmed,
                studentFullName: "Nguyen Van A",
                major: "IT",
                classification: "Gioi",
                studentId: Guid.NewGuid(),
                issuedAt: new DateTime(2026, 7, 1)
            );

            _mockRepo.Setup(r => r.GetVerificationSnapshotAsync("DEG-001", null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(snapshot);

            _mockHash.Setup(h => h.CalculateHashAsync(It.IsAny<DegreeData>(), "salt123", It.IsAny<CancellationToken>()))
                     .ReturnsAsync("hash123");

            var fixedBatchId = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetBatchIdByDegreeIdAsync(snapshot.DegreeId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(fixedBatchId);

            _mockBlockchain.Setup(b => b.GetBatchAsync(fixedBatchId.ToString(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(Result<BatchMetadata>.Success(new BatchMetadata("merkleRootOnChain", 123456, "0x0", "Issue", true)));

            _mockMerkle.Setup(m => m.VerifyProof("hash123", It.IsAny<MerkleProofData>(), "merkleRootOnChain"))
                       .Returns(false);

            var query = new VerifyDegreeQuery("DEG-001");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(DegreeErrors.BlockchainInvalid, result.Error);
        }

        [Fact]
        public async Task Handle_ValidVerification_ReturnsSuccess()
        {
            // Arrange
            var snapshot = new VerificationSnapshot(
                degreeId: Guid.NewGuid(),
                dataHash: "hash123",
                salt: "salt123",
                plainDataJson: "{}",
                txHash: "0x123",
                merkleProofJson: "{\"LeafIndex\":0,\"LeafHash\":\"hash123\",\"ProofHashes\":[],\"ProofDirections\":[]}",
                version: 1,
                status: StatusEnum.Confirmed,
                studentFullName: "Nguyen Van A",
                major: "IT",
                classification: "Gioi",
                studentId: Guid.NewGuid(),
                issuedAt: new DateTime(2026, 7, 1)
            );

            _mockRepo.Setup(r => r.GetVerificationSnapshotAsync("DEG-001", null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(snapshot);

            _mockHash.Setup(h => h.CalculateHashAsync(It.IsAny<DegreeData>(), "salt123", It.IsAny<CancellationToken>()))
                     .ReturnsAsync("hash123");

            var fixedBatchId = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetBatchIdByDegreeIdAsync(snapshot.DegreeId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(fixedBatchId);

            _mockBlockchain.Setup(b => b.GetBatchAsync(fixedBatchId.ToString(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(Result<BatchMetadata>.Success(new BatchMetadata("merkleRootOnChain", 123456, "0x0", "Issue", true)));

            _mockMerkle.Setup(m => m.VerifyProof("hash123", It.IsAny<MerkleProofData>(), "merkleRootOnChain"))
                       .Returns(true);

            var query = new VerifyDegreeQuery("DEG-001");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value.Verified);
            Assert.Equal("Confirmed", result.Value.Status);
            Assert.Equal("Nguyen Van A", result.Value.StudentFullName);

            _mockBehaviorLog.Verify(b => b.LogAsync(
                ActionTypeEnum.VERIFY_DEGREE,
                "DEGREES",
                Guid.Parse("00000000-0000-0000-0000-000000000002"),
                null,
                It.Is<string>(json => json.Contains("Verified")),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
