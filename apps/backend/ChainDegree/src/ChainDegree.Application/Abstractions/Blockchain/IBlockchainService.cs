using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChainDegree.Core.Application.Abstractions.Blockchain
{
    public interface IBlockchainService
    {
        Task<AnchorResult> AnchorMerkleRootAsync(
            string batchId,
            string merkleRoot,
            string institutionId,
            string actionType,
            CancellationToken ct = default);

        Task<TransactionStatus> GetTransactionStatusAsync(
            string txHash,
            CancellationToken ct = default);

        Task<string?> GetAnchoredMerkleRootAsync(
            string txHash,
            CancellationToken ct = default);

        Task<bool> CheckBatchExistsAsync(
            string batchId,
            CancellationToken ct = default);
    }

    public enum TransactionStatus
    {
        Pending,
        Confirmed,
        Failed,
        NotFound
    }

    public sealed record AnchorResult
    {
        public string TransactionHash { get; init; } = null!;
        public ulong? BlockNumber { get; init; }
        public DateTimeOffset SubmittedAt { get; init; }
    }
}
