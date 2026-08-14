using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Crypto;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Abstractions.Services;
using ChainDegree.Core.Application.Degrees.Commands.UpdateDegree;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Degrees.Entities;
using ChainDegree.Core.Domain.Degrees.Enums;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
using ChainDegree.Core.Domain.Degrees.Interfaces;
using ChainDegree.Core.Domain.SharedKernel.Enums;
using ChainDegree.SharedKernel.DomainErrors.Degrees.Degree;
using ChainDegree.SharedKernel.Result;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChainDegree.Application.Tests.Degrees
{
    public class UpdateDegreeCommandHandlerTests
    {
        private readonly Mock<IDegreeRepository> _mockRepo;
        private readonly Mock<ICurrentUserAccessor> _mockUserAccessor;
        private readonly Mock<IDegreeHashService> _mockHashService;
        private readonly Mock<IBehaviorLogService> _mockBehaviorLog;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<UpdateDegreeCommandHandler>> _mockLogger;
        private readonly UpdateDegreeCommandHandler _handler;
        private readonly CryptoSnapshot _fakeCrypto;
        private readonly CryptoSnapshot _newFakeCrypto;

        public UpdateDegreeCommandHandlerTests()
        {
            _mockRepo = new Mock<IDegreeRepository>();
            _mockUserAccessor = new Mock<ICurrentUserAccessor>();
            _mockHashService = new Mock<IDegreeHashService>();
            _mockBehaviorLog = new Mock<IBehaviorLogService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<UpdateDegreeCommandHandler>>();

            var mockTransaction = new Mock<ITransaction>();
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockTransaction.Object);

            var mockHashServiceDomain = new Mock<IHashService>();
            mockHashServiceDomain.Setup(h => h.GenerateSalt()).Returns(Result<string>.Success("salt1"));
            mockHashServiceDomain.Setup(h => h.HashData(It.IsAny<string>(), It.IsAny<string>())).Returns(Result<string>.Success("hash1"));
            _fakeCrypto = CryptoSnapshot.Create("plain", mockHashServiceDomain.Object).Value;

            var mockHashServiceDomain2 = new Mock<IHashService>();
            mockHashServiceDomain2.Setup(h => h.GenerateSalt()).Returns(Result<string>.Success("salt2"));
            mockHashServiceDomain2.Setup(h => h.HashData(It.IsAny<string>(), It.IsAny<string>())).Returns(Result<string>.Success("hash2"));
            _newFakeCrypto = CryptoSnapshot.Create("plain2", mockHashServiceDomain2.Object).Value;

            _handler = new UpdateDegreeCommandHandler(
                _mockRepo.Object,
                _mockUserAccessor.Object,
                _mockHashService.Object,
                _mockBehaviorLog.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_WithValidShortcutUpdate_UpdatesImmediatelyAndCommits()
        {
            // Arrange
            var institutionId = Guid.NewGuid();
            var registrarId = Guid.NewGuid();
            var degree = Degree.Create(0, institutionId, registrarId, Guid.NewGuid(), "IT", "Giỏi", _fakeCrypto).Value;

            _mockUserAccessor.Setup(u => u.InstitutionId).Returns(institutionId);
            _mockRepo.Setup(r => r.GetByIdAsync(degree.Id, It.IsAny<CancellationToken>())).ReturnsAsync(degree);
            _mockHashService.Setup(h => h.RecalculateAsync(It.IsAny<DegreeData>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync(_newFakeCrypto);

            var command = new UpdateDegreeCommand(degree.Id, "Artificial Intelligence", "Xuất sắc", "S-01");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusEnum.Pending_Confirmation.ToString(), result.Value.Status);
            Assert.True(result.Value.IsShortcut);
            Assert.Equal("Artificial Intelligence", degree.Major);
            Assert.Equal("Xuất sắc", degree.Classification);

            _mockBehaviorLog.Verify(b => b.LogAsync(ActionTypeEnum.ALTER_DEGREE, "DEGREES", degree.Id, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithValidConfirmedUpdate_InitiatesUpdateCreatesStagingAndCommits()
        {
            // Arrange
            var institutionId = Guid.NewGuid();
            var registrarId = Guid.NewGuid();
            var degree = Degree.Create(0, institutionId, registrarId, Guid.NewGuid(), "IT", "Giỏi", _fakeCrypto).Value;
            degree.ConfirmBlockchainSync("0xhash");

            _mockUserAccessor.Setup(u => u.InstitutionId).Returns(institutionId);
            _mockRepo.Setup(r => r.GetByIdAsync(degree.Id, It.IsAny<CancellationToken>())).ReturnsAsync(degree);
            _mockHashService.Setup(h => h.RecalculateAsync(It.IsAny<DegreeData>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync(_newFakeCrypto);

            var command = new UpdateDegreeCommand(degree.Id, "Artificial Intelligence", "Xuất sắc", "S-01");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusEnum.Pending_Update.ToString(), result.Value.Status);
            Assert.False(result.Value.IsShortcut);
            Assert.Equal("IT", degree.Major); // should not overwrite original data yet

            _mockRepo.Verify(r => r.AddUpdateRequestAsync(It.IsAny<DegreeUpdateRequest>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockBehaviorLog.Verify(b => b.LogAsync(ActionTypeEnum.ALTER_DEGREE, "DEGREES", degree.Id, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenAlreadyRevoked_ReturnsInvalidStateTransitionError()
        {
            // Arrange
            var institutionId = Guid.NewGuid();
            var registrarId = Guid.NewGuid();
            var degree = Degree.Create(0, institutionId, registrarId, Guid.NewGuid(), "IT", "Giỏi", _fakeCrypto).Value;
            degree.RevokeShortcut(DegreeActionReason.FromCode("R-01"));

            _mockUserAccessor.Setup(u => u.InstitutionId).Returns(institutionId);
            _mockRepo.Setup(r => r.GetByIdAsync(degree.Id, It.IsAny<CancellationToken>())).ReturnsAsync(degree);

            var command = new UpdateDegreeCommand(degree.Id, "AI", "Xuất sắc", "S-01");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(DegreeErrors.InvalidStateTransition, result.Error);
        }

        [Fact]
        public async Task Handle_WithInstitutionMismatch_ReturnsInstitutionMismatchError()
        {
            // Arrange
            var institutionId = Guid.NewGuid();
            var registrarId = Guid.NewGuid();
            var degree = Degree.Create(0, Guid.NewGuid(), registrarId, Guid.NewGuid(), "IT", "Giỏi", _fakeCrypto).Value;

            _mockUserAccessor.Setup(u => u.InstitutionId).Returns(institutionId);
            _mockRepo.Setup(r => r.GetByIdAsync(degree.Id, It.IsAny<CancellationToken>())).ReturnsAsync(degree);

            var command = new UpdateDegreeCommand(degree.Id, "AI", "Xuất sắc", "S-01");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(DegreeErrors.InstitutionMismatch, result.Error);
        }
    }
}
