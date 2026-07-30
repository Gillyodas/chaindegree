using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ApplicationEntity = ChainDegree.Core.Domain.Applications.Application;

namespace ChainDegree.Core.Application.Abstractions.Repositories
{
    public interface IApplicationRepository
    {
        Task<ApplicationEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<bool> ExistsAsync(Guid studentId, Guid jobId, CancellationToken ct = default);
        Task<IReadOnlyList<ApplicationEntity>> GetByJobIdAsync(Guid jobId, CancellationToken ct = default);
        Task AddAsync(ApplicationEntity application, CancellationToken ct = default);
        Task UpdateAsync(ApplicationEntity application, CancellationToken ct = default);
    }
}
