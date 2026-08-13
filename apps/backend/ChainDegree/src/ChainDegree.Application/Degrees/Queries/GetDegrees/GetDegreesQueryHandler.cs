using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Queries;
using ChainDegree.Core.Application.Degrees.Queries.DTOs;
using ChainDegree.SharedKernel.Common.Error;
using ChainDegree.SharedKernel.DomainErrors.Degrees.Degree;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Degrees.Queries.GetDegrees
{
    public class GetDegreesQueryHandler : IRequestHandler<GetDegreesQuery, Result<PagedResult<DegreeListDto>>>
    {
        private readonly IDegreeQueryService _degreeQueryService;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetDegreesQueryHandler(
            IDegreeQueryService degreeQueryService,
            ICurrentUserAccessor currentUserAccessor)
        {
            _degreeQueryService = degreeQueryService ?? throw new ArgumentNullException(nameof(degreeQueryService));
            _currentUserAccessor = currentUserAccessor ?? throw new ArgumentNullException(nameof(currentUserAccessor));
        }

        public async Task<Result<PagedResult<DegreeListDto>>> Handle(GetDegreesQuery request, CancellationToken ct)
        {
            // Bounded pagination validation
            if (request.PageIndex < 1)
            {
                return Result<PagedResult<DegreeListDto>>.Failure(
                    Error.Validation("Pagination.InvalidPageIndex", "PageIndex must be greater than or equal to 1."));
            }

            if (request.PageSize < 1 || request.PageSize > 100)
            {
                return Result<PagedResult<DegreeListDto>>.Failure(
                    Error.Validation("Pagination.InvalidPageSize", "PageSize must be between 1 and 100."));
            }

            // Tenant isolation: InstitutionId MUST come strictly from ICurrentUserAccessor
            var institutionId = _currentUserAccessor.InstitutionId;
            if (!institutionId.HasValue || institutionId.Value == Guid.Empty)
            {
                return Result<PagedResult<DegreeListDto>>.Failure(DegreeErrors.InstitutionMismatch);
            }

            var pagedResult = await _degreeQueryService.GetDegreesAsync(
                institutionId.Value,
                request.PageIndex,
                request.PageSize,
                ct);

            return Result<PagedResult<DegreeListDto>>.Success(pagedResult);
        }
    }
}
