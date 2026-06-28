using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.Core.Domain.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ChainDegree.Core.Infrastructure.Persistence.Interceptors
{
    public class DispatchDomainEventsInterceptor : SaveChangesInterceptor
    {
        private readonly IMediator _mediator;
        public DispatchDomainEventsInterceptor(IMediator mediator) => _mediator = mediator;

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
        {
            var context = eventData.Context;
            if (context is null) return await base.SavedChangesAsync(eventData, result, ct);

            var events = context.ChangeTracker.Entries<AggregateRoot>()
                .SelectMany(e => e.Entity.DomainEvents)
                .ToList();

            context.ChangeTracker.Entries<AggregateRoot>()
                .ToList().ForEach(e => e.Entity.ClearDomainEvents());

            foreach (var domainEvent in events)
                await _mediator.Publish(domainEvent, ct);

            return await base.SavedChangesAsync(eventData, result, ct);
        }
    }
}
