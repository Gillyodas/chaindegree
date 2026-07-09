using ChainDegree.Core.Domain.Degrees.Enums;

namespace ChainDegree.Core.Domain.Degrees.ValueObjects
{
    public class VerificationSnapshot
    {
        public string DataHash { get; }
        public string Salt { get; }
        public string PlainDataJson { get; }
        public string TxHash { get; }
        public string? MerkleProofJson { get; }
        public int Version { get; }
        public StatusEnum Status { get; }

        public VerificationSnapshot(
            string dataHash,
            string salt,
            string plainDataJson,
            string txHash,
            string? merkleProofJson,
            int version,
            StatusEnum status)
        {
            DataHash = dataHash;
            Salt = salt;
            PlainDataJson = plainDataJson;
            TxHash = txHash;
            MerkleProofJson = merkleProofJson;
            Version = version;
            Status = status;
        }
    }
}
