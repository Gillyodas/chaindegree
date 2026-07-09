using System;

namespace ChainDegree.Core.Application.Degrees.Queries.VerifyDegree
{
    public sealed record VerifyDegreeResponse(
        bool Verified,
        string Status,
        string DegreeCode,
        int Version,
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
