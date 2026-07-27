using System;
using ChainDegree.Core.Domain.Degrees.Enums;

namespace ChainDegree.Core.Domain.Degrees.ValueObjects
{
    public class VerificationSnapshot
    {
        public Guid DegreeId { get; }
        public string DataHash { get; }
        public string Salt { get; }
        public string PlainDataJson { get; }
        public string TxHash { get; }
        public string? MerkleProofJson { get; }
        public int Version { get; }
        public StatusEnum Status { get; }
        public string StudentFullName { get; }
        public string Major { get; }
        public string Classification { get; }
        public Guid StudentId { get; }
        public DateTime IssuedAt { get; }
        public string InstitutionName { get; }
        public Guid InstitutionId { get; }

        public VerificationSnapshot(
            Guid degreeId,
            string dataHash,
            string salt,
            string plainDataJson,
            string txHash,
            string? merkleProofJson,
            int version,
            StatusEnum status,
            string studentFullName,
            string major,
            string classification,
            Guid studentId,
            DateTime issuedAt,
            string institutionName,
            Guid institutionId)
        {
            DegreeId = degreeId;
            DataHash = dataHash;
            Salt = salt;
            PlainDataJson = plainDataJson;
            TxHash = txHash;
            MerkleProofJson = merkleProofJson;
            Version = version;
            Status = status;
            StudentFullName = studentFullName;
            Major = major;
            Classification = classification;
            StudentId = studentId;
            IssuedAt = issuedAt;
            InstitutionName = institutionName;
            InstitutionId = institutionId;
        }
    }
}
