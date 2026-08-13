using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Queries;
using ChainDegree.Core.Application.Degrees.Queries.DTOs;
using ChainDegree.SharedKernel.DomainErrors.Degrees.Degree;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Degrees.Queries.GetDegreeById
{
    public class GetDegreeByIdQueryHandler : IRequestHandler<GetDegreeByIdQuery, Result<DegreeDetailDto>>
    {
        private readonly IDegreeQueryService _degreeQueryService;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetDegreeByIdQueryHandler(
            IDegreeQueryService degreeQueryService,
            ICurrentUserAccessor currentUserAccessor)
        {
            _degreeQueryService = degreeQueryService ?? throw new ArgumentNullException(nameof(degreeQueryService));
            _currentUserAccessor = currentUserAccessor ?? throw new ArgumentNullException(nameof(currentUserAccessor));
        }

        public async Task<Result<DegreeDetailDto>> Handle(GetDegreeByIdQuery request, CancellationToken ct)
        {
            if (request.Id == Guid.Empty)
            {
                return Result<DegreeDetailDto>.Failure(DegreeErrors.NotFound);
            }

            // Tenant isolation: InstitutionId MUST come strictly from ICurrentUserAccessor
            var institutionId = _currentUserAccessor.InstitutionId;
            if (!institutionId.HasValue || institutionId.Value == Guid.Empty)
            {
                // Return NotFound instead of Forbidden to prevent resource enumeration
                return Result<DegreeDetailDto>.Failure(DegreeErrors.NotFound);
            }

            var degree = await _degreeQueryService.GetDegreeByIdAsync(
                request.Id,
                institutionId.Value,
                ct);

            if (degree == null)
            {
                // Resource non-existent OR belongs to another tenant -> return 404 NotFound
                return Result<DegreeDetailDto>.Failure(DegreeErrors.NotFound);
            }

            return Result<DegreeDetailDto>.Success(degree);
        }
    }
}
