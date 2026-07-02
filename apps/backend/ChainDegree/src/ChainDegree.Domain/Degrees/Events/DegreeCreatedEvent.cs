using System;
using ChainDegree.Core.Domain.SharedKernel.Interfaces;

namespace ChainDegree.Core.Domain.Degrees.Events
{
    public record DegreeCreatedEvent : IDomainEvent
    {
        public Guid DegreeId { get; init; }
        public Guid InstitutionId { get; init; }
        public Guid StudentId { get; init; }
        public string DegreeCode { get; init; } = null!;
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

        public DegreeCreatedEvent(Guid degreeId, Guid institutionId, Guid studentId, string degreeCode)
        {
            DegreeId = degreeId;
            InstitutionId = institutionId;
            StudentId = studentId;
            DegreeCode = degreeCode;
        }
    }
}
