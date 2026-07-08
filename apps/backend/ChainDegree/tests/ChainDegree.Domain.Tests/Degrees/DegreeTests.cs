using System;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Degrees.Enums;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
using ChainDegree.Core.Domain.Degrees.Interfaces;
using ChainDegree.Core.Domain.Degrees.Events;
using ChainDegree.SharedKernel.DomainErrors.Degrees.Degree;
using ChainDegree.SharedKernel.Result;
using Moq;
using Xunit;

namespace ChainDegree.Domain.Tests.Degrees
{
    public class DegreeTests
    {
        private readonly Mock<IHashService> _mockHashService;
        private readonly CryptoSnapshot _fakeCryptoSnapshot;

        public DegreeTests()
        {
            _mockHashService = new Mock<IHashService>();
            _mockHashService.Setup(h => h.GenerateSalt()).Returns(Result<string>.Success("a7d83bf92c81e3d0"));
            _mockHashService.Setup(h => h.HashData(It.IsAny<string>(), It.IsAny<string>()))
                            .Returns(Result<string>.Success("mocked_data_hash_local"));

            var cryptoResult = CryptoSnapshot.Create("{\"classification\":\"Gioi\",\"degreeCode\":\"DEG-2026-000001\",\"major\":\"Software Engineering\",\"studentId\":\"550e8400-e29b-41d4-a716-446655440000\"}", _mockHashService.Object);
            _fakeCryptoSnapshot = cryptoResult.Value;
        }

        [Fact]
        public void Create_WithValidData_ReturnsDegreeWithPendingStatus()
        {
            // Arrange
            var institutionId = Guid.NewGuid();
            var signedByRegistrarId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var major = "Software Engineering";
            var classification = "Giỏi";

            // Act
            var result = Degree.Create(
                0,
                institutionId,
                signedByRegistrarId,
                studentId,
                major,
                classification,
                _fakeCryptoSnapshot);

            // Assert
            Assert.True(result.IsSuccess);
            var degree = result.Value;
            Assert.Equal(StatusEnum.Pending_Confirmation, degree.Status);
            Assert.Equal(institutionId, degree.InstitutionId);
            Assert.Equal(signedByRegistrarId, degree.SignedByRegistrarId);
            Assert.Equal(studentId, degree.StudentId);
            Assert.Equal(major, degree.Major);
            Assert.Equal(classification, degree.Classification);
            Assert.Equal(_fakeCryptoSnapshot, degree.CryptoData);
            Assert.NotNull(degree.DegreeCode);
            Assert.NotEmpty(degree.DomainEvents);
        }

        [Fact]
        public void Create_WithInvalidTotalCount_ReturnsFailure()
        {
            // Act
            var result = Degree.Create(
                -1,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Software Engineering",
                "Giỏi",
                _fakeCryptoSnapshot);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(DegreeErrors.InvalidTotalCount, result.Error);
        }

        [Fact]
        public void ConfirmBlockchainSync_FromPendingConfirmation_SetsConfirmedAndTxHash()
        {
            // Arrange
            var degreeResult = Degree.Create(
                0,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Software Engineering",
                "Giỏi",
                _fakeCryptoSnapshot);
            var degree = degreeResult.Value;
            var txHash = "0x" + new string('a', 64);

            // Act
            var syncResult = degree.ConfirmBlockchainSync(txHash);

            // Assert
            Assert.True(syncResult.IsSuccess);
            Assert.Equal(StatusEnum.Confirmed, degree.Status);
            Assert.Equal(txHash, degree.TxHashBlockchain);
        }

        [Fact]
        public void ConfirmBlockchainSync_FromConfirmed_ReturnsFailure()
        {
            // Arrange
            var degreeResult = Degree.Create(
                0,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Software Engineering",
                "Giỏi",
                _fakeCryptoSnapshot);
            var degree = degreeResult.Value;
            var txHash = "0x" + new string('a', 64);
            degree.ConfirmBlockchainSync(txHash);

            // Act
            var secondSyncResult = degree.ConfirmBlockchainSync(txHash);

            // Assert
            Assert.True(secondSyncResult.IsFailure);
            Assert.Equal(DegreeErrors.InvalidStateTransition, secondSyncResult.Error);
        }

