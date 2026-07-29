using System;
using ChainDegree.Core.Domain.Reputation.Enums;
using ChainDegree.Core.Application.Reputation.Queries.GetReputationHistory;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Reputation.Commands.ApplyReputationPenalty;

public record ApplyReputationPenaltyCommand(
    Guid UniversityId,
    Guid EventId,
    PenaltyReasonEnum ReasonCode,
    string? Description = null) : IRequest<Result<ReputationHistoryItemDto>>;
