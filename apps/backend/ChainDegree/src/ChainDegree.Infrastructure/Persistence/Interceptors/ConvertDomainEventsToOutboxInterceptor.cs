using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Domain.SharedKernel;
using ChainDegree.Core.Domain.SharedKernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ChainDegree.Core.Infrastructure.Persistence.Interceptors
{
    public class ConvertDomainEventsToOutboxInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            ConvertDomainEvents(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            ConvertDomainEvents(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void ConvertDomainEvents(DbContext? context)
        {
            if (context == null) return;

            var entries = context.ChangeTracker.Entries<AggregateRoot>()
                .Where(e => e.Entity.DomainEvents.Any())
                .ToList();

            var domainEvents = entries
                .SelectMany(e => e.Entity.DomainEvents)
                .ToList();

            entries.ForEach(e => e.Entity.ClearDomainEvents());

            foreach (var domainEvent in domainEvents)
            {
                var eventType = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().FullName ?? domainEvent.GetType().Name;
                var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());

                var outboxMessage = new OutboxMessage(
                    Guid.NewGuid(),
                    eventType,
                    payload,
                    DateTime.UtcNow);

                context.Set<OutboxMessage>().Add(outboxMessage);
            }
        }
    }
}
