using System;
using ChainDegree.Core.Domain.Reputation.Enums;
using ChainDegree.Core.Domain.SharedKernel.Interfaces;

namespace ChainDegree.Core.Domain.Reputation.Events;

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
