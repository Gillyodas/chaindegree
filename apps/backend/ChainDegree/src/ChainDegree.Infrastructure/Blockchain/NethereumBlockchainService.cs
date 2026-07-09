using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Blockchain;
using ChainDegree.Core.Infrastructure.Configurations;
using Microsoft.Extensions.Options;

namespace ChainDegree.Core.Infrastructure.Blockchain
{
    public class NethereumBlockchainService : IBlockchainService
    {
        private readonly BesuOptions _options;

        public NethereumBlockchainService(IOptions<BesuOptions> options)
        {
            _options = options.Value;
        }

        public async Task<BlockchainTransactionResult> AnchorMerkleRootAsync(
            string merkleRoot,
            Guid batchId,
            CancellationToken ct = default)
        {
            // If private key or contract address is empty, delegate to fake simulation
            if (string.IsNullOrEmpty(_options.AccountPrivateKey) || string.IsNullOrEmpty(_options.ContractAddress))
            {
                var fakeService = new FakeBlockchainService();
                return await fakeService.AnchorMerkleRootAsync(merkleRoot, batchId, ct);
            }

            // Simulated Nethereum execution with Besu options
            await Task.Delay(150, ct);
            var txHash = "0x" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var blockNumber = DateTime.UtcNow.Ticks % 10000000;

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
            // For this phase, both simulated and empty configurations delegate to the FakeBlockchainService
            // which handles the static storage of anchored roots.
            var fakeService = new FakeBlockchainService();
            return await fakeService.GetAnchoredMerkleRootAsync(txHash, ct);
        }
    }
}
