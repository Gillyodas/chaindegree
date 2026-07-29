using System;
using ChainDegree.Core.Domain.SharedKernel.Interfaces;
using ChainDegree.Reputation.Domain.Enums;

namespace ChainDegree.Reputation.Domain.Events;

public record ReputationScoreChangedEvent : IDomainEvent
{
    public Guid ScoreId { get; init; }
    public Guid UniversityId { get; init; }
    public int OldScore { get; init; }
    public int NewScore { get; init; }
    public PenaltyReasonEnum ReasonCode { get; init; }
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    public ReputationScoreChangedEvent(
        Guid scoreId,
        Guid universityId,
        int oldScore,
        int newScore,
        PenaltyReasonEnum reasonCode,
        Guid eventId)
    {
        ScoreId = scoreId;
        UniversityId = universityId;
        OldScore = oldScore;
        NewScore = newScore;
        ReasonCode = reasonCode;
        EventId = eventId;
    }
}
