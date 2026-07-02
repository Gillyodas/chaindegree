using ChainDegree.SharedKernel.Common.Error;

namespace ChainDegree.SharedKernel.DomainErrors.Students;

public static class StudentErrors
{
    public static readonly Error DuplicateIdentityNumber =
        Error.Conflict("Student.DuplicateIdentityNumber", "A student with this identity number (CCCD) already exists.");

    public static readonly Error NotFound =
        Error.NotFound("Student.NotFound", "The requested student was not found.");

    public static readonly Error AlreadyEnrolled =
        Error.Conflict("Student.AlreadyEnrolled", "This student is already enrolled in the institution.");
}
