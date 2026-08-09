using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChainDegree.Core.Infrastructure.Blockchain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Nethereum.Web3;
using Xunit;

namespace ChainDegree.Infrastructure.Tests.Blockchain
{
    public class NonceManagerTests
    {
        private readonly Mock<IWeb3> _mockWeb3;
        private readonly Mock<IOptions<BlockchainOptions>> _mockOptions;
        private readonly Mock<ILogger<NonceManager>> _mockLogger;

        public NonceManagerTests()
        {
            _mockWeb3 = new Mock<IWeb3>();
            _mockOptions = new Mock<IOptions<BlockchainOptions>>();
            _mockLogger = new Mock<ILogger<NonceManager>>();

            _mockOptions.Setup(o => o.Value).Returns(new BlockchainOptions
            {
                RpcUrl = "http://localhost:8545",
                PrivateKey = "0x1234567890123456789012345678901234567890123456789012345678901234"
            });
        }

        [Fact]
        public async Task ReserveNonceAsync_ConcurrentCallers_ReturnsUniqueIncrementalNonces()
        {
            // Arrange
            var nonceManager = new NonceManager(_mockWeb3.Object, _mockOptions.Object, _mockLogger.Object);
            int callerCount = 50;
            var results = new ConcurrentBag<long>();

            // Act
            var tasks = Enumerable.Range(0, callerCount).Select(_ => Task.Run(async () =>
            {
                var nonce = await nonceManager.ReserveNonceAsync();
                results.Add(nonce);
            }));

            await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(callerCount, results.Count);
            var sortedResults = results.OrderBy(x => x).ToList();
            var distinctResults = results.Distinct().ToList();

            // Verify no duplicate nonces were issued
            Assert.Equal(callerCount, distinctResults.Count);

            // Verify nonces form a continuous sequence starting from 0
            for (int i = 0; i < callerCount; i++)
            {
                Assert.Equal(i, sortedResults[i]);
            }
        }
    }
}
