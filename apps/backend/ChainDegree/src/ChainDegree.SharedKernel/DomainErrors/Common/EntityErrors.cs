using ChainDegree.SharedKernel.Common.Error;

namespace ChainDegree.SharedKernel.DomainErrors.Common;

public static class EntityErrors
{
    public static readonly Error EmptyId =
        Error.Validation("Entity.EmptyId", "Identifier cannot be empty.");

    public static readonly Error EmptyCode =
        Error.Validation("Entity.EmptyCode", "Code cannot be empty.");

    public static readonly Error EmptyName =
        Error.Validation("Entity.EmptyName", "Name cannot be empty.");

    public static readonly Error EmptyEmail =
        Error.Validation("Entity.EmptyEmail", "Email address cannot be empty.");

    public static readonly Error InvalidEmail =
        Error.Validation("Entity.InvalidEmail", "Email address is not in a valid format.");
}
