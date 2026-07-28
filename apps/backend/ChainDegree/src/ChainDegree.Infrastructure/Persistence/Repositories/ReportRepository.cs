using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Domain.Reports;
using ChainDegree.Core.Domain.Reports.Enums;
using Microsoft.EntityFrameworkCore;

namespace ChainDegree.Core.Infrastructure.Persistence.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly ChainDegreeDbContext _context;

        public ReportRepository(ChainDegreeDbContext context)
        {
            _context = context;
        }

        public async Task<Report?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Reports.FirstOrDefaultAsync(r => r.Id == id, ct);
        }

        public async Task<bool> ExistsPendingReportAsync(Guid reporterId, Guid targetDegreeId, CancellationToken ct = default)
        {
            return await _context.Reports.AnyAsync(r =>
                r.ReporterId == reporterId &&
                r.TargetDegreeId == targetDegreeId &&
                r.Status == ReportStatusEnum.Pending_Review, ct);
        }

        public async Task AddAsync(Report report, CancellationToken ct = default)
        {
            await _context.Reports.AddAsync(report, ct);
        }

        public void Update(Report report)
        {
            _context.Reports.Update(report);
        }
    }
}
