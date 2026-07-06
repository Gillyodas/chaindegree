using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Queries;
using ChainDegree.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChainDegree.Core.Infrastructure.Services
{
    public class BatchTrackingService : IBatchQueryService
    {
        private readonly ChainDegreeDbContext _dbContext;

        public BatchTrackingService(ChainDegreeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<BatchQueryResponse?> GetBatchStatusAsync(Guid batchId, CancellationToken ct = default)
        {
            var batch = await _dbContext.BatchRecords.FirstOrDefaultAsync(x => x.Id == batchId, ct);
            if (batch == null)
            {
                return null;
            }

            return new BatchQueryResponse(
                batch.Id,
                batch.BatchName,
                batch.Status,
                batch.DegreeCount,
                batch.MerkleRoot,
                batch.TxHash,
                batch.BlockNumber,
                batch.EstimatedWaitTimeSeconds,
                batch.FailureReason,
                batch.CreatedAt,
                batch.CompletedAt
            );
        }
    }
}
