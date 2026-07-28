using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Abstractions.Services;
using ChainDegree.Core.Application.Reports.Commands.RejectReport;
using ChainDegree.Core.Domain.Reports;
using ChainDegree.Core.Domain.Reports.Enums;
using ChainDegree.Core.Domain.SharedKernel.Enums;
using ChainDegree.SharedKernel.DomainErrors.Reports;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChainDegree.Application.Tests.Reports
{
    public class RejectReportCommandHandlerTests
    {
        private readonly Mock<IReportRepository> _mockReportRepo;
        private readonly Mock<ICurrentUserAccessor> _mockUserAccessor;
        private readonly Mock<IEvidenceStorageService> _mockEvidenceStorage;
        private readonly Mock<IBehaviorLogService> _mockBehaviorLogService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<RejectReportCommandHandler>> _mockLogger;
        private readonly RejectReportCommandHandler _handler;

        public RejectReportCommandHandlerTests()
        {
            _mockReportRepo = new Mock<IReportRepository>();
            _mockUserAccessor = new Mock<ICurrentUserAccessor>();
            _mockEvidenceStorage = new Mock<IEvidenceStorageService>();
            _mockBehaviorLogService = new Mock<IBehaviorLogService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<RejectReportCommandHandler>>();

            _handler = new RejectReportCommandHandler(
                _mockReportRepo.Object,
                _mockUserAccessor.Object,
                _mockEvidenceStorage.Object,
                _mockBehaviorLogService.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_AdminRejectingReport_DeletesEvidenceAndReturnsSuccess()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var fileName = "evidence_to_delete.pdf";
            var report = Report.Create(Guid.NewGuid(), Guid.NewGuid(), UserRoleEnum.Student, ReportTypeEnum.Fraudulent_Data, "Fake claim", fileName);

            _mockUserAccessor.Setup(u => u.IsAuthenticated).Returns(true);
            _mockUserAccessor.Setup(u => u.UserId).Returns(adminId);
            _mockUserAccessor.Setup(u => u.Role).Returns(Roles.Admin);

            _mockReportRepo.Setup(r => r.GetByIdAsync(report.Id, It.IsAny<CancellationToken>())).ReturnsAsync(report);

            // Act
            var result = await _handler.Handle(new RejectReportCommand(report.Id, "Insufficient proof"), CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ReportStatusEnum.Rejected, report.Status);
            Assert.Equal("Insufficient proof", report.RejectionReason);

            _mockEvidenceStorage.Verify(e => e.DeleteEvidenceAsync(fileName, It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
