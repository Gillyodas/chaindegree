using ChainDegree.SharedKernel.Common.Error;

namespace ChainDegree.SharedKernel.DomainErrors.Applications
{
    public static class ApplicationErrors
    {
        public static readonly Error EmptyIdentifiers =
            Error.Validation("Application.EmptyIdentifiers", "Job ID, Student ID, and Degree ID cannot be empty.");

        public static readonly Error FilterCriteriaNotSatisfied =
            Error.Validation("Application.FilterCriteriaNotSatisfied", "The provided degree does not satisfy the job filter criteria. Force submit to proceed.");

        public static readonly Error RevokedDegreeCannotBeSubmitted =
            Error.Validation("Application.RevokedDegreeCannotBeSubmitted", "Degrees with status Revoked or Pending_Revocation cannot be used to apply for jobs.");

        public static readonly Error DuplicateApplication =
            Error.Conflict("Application.DuplicateApplication", "The student has already applied for this job.");

        public static readonly Error JobClosedOrExpired =
            Error.Validation("Application.JobClosedOrExpired", "Cannot apply to a job that is closed or expired.");

        public static readonly Error DegreeOwnershipMismatch =
            Error.Validation("Application.DegreeOwnershipMismatch", "The specified degree does not belong to the current student.");
    }
}
