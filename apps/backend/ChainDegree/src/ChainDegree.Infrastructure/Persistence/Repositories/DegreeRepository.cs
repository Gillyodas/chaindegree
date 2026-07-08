using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Degrees.Entities;
using ChainDegree.Core.Infrastructure.Persistence.Locking;
using Microsoft.EntityFrameworkCore;

namespace ChainDegree.Core.Infrastructure.Persistence.Repositories
{
    public class DegreeRepository : IDegreeRepository
    {
        private readonly ChainDegreeDbContext _context;
        private readonly IPendingDegreeLockStrategy _lockStrategy;

        public DegreeRepository(ChainDegreeDbContext context, IPendingDegreeLockStrategy lockStrategy)
        {
            _context = context;
            _lockStrategy = lockStrategy;
        }

        public async Task<Degree?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Degrees.FirstOrDefaultAsync(x => x.Id == id, ct);
        }

        public async Task<bool> ExistsDuplicateAsync(Guid institutionId, Guid studentId, string major, CancellationToken ct = default)
        {
            return await _context.Degrees.AnyAsync(x =>
                x.InstitutionId == institutionId &&
                x.StudentId == studentId &&
                x.Major == major &&
                x.DeletedAt == null, ct);
        }

        public async Task<bool> ExistsDuplicatePolicyAsync(Guid institutionId, Guid studentId, string major, int issuedYear, CancellationToken ct = default)
        {
            return await _context.Degrees.AnyAsync(x =>
                x.InstitutionId == institutionId &&
                x.StudentId == studentId &&
                x.Major == major &&
                x.IssuedAt.Year == issuedYear &&
                x.DeletedAt == null, ct);
        }

        public async Task<long> GetTotalCountAsync(CancellationToken ct = default)
        {
            return await _context.Degrees.LongCountAsync(ct);
        }

        public async Task AddAsync(Degree degree, CancellationToken ct = default)
        {
            await _context.Degrees.AddAsync(degree, ct);
        }

        public async Task AddRangeAsync(IEnumerable<Degree> degrees, CancellationToken ct = default)
        {
            await _context.Degrees.AddRangeAsync(degrees, ct);
        }

        public async Task<List<Degree>> GetPendingConfirmationAsync(int batchSize, CancellationToken ct = default)
        {
            return await _lockStrategy.GetAndLockPendingDegreesAsync(_context, batchSize, ct);
        }

        public async Task AddUpdateRequestAsync(DegreeUpdateRequest request, CancellationToken ct = default)
        {
            await _context.DegreeUpdateRequests.AddAsync(request, ct);
        }

        public async Task<DegreeUpdateRequest?> GetUpdateRequestByDegreeIdAsync(Guid degreeId, CancellationToken ct = default)
        {
            return await _context.DegreeUpdateRequests.FirstOrDefaultAsync(x => x.DegreeId == degreeId, ct);
        }

        public void RemoveUpdateRequest(DegreeUpdateRequest request)
        {
            _context.DegreeUpdateRequests.Remove(request);
        }
    }
}
