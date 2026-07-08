using System;
using ChainDegree.Core.Domain.SharedKernel.Interfaces;

namespace ChainDegree.Core.Domain.Degrees.Events
{
    public record DegreeRevokedEvent : IDomainEvent
    {
        public Guid DegreeId { get; init; }
        public Guid InstitutionId { get; init; }
        public string ReasonCode { get; init; }
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

        public DegreeRevokedEvent(Guid degreeId, Guid institutionId, string reasonCode)
        {
            DegreeId = degreeId;
            InstitutionId = institutionId;
            ReasonCode = reasonCode;
        }
    }
}
