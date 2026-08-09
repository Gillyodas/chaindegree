using System;

namespace ChainDegree.Core.Infrastructure.Persistence.Entities
{
    public class BatchRecord
    {
        public Guid Id { get; set; }
        public Guid InstitutionId { get; set; }
        public string BatchName { get; set; } = null!;
        public string Status { get; set; } = BatchStatus.Pending; // Pending, Processing, Unknown, Submitted, Completed, Failed
        public int DegreeCount { get; set; }
        public string? MerkleRoot { get; set; }
        public string? TxHash { get; set; }
        public long? BlockNumber { get; set; }
        public int EstimatedWaitTimeSeconds { get; set; }
        public string? FailureReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
