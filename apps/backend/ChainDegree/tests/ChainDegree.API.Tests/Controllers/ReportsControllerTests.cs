using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.API.Contracts.Reports;
using ChainDegree.API.Controllers;
using ChainDegree.Core.Application.Reports.Commands.ApproveReport;
using ChainDegree.Core.Application.Reports.Commands.RejectReport;
using ChainDegree.Core.Application.Reports.Commands.SubmitReport;
using ChainDegree.Core.Application.Reports.Queries.GetEvidence;
using ChainDegree.Core.Domain.Reports.Enums;
using ChainDegree.SharedKernel.DomainErrors.Reports;
using ChainDegree.SharedKernel.Result;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ChainDegree.API.Tests.Controllers
{
    public class ReportsControllerTests
    {
        private readonly Mock<ISender> _mockSender;
        private readonly ReportsController _controller;

        public ReportsControllerTests()
        {
            _mockSender = new Mock<ISender>();
            _controller = new ReportsController(_mockSender.Object);
        }

        [Fact]
        public async Task SubmitReport_ValidMultipartRequest_ReturnsCreatedAtAction()
        {
            // Arrange
            var degreeId = Guid.NewGuid();
            var reportId = Guid.NewGuid();
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");
            mockFile.Setup(f => f.FileName).Returns("evidence.pdf");
            mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.4 sample")));

            var request = new SubmitReportRequest
            {
                DegreeId = degreeId,
                ReportType = ReportTypeEnum.Administrative_Error,
                Description = "Name typo",
                EvidenceFile = mockFile.Object
            };

            var expectedResponse = new SubmitReportResponse(reportId, degreeId, "Pending_Review", "safe_123.pdf", DateTime.UtcNow);
            _mockSender.Setup(s => s.Send(It.IsAny<SubmitReportCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SubmitReportResponse>.Success(expectedResponse));

            // Act
            var actionResult = await _controller.SubmitReport(request, CancellationToken.None);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
            var value = Assert.IsType<SubmitReportResponse>(createdResult.Value);
            Assert.Equal(reportId, value.ReportId);
        }

        [Fact]
        public async Task SubmitReport_MissingEvidenceFile_ReturnsUnprocessableEntity()
        {
            // Arrange
            var request = new SubmitReportRequest
            {
                DegreeId = Guid.NewGuid(),
                ReportType = ReportTypeEnum.Administrative_Error,
                Description = "No file",
                EvidenceFile = null!
            };

            // Act
            var actionResult = await _controller.SubmitReport(request, CancellationToken.None);

            // Assert
            Assert.IsType<UnprocessableEntityObjectResult>(actionResult);
        }

        [Fact]
        public async Task SubmitReport_CommandFailure_ReturnsUnprocessableEntityWithError()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");
            mockFile.Setup(f => f.FileName).Returns("evidence.pdf");
            mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.4 sample")));

            var request = new SubmitReportRequest
            {
                DegreeId = Guid.NewGuid(),
                ReportType = ReportTypeEnum.Administrative_Error,
                Description = "Typo",
                EvidenceFile = mockFile.Object
            };

            _mockSender.Setup(s => s.Send(It.IsAny<SubmitReportCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SubmitReportResponse>.Failure(ReportErrors.StudentCannotReportOthersDegree));

            // Act
            var actionResult = await _controller.SubmitReport(request, CancellationToken.None);

            // Assert
            var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(actionResult);
            Assert.NotNull(unprocessable.Value);
        }

        [Fact]
        public async Task GetReportEvidence_ValidReport_ReturnsFileStream()
        {
            // Arrange
            var reportId = Guid.NewGuid();
            var fakeStream = new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.4 content"));
            var response = new GetEvidenceResponse(fakeStream, "application/pdf", "evidence_safe.pdf");

            _mockSender.Setup(s => s.Send(It.IsAny<GetEvidenceQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetEvidenceResponse>.Success(response));

            // Act
            var actionResult = await _controller.GetReportEvidence(reportId, CancellationToken.None);

            // Assert
            var fileResult = Assert.IsType<FileStreamResult>(actionResult);
            Assert.Equal("application/pdf", fileResult.ContentType);
            Assert.Equal("evidence_safe.pdf", fileResult.FileDownloadName);
        }

        [Fact]
        public async Task GetReportEvidence_Unauthorized_ReturnsForbid()
        {
            // Arrange
            var reportId = Guid.NewGuid();
            _mockSender.Setup(s => s.Send(It.IsAny<GetEvidenceQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetEvidenceResponse>.Failure(ReportErrors.UnauthorizedEvidenceDownload));

            // Act
            var actionResult = await _controller.GetReportEvidence(reportId, CancellationToken.None);

            // Assert
            Assert.IsType<ForbidResult>(actionResult);
        }

        [Fact]
        public async Task ApproveReport_ValidId_ReturnsAccepted()
        {
            // Arrange
            var reportId = Guid.NewGuid();
            var response = new ApproveReportResponse("Approved", reportId, new[] { "Notification" }, DateTime.UtcNow);

            _mockSender.Setup(s => s.Send(It.IsAny<ApproveReportCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ApproveReportResponse>.Success(response));

            // Act
            var actionResult = await _controller.ApproveReport(reportId, CancellationToken.None);

            // Assert
            var acceptedResult = Assert.IsType<AcceptedResult>(actionResult);
            Assert.Equal(response, acceptedResult.Value);
        }

        [Fact]
        public async Task ApproveReport_NotFound_ReturnsNotFound()
        {
            // Arrange
            var reportId = Guid.NewGuid();
            _mockSender.Setup(s => s.Send(It.IsAny<ApproveReportCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ApproveReportResponse>.Failure(ReportErrors.NotFound));

            // Act
            var actionResult = await _controller.ApproveReport(reportId, CancellationToken.None);

            // Assert
            Assert.IsType<NotFoundObjectResult>(actionResult);
        }

        [Fact]
        public async Task RejectReport_ValidIdAndReason_ReturnsAccepted()
        {
            // Arrange
            var reportId = Guid.NewGuid();
            var request = new RejectReportRequest { Reason = "Insufficient evidence" };
            var response = new RejectReportResponse("Rejected", reportId, request.Reason, DateTime.UtcNow);

            _mockSender.Setup(s => s.Send(It.IsAny<RejectReportCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<RejectReportResponse>.Success(response));

            // Act
            var actionResult = await _controller.RejectReport(reportId, request, CancellationToken.None);

            // Assert
            var acceptedResult = Assert.IsType<AcceptedResult>(actionResult);
            Assert.Equal(response, acceptedResult.Value);
        }

        [Fact]
        public async Task RejectReport_NotFound_ReturnsNotFound()
        {
            // Arrange
            var reportId = Guid.NewGuid();
            var request = new RejectReportRequest { Reason = "Insufficient evidence" };
            _mockSender.Setup(s => s.Send(It.IsAny<RejectReportCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<RejectReportResponse>.Failure(ReportErrors.NotFound));

            // Act
            var actionResult = await _controller.RejectReport(reportId, request, CancellationToken.None);

            // Assert
            Assert.IsType<NotFoundObjectResult>(actionResult);
        }
    }
}
