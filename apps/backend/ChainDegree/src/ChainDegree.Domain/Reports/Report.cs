using System;
using ChainDegree.Core.Domain.Reports.Enums;
using ChainDegree.Core.Domain.Reports.Events;
using ChainDegree.Core.Domain.SharedKernel;
using ChainDegree.Core.Domain.SharedKernel.Enums;
using ChainDegree.SharedKernel.Common.Error;
using ChainDegree.SharedKernel.Result;

namespace ChainDegree.Core.Domain.Reports
{
    public class Report : AggregateRoot
    {
        public Guid TargetDegreeId { get; private set; }
        public Guid ReporterId { get; private set; }
        public UserRoleEnum ReporterRole { get; private set; }
        public ReportTypeEnum ReportType { get; private set; }
        public string Description { get; private set; } = null!;
        public string? EvidenceFileName { get; private set; }
        public ReportStatusEnum Status { get; private set; }
        public DateTime? ReviewedAt { get; private set; }
        public string? RejectionReason { get; private set; }

        private Report() { }

        public static Report Create(
            Guid targetDegreeId,
            Guid reporterId,
            UserRoleEnum reporterRole,
            ReportTypeEnum reportType,
            string description,
            string? evidenceFileName)
        {
            if (targetDegreeId == Guid.Empty)
                throw new ArgumentException("Target degree id cannot be empty.", nameof(targetDegreeId));
            if (reporterId == Guid.Empty)
                throw new ArgumentException("Reporter id cannot be empty.", nameof(reporterId));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description cannot be empty.", nameof(description));

            var report = new Report
            {
                Id = Guid.NewGuid(),
                TargetDegreeId = targetDegreeId,
                ReporterId = reporterId,
                ReporterRole = reporterRole,
                ReportType = reportType,
                Description = description.Trim(),
                EvidenceFileName = evidenceFileName,
                Status = ReportStatusEnum.Pending_Review
            };

            report.RaiseDomainEvent(new ReportSubmittedEvent(
                report.Id,
                report.TargetDegreeId,
                report.ReporterId,
                report.ReporterRole,
                report.ReportType));

            return report;
        }

        public Result Approve(Guid? universityId = null)
        {
            if (Status != ReportStatusEnum.Pending_Review)
            {
                return Result.Failure(new Error("Report.AlreadyReviewed", "Report has already been reviewed.", ErrorType.Conflict));
            }

            Status = ReportStatusEnum.Approved;
            ReviewedAt = DateTime.UtcNow;

            RaiseDomainEvent(new ReportApprovedEvent(Id, TargetDegreeId, ReportType));

            if (ReportType == ReportTypeEnum.Fraudulent_Data)
            {
                RaiseDomainEvent(new FraudulentDataDetectedEvent(
                    universityId ?? Guid.Empty,
                    TargetDegreeId,
                    ReportType.ToString(),
                    Id,
                    Description));
            }

            return Result.Success();
        }

        public Result Reject(string reason)
        {
            if (Status != ReportStatusEnum.Pending_Review)
            {
                return Result.Failure(new Error("Report.AlreadyReviewed", "Report has already been reviewed.", ErrorType.Conflict));
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return Result.Failure(new Error("Report.EmptyRejectionReason", "Rejection reason cannot be empty.", ErrorType.Validation));
            }

            Status = ReportStatusEnum.Rejected;
            ReviewedAt = DateTime.UtcNow;
            RejectionReason = reason.Trim();

            RaiseDomainEvent(new ReportRejectedEvent(Id, TargetDegreeId, RejectionReason));

            return Result.Success();
        }
    }
}
