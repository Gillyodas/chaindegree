using System;

namespace ChainDegree.Core.Infrastructure.Persistence.Entities
{
    public class IdempotencyRecord
    {
        public string IdempotencyKey { get; set; } = null!;
        public string ResponseBodyJson { get; set; } = null!;
        public int ResponseStatusCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
