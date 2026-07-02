using ChainDegree.SharedKernel.Common.Error;

namespace ChainDegree.SharedKernel.DomainErrors.Institutions;

public static class InstitutionErrors
{
    public static readonly Error DuplicateCode =
        Error.Conflict("Institution.DuplicateCode", "An institution with this code already exists.");

    public static readonly Error DuplicateEmail =
        Error.Conflict("Institution.DuplicateEmail", "An institution with this email address already exists.");

    public static readonly Error NotFound =
        Error.NotFound("Institution.NotFound", "The requested education institution was not found.");
}
