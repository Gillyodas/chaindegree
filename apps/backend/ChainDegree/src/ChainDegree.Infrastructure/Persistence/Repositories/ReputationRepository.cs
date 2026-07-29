using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Domain.Reputation;
using Microsoft.EntityFrameworkCore;

namespace ChainDegree.Core.Infrastructure.Persistence.Repositories;

public class ReputationRepository : IReputationRepository
{
    private readonly ChainDegreeDbContext _context;

    public ReputationRepository(ChainDegreeDbContext context)
    {
        _context = context;
    }

    public async Task<ReputationScore?> GetByUniversityIdAsync(Guid universityId, CancellationToken ct = default)
    {
        return await _context.ReputationScores
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UniversityId == universityId, ct);
    }

    public async Task<ReputationScore?> GetByUniversityIdWithHistoriesAsync(Guid universityId, CancellationToken ct = default)
    {
        return await _context.ReputationScores
            .Include(r => r.Histories)
            .FirstOrDefaultAsync(r => r.UniversityId == universityId, ct);
    }

    public async Task<bool> HasEventBeenProcessedAsync(Guid eventId, CancellationToken ct = default)
    {
        return await _context.ReputationHistories
            .AnyAsync(h => h.EventId == eventId, ct);
    }

    public async Task AddAsync(ReputationScore reputationScore, CancellationToken ct = default)
    {
        await _context.ReputationScores.AddAsync(reputationScore, ct);
    }

    public void Update(ReputationScore reputationScore)
    {
        _context.ReputationScores.Update(reputationScore);
    }
}
