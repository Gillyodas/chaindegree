using System;

namespace ChainDegree.Core.Domain.SharedKernel
{
    public sealed class OutboxMessage
    {
        public Guid Id { get; private set; }
        public string EventType { get; private set; } = null!;
        public string Payload { get; private set; } = null!;
        public DateTime OccurredOn { get; private set; }
        public DateTime? ProcessedOn { get; private set; }
        public string? Error { get; private set; }
        public int RetryCount { get; private set; }

        private OutboxMessage() { }

        public OutboxMessage(Guid id, string eventType, string payload, DateTime occurredOn)
        {
            Id = id;
            EventType = eventType;
            Payload = payload;
            OccurredOn = occurredOn;
        }

        public void MarkAsProcessed()
        {
            ProcessedOn = DateTime.UtcNow;
            Error = null;
        }

        public void MarkAsFailed(string error)
        {
            Error = error;
            RetryCount++;
        }
    }
}
