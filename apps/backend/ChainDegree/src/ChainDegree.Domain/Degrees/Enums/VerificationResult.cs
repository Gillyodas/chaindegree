namespace ChainDegree.Core.Domain.Degrees.Enums
{
    public enum VerificationResult
    {
        Verified,             // Hợp lệ, hash + blockchain đều khớp
        Revoked,              // Bằng đã bị thu hồi
        CryptoHashMismatch,   // Dữ liệu bị can thiệp cục bộ (hash tính lại khác hash lưu)
        BlockchainInvalid,    // Hash lưu không khớp Merkle Root trên blockchain
        DegreeNotFound,       // Không tìm thấy bằng cấp với DegreeCode
        UnsupportedVersion    // Phiên bản được chỉ định không tồn tại
    }
}
