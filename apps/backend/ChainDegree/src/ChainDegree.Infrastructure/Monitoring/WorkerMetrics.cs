using Prometheus;

namespace ChainDegree.Core.Infrastructure.Monitoring
{
    public class WorkerMetrics
    {
        public Gauge QueueLength { get; } = Metrics.CreateGauge(
            "chaindegree_worker_queue_length",
            "Number of degrees waiting in queue to be batched and anchored.");

        public Counter BatchesProcessed { get; } = Metrics.CreateCounter(
            "chaindegree_worker_batches_processed_total",
            "Total number of degree batches successfully processed and anchored.");

        public Counter BatchesFailed { get; } = Metrics.CreateCounter(
            "chaindegree_worker_batches_failed_total",
            "Total number of degree batches that failed processing.");

        public Histogram BatchLatency { get; } = Metrics.CreateHistogram(
            "chaindegree_worker_batch_latency_seconds",
            "Total batch processing duration from formation to confirmation.",
            new HistogramConfiguration
            {
                Buckets = new double[] { 0.1, 0.5, 1, 2, 5, 10, 30, 60, 120, 300 }
            });

        public Histogram MerkleBuildTime { get; } = Metrics.CreateHistogram(
            "chaindegree_worker_merkle_build_time_seconds",
            "Duration to construct the Merkle Tree for a batch.",
            new HistogramConfiguration
            {
                Buckets = new double[] { 0.001, 0.005, 0.01, 0.05, 0.1, 0.5, 1 }
            });

        public Histogram BlockchainTxTime { get; } = Metrics.CreateHistogram(
            "chaindegree_worker_blockchain_tx_time_seconds",
            "Duration of blockchain transaction submission and confirmation.",
            new HistogramConfiguration
            {
                Buckets = new double[] { 0.1, 0.5, 1, 2, 5, 10, 30, 60 }
            });

        public Counter RetryCount { get; } = Metrics.CreateCounter(
            "chaindegree_worker_retry_count_total",
            "Total number of transient retries performed by the worker.");

        public Gauge LeaseOrphanCount { get; } = Metrics.CreateGauge(
            "chaindegree_worker_lease_orphan_count",
            "Number of orphaned processing records whose worker leases have expired.");
    }
}
