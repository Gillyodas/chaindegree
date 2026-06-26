using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.Core.Domain.SharedKernel.Interfaces;

namespace ChainDegree.Core.Domain.Students.Events
{
    internal record StudentDeactivatedEvent : IDomainEvent
    {
        public Guid StudentId { get; init; }
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

        public StudentDeactivatedEvent(Guid studentId)
        {
            StudentId = studentId;
        }
    }
}
