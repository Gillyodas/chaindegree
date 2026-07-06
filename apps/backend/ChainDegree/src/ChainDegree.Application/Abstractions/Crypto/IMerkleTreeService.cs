using System;
using System.Collections.Generic;

namespace ChainDegree.Core.Application.Abstractions.Crypto
{
    public interface IMerkleTreeService
    {
        MerkleTreeResult BuildTree(IReadOnlyList<string> leafHashes);
        bool VerifyProof(string leafHash, MerkleProofData proof, string merkleRoot);
    }

    public record MerkleTreeResult(
        string MerkleRoot,
        List<MerkleProofData> Proofs
    );

    public record MerkleProofData(
        int LeafIndex,
        string LeafHash,
        List<string> ProofHashes,
        List<bool> ProofDirections // true = right sibling, false = left sibling
    );
}
