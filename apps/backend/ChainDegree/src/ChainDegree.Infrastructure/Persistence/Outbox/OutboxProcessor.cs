using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Domain.SharedKernel;
using ChainDegree.Core.Domain.SharedKernel.Interfaces;
using ChainDegree.Core.Infrastructure.Configurations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChainDegree.Core.Infrastructure.Persistence.Outbox
{
    public class OutboxProcessor : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutboxProcessor> _logger;
        private readonly OutboxOptions _options;

        public OutboxProcessor(
            IServiceProvider serviceProvider,
            ILogger<OutboxProcessor> logger,
            IOptions<OutboxOptions> options)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox processor background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOutboxMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing outbox messages.");
                }

                await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
            }

            _logger.LogInformation("Outbox processor background service stopped.");
        }

        private async Task ProcessOutboxMessagesAsync(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ChainDegreeDbContext>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var messages = await context.Set<OutboxMessage>()
                .IgnoreQueryFilters()
                .Where(m => m.ProcessedOn == null && m.RetryCount < _options.MaxRetryCount)
                .OrderBy(m => m.OccurredOn)
                .Take(_options.BatchSize)
                .ToListAsync(ct);

            if (!messages.Any())
            {
                return;
            }

            var sw = Stopwatch.StartNew();
            _logger.LogInformation("Processing {Count} outbox messages...", messages.Count);

            foreach (var message in messages)
            {
                try
                {
                    var eventType = Type.GetType(message.EventType);
                    if (eventType == null)
                    {
                        throw new InvalidOperationException($"Could not load event type: {message.EventType}");
                    }

                    var domainEvent = JsonSerializer.Deserialize(message.Payload, eventType) as IDomainEvent;
                    if (domainEvent == null)
                    {
                        throw new InvalidOperationException($"Could not deserialize event payload to IDomainEvent: {message.EventType}");
                    }

                    await mediator.Publish(domainEvent, ct);

                    message.MarkAsProcessed();
                    _logger.LogInformation("Outbox message published. MessageId={MessageId}, EventType={EventType}", message.Id, message.EventType);
                }
                catch (Exception ex)
                {
                    message.MarkAsFailed(ex.Message);
                    _logger.LogError(ex, "Publish failed. MessageId={MessageId}, EventType={EventType}, Retry={RetryCount}/{MaxRetry}, Reason={Reason}", 
                        message.Id, message.EventType, message.RetryCount, _options.MaxRetryCount, ex.Message);
                }
            }

            await context.SaveChangesAsync(ct);
            sw.Stop();
            _logger.LogInformation("Completed. Processed={Count}, Elapsed={ElapsedMs} ms", messages.Count, sw.ElapsedMilliseconds);
        }
    }
}
