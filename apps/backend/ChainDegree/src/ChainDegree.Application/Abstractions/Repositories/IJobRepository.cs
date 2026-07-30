using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Domain.Jobs;

namespace ChainDegree.Core.Application.Abstractions.Repositories
{
    public interface IJobRepository
    {
        Task<Job?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<Job>> GetActiveJobsAsync(string? searchTerm = null, CancellationToken ct = default);
        Task AddAsync(Job job, CancellationToken ct = default);
        Task UpdateAsync(Job job, CancellationToken ct = default);
    }
}
