using System;
using ChainDegree.Core.Domain.Degrees.Enums;

namespace ChainDegree.Core.Application.Degrees.Queries.VerifyDegree
{
    public sealed record VerifyDegreeResponse(
        bool Verified,
        string Status,
        VerificationSource? VerificationSource,
        string DegreeCode,
        int Version,
        string? InstitutionName,
        string? StudentFullName,
        string? Major,
        string? Classification,
        DateTime? IssuedAt,
        BlockchainDetails? Blockchain);

    public sealed record BlockchainDetails(
        string TxHash,
        long? BlockNumber,
        string MerkleRoot,
        string? MerkleProofJson);
}
