using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Blockchain;

namespace ChainDegree.Core.Infrastructure.Blockchain
{
    public class FakeBlockchainService : IBlockchainService
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _anchoredRoots = 
            new(StringComparer.OrdinalIgnoreCase);

        public async Task<BlockchainTransactionResult> AnchorMerkleRootAsync(
            string merkleRoot,
            Guid batchId,
            CancellationToken ct = default)
        {
            // Simulate network latency
            await Task.Delay(100, ct);

            var random = new Random();
            if (random.Next(1, 100) == 99) // 1% failure rate for simulation
            {
                return new BlockchainTransactionResult(
                    IsSuccess: false,
                    TxHash: null,
                    BlockNumber: null,
                    ErrorMessage: "Simulated blockchain connection timeout."
                );
            }

            var txHash = "0x" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var blockNumber = DateTime.UtcNow.Ticks % 10000000;

            _anchoredRoots[txHash] = merkleRoot;

            return new BlockchainTransactionResult(
                IsSuccess: true,
                TxHash: txHash,
                BlockNumber: blockNumber,
                ErrorMessage: null
            );
        }

        public async Task<string?> GetAnchoredMerkleRootAsync(
            string txHash,
            CancellationToken ct = default)
        {
            await Task.Delay(50, ct);
            if (_anchoredRoots.TryGetValue(txHash, out var root))
            {
                return root;
            }

            // Fallback for tests that might use hardcoded txHashes
            if (txHash.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return "0x7777777777777777777777777777777777777777777777777777777777777777";
            }

            return null;
        }
    }
}
