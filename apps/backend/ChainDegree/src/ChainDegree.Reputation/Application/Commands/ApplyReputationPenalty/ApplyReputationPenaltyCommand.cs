using System;
using ChainDegree.Reputation.Application.Queries.GetReputationHistory;
using ChainDegree.Reputation.Domain.Enums;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Reputation.Application.Commands.ApplyReputationPenalty;

public record ApplyReputationPenaltyCommand(
    Guid UniversityId,
    Guid EventId,
    PenaltyReasonEnum ReasonCode,
    string? Description = null) : IRequest<Result<ReputationHistoryItemDto>>;