        [Fact]
        public void MarkAsSyncError_FromPendingConfirmation_SetsConfirmationError()
        {
            // Arrange
            var degreeResult = Degree.Create(
                0,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Software Engineering",
                "Giỏi",
                _fakeCryptoSnapshot);
            var degree = degreeResult.Value;

            // Act
            degree.MarkAsSyncError();

            // Assert
            Assert.Equal(StatusEnum.Confirmation_Error, degree.Status);
        }

        [Fact]
        public void MarkReadyForRetry_FromConfirmationError_SetsPendingConfirmation()
        {
            // Arrange
            var degreeResult = Degree.Create(
                0,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Software Engineering",
                "Giỏi",
                _fakeCryptoSnapshot);
            var degree = degreeResult.Value;
            degree.MarkAsSyncError();

            // Act
            var result = degree.MarkReadyForRetry();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusEnum.Pending_Confirmation, degree.Status);
        }

        [Fact]
        public void MarkReadyForRetry_FromConfirmed_ReturnsFailure()
        {
            // Arrange
            var degreeResult = Degree.Create(
                0,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Software Engineering",
                "Giỏi",
                _fakeCryptoSnapshot);
            var degree = degreeResult.Value;
            degree.ConfirmBlockchainSync("0x" + new string('a', 64));

            // Act
            var result = degree.MarkReadyForRetry();

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(DegreeErrors.InvalidStateTransition, result.Error);
        }

