using System;
using ChainDegree.Core.Domain.SharedKernel.Interfaces;
using ChainDegree.Reputation.Domain.Enums;

namespace ChainDegree.Reputation.Domain.Events;

public record InstitutionFrozenEvent : IDomainEvent
{
    public Guid ScoreId { get; init; }
    public Guid UniversityId { get; init; }
    public PenaltyReasonEnum ReasonCode { get; init; }
    public string Reason { get; init; }
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    public InstitutionFrozenEvent(
        Guid scoreId,
        Guid universityId,
        PenaltyReasonEnum reasonCode,
        string reason)
    {
        ScoreId = scoreId;
        UniversityId = universityId;
        ReasonCode = reasonCode;
        Reason = reason;
    }
}
