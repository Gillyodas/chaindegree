using System;

namespace ChainDegree.Core.Infrastructure.Configurations
{
    public class BatchingWorkerOptions
    {
        public const string SectionName = "Worker:Batching";
        public int MaxBatchSize { get; set; } = 500;
        public int MaxWaitTimeSeconds { get; set; } = 180;
        public int PollingIntervalSeconds { get; set; } = 10;
        public int ConsumerCount { get; set; } = 4;
        public int ChannelCapacity { get; set; } = 100;
        public int LeaseDurationMinutes { get; set; } = 5;
    }
}
