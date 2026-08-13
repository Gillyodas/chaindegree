using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Degrees.Queries.DTOs;
using ChainDegree.SharedKernel.Result;

namespace ChainDegree.Core.Application.Abstractions.Queries
{
    public interface IDegreeQueryService
    {
        Task<PagedResult<DegreeListDto>> GetDegreesAsync(Guid institutionId, int pageIndex, int pageSize, CancellationToken ct);
        Task<DegreeDetailDto?> GetDegreeByIdAsync(Guid degreeId, Guid institutionId, CancellationToken ct);
    }
}
