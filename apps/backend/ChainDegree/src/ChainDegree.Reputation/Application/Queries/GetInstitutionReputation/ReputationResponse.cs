using System;

namespace ChainDegree.Reputation.Application.Queries.GetInstitutionReputation;

public record ReputationResponse(
    Guid UniversityId,
    int CurrentScore,
    bool IsFrozen,
    DateTime LastUpdatedAt);
