using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Blockchain;
using ChainDegree.Core.Application.Abstractions.Crypto;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Degrees.Interfaces;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
using ChainDegree.Core.Infrastructure.BackgroundWorkers;
using ChainDegree.Core.Infrastructure.Configurations;
using ChainDegree.Core.Infrastructure.Persistence;
using ChainDegree.Core.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ChainDegree.Infrastructure.Tests.BackgroundWorkers
{
    public class BatchingDegreeWorkerTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IMerkleTreeService> _mockMerkleTree;
        private readonly Mock<IBlockchainService> _mockBlockchain;
        private readonly Mock<IDegreeRepository> _mockRepo;
        private readonly Mock<IOptions<BatchingWorkerOptions>> _mockOptions;
        private readonly Mock<INonceManager> _mockNonceManager;
        private readonly Mock<ILogger<BatchingDegreeWorker>> _mockLogger;

        public BatchingDegreeWorkerTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockMerkleTree = new Mock<IMerkleTreeService>();
            _mockBlockchain = new Mock<IBlockchainService>();
            _mockRepo = new Mock<IDegreeRepository>();
            _mockOptions = new Mock<IOptions<BatchingWorkerOptions>>();
            _mockNonceManager = new Mock<INonceManager>();
            _mockLogger = new Mock<ILogger<BatchingDegreeWorker>>();

            var options = new BatchingWorkerOptions
            {
                MaxBatchSize = 2,
                MaxWaitTimeSeconds = 180,
                PollingIntervalSeconds = 1
            };
            _mockOptions.Setup(o => o.Value).Returns(options);
        }

        // We can test the constructor and verify HostedService lifecycle runs cleanly
        [Fact]
        public void Constructor_WithValidArguments_ShouldInstantiate()
        {
            // Act
            var worker = new BatchingDegreeWorker(
                _mockServiceProvider.Object,
                _mockOptions.Object,
                _mockNonceManager.Object,
                _mockLogger.Object);

            // Assert
            Assert.NotNull(worker);
        }
    }
}
