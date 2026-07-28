using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Abstractions.Services;
using ChainDegree.Core.Application.Reports.Commands.SubmitReport;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Degrees.Interfaces;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
using ChainDegree.Core.Domain.Reports;
using ChainDegree.Core.Domain.Reports.Enums;
using ChainDegree.Core.Domain.SharedKernel.Enums;
using ChainDegree.SharedKernel.DomainErrors.Reports;
using ChainDegree.SharedKernel.Result;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChainDegree.Application.Tests.Reports
{
    public class SubmitReportCommandHandlerTests
    {
        private readonly Mock<IReportRepository> _mockReportRepo;
        private readonly Mock<IDegreeRepository> _mockDegreeRepo;
        private readonly Mock<ICurrentUserAccessor> _mockUserAccessor;
        private readonly Mock<IEvidenceStorageService> _mockEvidenceStorage;
        private readonly Mock<IBehaviorLogService> _mockBehaviorLogService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<SubmitReportCommandHandler>> _mockLogger;
        private readonly SubmitReportCommandHandler _handler;
        private readonly CryptoSnapshot _fakeCrypto;

        public SubmitReportCommandHandlerTests()
        {
            _mockReportRepo = new Mock<IReportRepository>();
            _mockDegreeRepo = new Mock<IDegreeRepository>();
            _mockUserAccessor = new Mock<ICurrentUserAccessor>();
            _mockEvidenceStorage = new Mock<IEvidenceStorageService>();
            _mockBehaviorLogService = new Mock<IBehaviorLogService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<SubmitReportCommandHandler>>();

            var mockHashService = new Mock<IHashService>();
            mockHashService.Setup(h => h.GenerateSalt()).Returns(Result<string>.Success("salt"));
            mockHashService.Setup(h => h.HashData(It.IsAny<string>(), It.IsAny<string>())).Returns(Result<string>.Success("hash"));
            _fakeCrypto = CryptoSnapshot.Create("plain", mockHashService.Object).Value;

            _handler = new SubmitReportCommandHandler(
                _mockReportRepo.Object,
                _mockDegreeRepo.Object,
                _mockUserAccessor.Object,
                _mockEvidenceStorage.Object,
                _mockBehaviorLogService.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_StudentSubmittingOwnDegree_ReturnsSuccess()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var degree = Degree.Create(0, Guid.NewGuid(), Guid.NewGuid(), studentId, "CS", "Gioi", _fakeCrypto).Value;

            _mockUserAccessor.Setup(u => u.IsAuthenticated).Returns(true);
            _mockUserAccessor.Setup(u => u.UserId).Returns(studentId);
            _mockUserAccessor.Setup(u => u.Role).Returns(Roles.Student);

            _mockDegreeRepo.Setup(d => d.GetByIdAsync(degree.Id, It.IsAny<CancellationToken>())).ReturnsAsync(degree);
            _mockReportRepo.Setup(r => r.ExistsPendingReportAsync(studentId, degree.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _mockEvidenceStorage.Setup(e => e.SaveEvidenceAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("saved_123.pdf");

            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var command = new SubmitReportCommand(degree.Id, ReportTypeEnum.Administrative_Error, "Typo in name", stream, "application/pdf", "transcript.pdf");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(degree.Id, result.Value.DegreeId);
            Assert.Equal("saved_123.pdf", result.Value.EvidenceFileName);

            _mockReportRepo.Verify(r => r.AddAsync(It.IsAny<Report>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_StudentSubmittingOthersDegree_ReturnsFailure()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var otherStudentId = Guid.NewGuid();
            var degree = Degree.Create(0, Guid.NewGuid(), Guid.NewGuid(), otherStudentId, "CS", "Gioi", _fakeCrypto).Value;

            _mockUserAccessor.Setup(u => u.IsAuthenticated).Returns(true);
            _mockUserAccessor.Setup(u => u.UserId).Returns(studentId);
            _mockUserAccessor.Setup(u => u.Role).Returns(Roles.Student);

            _mockDegreeRepo.Setup(d => d.GetByIdAsync(degree.Id, It.IsAny<CancellationToken>())).ReturnsAsync(degree);

            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var command = new SubmitReportCommand(degree.Id, ReportTypeEnum.Administrative_Error, "Typo in name", stream, "application/pdf", "transcript.pdf");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ReportErrors.StudentCannotReportOthersDegree.Code, result.Error.Code);
            _mockEvidenceStorage.Verify(e => e.SaveEvidenceAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_DuplicateReportUnderReview_ReturnsConflict()
        {
            // Arrange
            var recruiterId = Guid.NewGuid();
            var degree = Degree.Create(0, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "CS", "Gioi", _fakeCrypto).Value;

            _mockUserAccessor.Setup(u => u.IsAuthenticated).Returns(true);
            _mockUserAccessor.Setup(u => u.UserId).Returns(recruiterId);
            _mockUserAccessor.Setup(u => u.Role).Returns(Roles.Recruiter);

            _mockDegreeRepo.Setup(d => d.GetByIdAsync(degree.Id, It.IsAny<CancellationToken>())).ReturnsAsync(degree);
            _mockReportRepo.Setup(r => r.ExistsPendingReportAsync(recruiterId, degree.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var command = new SubmitReportCommand(degree.Id, ReportTypeEnum.Fraudulent_Data, "Fake degree", stream, "application/pdf", "proof.pdf");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ReportErrors.ReportAlreadyExistsUnderReview.Code, result.Error.Code);
        }

        [Fact]
        public async Task Handle_DbFailure_RollsBackStorageFile()
        {
            // Arrange
            var recruiterId = Guid.NewGuid();
            var degree = Degree.Create(0, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "CS", "Gioi", _fakeCrypto).Value;

            _mockUserAccessor.Setup(u => u.IsAuthenticated).Returns(true);
            _mockUserAccessor.Setup(u => u.UserId).Returns(recruiterId);
            _mockUserAccessor.Setup(u => u.Role).Returns(Roles.Recruiter);

            _mockDegreeRepo.Setup(d => d.GetByIdAsync(degree.Id, It.IsAny<CancellationToken>())).ReturnsAsync(degree);
            _mockReportRepo.Setup(r => r.ExistsPendingReportAsync(recruiterId, degree.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _mockEvidenceStorage.Setup(e => e.SaveEvidenceAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("temp_saved.pdf");

            _mockUnitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var command = new SubmitReportCommand(degree.Id, ReportTypeEnum.Fraudulent_Data, "Fake degree", stream, "application/pdf", "proof.pdf");

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));

            _mockEvidenceStorage.Verify(e => e.DeleteEvidenceAsync("temp_saved.pdf", It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
