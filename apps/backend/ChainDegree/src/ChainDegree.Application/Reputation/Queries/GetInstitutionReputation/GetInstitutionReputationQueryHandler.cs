using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Reputation.Queries.GetInstitutionReputation;

public class GetInstitutionReputationQueryHandler : IRequestHandler<GetInstitutionReputationQuery, Result<ReputationResponse>>
{
    private readonly IReputationRepository _reputationRepository;

    public GetInstitutionReputationQueryHandler(IReputationRepository reputationRepository)
    {
        _reputationRepository = reputationRepository;
    }

    public async Task<Result<ReputationResponse>> Handle(GetInstitutionReputationQuery request, CancellationToken cancellationToken)
    {
        if (request.UniversityId == Guid.Empty)
        {
            return Result<ReputationResponse>.Failure(
                new SharedKernel.Common.Error.Error("Reputation.InvalidUniversityId", "UniversityId cannot be empty.", SharedKernel.Common.Error.ErrorType.Validation));
        }

        var reputation = await _reputationRepository.GetByUniversityIdAsync(request.UniversityId, cancellationToken);
        if (reputation == null)
        {
            // Default response if no score record has been initialized yet (Default initial score 1000)
            return Result<ReputationResponse>.Success(new ReputationResponse(
                UniversityId: request.UniversityId,
                CurrentScore: 1000,
                IsFrozen: false,
                LastUpdatedAt: DateTime.UtcNow));
        }

        return Result<ReputationResponse>.Success(new ReputationResponse(
            UniversityId: reputation.UniversityId,
            CurrentScore: reputation.CurrentScore,
            IsFrozen: reputation.IsFrozen,
            LastUpdatedAt: reputation.UpdatedAt));
    }
}
