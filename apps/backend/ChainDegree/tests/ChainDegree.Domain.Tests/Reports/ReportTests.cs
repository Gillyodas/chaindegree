using System;
using System.Linq;
using ChainDegree.Core.Domain.Reports;
using ChainDegree.Core.Domain.Reports.Enums;
using ChainDegree.Core.Domain.Reports.Events;
using ChainDegree.Core.Domain.SharedKernel.Enums;
using Xunit;

namespace ChainDegree.Domain.Tests.Reports
{
    public class ReportTests
    {
        [Fact]
        public void Create_ValidParameters_ReturnsReportWithPendingReviewAndSubmittedEvent()
        {
            // Arrange
            var degreeId = Guid.NewGuid();
            var reporterId = Guid.NewGuid();
            var role = UserRoleEnum.Student;
            var reportType = ReportTypeEnum.Administrative_Error;
            var description = "Incorrect major name on degree.";
            var fileName = "evidence_123.pdf";

            // Act
            var report = Report.Create(degreeId, reporterId, role, reportType, description, fileName).Value;

            // Assert
            Assert.NotNull(report);
            Assert.NotEqual(Guid.Empty, report.Id);
            Assert.Equal(degreeId, report.TargetDegreeId);
            Assert.Equal(reporterId, report.ReporterId);
            Assert.Equal(role, report.ReporterRole);
            Assert.Equal(reportType, report.ReportType);
            Assert.Equal(description, report.Description);
            Assert.Equal(fileName, report.EvidenceFileName);
            Assert.Equal(ReportStatusEnum.Pending_Review, report.Status);

            Assert.Single(report.DomainEvents);
            var submittedEvent = Assert.IsType<ReportSubmittedEvent>(report.DomainEvents.First());
            Assert.Equal(report.Id, submittedEvent.ReportId);
        }

        [Fact]
        public void Create_EmptyDegreeId_ReturnsFailure()
        {
            var result = Report.Create(Guid.Empty, Guid.NewGuid(), UserRoleEnum.Student, ReportTypeEnum.Administrative_Error, "Desc", "file.pdf");
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void Approve_PendingReview_FraudulentData_RaisesBothApprovedAndFraudulentEvents()
        {
            // Arrange
            var report = Report.Create(Guid.NewGuid(), Guid.NewGuid(), UserRoleEnum.Recruiter, ReportTypeEnum.Fraudulent_Data, "Fake degree detected", "proof.png").Value;
            report.ClearDomainEvents();
            var universityId = Guid.NewGuid();

            // Act
            var result = report.Approve(universityId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ReportStatusEnum.Approved, report.Status);
            Assert.NotNull(report.ReviewedAt);

            Assert.Equal(2, report.DomainEvents.Count);
            Assert.Contains(report.DomainEvents, e => e is ReportApprovedEvent);
            var fraudEvent = Assert.IsType<FraudulentDataDetectedEvent>(report.DomainEvents.First(e => e is FraudulentDataDetectedEvent));
            Assert.Equal(universityId, fraudEvent.UniversityId);
            Assert.Equal(report.TargetDegreeId, fraudEvent.DegreeId);
        }

        [Fact]
        public void Approve_PendingReview_AdministrativeError_RaisesOnlyReportApprovedEvent()
        {
            // Arrange
            var report = Report.Create(Guid.NewGuid(), Guid.NewGuid(), UserRoleEnum.Student, ReportTypeEnum.Administrative_Error, "Typo in name", "proof.pdf").Value;
            report.ClearDomainEvents();

            // Act
            var result = report.Approve();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ReportStatusEnum.Approved, report.Status);

            Assert.Single(report.DomainEvents);
            Assert.IsType<ReportApprovedEvent>(report.DomainEvents.First());
            Assert.DoesNotContain(report.DomainEvents, e => e is FraudulentDataDetectedEvent);
        }

        [Fact]
        public void Approve_AlreadyApproved_ReturnsFailure()
        {
            // Arrange
            var report = Report.Create(Guid.NewGuid(), Guid.NewGuid(), UserRoleEnum.Student, ReportTypeEnum.Administrative_Error, "Typo", "file.pdf").Value;
            report.Approve();

            // Act
            var result = report.Approve();

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Report.AlreadyReviewed", result.Error.Code);
        }

        [Fact]
        public void Reject_ValidReason_SetsStatusRejectedAndRaisesEvent()
        {
            // Arrange
            var report = Report.Create(Guid.NewGuid(), Guid.NewGuid(), UserRoleEnum.Recruiter, ReportTypeEnum.Fraudulent_Data, "Invalid claim", "file.png").Value;
            report.ClearDomainEvents();
            var reason = "Insufficient evidence provided.";

            // Act
            var result = report.Reject(reason);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ReportStatusEnum.Rejected, report.Status);
            Assert.Equal(reason, report.RejectionReason);

            Assert.Single(report.DomainEvents);
            var rejectedEvent = Assert.IsType<ReportRejectedEvent>(report.DomainEvents.First());
            Assert.Equal(reason, rejectedEvent.Reason);
        }

        [Fact]
        public void Reject_EmptyReason_ReturnsFailure()
        {
            // Arrange
            var report = Report.Create(Guid.NewGuid(), Guid.NewGuid(), UserRoleEnum.Recruiter, ReportTypeEnum.Fraudulent_Data, "Invalid claim", "file.png").Value;

            // Act
            var result = report.Reject("");

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Report.EmptyRejectionReason", result.Error.Code);
        }
    }
}
