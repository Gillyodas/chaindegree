using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Domain.Reports;

namespace ChainDegree.Core.Application.Abstractions.Repositories
{
    public interface IReportRepository
    {
        Task<Report?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<bool> ExistsPendingReportAsync(Guid reporterId, Guid targetDegreeId, CancellationToken ct = default);
        Task AddAsync(Report report, CancellationToken ct = default);
        void Update(Report report);
    }
}
