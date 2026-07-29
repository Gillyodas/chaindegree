using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Reputation.Queries.GetReputationHistory;
using ChainDegree.Core.Domain.Reputation;
using ChainDegree.SharedKernel.Common.Error;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Reputation.Commands.ApplyReputationPenalty;

public class ApplyReputationPenaltyCommandHandler : IRequestHandler<ApplyReputationPenaltyCommand, Result<ReputationHistoryItemDto>>
{
    private readonly IReputationRepository _reputationRepository;

    public ApplyReputationPenaltyCommandHandler(IReputationRepository reputationRepository)
    {
        _reputationRepository = reputationRepository;
    }

    public async Task<Result<ReputationHistoryItemDto>> Handle(ApplyReputationPenaltyCommand request, CancellationToken cancellationToken)
    {
        if (request.UniversityId == Guid.Empty)
            return Result<ReputationHistoryItemDto>.Failure(new Error("Reputation.InvalidUniversityId", "UniversityId cannot be empty.", ErrorType.Validation));
        if (request.EventId == Guid.Empty)
            return Result<ReputationHistoryItemDto>.Failure(new Error("Reputation.InvalidEventId", "EventId cannot be empty.", ErrorType.Validation));

        // 1. Idempotency Check
        var isProcessed = await _reputationRepository.HasEventBeenProcessedAsync(request.EventId, cancellationToken);
        var reputation = await _reputationRepository.GetByUniversityIdWithHistoriesAsync(request.UniversityId, cancellationToken);

        if (reputation == null)
        {
            reputation = ReputationScore.Create(request.UniversityId);
            await _reputationRepository.AddAsync(reputation, cancellationToken);
        }

        if (isProcessed)
        {
            var existing = System.Linq.Enumerable.FirstOrDefault(reputation.Histories, h => h.EventId == request.EventId);
            if (existing != null)
            {
                return Result<ReputationHistoryItemDto>.Success(MapToDto(existing));
            }
        }

        // 2. Execute Aggregate Domain Logic
        var result = reputation.ApplyPenalty(request.EventId, request.ReasonCode, request.Description);
        if (result.IsFailure)
        {
            return Result<ReputationHistoryItemDto>.Failure(result.Error);
        }

        _reputationRepository.Update(reputation);

        return Result<ReputationHistoryItemDto>.Success(MapToDto(result.Value));
    }

    private static ReputationHistoryItemDto MapToDto(ReputationHistory h)
    {
        return new ReputationHistoryItemDto(
            Id: h.Id,
            EventId: h.EventId,
            ScoreChange: h.ScoreChange,
            NewScore: h.NewScore,
            ReasonCode: h.ReasonCode.ToString(),
            Description: h.Description,
            AnchorStatus: h.AnchorStatus.ToString(),
            HistoryHash: h.HistoryHash,
            TxHash: h.TxHash,
            Timestamp: h.Timestamp);
    }
}
