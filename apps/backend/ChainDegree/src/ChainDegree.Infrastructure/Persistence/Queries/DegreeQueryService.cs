using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ChainDegree.Core.Application.Abstractions.Queries;
using ChainDegree.Core.Application.Degrees.Queries.DTOs;
using ChainDegree.SharedKernel.Result;

namespace ChainDegree.Core.Infrastructure.Persistence.Queries
{
    public class DegreeQueryService : IDegreeQueryService
    {
        private readonly ChainDegreeDbContext _context;

        public DegreeQueryService(ChainDegreeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<PagedResult<DegreeListDto>> GetDegreesAsync(
            Guid institutionId,
            int pageIndex,
            int pageSize,
            CancellationToken ct)
        {
            if (institutionId == Guid.Empty)
            {
                return new PagedResult<DegreeListDto>(Array.Empty<DegreeListDto>(), 0, pageIndex, pageSize);
            }

            // Bounded pagination guards to prevent memory/CPU exhaustion or integer overflow
            int safePageIndex = pageIndex < 1 ? 1 : pageIndex;
            int safePageSize = pageSize < 1 ? 20 : (pageSize > 100 ? 100 : pageSize);

            var baseQuery = from d in _context.Degrees.AsNoTracking()
                            join s in _context.Students.AsNoTracking() on d.StudentId equals s.Id
                            where d.InstitutionId == institutionId
                            select new
                            {
                                Degree = d,
                                StudentFullName = s.FullName
                            };

            // Count total matching records asynchronously
            int totalCount = await baseQuery.CountAsync(ct);

            if (totalCount == 0)
            {
                return new PagedResult<DegreeListDto>(Array.Empty<DegreeListDto>(), 0, safePageIndex, safePageSize);
            }

            // Deterministic ordering: CreatedAt DESC, then Id DESC to prevent item skipping/duplication across pages
            int skipCount = (safePageIndex - 1) * safePageSize;

            var items = await baseQuery
                .OrderByDescending(x => x.Degree.CreatedAt)
                .ThenByDescending(x => x.Degree.Id)
                .Skip(skipCount)
                .Take(safePageSize)
                .Select(x => new DegreeListDto(
                    x.Degree.Id,
                    x.Degree.DegreeCode,
                    x.Degree.StudentId,
                    x.StudentFullName,
                    x.Degree.Major,
                    x.Degree.Classification,
                    x.Degree.Status.ToString(),
                    x.Degree.IssuedAt,
                    x.Degree.TxHashBlockchain
                ))
                .ToListAsync(ct);

            return new PagedResult<DegreeListDto>(items, totalCount, safePageIndex, safePageSize);
        }

        public async Task<DegreeDetailDto?> GetDegreeByIdAsync(
            Guid degreeId,
            Guid institutionId,
            CancellationToken ct)
        {
            if (degreeId == Guid.Empty || institutionId == Guid.Empty)
            {
                return null;
            }

            // Strict tenant isolation at SQL level: degreeId AND institutionId filter
            var query = from d in _context.Degrees.AsNoTracking()
                        join s in _context.Students.AsNoTracking() on d.StudentId equals s.Id
                        where d.Id == degreeId && d.InstitutionId == institutionId
                        select new DegreeDetailDto(
                            d.Id,
                            d.DegreeCode,
                            d.InstitutionId,
                            d.SignedByRegistrarId,
                            d.StudentId,
                            s.FullName,
                            d.Major,
                            d.Classification,
                            d.Status.ToString(),
                            d.IssuedAt,
                            d.TxHashBlockchain,
                            d.CurrentVersion,
                            d.CreatedAt,
                            d.UpdatedAt
                        );

            return await query.FirstOrDefaultAsync(ct);
        }
    }
}
