using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.Core.Domain.SharedKernel.Interfaces;

namespace ChainDegree.Core.Domain.Recruiters.Events
{
    public record CompanyDeactivatedEvent : IDomainEvent
    {
        public Guid CompanyId { get; init; }
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

        public CompanyDeactivatedEvent(Guid companyId)
        {
            CompanyId = companyId;
        }
    }
}
