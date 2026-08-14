using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Abstractions.Services;
using ChainDegree.Core.Application.Degrees.Commands.RevokeDegree;
using ChainDegree.Core.Domain.Degrees;
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
    public class RevokeDegreeCommandHandlerTests
    {
        private readonly Mock<IDegreeRepository> _mockRepo;
        private readonly Mock<ICurrentUserAccessor> _mockUserAccessor;
        private readonly Mock<IBehaviorLogService> _mockBehaviorLog;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<RevokeDegreeCommandHandler>> _mockLogger;
        private readonly RevokeDegreeCommandHandler _handler;
        private readonly CryptoSnapshot _fakeCrypto;

        public RevokeDegreeCommandHandlerTests()
        {
            _mockRepo = new Mock<IDegreeRepository>();
            _mockUserAccessor = new Mock<ICurrentUserAccessor>();
            _mockBehaviorLog = new Mock<IBehaviorLogService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<RevokeDegreeCommandHandler>>();

            var mockTransaction = new Mock<ITransaction>();
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockTransaction.Object);

            var mockHashService = new Mock<IHashService>();
            mockHashService.Setup(h => h.GenerateSalt()).Returns(Result<string>.Success("salt"));
            mockHashService.Setup(h => h.HashData(It.IsAny<string>(), It.IsAny<string>())).Returns(Result<string>.Success("hash"));
            _fakeCrypto = CryptoSnapshot.Create("plain", mockHashService.Object).Value;

            _handler = new RevokeDegreeCommandHandler(
                _mockRepo.Object,
                _mockUserAccessor.Object,
                _mockBehaviorLog.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_WithValidShortcutRevoke_RevokesImmediatelyAndCommits()
        {
            // Arrange
            var institutionId = Guid.NewGuid();
            var registrarId = Guid.NewGuid();
            var degree = Degree.Create(0, institutionId, registrarId, Guid.NewGuid(), "IT", "Giỏi", _fakeCrypto).Value;

            _mockUserAccessor.Setup(u => u.InstitutionId).Returns(institutionId);
            _mockRepo.Setup(r => r.GetByIdAsync(degree.Id, It.IsAny<CancellationToken>())).ReturnsAsync(degree);

            var command = new RevokeDegreeCommand(degree.Id, "R-01");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusEnum.Revoked.ToString(), result.Value.Status);
            Assert.True(result.Value.IsShortcut);

            _mockBehaviorLog.Verify(b => b.LogAsync(ActionTypeEnum.ALTER_DEGREE, "DEGREES", degree.Id, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithValidConfirmedRevoke_InitiatesRevocationAndCommits()
        {
            // Arrange
            var institutionId = Guid.NewGuid();
            var registrarId = Guid.NewGuid();
            var degree = Degree.Create(0, institutionId, registrarId, Guid.NewGuid(), "IT", "Giỏi", _fakeCrypto).Value;
            degree.ConfirmBlockchainSync("0xhash");

            _mockUserAccessor.Setup(u => u.InstitutionId).Returns(institutionId);
            _mockRepo.Setup(r => r.GetByIdAsync(degree.Id, It.IsAny<CancellationToken>())).ReturnsAsync(degree);

            var command = new RevokeDegreeCommand(degree.Id, "R-01");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusEnum.Pending_Revocation.ToString(), result.Value.Status);
            Assert.False(result.Value.IsShortcut);

            _mockBehaviorLog.Verify(b => b.LogAsync(ActionTypeEnum.ALTER_DEGREE, "DEGREES", degree.Id, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithInstitutionMismatch_ReturnsFailure()
        {
            // Arrange
            var institutionId = Guid.NewGuid();
            var registrarId = Guid.NewGuid();
            var degree = Degree.Create(0, Guid.NewGuid(), registrarId, Guid.NewGuid(), "IT", "Giỏi", _fakeCrypto).Value;

            _mockUserAccessor.Setup(u => u.InstitutionId).Returns(institutionId);
            _mockRepo.Setup(r => r.GetByIdAsync(degree.Id, It.IsAny<CancellationToken>())).ReturnsAsync(degree);

            var command = new RevokeDegreeCommand(degree.Id, "R-01");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(DegreeErrors.InstitutionMismatch, result.Error);
        }
    }
}
