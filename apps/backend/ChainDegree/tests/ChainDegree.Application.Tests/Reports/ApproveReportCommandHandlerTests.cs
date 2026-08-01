using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Reports.Commands.ApproveReport;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Reports;
using ChainDegree.Core.Domain.Reports.Enums;
using ChainDegree.Core.Domain.SharedKernel.Enums;
using ChainDegree.SharedKernel.DomainErrors.Reports;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChainDegree.Application.Tests.Reports
{
    public class ApproveReportCommandHandlerTests
    {
        private readonly Mock<IReportRepository> _mockReportRepo;
        private readonly Mock<IDegreeRepository> _mockDegreeRepo;
        private readonly Mock<ICurrentUserAccessor> _mockUserAccessor;
        private readonly Mock<IBehaviorLogService> _mockBehaviorLogService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ApproveReportCommandHandler>> _mockLogger;
        private readonly ApproveReportCommandHandler _handler;

        public ApproveReportCommandHandlerTests()
        {
            _mockReportRepo = new Mock<IReportRepository>();
            _mockDegreeRepo = new Mock<IDegreeRepository>();
            _mockUserAccessor = new Mock<ICurrentUserAccessor>();
            _mockBehaviorLogService = new Mock<IBehaviorLogService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ApproveReportCommandHandler>>();

            _handler = new ApproveReportCommandHandler(
                _mockReportRepo.Object,
                _mockDegreeRepo.Object,
                _mockUserAccessor.Object,
                _mockBehaviorLogService.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_AdminApprovingReport_ReturnsSuccess()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var report = Report.Create(Guid.NewGuid(), Guid.NewGuid(), UserRoleEnum.Student, ReportTypeEnum.Fraudulent_Data, "Fake degree", "evidence.pdf").Value;

            _mockUserAccessor.Setup(u => u.IsAuthenticated).Returns(true);
            _mockUserAccessor.Setup(u => u.UserId).Returns(adminId);
            _mockUserAccessor.Setup(u => u.Role).Returns(Roles.Admin);

            _mockReportRepo.Setup(r => r.GetByIdAsync(report.Id, It.IsAny<CancellationToken>())).ReturnsAsync(report);

            // Act
            var result = await _handler.Handle(new ApproveReportCommand(report.Id), CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ReportStatusEnum.Approved, report.Status);
            _mockReportRepo.Verify(r => r.Update(report), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_NonAdminApproving_ReturnsUnauthorized()
        {
            // Arrange
            _mockUserAccessor.Setup(u => u.IsAuthenticated).Returns(true);
            _mockUserAccessor.Setup(u => u.Role).Returns(Roles.Student);

            // Act
            var result = await _handler.Handle(new ApproveReportCommand(Guid.NewGuid()), CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ReportErrors.UnauthorizedReporter.Code, result.Error.Code);
        }
    }
}
