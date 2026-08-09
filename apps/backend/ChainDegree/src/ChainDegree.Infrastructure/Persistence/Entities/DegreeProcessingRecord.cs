using System;

namespace ChainDegree.Core.Infrastructure.Persistence.Entities
{
    public class DegreeProcessingRecord
    {
        public Guid DegreeId { get; set; }
        public string ActionType { get; set; } = null!; // Issue, Update, Revoke
        public string State { get; set; } = DegreeProcessingState.Queued; // Queued, Processing, Unknown, Submitted, Completed, Failed
        public int RetryCount { get; set; }
        public DateTime? NextRetryAt { get; set; }
        public DateTime? LastRetryAt { get; set; }
        public DateTime? LeaseUntil { get; set; }
        public string? LeaseId { get; set; }
        public string? WorkerId { get; set; }
        public string? LastError { get; set; }
        public string? BlockchainTxHash { get; set; }
        public string? CorrelationId { get; set; }
    }
}
