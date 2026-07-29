using System;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Reputation.Queries.GetReputationHistory;

public record GetReputationHistoryQuery(
    Guid UniversityId,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<ReputationHistoryResponse>>;
