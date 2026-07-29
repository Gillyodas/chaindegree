using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Reputation.Domain;

namespace ChainDegree.Reputation.Application.Abstractions;

public interface IReputationRepository
{
    Task<ReputationScore?> GetByUniversityIdAsync(Guid universityId, CancellationToken ct = default);
    Task<ReputationScore?> GetByUniversityIdWithHistoriesAsync(Guid universityId, CancellationToken ct = default);
    Task<bool> HasEventBeenProcessedAsync(Guid eventId, CancellationToken ct = default);
    Task AddAsync(ReputationScore reputationScore, CancellationToken ct = default);
    void Update(ReputationScore reputationScore);
}