        [Fact]
        public void InitiateUpdate_FromConfirmed_SetsPendingUpdateAndRaisesEvent()
        {
            // Arrange
            var degreeResult = Degree.Create(
                0,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Software Engineering",
                "Giỏi",
                _fakeCryptoSnapshot);
            var degree = degreeResult.Value;
            degree.ConfirmBlockchainSync("0x" + new string('a', 64));
            degree.ClearDomainEvents();
            var reason = DegreeActionReason.FromCode("S-01");

            // Act
            var result = degree.InitiateUpdate("new_mocked_hash", reason);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusEnum.Pending_Update, degree.Status);
            var updateEvent = Assert.Single(degree.DomainEvents);
            Assert.IsType<DegreeUpdatedEvent>(updateEvent);
            var typedEvent = (DegreeUpdatedEvent)updateEvent;
            Assert.Equal(degree.Id, typedEvent.DegreeId);
            Assert.Equal("S-01", typedEvent.ReasonCode);
        }

        [Fact]
        public void ConfirmUpdate_FromPendingUpdate_SetsConfirmedUpdatesDataAndIncrementsVersion()
        {
            // Arrange
            var degreeResult = Degree.Create(
                0,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Software Engineering",
                "Giỏi",
                _fakeCryptoSnapshot);
            var degree = degreeResult.Value;
            degree.ConfirmBlockchainSync("0x" + new string('a', 64));
            var reason = DegreeActionReason.FromCode("S-01");
            degree.InitiateUpdate("new_mocked_hash", reason);
            var newCryptoSnapshot = CryptoSnapshot.Create("{\"major\":\"Artificial Intelligence\"}", _mockHashService.Object).Value;
            var txHash = "0x" + new string('b', 64);

            // Act
            var result = degree.ConfirmUpdate("Artificial Intelligence", "Xuất sắc", newCryptoSnapshot, txHash);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusEnum.Confirmed, degree.Status);
            Assert.Equal(2, degree.CurrentVersion);
            Assert.Equal("Artificial Intelligence", degree.Major);
            Assert.Equal("Xuất sắc", degree.Classification);
            Assert.Equal(txHash, degree.TxHashBlockchain);
        }

        [Fact]
        public void UpdateShortcut_FromPendingConfirmation_UpdatesDataImmediatelyAndRaisesShortcutEvent()
        {
            // Arrange
            var degreeResult = Degree.Create(
                0,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Software Engineering",
                "Giỏi",
                _fakeCryptoSnapshot);
            var degree = degreeResult.Value;
            degree.ClearDomainEvents();
            var reason = DegreeActionReason.FromCode("S-01");
            var newCryptoSnapshot = CryptoSnapshot.Create("{\"major\":\"Artificial Intelligence\"}", _mockHashService.Object).Value;

            // Act
            var result = degree.UpdateShortcut("Artificial Intelligence", "Xuất sắc", newCryptoSnapshot, reason);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusEnum.Pending_Confirmation, degree.Status);
            Assert.Equal(1, degree.CurrentVersion);
            Assert.Equal("Artificial Intelligence", degree.Major);
            var updateEvent = Assert.Single(degree.DomainEvents);
            Assert.IsType<DegreeUpdatedWithoutConfirmationEvent>(updateEvent);
            var typedEvent = (DegreeUpdatedWithoutConfirmationEvent)updateEvent;
            Assert.Equal(degree.Id, typedEvent.DegreeId);
            Assert.Equal("S-01", typedEvent.ReasonCode);
        }

        [Fact]
        public void InitiateRevocation_FromConfirmed_SetsPendingRevocationAndRaisesEvent()
        {
            // Arrange
            var degreeResult = Degree.Create(
                0,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Software Engineering",
                "Giỏi",
                _fakeCryptoSnapshot);
            var degree = degreeResult.Value;
            degree.ConfirmBlockchainSync("0x" + new string('a', 64));
            degree.ClearDomainEvents();
            var reason = DegreeActionReason.FromCode("R-01");

            // Act
            var result = degree.InitiateRevocation(reason);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusEnum.Pending_Revocation, degree.Status);
            var revokeEvent = Assert.Single(degree.DomainEvents);
            Assert.IsType<DegreeRevokedEvent>(revokeEvent);
            var typedEvent = (DegreeRevokedEvent)revokeEvent;
            Assert.Equal(degree.Id, typedEvent.DegreeId);
            Assert.Equal("R-01", typedEvent.ReasonCode);
        }

        [Fact]
        public void ConfirmRevocation_FromPendingRevocation_SetsRevokedAndTxHash()
        {
            // Arrange
            var degreeResult = Degree.Create(
                0,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Software Engineering",
                "Giỏi",
                _fakeCryptoSnapshot);
            var degree = degreeResult.Value;
            degree.ConfirmBlockchainSync("0x" + new string('a', 64));
            var reason = DegreeActionReason.FromCode("R-01");
            degree.InitiateRevocation(reason);
            var txHash = "0x" + new string('c', 64);

            // Act
            var result = degree.ConfirmRevocation(txHash);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusEnum.Revoked, degree.Status);
            Assert.Equal(txHash, degree.TxHashBlockchain);
        }

        [Fact]
        public void RevokeShortcut_FromPendingConfirmation_SetsRevokedImmediatelyAndRaisesShortcutEvent()
        {
            // Arrange
            var degreeResult = Degree.Create(
                0,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Software Engineering",
                "Giỏi",
                _fakeCryptoSnapshot);
            var degree = degreeResult.Value;
            degree.ClearDomainEvents();
            var reason = DegreeActionReason.FromCode("R-01");

            // Act
            var result = degree.RevokeShortcut(reason);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusEnum.Revoked, degree.Status);
            var revokeEvent = Assert.Single(degree.DomainEvents);
            Assert.IsType<DegreeRevokedWithoutConfirmationEvent>(revokeEvent);
            var typedEvent = (DegreeRevokedWithoutConfirmationEvent)revokeEvent;
            Assert.Equal(degree.Id, typedEvent.DegreeId);
            Assert.Equal("R-01", typedEvent.ReasonCode);
        }
    }
}
