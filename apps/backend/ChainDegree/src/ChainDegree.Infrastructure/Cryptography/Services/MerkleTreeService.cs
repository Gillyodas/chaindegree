using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using ChainDegree.Core.Application.Abstractions.Crypto;

namespace ChainDegree.Core.Infrastructure.Cryptography.Services
{
    public class MerkleTreeService : IMerkleTreeService
    {
        public MerkleTreeResult BuildTree(IReadOnlyList<string> leafHashes)
        {
            if (leafHashes == null || leafHashes.Count == 0)
            {
                throw new ArgumentException("Leaf hashes cannot be null or empty.");
            }

            List<string> currentLevel = new List<string>(leafHashes);
            List<List<string>> tree = new List<List<string>> { currentLevel };

            while (currentLevel.Count > 1)
            {
                List<string> nextLevel = new List<string>();
                for (int i = 0; i < currentLevel.Count; i += 2)
                {
                    string left = currentLevel[i];
                    string right = (i + 1 < currentLevel.Count) ? currentLevel[i + 1] : left;

                    nextLevel.Add(HashNodes(left, right));
                }
                currentLevel = nextLevel;
                tree.Add(currentLevel);
            }

            string root = currentLevel[0];
            List<MerkleProofData> proofs = new List<MerkleProofData>();

            for (int i = 0; i < leafHashes.Count; i++)
            {
                List<string> proofHashes = new List<string>();
                List<bool> proofDirections = new List<bool>();

                int currentIndex = i;
                for (int level = 0; level < tree.Count - 1; level++)
                {
                    var levelNodes = tree[level];
                    bool isSiblingRight = currentIndex % 2 == 0;
                    int siblingIndex = isSiblingRight
                        ? (currentIndex + 1 < levelNodes.Count ? currentIndex + 1 : currentIndex)
                        : currentIndex - 1;

                    proofHashes.Add(levelNodes[siblingIndex]);
                    proofDirections.Add(isSiblingRight);

                    currentIndex /= 2;
                }

                proofs.Add(new MerkleProofData(i, leafHashes[i], proofHashes, proofDirections));
            }

            return new MerkleTreeResult(root, proofs);
        }

        public bool VerifyProof(string leafHash, MerkleProofData proof, string merkleRoot)
        {
            if (proof == null || proof.ProofHashes.Count != proof.ProofDirections.Count)
            {
                return false;
            }

            string current = leafHash;
            for (int i = 0; i < proof.ProofHashes.Count; i++)
            {
                string sibling = proof.ProofHashes[i];
                bool isSiblingRight = proof.ProofDirections[i];

                current = isSiblingRight ? HashNodes(current, sibling) : HashNodes(sibling, current);
            }

            return string.Equals(current, merkleRoot, StringComparison.OrdinalIgnoreCase);
        }

        private string HashNodes(string left, string right)
        {
            byte[] leftBytes = Convert.FromHexString(left);
            byte[] rightBytes = Convert.FromHexString(right);

            byte[] combined = new byte[leftBytes.Length + rightBytes.Length];
            Buffer.BlockCopy(leftBytes, 0, combined, 0, leftBytes.Length);
            Buffer.BlockCopy(rightBytes, 0, combined, leftBytes.Length, rightBytes.Length);

            byte[] hash = SHA256.HashData(combined);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
