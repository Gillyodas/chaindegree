using System;
using System.Collections.Generic;
using ChainDegree.Core.Application.Abstractions.Crypto;
using ChainDegree.Core.Infrastructure.Cryptography.Services;
using Xunit;

namespace ChainDegree.Infrastructure.Tests.Crypto
{
    public class MerkleTreeServiceTests
    {
        private readonly MerkleTreeService _service;

        public MerkleTreeServiceTests()
        {
            _service = new MerkleTreeService();
        }

        [Fact]
        public void BuildTree_WithSingleLeaf_ReturnsCorrectTree()
        {
            // Arrange
            var leafHash = new string('a', 64);
            var leaves = new List<string> { leafHash };

            // Act
            var result = _service.BuildTree(leaves);

            // Assert
            Assert.Equal(leafHash, result.MerkleRoot);
            Assert.Single(result.Proofs);
            Assert.Equal(leafHash, result.Proofs[0].LeafHash);
            Assert.Empty(result.Proofs[0].ProofHashes);
        }

        [Fact]
        public void BuildTree_WithEvenLeaves_ReturnsRootAndCorrectProofs()
        {
            // Arrange
            var leaf1 = new string('a', 64);
            var leaf2 = new string('b', 64);
            var leaves = new List<string> { leaf1, leaf2 };

            // Act
            var result = _service.BuildTree(leaves);

            // Assert
            Assert.NotNull(result.MerkleRoot);
            Assert.Equal(2, result.Proofs.Count);
            
            // Verify proof 1
            var proof1 = result.Proofs[0];
            Assert.Equal(leaf1, proof1.LeafHash);
            Assert.Single(proof1.ProofHashes);
            Assert.Equal(leaf2, proof1.ProofHashes[0]);
            Assert.True(proof1.ProofDirections[0]); // Sibling on the right

            // Verify proof 2
            var proof2 = result.Proofs[1];
            Assert.Equal(leaf2, proof2.LeafHash);
            Assert.Single(proof2.ProofHashes);
            Assert.Equal(leaf1, proof2.ProofHashes[0]);
            Assert.False(proof2.ProofDirections[0]); // Sibling on the left

            // Verify verification logic
            Assert.True(_service.VerifyProof(leaf1, proof1, result.MerkleRoot));
            Assert.True(_service.VerifyProof(leaf2, proof2, result.MerkleRoot));
        }

        [Fact]
        public void VerifyProof_WithTamperedLeaf_ReturnsFalse()
        {
            // Arrange
            var leaf1 = new string('a', 64);
            var leaf2 = new string('b', 64);
            var leaves = new List<string> { leaf1, leaf2 };
            var result = _service.BuildTree(leaves);
            var proof1 = result.Proofs[0];

            // Act
            var verified = _service.VerifyProof(new string('c', 64), proof1, result.MerkleRoot);

            // Assert
            Assert.False(verified);
        }
    }
}
