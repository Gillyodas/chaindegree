using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChainDegree.Core.Application.Abstractions.Queries
{
    public interface IBatchQueryService
    {
        Task<BatchQueryResponse?> GetBatchStatusAsync(Guid batchId, CancellationToken ct = default);
    }

    public sealed record BatchQueryResponse(
        Guid BatchId,
        string BatchName,
        string Status,
        int DegreeCount,
        string? MerkleRoot,
        string? TxHash,
        long? BlockNumber,
        int EstimatedWaitTimeSeconds,
        string? FailureReason,
        DateTime CreatedAt,
        DateTime? CompletedAt
    );
}
