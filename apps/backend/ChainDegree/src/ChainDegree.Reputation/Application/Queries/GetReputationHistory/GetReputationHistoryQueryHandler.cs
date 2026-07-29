using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Reputation.Application.Abstractions;
using ChainDegree.SharedKernel.Common.Error;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Reputation.Application.Queries.GetReputationHistory;

public class GetReputationHistoryQueryHandler : IRequestHandler<GetReputationHistoryQuery, Result<ReputationHistoryResponse>>
{
    private readonly IReputationRepository _reputationRepository;

    public GetReputationHistoryQueryHandler(IReputationRepository reputationRepository)
    {
        _reputationRepository = reputationRepository;
    }

    public async Task<Result<ReputationHistoryResponse>> Handle(GetReputationHistoryQuery request, CancellationToken cancellationToken)
    {
        if (request.UniversityId == Guid.Empty)
        {
            return Result<ReputationHistoryResponse>.Failure(
                new Error("Reputation.InvalidUniversityId", "UniversityId cannot be empty.", ErrorType.Validation));
        }

        var reputation = await _reputationRepository.GetByUniversityIdWithHistoriesAsync(request.UniversityId, cancellationToken);
        if (reputation == null)
        {
            return Result<ReputationHistoryResponse>.Success(new ReputationHistoryResponse(
                UniversityId: request.UniversityId,
                TotalCount: 0,
                Items: Array.Empty<ReputationHistoryItemDto>()));
        }

        var histories = reputation.Histories
            .OrderByDescending(h => h.Timestamp)
            .Skip((Math.Max(1, request.Page) - 1) * Math.Max(1, request.PageSize))
            .Take(Math.Max(1, request.PageSize))
            .Select(h => new ReputationHistoryItemDto(
                Id: h.Id,
                EventId: h.EventId,
                ScoreChange: h.ScoreChange,
                NewScore: h.NewScore,
                ReasonCode: h.ReasonCode.ToString(),
                Description: h.Description,
                AnchorStatus: h.AnchorStatus.ToString(),
                HistoryHash: h.HistoryHash,
                TxHash: h.TxHash,
                Timestamp: h.Timestamp))
            .ToList();

        return Result<ReputationHistoryResponse>.Success(new ReputationHistoryResponse(
            UniversityId: request.UniversityId,
            TotalCount: reputation.Histories.Count,
            Items: histories));
    }
}
