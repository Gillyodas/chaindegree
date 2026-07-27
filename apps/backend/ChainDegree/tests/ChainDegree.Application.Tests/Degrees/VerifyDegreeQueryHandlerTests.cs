using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Blockchain;
using ChainDegree.Core.Application.Abstractions.Crypto;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Degrees.Queries.VerifyDegree;
using ChainDegree.Core.Domain.Degrees.Enums;
using ChainDegree.Core.Domain.Degrees.Interfaces;
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
        private readonly Mock<IJsonCanonicalizer> _mockCanonicalizer;
        private readonly Mock<IHashService> _mockHashService;
        private readonly Mock<IBehaviorLogService> _mockBehaviorLog;
        private readonly Mock<ILogger<VerifyDegreeQueryHandler>> _mockLogger;
        private readonly VerifyDegreeQueryHandler _handler;

        public VerifyDegreeQueryHandlerTests()
        {
            _mockRepo = new Mock<IDegreeRepository>();
            _mockBlockchain = new Mock<IBlockchainService>();
            _mockMerkle = new Mock<IMerkleTreeService>();
            _mockHash = new Mock<IDegreeHashService>();
            _mockCanonicalizer = new Mock<IJsonCanonicalizer>();
            _mockHashService = new Mock<IHashService>();
            _mockBehaviorLog = new Mock<IBehaviorLogService>();
            _mockLogger = new Mock<ILogger<VerifyDegreeQueryHandler>>();

            _handler = new VerifyDegreeQueryHandler(
                _mockRepo.Object,
                _mockBlockchain.Object,
                _mockMerkle.Object,
                _mockHash.Object,
                _mockCanonicalizer.Object,
                _mockHashService.Object,
                _mockBehaviorLog.Object,
                _mockLogger.Object
            );
        }

        private static VerificationSnapshot CreateTestSnapshot(
            Guid? degreeId = null,
            string dataHash = "hash123",
            string salt = "0123456789abcdef",
            StatusEnum status = StatusEnum.Confirmed,
            string txHash = "0x123",
            string merkleProofJson = "{\"LeafIndex\":0,\"LeafHash\":\"hash123\",\"ProofHashes\":[],\"ProofDirections\":[]}",
            int version = 1)
        {
            return new VerificationSnapshot(
                degreeId: degreeId ?? Guid.NewGuid(),
                dataHash: dataHash,
                salt: salt,
                plainDataJson: "{\"classification\":\"Gioi\",\"degreeCode\":\"DEG-001\",\"major\":\"IT\"}",
                txHash: txHash,
                merkleProofJson: merkleProofJson,
                version: version,
                status: status,
                studentFullName: "Nguyen Van A",
                major: "IT",
                classification: "Gioi",
                studentId: Guid.NewGuid(),
                issuedAt: new DateTime(2026, 7, 1),
                institutionName: "Test Institution",
                institutionId: Guid.NewGuid()
            );
        }

        [Fact]
        public async Task Handle_DegreeNotFound_ReturnsFailure_AndDoesNotWriteBehaviorLog()
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

            // Verify selective logging: 404 should NOT write to BehaviorLogs table
            _mockBehaviorLog.Verify(b => b.LogAsync(
                It.IsAny<ActionTypeEnum>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
                Times.Never);
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
            var snapshot = CreateTestSnapshot();
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
            var snapshot = CreateTestSnapshot(status: StatusEnum.Revoked);

            _mockRepo.Setup(r => r.GetVerificationSnapshotAsync("DEG-001", null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(snapshot);

            var query = new VerifyDegreeQuery("DEG-001");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.Value.Verified);
            Assert.Equal("Revoked", result.Value.Status);
            Assert.Equal("Test Institution", result.Value.InstitutionName);

            _mockBehaviorLog.Verify(b => b.LogAsync(
                ActionTypeEnum.VERIFY_DEGREE,
                "DEGREES",
                snapshot.DegreeId,
                null,
                It.Is<string>(json => json != null && json.Contains("Revoked")),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_CryptoHashMismatch_ReturnsFailure()
        {
            // Arrange
            var snapshot = CreateTestSnapshot();

            _mockRepo.Setup(r => r.GetVerificationSnapshotAsync("DEG-001", null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(snapshot);

            _mockHash.Setup(h => h.CalculateHashAsync(It.IsAny<DegreeData>(), snapshot.Salt, It.IsAny<CancellationToken>()))
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
            var snapshot = CreateTestSnapshot();

            _mockRepo.Setup(r => r.GetVerificationSnapshotAsync("DEG-001", null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(snapshot);

            _mockHash.Setup(h => h.CalculateHashAsync(It.IsAny<DegreeData>(), snapshot.Salt, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(snapshot.DataHash);

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
            var snapshot = CreateTestSnapshot();

            _mockRepo.Setup(r => r.GetVerificationSnapshotAsync("DEG-001", null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(snapshot);

            _mockHash.Setup(h => h.CalculateHashAsync(It.IsAny<DegreeData>(), snapshot.Salt, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(snapshot.DataHash);

            var fixedBatchId = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetBatchIdByDegreeIdAsync(snapshot.DegreeId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(fixedBatchId);

            _mockBlockchain.Setup(b => b.GetBatchAsync(fixedBatchId.ToString(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(Result<BatchMetadata>.Success(new BatchMetadata("merkleRootOnChain", 123456, "0x0", "Issue", true)));

            _mockMerkle.Setup(m => m.VerifyProof(snapshot.DataHash, It.IsAny<MerkleProofData>(), "merkleRootOnChain"))
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
            var snapshot = CreateTestSnapshot();

            _mockRepo.Setup(r => r.GetVerificationSnapshotAsync("DEG-001", null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(snapshot);

            _mockHash.Setup(h => h.CalculateHashAsync(It.IsAny<DegreeData>(), snapshot.Salt, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(snapshot.DataHash);

            var fixedBatchId = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetBatchIdByDegreeIdAsync(snapshot.DegreeId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(fixedBatchId);

            _mockBlockchain.Setup(b => b.GetBatchAsync(fixedBatchId.ToString(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(Result<BatchMetadata>.Success(new BatchMetadata("merkleRootOnChain", 123456, "0x0", "Issue", true)));

            _mockMerkle.Setup(m => m.VerifyProof(snapshot.DataHash, It.IsAny<MerkleProofData>(), "merkleRootOnChain"))
                       .Returns(true);

            var query = new VerifyDegreeQuery("DEG-001");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value.Verified);
            Assert.Equal("Confirmed", result.Value.Status);
            Assert.Equal("Nguyen Van A", result.Value.StudentFullName);
            Assert.Equal("Test Institution", result.Value.InstitutionName);
            Assert.Equal(VerificationSource.Blockchain_Merkle_Root, result.Value.VerificationSource);

            _mockBehaviorLog.Verify(b => b.LogAsync(
                ActionTypeEnum.VERIFY_DEGREE,
                "DEGREES",
                snapshot.DegreeId,
                null,
                It.Is<string>(json => json.Contains("Verified")),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_DirectDataMode_ValidData_ReturnsVerified()
        {
            // Arrange
            var snapshot = CreateTestSnapshot(dataHash: "expectedDirectHash");
            string salt16 = "0123456789abcdef";
            string rawDataJson = "{\"major\":\"IT\",\"classification\":\"Gioi\"}";

            _mockRepo.Setup(r => r.GetVerificationSnapshotAsync("DEG-001", null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(snapshot);

            _mockCanonicalizer.Setup(c => c.Canonicalize(It.IsAny<JsonNode>()))
                              .Returns(Result<string>.Success("{\"classification\":\"Gioi\",\"major\":\"IT\"}"));

            _mockHashService.Setup(h => h.HashData("{\"classification\":\"Gioi\",\"major\":\"IT\"}", salt16))
                            .Returns(Result<string>.Success("expectedDirectHash"));

            var fixedBatchId = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetBatchIdByDegreeIdAsync(snapshot.DegreeId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(fixedBatchId);

            _mockBlockchain.Setup(b => b.GetBatchAsync(fixedBatchId.ToString(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(Result<BatchMetadata>.Success(new BatchMetadata("merkleRootOnChain", 123456, "0x0", "Issue", true)));

            _mockMerkle.Setup(m => m.VerifyProof("expectedDirectHash", It.IsAny<MerkleProofData>(), "merkleRootOnChain"))
                       .Returns(true);

            var query = new VerifyDegreeQuery("DEG-001", null, null, rawDataJson, salt16);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.Value.Verified);
            _mockCanonicalizer.Verify(c => c.Canonicalize(It.IsAny<JsonNode>()), Times.Once);
        }

        [Fact]
        public async Task Handle_DirectDataMode_InvalidSaltLength_ReturnsError()
        {
            // Arrange
            var snapshot = CreateTestSnapshot();

            _mockRepo.Setup(r => r.GetVerificationSnapshotAsync("DEG-001", null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(snapshot);

            var query = new VerifyDegreeQuery("DEG-001", null, null, "{\"major\":\"IT\"}", "shortsalt");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(DegreeErrors.InvalidSaltFormat, result.Error);
        }
    }
}
