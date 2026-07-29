using ChainDegree.SharedKernel.Common.Error;

namespace ChainDegree.SharedKernel.DomainErrors.Reputation;

public static class ReputationErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Reputation.NotFound", "Reputation score for the specified institution was not found.");

    public static readonly Error AlreadyFrozen =
        Error.Conflict("Reputation.AlreadyFrozen", "The institution reputation is already frozen.");

    public static readonly Error NotFrozen =
        Error.Conflict("Reputation.NotFrozen", "The institution reputation is not currently frozen.");

    public static readonly Error ConcurrencyConflict =
        Error.Conflict("Reputation.ConcurrencyConflict", "A concurrency conflict occurred while updating reputation score.");
}
