using System;
using ChainDegree.Core.Domain.Reports.Enums;
using ChainDegree.Core.Domain.SharedKernel.Enums;
using ChainDegree.Core.Domain.SharedKernel.Interfaces;

namespace ChainDegree.Core.Domain.Reports.Events
{
    public record ReportSubmittedEvent : IDomainEvent
    {
        public Guid ReportId { get; init; }
        public Guid TargetDegreeId { get; init; }
        public Guid ReporterId { get; init; }
        public UserRoleEnum ReporterRole { get; init; }
        public ReportTypeEnum ReportType { get; init; }
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

        public ReportSubmittedEvent(Guid reportId, Guid targetDegreeId, Guid reporterId, UserRoleEnum reporterRole, ReportTypeEnum reportType)
        {
            ReportId = reportId;
            TargetDegreeId = targetDegreeId;
            ReporterId = reporterId;
            ReporterRole = reporterRole;
            ReportType = reportType;
        }
    }
}
