using System;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Reputation.Application.Queries.GetReputationHistory;

public record GetReputationHistoryQuery(
    Guid UniversityId,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<ReputationHistoryResponse>>;
