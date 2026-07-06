using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Domain.Degrees;

namespace ChainDegree.Core.Infrastructure.Persistence.Locking
{
    public interface IPendingDegreeLockStrategy
    {
        Task<List<Degree>> GetAndLockPendingDegreesAsync(
            ChainDegreeDbContext dbContext,
            int batchSize,
            CancellationToken ct = default);
    }
}
