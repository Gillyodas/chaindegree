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

        [Fact]
        [Trait("Category", "WorkerLogging")]
        public void TestLogger_ShouldCaptureScope_WithBatchId_BlockchainTxHash_AndCorrelationId()
        {
            // Arrange
            var testLogger = new TestLogger<BatchingDegreeWorker>();
            var expectedBatchId = Guid.NewGuid();
            var expectedTxHash = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef";
            var expectedCorrelationId = Guid.NewGuid().ToString();

            // Act
            using (testLogger.BeginScope(new Dictionary<string, object>
            {
                ["BatchCorrelationId"] = expectedCorrelationId,
                ["BatchId"] = expectedBatchId,
                ["BlockchainTxHash"] = expectedTxHash
            }))
            {
                testLogger.LogInformation("Batch {BatchId} confirmed. TxHash={BlockchainTxHash}, TotalElapsedMs={ElapsedMs}",
                    expectedBatchId, expectedTxHash, 150);
            }

            // Assert
            Assert.Single(testLogger.CapturedScopes);
            var scope = testLogger.CapturedScopes[0];
            Assert.Equal(expectedCorrelationId, scope["BatchCorrelationId"]);
            Assert.Equal(expectedBatchId, scope["BatchId"]);
            Assert.Equal(expectedTxHash, scope["BlockchainTxHash"]);

            Assert.Single(testLogger.CapturedLogs);
            var log = testLogger.CapturedLogs[0];
            Assert.Contains(expectedBatchId.ToString(), log.Message);
            Assert.Contains(expectedTxHash, log.Message);
            Assert.Contains("150", log.Message);
        }

        [Fact]
        [Trait("Category", "WorkerLogging")]
        public void WorkerLogging_BatchCorrelationId_ShouldBeUniquePerScopeCycle()
        {
            // Arrange
            var testLogger = new TestLogger<BatchingDegreeWorker>();

            // Act - Simulate 3 processing cycles
            for (int i = 0; i < 3; i++)
            {
                var correlationId = Guid.NewGuid().ToString();
                using (testLogger.BeginScope(new Dictionary<string, object>
                {
                    ["BatchCorrelationId"] = correlationId,
                    ["BatchId"] = Guid.NewGuid(),
                    ["BlockchainTxHash"] = $"0x{i}"
                }))
                {
                    testLogger.LogInformation("Cycle {CycleIndex} processed", i);
                }
            }

            // Assert
            Assert.Equal(3, testLogger.CapturedScopes.Count);
            var correlationIds = new HashSet<string>();
            foreach (var scope in testLogger.CapturedScopes)
            {
                var id = scope["BatchCorrelationId"]?.ToString();
                Assert.NotNull(id);
                Assert.True(correlationIds.Add(id!), $"BatchCorrelationId {id} was repeated!");
            }

            Assert.Equal(3, correlationIds.Count);
        }
    }

    public class TestLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> CapturedLogs { get; } = new();
        public List<Dictionary<string, object>> CapturedScopes { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            if (state is IEnumerable<KeyValuePair<string, object>> dict)
            {
                var scopeDict = new Dictionary<string, object>();
                foreach (var kvp in dict)
                {
                    scopeDict[kvp.Key] = kvp.Value;
                }
                CapturedScopes.Add(scopeDict);
            }
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            CapturedLogs.Add((logLevel, message));
        }

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();
            public void Dispose() { }
        }
    }
}
