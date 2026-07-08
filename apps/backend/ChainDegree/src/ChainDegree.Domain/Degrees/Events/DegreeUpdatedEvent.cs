using System;
using ChainDegree.Core.Domain.SharedKernel.Interfaces;

namespace ChainDegree.Core.Domain.Degrees.Events
{
    public record DegreeUpdatedEvent : IDomainEvent
    {
        public Guid DegreeId { get; init; }
        public Guid InstitutionId { get; init; }
        public string ReasonCode { get; init; }
        public string PreviousHash { get; init; }
        public string NewHash { get; init; }
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

        public DegreeUpdatedEvent(Guid degreeId, Guid institutionId, string reasonCode, string previousHash, string newHash)
        {
            DegreeId = degreeId;
            InstitutionId = institutionId;
            ReasonCode = reasonCode;
            PreviousHash = previousHash;
            NewHash = newHash;
        }
    }
}
