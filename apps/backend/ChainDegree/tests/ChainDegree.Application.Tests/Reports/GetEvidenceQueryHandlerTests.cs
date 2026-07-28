using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Abstractions.Services;
using ChainDegree.Core.Application.Reports.Queries.GetEvidence;
using ChainDegree.Core.Domain.Reports;
using ChainDegree.Core.Domain.Reports.Enums;
using ChainDegree.Core.Domain.SharedKernel.Enums;
using ChainDegree.SharedKernel.DomainErrors.Reports;
using Moq;
using Xunit;

namespace ChainDegree.Application.Tests.Reports
{
    public class GetEvidenceQueryHandlerTests
    {
        private readonly Mock<IReportRepository> _mockReportRepo;
        private readonly Mock<IEvidenceStorageService> _mockEvidenceStorage;
        private readonly Mock<ICurrentUserAccessor> _mockUserAccessor;
        private readonly GetEvidenceQueryHandler _handler;

        public GetEvidenceQueryHandlerTests()
        {
            _mockReportRepo = new Mock<IReportRepository>();
            _mockEvidenceStorage = new Mock<IEvidenceStorageService>();
            _mockUserAccessor = new Mock<ICurrentUserAccessor>();

            _handler = new GetEvidenceQueryHandler(
                _mockReportRepo.Object,
                _mockEvidenceStorage.Object,
                _mockUserAccessor.Object);
        }

        [Fact]
        public async Task Handle_StudentDownloadingOwnEvidence_ReturnsSuccess()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var report = Report.Create(Guid.NewGuid(), studentId, UserRoleEnum.Student, ReportTypeEnum.Administrative_Error, "Typo", "file123.pdf");

            _mockUserAccessor.Setup(u => u.IsAuthenticated).Returns(true);
            _mockUserAccessor.Setup(u => u.UserId).Returns(studentId);
            _mockUserAccessor.Setup(u => u.Role).Returns(Roles.Student);

            _mockReportRepo.Setup(r => r.GetByIdAsync(report.Id, It.IsAny<CancellationToken>())).ReturnsAsync(report);
            
            var fakeStream = new MemoryStream(new byte[] { 1, 2, 3 });
            _mockEvidenceStorage.Setup(e => e.GetEvidenceAsync("file123.pdf", It.IsAny<CancellationToken>()))
                .ReturnsAsync((fakeStream, "application/pdf", "evidence_file123.pdf"));

            // Act
            var result = await _handler.Handle(new GetEvidenceQuery(report.Id), CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("application/pdf", result.Value.ContentType);
            Assert.Equal("evidence_file123.pdf", result.Value.DownloadFileName);
        }

        [Fact]
        public async Task Handle_StudentDownloadingOthersEvidence_ReturnsForbidden()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var otherStudentId = Guid.NewGuid();
            var report = Report.Create(Guid.NewGuid(), otherStudentId, UserRoleEnum.Student, ReportTypeEnum.Administrative_Error, "Typo", "file123.pdf");

            _mockUserAccessor.Setup(u => u.IsAuthenticated).Returns(true);
            _mockUserAccessor.Setup(u => u.UserId).Returns(studentId);
            _mockUserAccessor.Setup(u => u.Role).Returns(Roles.Student);

            _mockReportRepo.Setup(r => r.GetByIdAsync(report.Id, It.IsAny<CancellationToken>())).ReturnsAsync(report);

            // Act
            var result = await _handler.Handle(new GetEvidenceQuery(report.Id), CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ReportErrors.UnauthorizedEvidenceDownload.Code, result.Error.Code);
        }

        [Fact]
        public async Task Handle_AdminDownloadingAnyEvidence_ReturnsSuccess()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var reporterId = Guid.NewGuid();
            var report = Report.Create(Guid.NewGuid(), reporterId, UserRoleEnum.Student, ReportTypeEnum.Fraudulent_Data, "Fake degree", "file123.pdf");

            _mockUserAccessor.Setup(u => u.IsAuthenticated).Returns(true);
            _mockUserAccessor.Setup(u => u.UserId).Returns(adminId);
            _mockUserAccessor.Setup(u => u.Role).Returns(Roles.Admin);

            _mockReportRepo.Setup(r => r.GetByIdAsync(report.Id, It.IsAny<CancellationToken>())).ReturnsAsync(report);

            var fakeStream = new MemoryStream(new byte[] { 1, 2, 3 });
            _mockEvidenceStorage.Setup(e => e.GetEvidenceAsync("file123.pdf", It.IsAny<CancellationToken>()))
                .ReturnsAsync((fakeStream, "application/pdf", "evidence_file123.pdf"));

            // Act
            var result = await _handler.Handle(new GetEvidenceQuery(report.Id), CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
        }
    }
}
