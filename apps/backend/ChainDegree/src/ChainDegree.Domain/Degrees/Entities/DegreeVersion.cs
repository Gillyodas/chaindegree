using System;
using ChainDegree.Core.Domain.SharedKernel;

namespace ChainDegree.Core.Domain.Degrees.Entities
{
    public class DegreeVersion : Entity
    {
        public Guid DegreeId { get; private set; }
        public int Version { get; private set; }
        public string PreviousHash { get; private set; } = null!;
        public string CurrentHash { get; private set; } = null!;
        public string BlockchainTxHash { get; private set; } = null!;
        public string? MerkleProofJson { get; private set; } // TODO: Cân nhắc lưu trữ thêm MerkleProof để Verifier sử dụng sau này
        public string PlainDataJson { get; private set; } = null!;
        public string Salt { get; private set; } = null!;
        public string Major { get; private set; } = null!;
        public string Classification { get; private set; } = null!;
        public DateTime EffectiveAt { get; private set; }

        private DegreeVersion(
            Guid id,
            Guid degreeId,
            int version,
            string previousHash,
            string currentHash,
            string blockchainTxHash,
            DateTime effectiveAt,
            string plainDataJson,
            string salt,
            string major,
            string classification,
            string? merkleProofJson)
        {
            Id = id;
            DegreeId = degreeId;
            Version = version;
            PreviousHash = previousHash;
            CurrentHash = currentHash;
            BlockchainTxHash = blockchainTxHash;
            EffectiveAt = effectiveAt;
            PlainDataJson = plainDataJson;
            Salt = salt;
            Major = major;
            Classification = classification;
            MerkleProofJson = merkleProofJson;
            CreatedAt = DateTime.UtcNow;
        }

        private DegreeVersion() { }

        public static DegreeVersion Create(
            Guid degreeId,
            int version,
            string previousHash,
            string currentHash,
            string blockchainTxHash,
            DateTime effectiveAt,
            string plainDataJson,
            string salt,
            string major,
            string classification,
            string? merkleProofJson)
        {
            return new DegreeVersion(
                Guid.NewGuid(),
                degreeId,
                version,
                previousHash,
                currentHash,
                blockchainTxHash,
                effectiveAt,
                plainDataJson,
                salt,
                major,
                classification,
                merkleProofJson);
        }
    }
}
