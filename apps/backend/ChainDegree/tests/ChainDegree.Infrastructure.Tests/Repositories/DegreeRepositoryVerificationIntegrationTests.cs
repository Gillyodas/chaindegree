using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Blockchain;
using ChainDegree.Core.Application.Abstractions.Crypto;
using ChainDegree.Core.Application.Degrees.Queries.VerifyDegree;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Degrees.Entities;
using ChainDegree.Core.Domain.Degrees.Enums;
using ChainDegree.Core.Domain.Degrees.Interfaces;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
using ChainDegree.Core.Domain.Students;
using ChainDegree.Core.Domain.Universities;
using ChainDegree.Core.Infrastructure.Cryptography.Services;
using ChainDegree.Core.Infrastructure.Persistence;
using ChainDegree.Core.Infrastructure.Persistence.Entities;
using ChainDegree.Core.Infrastructure.Persistence.Locking;
using ChainDegree.Core.Infrastructure.Persistence.Repositories;
using ChainDegree.SharedKernel.Result;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ChainDegree.Infrastructure.Tests.Repositories
{
    public class DegreeRepositoryVerificationIntegrationTests : IDisposable
    {
        private readonly ChainDegreeDbContext _dbContext;
        private readonly DegreeRepository _degreeRepository;
        private readonly MerkleTreeService _merkleTreeService;
        private readonly Sha256HashService _hashService;

        public DegreeRepositoryVerificationIntegrationTests()
        {
            var dbOptions = new DbContextOptionsBuilder<ChainDegreeDbContext>()
                .UseInMemoryDatabase(databaseName: $"ChainDegree_TestDb_{Guid.NewGuid()}")
                .Options;

            var mockCurrentUser = new Mock<ICurrentUserAccessor>();
            mockCurrentUser.Setup(x => x.InstitutionId).Returns((Guid?)null);

            _dbContext = new ChainDegreeDbContext(dbOptions, mockCurrentUser.Object, NullLogger<ChainDegreeDbContext>.Instance);
            var mockLockStrategy = new Mock<IPendingDegreeLockStrategy>();

            _degreeRepository = new DegreeRepository(_dbContext, mockLockStrategy.Object);
            _merkleTreeService = new MerkleTreeService();
            _hashService = new Sha256HashService();
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        [Fact]
        public async Task GetVerificationSnapshotAsync_WithBatchDegreeRecordSerializedAsMerkleProofData_ConstructsValidSnapshotWithProofJson()
        {
            // Arrange: Setup Institution, Student, and Confirmed Degree in DB
            var institution = EducationInstitution.Create("UIT", "University of Information Technology", "contact@uit.edu.vn");
            var studentResult = Student.Create("STU-001", "John Doe", "john@example.com", Guid.NewGuid());
            Assert.True(studentResult.IsSuccess);
            var student = studentResult.Value;

            _dbContext.EducationInstitutions.Add(institution);
            _dbContext.Students.Add(student);

            string plainDataJson = "{\"classification\":\"Gioi\",\"degreeCode\":\"DEG-2026-000010\",\"major\":\"Computer Science\"}";
            string salt = _hashService.GenerateSalt().Value;
            string dataHash = _hashService.HashData(plainDataJson, salt).Value;

            var cryptoSnapshot = CryptoSnapshot.Reconstruct(plainDataJson, salt, dataHash);
            var degreeResult = Degree.Create(
                totalDegree: 9,
                institutionId: institution.Id,
                signedByRegistrarId: Guid.NewGuid(),
                studentId: student.Id,
                major: "Computer Science",
                classification: "Gioi",
                cryptoData: cryptoSnapshot
            );
            Assert.True(degreeResult.IsSuccess);
            var degree = degreeResult.Value;
            string degreeCode = degree.DegreeCode;

            string txHash = "0x42fda1ef72cb74406ab5e2cfb96abd44097add4d94c07dc4cfaeba6679d52a45";
            degree.ConfirmBlockchainSync(txHash);
            degree.SetRowVersionForTesting();
            _dbContext.Degrees.Add(degree);

            // Calculate Merkle Tree as BatchingDegreeWorker does
            var treeResult = _merkleTreeService.BuildTree(new List<string> { dataHash });
            var proofData = treeResult.Proofs[0];
            string proofJson = JsonSerializer.Serialize(proofData); // Serialized MerkleProofData object

            var batchId = Guid.NewGuid();
            var batchRecord = new BatchRecord
            {
                Id = batchId,
                InstitutionId = institution.Id,
                BatchName = "BATCH_UIT_ISSUE_20260816",
                Status = BatchStatus.Completed,
                DegreeCount = 1,
                MerkleRoot = treeResult.MerkleRoot,
                TxHash = txHash,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.BatchRecords.Add(batchRecord);

            var batchDegreeRecord = new BatchDegreeRecord
            {
                BatchId = batchId,
                DegreeId = degree.Id,
                Version = degree.CurrentVersion,
                LeafIndex = proofData.LeafIndex,
                ProofHashesJson = proofJson
            };
            _dbContext.BatchDegreeRecords.Add(batchDegreeRecord);
            await _dbContext.SaveChangesAsync();

            // Act: Read verification snapshot from DB via repository
            var snapshot = await _degreeRepository.GetVerificationSnapshotAsync(degreeCode, null, CancellationToken.None);

            // Assert
            Assert.NotNull(snapshot);
            Assert.Equal(degree.Id, snapshot.DegreeId);
            Assert.Equal(dataHash, snapshot.DataHash);
            Assert.Equal(salt, snapshot.Salt);
            Assert.Equal(txHash, snapshot.TxHash);
            Assert.Equal(StatusEnum.Confirmed, snapshot.Status);
            Assert.Equal("John Doe", snapshot.StudentFullName);
            Assert.Equal("University of Information Technology", snapshot.InstitutionName);
            Assert.NotNull(snapshot.MerkleProofJson);

            // Verify the deserialized Merkle proof is cryptographically valid against the on-chain Merkle Root
            var deserializedProof = JsonSerializer.Deserialize<MerkleProofData>(snapshot.MerkleProofJson);
            Assert.NotNull(deserializedProof);
            Assert.Equal(0, deserializedProof.LeafIndex);
            Assert.Equal(dataHash, deserializedProof.LeafHash);

            bool isProofValid = _merkleTreeService.VerifyProof(snapshot.DataHash, deserializedProof, treeResult.MerkleRoot);
            Assert.True(isProofValid);
        }

        [Fact]
        public async Task VerifyDegreeQueryHandler_EndToEnd_WithDatabaseSnapshot_ReturnsVerifiedSuccess()
        {
            // Arrange
            var institution = EducationInstitution.Create("HCMUS", "University of Science", "contact@hcmus.edu.vn");
            var studentResult = Student.Create("STU-002", "Jane Smith", "jane@example.com", Guid.NewGuid());
            Assert.True(studentResult.IsSuccess);
            var student = studentResult.Value;

            _dbContext.EducationInstitutions.Add(institution);
            _dbContext.Students.Add(student);

            string degreeCode = "DEG-2026-000010";
            var issuedDate = new DateTime(2026, 6, 15, 8, 0, 0, DateTimeKind.Utc);
            string salt = _hashService.GenerateSalt().Value;

            var mockDegreeHashService = new Mock<IDegreeHashService>();
            mockDegreeHashService
                .Setup(s => s.CalculateHashAsync(It.IsAny<DegreeData>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("0xcomputedhash1234567890abcdef");

            string dataHash = "0xcomputedhash1234567890abcdef";
            string plainDataJson = JsonSerializer.Serialize(new
            {
                degreeCode = degreeCode,
                studentId = student.Id,
                major = "Software Engineering",
                classification = "Xuat Sac",
                issuedAt = issuedDate.ToString("o")
            });

            var cryptoSnapshot = CryptoSnapshot.Reconstruct(plainDataJson, salt, dataHash);
            var degreeResult = Degree.Create(
                totalDegree: 9,
                institutionId: institution.Id,
                signedByRegistrarId: Guid.NewGuid(),
                studentId: student.Id,
                major: "Software Engineering",
                classification: "Xuat Sac",
                cryptoData: cryptoSnapshot
            );
            Assert.True(degreeResult.IsSuccess);
            var degree = degreeResult.Value;

            string txHash = "0x42fda1ef72cb74406ab5e2cfb96abd44097add4d94c07dc4cfaeba6679d52a45";
            degree.ConfirmBlockchainSync(txHash);
            degree.SetRowVersionForTesting();
            _dbContext.Degrees.Add(degree);

            var treeResult = _merkleTreeService.BuildTree(new List<string> { dataHash });
            var proofData = treeResult.Proofs[0];

            var batchId = Guid.NewGuid();
            var batchRecord = new BatchRecord
            {
                Id = batchId,
                InstitutionId = institution.Id,
                BatchName = "BATCH_HCMUS_ISSUE_20260816",
                Status = BatchStatus.Completed,
                DegreeCount = 1,
                MerkleRoot = treeResult.MerkleRoot,
                TxHash = txHash,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.BatchRecords.Add(batchRecord);

            var batchDegreeRecord = new BatchDegreeRecord
            {
                BatchId = batchId,
                DegreeId = degree.Id,
                Version = degree.CurrentVersion,
                LeafIndex = proofData.LeafIndex,
                ProofHashesJson = JsonSerializer.Serialize(proofData)
            };
            _dbContext.BatchDegreeRecords.Add(batchDegreeRecord);
            await _dbContext.SaveChangesAsync();

            // Mock Blockchain returning on-chain batch metadata
            var mockBlockchain = new Mock<IBlockchainService>();
            mockBlockchain
                .Setup(b => b.GetBatchAsync(batchId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<BatchMetadata>.Success(new BatchMetadata(
                    MerkleRoot: treeResult.MerkleRoot,
                    Timestamp: 1723800000,
                    InstitutionId: institution.Id.ToString(),
                    ActionType: "Issue",
                    Exists: true
                )));

            var mockCanonicalizer = new Mock<IJsonCanonicalizer>();
            var mockBehaviorLog = new Mock<IBehaviorLogService>();

            var handler = new VerifyDegreeQueryHandler(
                _degreeRepository,
                mockBlockchain.Object,
                _merkleTreeService,
                mockDegreeHashService.Object,
                mockCanonicalizer.Object,
                _hashService,
                mockBehaviorLog.Object,
                NullLogger<VerifyDegreeQueryHandler>.Instance
            );

            // Act: Execute Verification Query
            var verifyResult = await handler.Handle(new VerifyDegreeQuery(degreeCode, null), CancellationToken.None);

            // Assert
            Assert.True(verifyResult.IsSuccess);
            Assert.True(verifyResult.Value.Verified);
            Assert.Equal("Confirmed", verifyResult.Value.Status);
            Assert.Equal(degreeCode, verifyResult.Value.DegreeCode);
            Assert.Equal("University of Science", verifyResult.Value.InstitutionName);
            Assert.Equal("Jane Smith", verifyResult.Value.StudentFullName);
            Assert.Equal("Software Engineering", verifyResult.Value.Major);
            Assert.Equal("Xuat Sac", verifyResult.Value.Classification);
            Assert.Equal(txHash, verifyResult.Value.Blockchain?.TxHash);
            Assert.Equal(treeResult.MerkleRoot, verifyResult.Value.Blockchain?.MerkleRoot);
        }

        [Fact]
        public async Task GetDegreeVersionsAsync_ReturnsCorrectVersionsListFromDatabase()
        {
            // Arrange
            var institution = EducationInstitution.Create("VNU", "Vietnam National University", "vnu@vnu.edu.vn");
            var studentResult = Student.Create("STU-003", "Alice Walker", "alice@example.com", Guid.NewGuid());
            Assert.True(studentResult.IsSuccess);
            var student = studentResult.Value;

            _dbContext.EducationInstitutions.Add(institution);
            _dbContext.Students.Add(student);

            var cryptoSnapshot = CryptoSnapshot.Reconstruct("{}", "salt_v2", "hash_v2");
            var degreeResult = Degree.Create(
                totalDegree: 9,
                institutionId: institution.Id,
                signedByRegistrarId: Guid.NewGuid(),
                studentId: student.Id,
                major: "Data Science",
                classification: "Gioi",
                cryptoData: cryptoSnapshot
            );
            Assert.True(degreeResult.IsSuccess);
            var degree = degreeResult.Value;
            string degreeCode = degree.DegreeCode;

            // Set current version to 2
            degree.SetVersionForTesting(2);
            degree.SetRowVersionForTesting();
            _dbContext.Degrees.Add(degree);

            // Add historical version 1
            var historicalV1 = DegreeVersion.Create(
                degreeId: degree.Id,
                version: 1,
                previousHash: "0xprev",
                currentHash: "hash_v1",
                blockchainTxHash: "0xtx1",
                effectiveAt: DateTime.UtcNow.AddYears(-1),
                plainDataJson: "{}",
                salt: "salt_v1",
                major: "Information Systems",
                classification: "Kha",
                merkleProofJson: "{}"
            );
            _dbContext.DegreeVersions.Add(historicalV1);
            await _dbContext.SaveChangesAsync();

            // Act
            var versionsResponse = await _degreeRepository.GetDegreeVersionsAsync(degreeCode);

            // Assert
            Assert.NotNull(versionsResponse);
            Assert.Equal(degreeCode, versionsResponse.DegreeCode);
            Assert.Equal(2, versionsResponse.CurrentVersion);
            Assert.Equal(2, versionsResponse.Versions.Count);

            // Version 2 should be current
            Assert.Equal(2, versionsResponse.Versions[0].Version);
            Assert.True(versionsResponse.Versions[0].IsCurrent);

            // Version 1 should be historical
            Assert.Equal(1, versionsResponse.Versions[1].Version);
            Assert.False(versionsResponse.Versions[1].IsCurrent);
        }
    }

    public static class DegreeTestingExtensions
    {
        public static void SetVersionForTesting(this Degree degree, int version)
        {
            var prop = typeof(Degree).GetProperty(nameof(Degree.CurrentVersion));
            prop?.SetValue(degree, version);
        }

        public static void SetRowVersionForTesting(this Degree degree, byte[]? rowVersion = null)
        {
            var prop = typeof(Degree).GetProperty(nameof(Degree.RowVersion));
            prop?.SetValue(degree, rowVersion ?? new byte[] { 0, 0, 0, 1 });
        }
    }
}
