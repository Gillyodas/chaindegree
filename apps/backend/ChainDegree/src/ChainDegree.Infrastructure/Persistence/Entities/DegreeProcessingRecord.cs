using System;

namespace ChainDegree.Core.Infrastructure.Persistence.Entities
{
    public class DegreeProcessingRecord
    {
        public Guid DegreeId { get; set; }
        public int RetryCount { get; set; }
        public DateTime? NextRetryAt { get; set; }
        public DateTime? LastRetryAt { get; set; }
        public DateTime? LeaseUntil { get; set; }
        public string? WorkerId { get; set; }
    }
}
