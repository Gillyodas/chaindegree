using System;
using ChainDegree.Core.Domain.SharedKernel.Interfaces;

namespace ChainDegree.Core.Domain.Reports.Events
{
    public record ReportRejectedEvent : IDomainEvent
    {
        public Guid ReportId { get; init; }
        public Guid TargetDegreeId { get; init; }
        public string Reason { get; init; }
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

        public ReportRejectedEvent(Guid reportId, Guid targetDegreeId, string reason)
        {
            ReportId = reportId;
            TargetDegreeId = targetDegreeId;
            Reason = reason;
        }
    }
}
