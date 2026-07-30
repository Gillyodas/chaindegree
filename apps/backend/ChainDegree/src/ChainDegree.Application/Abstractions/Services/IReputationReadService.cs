using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ChainDegree.Core.Application.Abstractions.Services
{
    public interface IReputationReadService
    {
        Task<Dictionary<Guid, int>> GetReputationScoresAsync(IEnumerable<Guid> partnerUniversityIds, CancellationToken ct = default);
        Task<int> GetReputationScoreAsync(Guid? partnerUniversityId, CancellationToken ct = default);
    }
}
