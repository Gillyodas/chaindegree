using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApplicationEntity = ChainDegree.Core.Domain.Applications.Application;
using ChainDegree.Core.Application.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ChainDegree.Core.Infrastructure.Persistence.Repositories
{
    public class ApplicationRepository : IApplicationRepository
    {
        private readonly ChainDegreeDbContext _dbContext;

        public ApplicationRepository(ChainDegreeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ApplicationEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _dbContext.Applications
                .Include(a => a.AttachedDegrees)
                .FirstOrDefaultAsync(a => a.Id == id, ct);
        }

        public async Task<bool> ExistsAsync(Guid studentId, Guid jobId, CancellationToken ct = default)
        {
            return await _dbContext.Applications
                .AnyAsync(a => a.StudentId == studentId && a.JobId == jobId, ct);
        }

        public async Task<IReadOnlyList<ApplicationEntity>> GetByJobIdAsync(Guid jobId, CancellationToken ct = default)
        {
            return await _dbContext.Applications
                .AsNoTracking()
                .Include(a => a.AttachedDegrees)
                .Where(a => a.JobId == jobId)
                .ToListAsync(ct);
        }

        public async Task AddAsync(ApplicationEntity application, CancellationToken ct = default)
        {
            await _dbContext.Applications.AddAsync(application, ct);
        }

        public Task UpdateAsync(ApplicationEntity application, CancellationToken ct = default)
        {
            _dbContext.Applications.Update(application);
            return Task.CompletedTask;
        }
    }
}
