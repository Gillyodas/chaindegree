namespace ChainDegree.Core.Infrastructure.Configurations
{
    public class OutboxOptions
    {
        public const string SectionName = "Outbox";
        public int PollingIntervalSeconds { get; set; } = 5;
        public int BatchSize { get; set; } = 20;
        public int MaxRetryCount { get; set; } = 5;
    }
}
