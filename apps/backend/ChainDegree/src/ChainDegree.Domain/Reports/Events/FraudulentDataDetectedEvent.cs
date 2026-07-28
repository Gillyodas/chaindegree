using System;
using ChainDegree.Core.Domain.SharedKernel.Interfaces;

namespace ChainDegree.Core.Domain.Reports.Events
{
    public record FraudulentDataDetectedEvent : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public Guid UniversityId { get; init; }
        public Guid DegreeId { get; init; }
        public string ViolationType { get; init; } = null!;
        public Guid ReportId { get; init; }
        public string ViolationDetails { get; init; } = null!;
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

        public FraudulentDataDetectedEvent(Guid universityId, Guid degreeId, string violationType, Guid reportId, string violationDetails)
        {
            UniversityId = universityId;
            DegreeId = degreeId;
            ViolationType = violationType;
            ReportId = reportId;
            ViolationDetails = violationDetails;
        }
    }
}
