using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Queries;
using ChainDegree.SharedKernel.DomainErrors.Degrees.Degree;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Degrees.Queries.GetBatchStatus
{
    public class GetBatchStatusQueryHandler : IRequestHandler<GetBatchStatusQuery, Result<BatchQueryResponse>>
    {
        private readonly IBatchQueryService _batchQueryService;

        public GetBatchStatusQueryHandler(IBatchQueryService batchQueryService)
        {
            _batchQueryService = batchQueryService;
        }

        public async Task<Result<BatchQueryResponse>> Handle(GetBatchStatusQuery request, CancellationToken ct)
        {
            var response = await _batchQueryService.GetBatchStatusAsync(request.BatchId, ct);
            if (response == null)
            {
                return Result<BatchQueryResponse>.Failure(DegreeErrors.BatchNotFound);
            }

            return Result<BatchQueryResponse>.Success(response);
        }
    }
}
