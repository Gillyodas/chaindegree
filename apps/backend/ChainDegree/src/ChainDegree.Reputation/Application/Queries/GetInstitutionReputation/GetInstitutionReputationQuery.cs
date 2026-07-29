using System;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Reputation.Application.Queries.GetInstitutionReputation;

public record GetInstitutionReputationQuery(Guid UniversityId) : IRequest<Result<ReputationResponse>>;
