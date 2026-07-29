using System;
using System.Collections.Generic;
using System.Linq;
using ChainDegree.Core.Domain.Reputation.Enums;
using ChainDegree.Core.Domain.Reputation.Events;
using ChainDegree.Core.Domain.Reputation.ValueObjects;
using ChainDegree.Core.Domain.SharedKernel;
using ChainDegree.SharedKernel.Common.Error;
using ChainDegree.SharedKernel.Result;

namespace ChainDegree.Core.Domain.Reputation;

public class ReputationScore : AggregateRoot
{
    public Guid UniversityId { get; private set; }
    public int CurrentScore { get; private set; }
    public bool IsFrozen { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private readonly List<ReputationHistory> _histories = new();
    public IReadOnlyCollection<ReputationHistory> Histories => _histories.AsReadOnly();

    private ReputationScore() { }

    public static ReputationScore Create(Guid universityId, int initialScore = 1000)
    {
        if (universityId == Guid.Empty)
            throw new ArgumentException("UniversityId cannot be empty.", nameof(universityId));
        if (initialScore < 0)
            throw new ArgumentException("Initial score cannot be negative.", nameof(initialScore));

        var score = new ReputationScore
        {
            Id = Guid.NewGuid(),
            UniversityId = universityId,
            CurrentScore = initialScore,
            IsFrozen = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return score;
    }

    public Result<ReputationHistory> ApplyPenalty(Guid eventId, PenaltyReasonEnum reasonCode, string? description = null)
    {
        if (eventId == Guid.Empty)
            return Result<ReputationHistory>.Failure(new Error("Reputation.EmptyEventId", "EventId cannot be empty.", ErrorType.Validation));

        // Idempotency check at Aggregate level
        var existingHistory = _histories.FirstOrDefault(h => h.EventId == eventId);
        if (existingHistory != null)
        {
            return Result<ReputationHistory>.Success(existingHistory);
        }

        var rule = PenaltyPolicy.GetRule(reasonCode);
        var oldScore = CurrentScore;
        var newScore = Math.Max(0, CurrentScore - rule.ScoreDeduction);

        CurrentScore = newScore;
        UpdatedAt = DateTime.UtcNow;

        if (rule.TriggersFreeze && !IsFrozen)
        {
            IsFrozen = true;
            RaiseDomainEvent(new InstitutionFrozenEvent(Id, UniversityId, reasonCode, description ?? $"Institution frozen due to penalty {reasonCode}"));
        }

        var history = ReputationHistory.Create(
            reputationScoreId: Id,
            universityId: UniversityId,
            eventId: eventId,
            scoreChange: -rule.ScoreDeduction,
            newScore: CurrentScore,
            reasonCode: reasonCode,
            description: description);

        _histories.Add(history);

        RaiseDomainEvent(new ReputationScoreChangedEvent(
            scoreId: Id,
            universityId: UniversityId,
            oldScore: oldScore,
            newScore: CurrentScore,
            reasonCode: reasonCode,
            eventId: eventId));

        return Result<ReputationHistory>.Success(history);
    }

    public Result<ReputationHistory> ApplyExemption(Guid eventId, string? description = null)
    {
        return ApplyPenalty(eventId, PenaltyReasonEnum.Shortcut_Exemption, description ?? "Exempted from reputation penalty via pending degree shortcut.");
    }

    public Result Freeze(PenaltyReasonEnum reasonCode, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(new Error("Reputation.EmptyFreezeReason", "Freeze reason cannot be empty.", ErrorType.Validation));

        if (IsFrozen)
            return Result.Failure(new Error("Reputation.AlreadyFrozen", "Institution is already frozen.", ErrorType.Conflict));

        IsFrozen = true;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new InstitutionFrozenEvent(Id, UniversityId, reasonCode, reason));
        return Result.Success();
    }

    public Result Unfreeze()
    {
        if (!IsFrozen)
            return Result.Failure(new Error("Reputation.NotFrozen", "Institution is not currently frozen.", ErrorType.Conflict));

        IsFrozen = false;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
