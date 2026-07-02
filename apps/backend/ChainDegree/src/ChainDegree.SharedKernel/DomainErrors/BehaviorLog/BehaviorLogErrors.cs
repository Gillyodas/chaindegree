using ChainDegree.SharedKernel.Common.Error;

namespace ChainDegree.SharedKernel.DomainErrors.BehaviorLog;

public static class BehaviorLogErrors
{
    public static readonly Error InvalidActionType =
        Error.Validation("BehaviorLog.InvalidActionType", "The action type is invalid.");

    public static readonly Error EmptyActorInfo =
        Error.Validation("BehaviorLog.EmptyActorInfo", "Actor ID and Actor Role must be provided.");

    public static readonly Error EmptyTargetInfo =
        Error.Validation("BehaviorLog.EmptyTargetInfo", "Target ID and Target Table must be provided.");
}
