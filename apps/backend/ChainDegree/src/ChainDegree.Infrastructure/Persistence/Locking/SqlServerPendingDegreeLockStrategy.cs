using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Domain.Degrees;
using Microsoft.EntityFrameworkCore;

namespace ChainDegree.Core.Infrastructure.Persistence.Locking
{
    public class SqlServerPendingDegreeLockStrategy : IPendingDegreeLockStrategy
    {
        public async Task<List<Degree>> GetAndLockPendingDegreesAsync(
            ChainDegreeDbContext dbContext,
            int batchSize,
            CancellationToken ct = default)
        {
            var query = @"
                SELECT TOP ({0}) d.*
                FROM DEGREES d WITH (UPDLOCK, READPAST, ROWLOCK)
                LEFT JOIN DEGREE_PROCESSING_RECORDS pr ON d.Id = pr.DegreeId
                WHERE (d.Status = 'Pending_Confirmation' 
                       OR (d.Status = 'Confirmation_Error' AND pr.NextRetryAt IS NOT NULL AND pr.NextRetryAt <= {1}))
                  AND d.DeletedAt IS NULL
                ORDER BY d.CreatedAt";

            return await dbContext.Degrees
                .FromSqlRaw(query, batchSize, System.DateTime.UtcNow)
                .ToListAsync(ct);
        }
    }
}
