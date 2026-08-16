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

            List<string> cleanLeafHashes = new List<string>(leafHashes.Count);
            foreach (var hash in leafHashes)
            {
                string clean = hash.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? hash.Substring(2) : hash;
                cleanLeafHashes.Add(clean.ToLowerInvariant());
            }

            List<string> currentLevel = new List<string>(cleanLeafHashes);
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

            for (int i = 0; i < cleanLeafHashes.Count; i++)
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

                proofs.Add(new MerkleProofData(i, cleanLeafHashes[i], proofHashes, proofDirections));
            }

            return new MerkleTreeResult(root, proofs);
        }

        public bool VerifyProof(string leafHash, MerkleProofData proof, string merkleRoot)
        {
            if (proof == null || proof.ProofHashes.Count != proof.ProofDirections.Count)
            {
                return false;
            }

            string cleanLeafHash = leafHash.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? leafHash.Substring(2) : leafHash;
            string cleanMerkleRoot = merkleRoot.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? merkleRoot.Substring(2) : merkleRoot;

            string current = cleanLeafHash.ToLowerInvariant();
            for (int i = 0; i < proof.ProofHashes.Count; i++)
            {
                string sibling = proof.ProofHashes[i];
                string cleanSibling = sibling.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? sibling.Substring(2) : sibling;
                bool isSiblingRight = proof.ProofDirections[i];

                current = isSiblingRight ? HashNodes(current, cleanSibling) : HashNodes(cleanSibling, current);
            }

            return string.Equals(current, cleanMerkleRoot, StringComparison.OrdinalIgnoreCase);
        }

        private string HashNodes(string left, string right)
        {
            string cleanLeft = left.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? left.Substring(2) : left;
            string cleanRight = right.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? right.Substring(2) : right;

            byte[] leftBytes = Convert.FromHexString(cleanLeft);
            byte[] rightBytes = Convert.FromHexString(cleanRight);

            byte[] combined = new byte[leftBytes.Length + rightBytes.Length];
            Buffer.BlockCopy(leftBytes, 0, combined, 0, leftBytes.Length);
            Buffer.BlockCopy(rightBytes, 0, combined, leftBytes.Length, rightBytes.Length);

            byte[] hash = SHA256.HashData(combined);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
