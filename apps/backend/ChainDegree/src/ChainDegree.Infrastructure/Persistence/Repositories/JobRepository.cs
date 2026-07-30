using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Domain.Jobs;
using ChainDegree.Core.Domain.Jobs.Enums;
using Microsoft.EntityFrameworkCore;

namespace ChainDegree.Core.Infrastructure.Persistence.Repositories
{
    public class JobRepository : IJobRepository
    {
        private readonly ChainDegreeDbContext _dbContext;

        public JobRepository(ChainDegreeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Job?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _dbContext.Jobs
                .Include(j => j.JobDegreeFilters)
                .FirstOrDefaultAsync(j => j.Id == id, ct);
        }

        public async Task<IReadOnlyList<Job>> GetActiveJobsAsync(string? searchTerm = null, CancellationToken ct = default)
        {
            var query = _dbContext.Jobs
                .AsNoTracking()
                .Include(j => j.JobDegreeFilters)
                .Where(j => j.Status == JobStatusEnum.Active);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(j => EF.Functions.Like(j.Title.ToLower(), $"%{term}%") || EF.Functions.Like(j.Description.ToLower(), $"%{term}%"));
            }

            return await query.ToListAsync(ct);
        }

        public async Task AddAsync(Job job, CancellationToken ct = default)
        {
            await _dbContext.Jobs.AddAsync(job, ct);
        }

        public Task UpdateAsync(Job job, CancellationToken ct = default)
        {
            _dbContext.Jobs.Update(job);
            return Task.CompletedTask;
        }
    }
}
