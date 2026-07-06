using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Abstractions.Services;
using ChainDegree.Core.Application.Degrees.Commands.IssueDegree;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
using ChainDegree.Core.Domain.Degrees.Interfaces;
using ChainDegree.Core.Domain.SharedKernel.Enums;
using ChainDegree.SharedKernel.Common.Error;
using ChainDegree.SharedKernel.Result;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChainDegree.Application.Tests.Degrees
{
    public class IssueDegreeCommandHandlerTests
    {
        private readonly Mock<IDegreeIssuanceService> _mockIssuanceService;
        private readonly Mock<IDegreeRepository> _mockRepo;
        private readonly Mock<ICurrentUserAccessor> _mockUserAccessor;
        private readonly Mock<IBehaviorLogService> _mockBehaviorLog;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<IssueDegreeCommandHandler>> _mockLogger;
        private readonly IssueDegreeCommandHandler _handler;

        public IssueDegreeCommandHandlerTests()
        {
            _mockIssuanceService = new Mock<IDegreeIssuanceService>();
            _mockRepo = new Mock<IDegreeRepository>();
            _mockUserAccessor = new Mock<ICurrentUserAccessor>();
            _mockBehaviorLog = new Mock<IBehaviorLogService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<IssueDegreeCommandHandler>>();

            // Setup transactional mock
            var mockTransaction = new Mock<ITransaction>();
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockTransaction.Object);

            _handler = new IssueDegreeCommandHandler(
                _mockIssuanceService.Object,
                _mockRepo.Object,
                _mockUserAccessor.Object,
                _mockBehaviorLog.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_WithValidCommand_IssuesDegreesAndCommitsTransaction()
        {
            // Arrange
            var institutionId = Guid.NewGuid();
            var registrarId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            
            _mockUserAccessor.Setup(u => u.InstitutionId).Returns(institutionId);
            _mockUserAccessor.Setup(u => u.UserId).Returns(registrarId);

            var command = new IssueDegreeCommand(new List<IssueDegreeItemDto>
            {
                new(studentId, "Software Engineering", "Giỏi", DateTime.UtcNow)
            });

            var mockHashService = new Mock<IHashService>();
            mockHashService.Setup(h => h.GenerateSalt()).Returns(Result<string>.Success("salt"));
            mockHashService.Setup(h => h.HashData(It.IsAny<string>(), It.IsAny<string>())).Returns(Result<string>.Success("hash"));
            var crypto = CryptoSnapshot.Create("plain", mockHashService.Object).Value;

            var mockDegree = Degree.Create(0, institutionId, registrarId, studentId, "Software Engineering", "Giỏi", crypto).Value;

            var successes = new List<Degree> { mockDegree };
            var failures = new List<IssueDegreeFailureDto>();
            var partialResult = PartialResult<Degree, IssueDegreeFailureDto>.Create(successes, failures);

            _mockIssuanceService.Setup(s => s.IssueDegreesAsync(institutionId, registrarId, command.Degrees, It.IsAny<CancellationToken>()))
                                .ReturnsAsync(partialResult);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value.AcceptedCount);
            Assert.Single(result.Value.DegreeIds);
            Assert.Equal(mockDegree.Id, result.Value.DegreeIds[0]);

            _mockRepo.Verify(r => r.AddRangeAsync(successes, It.IsAny<CancellationToken>()), Times.Once);
            _mockBehaviorLog.Verify(b => b.LogAsync(ActionTypeEnum.CREATE_DEGREE, "DEGREES", mockDegree.Id, null, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithAllDuplicates_RollbacksTransaction()
        {
            // Arrange
            var institutionId = Guid.NewGuid();
            var registrarId = Guid.NewGuid();
            var studentId = Guid.NewGuid();

            _mockUserAccessor.Setup(u => u.InstitutionId).Returns(institutionId);
            _mockUserAccessor.Setup(u => u.UserId).Returns(registrarId);

            var command = new IssueDegreeCommand(new List<IssueDegreeItemDto>
            {
                new(studentId, "Software Engineering", "Giỏi", DateTime.UtcNow)
            });

            var successes = new List<Degree>();
            var failures = new List<IssueDegreeFailureDto> { new(studentId, "Software Engineering", "Duplicate") };
            var partialResult = PartialResult<Degree, IssueDegreeFailureDto>.Create(successes, failures);

            _mockIssuanceService.Setup(s => s.IssueDegreesAsync(institutionId, registrarId, command.Degrees, It.IsAny<CancellationToken>()))
                                .ReturnsAsync(partialResult);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.Value.AcceptedCount);
            Assert.Single(result.Value.Failures);
            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
