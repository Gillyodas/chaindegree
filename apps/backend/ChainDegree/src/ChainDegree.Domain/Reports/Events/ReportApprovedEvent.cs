using System;
using ChainDegree.Core.Domain.Reports.Enums;
using ChainDegree.Core.Domain.SharedKernel.Interfaces;

namespace ChainDegree.Core.Domain.Reports.Events
{
    public record ReportApprovedEvent : IDomainEvent
    {
        public Guid ReportId { get; init; }
        public Guid TargetDegreeId { get; init; }
        public ReportTypeEnum ReportType { get; init; }
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

        public ReportApprovedEvent(Guid reportId, Guid targetDegreeId, ReportTypeEnum reportType)
        {
            ReportId = reportId;
            TargetDegreeId = targetDegreeId;
            ReportType = reportType;
        }
    }
}
