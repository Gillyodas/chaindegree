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
    }
}
