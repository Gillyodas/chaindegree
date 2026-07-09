using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChainDegree.Core.Application.Abstractions.Blockchain
{
    public interface IBlockchainService
    {
        Task<BlockchainTransactionResult> AnchorMerkleRootAsync(
            string merkleRoot,
            Guid batchId,
            CancellationToken ct = default);

        Task<string?> GetAnchoredMerkleRootAsync(
            string txHash,
            CancellationToken ct = default);
    }

    public record BlockchainTransactionResult(
        bool IsSuccess,
        string? TxHash,
        long? BlockNumber,
        string? ErrorMessage
    );
}
