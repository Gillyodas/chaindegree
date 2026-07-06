using System;

namespace ChainDegree.Core.Infrastructure.Persistence.Entities
{
    public class BatchDegreeRecord
    {
        public Guid BatchId { get; set; }
        public Guid DegreeId { get; set; }
        public int LeafIndex { get; set; }
        public string? ProofHashesJson { get; set; }
    }
}
