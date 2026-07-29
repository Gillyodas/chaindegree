using System;

namespace ChainDegree.Core.Application.Reputation.Queries.GetInstitutionReputation;

public record ReputationResponse(
    Guid UniversityId,
    int CurrentScore,
    bool IsFrozen,
    DateTime LastUpdatedAt);
