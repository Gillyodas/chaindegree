using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.SharedKernel.DomainErrors.Degrees.Degree;
using ChainDegree.SharedKernel.Result;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChainDegree.Core.Application.Degrees.Queries.ListDegreeVersions
{
    public class ListDegreeVersionsQueryHandler : IRequestHandler<ListDegreeVersionsQuery, Result<DegreeVersionListResponse>>
    {
        private readonly IDegreeRepository _degreeRepository;
        private readonly ILogger<ListDegreeVersionsQueryHandler> _logger;

        public ListDegreeVersionsQueryHandler(
            IDegreeRepository degreeRepository,
            ILogger<ListDegreeVersionsQueryHandler> logger)
        {
            _degreeRepository = degreeRepository;
            _logger = logger;
        }

        public async Task<Result<DegreeVersionListResponse>> Handle(ListDegreeVersionsQuery request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.DegreeCode))
            {
                return Result<DegreeVersionListResponse>.Failure(DegreeErrors.NotFound);
            }

            var result = await _degreeRepository.GetDegreeVersionsAsync(request.DegreeCode.Trim(), ct);
            if (result == null)
            {
                _logger.LogInformation("Degree versions lookup failed: DegreeCode={DegreeCode} not found", request.DegreeCode);
                return Result<DegreeVersionListResponse>.Failure(DegreeErrors.NotFound);
            }

            return Result<DegreeVersionListResponse>.Success(result);
        }
    }
}
