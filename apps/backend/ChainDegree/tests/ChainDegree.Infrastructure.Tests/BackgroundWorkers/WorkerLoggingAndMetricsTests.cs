using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChainDegree.Core.Infrastructure.BackgroundWorkers;
using ChainDegree.Core.Infrastructure.Configurations;
using ChainDegree.Core.Infrastructure.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ChainDegree.Infrastructure.Tests.BackgroundWorkers
{
    public class WorkerLoggingAndMetricsTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IOptions<BatchingWorkerOptions>> _mockOptions;
        private readonly Mock<ILogger<BatchingDegreeWorker>> _mockLogger;

        public WorkerLoggingAndMetricsTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockOptions = new Mock<IOptions<BatchingWorkerOptions>>();
            _mockLogger = new Mock<ILogger<BatchingDegreeWorker>>();

            var options = new BatchingWorkerOptions
            {
                MaxBatchSize = 500,
                MaxWaitTimeSeconds = 180,
                PollingIntervalSeconds = 15
            };
            _mockOptions.Setup(o => o.Value).Returns(options);
        }

        [Fact]
        [Trait("Category", "WorkerLogging")]
        public void WorkerMetrics_ShouldInitializeMetricTypes()
        {
            // Act
            var metrics = new WorkerMetrics();

            // Assert
            Assert.NotNull(metrics.QueueLength);
            Assert.NotNull(metrics.BatchesProcessed);
            Assert.NotNull(metrics.BatchesFailed);
            Assert.NotNull(metrics.BatchLatency);
            Assert.NotNull(metrics.MerkleBuildTime);
            Assert.NotNull(metrics.BlockchainTxTime);
            Assert.NotNull(metrics.RetryCount);
            Assert.NotNull(metrics.LeaseOrphanCount);
        }

        [Fact]
        [Trait("Category", "WorkerMetrics")]
        public void BatchingDegreeWorker_WithMetricsInjected_ShouldInstantiateSuccessfully()
        {
            // Arrange
            var metrics = new WorkerMetrics();

            // Act
            var worker = new BatchingDegreeWorker(
                _mockServiceProvider.Object,
                _mockOptions.Object,
                _mockLogger.Object,
                metrics);

            // Assert
            Assert.NotNull(worker);
        }
    }
}
