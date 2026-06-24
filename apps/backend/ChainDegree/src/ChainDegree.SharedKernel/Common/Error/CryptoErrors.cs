using System;
using System.Collections.Generic;
using System.Text;

namespace ChainDegree.SharedKernel.Common.Error;

public static class CryptoErrors
{
    public static readonly Error SaltGenerationFailed =
        Error.Failure("Cryptography.SaltGenerationFailed", "Failed to generate secure random salt.");

    public static readonly Error HashingFailed =
        Error.Failure("Cryptography.HashingFailed", "An error occurred while computing the data hash.");

    public static readonly Error EmptyPlainText =
        Error.Validation("Cryptography.EmptyPlainText", "Plain text value cannot be empty for hashing.");

    public static readonly Error EmptySalt =
        Error.Validation("Cryptography.EmptySalt", "Salt value cannot be empty for hashing.");

    public static readonly Error CanonicalizationFailed =
        Error.Failure("Cryptography.CanonicalizationFailed", "An error occurred while canonicalizing the object into a deterministic JSON string.");

    public static readonly Error NullDataPayload =
        Error.Validation("Cryptography.NullDataPayload", "Data payload cannot be null for canonicalization.");
}
