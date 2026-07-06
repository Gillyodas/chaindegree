using System;

namespace ChainDegree.Core.Infrastructure.Configurations
{
    public class BatchingWorkerOptions
    {
        public const string SectionName = "Worker:Batching";
        public int MaxBatchSize { get; set; } = 500;
        public int MaxWaitTimeSeconds { get; set; } = 180;
        public int PollingIntervalSeconds { get; set; } = 10;
    }
}
