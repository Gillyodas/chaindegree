using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace ChainDegree.Core.Domain.SharedKernel.Interfaces
{
    public interface IDomainEvent : INotification
    {
        Guid EventId { get; }
        DateTime OccurredOn { get; }
    }
}
