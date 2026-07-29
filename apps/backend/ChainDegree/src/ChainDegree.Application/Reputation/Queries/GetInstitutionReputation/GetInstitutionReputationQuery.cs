using System;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Reputation.Queries.GetInstitutionReputation;

public record GetInstitutionReputationQuery(Guid UniversityId) : IRequest<Result<ReputationResponse>>;
