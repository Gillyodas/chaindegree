using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.SharedKernel.Result;

namespace ChainDegree.Core.Application.Abstractions.Blockchain
{
    public interface IBlockchainService
    {
        Task<Result<AnchorResult>> AnchorMerkleRootAsync(
            string batchId,
            string merkleRoot,
            string institutionId,
            string actionType,
            CancellationToken ct = default);

        Task<Result<TransactionStatus>> GetTransactionStatusAsync(
            string txHash,
            CancellationToken ct = default);

        Task<Result<BatchMetadata>> GetBatchAsync(
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

    public sealed record AnchorResult(
        string TransactionHash,
        ulong? BlockNumber,
        DateTimeOffset SubmittedAt
    );

    public sealed record BatchMetadata(
        string MerkleRoot,
        ulong Timestamp,
        string InstitutionId,
        string ActionType,
        bool Exists
    );
}
