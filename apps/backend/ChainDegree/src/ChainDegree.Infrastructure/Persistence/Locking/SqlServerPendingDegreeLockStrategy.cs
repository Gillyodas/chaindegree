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
                SELECT TOP ({0}) *
                FROM DEGREES WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE Status = 'Pending_Confirmation' AND DeletedAt IS NULL
                ORDER BY CreatedAt";

            return await dbContext.Degrees
                .FromSqlRaw(query, batchSize)
                .ToListAsync(ct);
        }
    }
}
