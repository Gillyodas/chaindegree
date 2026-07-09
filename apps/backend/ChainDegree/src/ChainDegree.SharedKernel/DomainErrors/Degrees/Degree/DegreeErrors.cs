using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.SharedKernel.Common.Error;

namespace ChainDegree.SharedKernel.DomainErrors.Degrees.Degree
{
    public static class DegreeErrors
    {
        public static readonly Error InvalidTotalCount =
        Error.Validation("Degree.InvalidTotalCount", "Total degree count cannot be negative.");

        public static readonly Error EmptyIdentifiers =
            Error.Validation("Degree.EmptyIdentifiers", "Required organization, registrar, or student identifiers cannot be empty.");

        public static readonly Error MissingAcademicDetails =
            Error.Validation("Degree.MissingAcademicDetails", "Major and graduation classification cannot be empty.");

        public static readonly Error InvalidCryptoSnapshot =
            Error.Validation("Degree.InvalidCryptoSnapshot", "The cryptographic data snapshot or local hash is invalid.");

        public static readonly Error EmptyCryptoSnapshot =
            Error.Validation("Degree.EmptyCryptoSnapshot", "Crypto snapshot value cannot be empty");

        public static readonly Error EmptyTransactionHash =
            Error.Validation("Degree.EmptyTransactionHash", "Blockchain transaction hash cannot be empty.");

        public static readonly Error InvalidStateTransition =
            Error.Validation("Degree.InvalidStateTransition", "The degree cannot transition from its current state under this operation.");

        public static readonly Error DuplicateDegree =
            Error.Conflict("Degree.DuplicateDegree", "A degree of the same type already exists for this student at this institution.");

        public static readonly Error StudentNotFound =
            Error.NotFound("Degree.StudentNotFound", "The specified student was not found.");

        public static readonly Error NotFound =
            Error.NotFound("Degree.NotFound", "The specified degree was not found.");

        public static readonly Error InstitutionMismatch =
            Error.Validation("Degree.InstitutionMismatch", "The registrar does not belong to the institution that manages this degree.");

        public static readonly Error NoDegreeToIssue =
            Error.Validation("Degree.NoDegreeToIssue", "No valid degrees to issue after validation.");

        public static readonly Error BatchNotFound =
            Error.NotFound("Degree.BatchNotFound", "The specified batch was not found.");

        public static readonly Error CannotRetry =
            Error.Validation("Degree.CannotRetry", "Only degrees with Confirmation_Error status can be retried.");

        public static readonly Error CryptoHashMismatch =
            Error.Validation("Degree.CryptoHashMismatch", "The recalculated hash does not match the stored local hash.");

        public static readonly Error BlockchainInvalid =
            Error.Validation("Degree.BlockchainInvalid", "The stored hash does not match the anchored Merkle root on the blockchain.");

        public static readonly Error UnsupportedVersion =
            Error.NotFound("Degree.UnsupportedVersion", "The requested version of the degree does not exist.");
    }
}
