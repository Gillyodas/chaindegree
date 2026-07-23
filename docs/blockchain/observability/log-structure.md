# Cấu Trúc Log Giám Sát (Worker Log Structure)

Tài liệu này quy định chuẩn định dạng và các trường dữ liệu truy vết trong hệ thống logging của `BatchingDegreeWorker`.

---

## 1. Nguyên Tắc Thiết Kế Log

1. **Structured Logging**: Mọi log entry đều sử dụng placeholders dạng `{Field}` để các hệ thống thu gom log (ELK, Loki, SEQ) có thể index dữ liệu.
2. **Context Correlation**:
   - Tầng API: Sử dụng `CorrelationId` từ `CorrelationIdMiddleware` (`X-Correlation-ID`) cho chu kỳ xử lý request HTTP.
   - Tầng Worker: Sinh `BatchCorrelationId` (Guid) mới cho mỗi chu kỳ xử lý batch ngầm, cho phép gom nhóm toàn bộ các thông điệp log thuộc cùng một batch lifecycle.
3. **Audit & Traceability**: Tất cả log xử lý batch bắt buộc chứa 4 trường cốt lõi:
   - `BatchCorrelationId`: Mã tương quan của vòng xử lý batch.
   - `BatchId`: GUID định danh lô bằng.
   - `BlockchainTxHash`: Mã băm giao dịch on-chain (nếu đã có).
   - `ElapsedMs`: Thời gian thực thi tính bằng miligiây.
4. **Bảo Mật**: Tuyệt đối **KHÔNG** log Private Key, Secret, Password, hoặc thông tin cá nhân (PII) của sinh viên.

---

## 2. Danh Sách Các Trường Correlation

| Tên trường | Kiểu dữ liệu | Mô tả | Ví dụ |
|---|---|---|---|
| `BatchCorrelationId` | `string` (Guid) | Mã tương quan chu kỳ xử lý Worker | `3f8a42b1-9c12-4d8e-b521-123456789abc` |
| `BatchId` | `string` (Guid) | GUID định danh duy nhất của BatchRecord | `a1b2c3d4-e5f6-7890-abcd-ef1234567890` |
| `BlockchainTxHash` | `string` | Hash của giao dịch trên Hyperledger Besu | `0x8f2a55949038a9610f50fb23b5883af3b4ca1366f1a09c2a114f10c14457e937` |
| `ElapsedMs` | `long` | Thời gian thực thi đo bằng `Stopwatch` (ms) | `1450` |
| `MerkleRoot` | `string` | Hash Merkle Root (32 bytes hex) | `0x42699A7612A82f1d9C36148af9C77354759b210b...` |
| `DegreeCount` | `int` | Số lượng văn bằng trong batch | `500` |

---

## 3. Mẫu Log Điển Hình (Log Templates)

### 3.1. Gom batch thành công & dựng Merkle Tree
```text
[INFO] Processing batch a1b2c3d4-e5f6-7890-abcd-ef1234567890 with 500 degrees. BatchCorrelationId=3f8a42b1-9c12-4d8e-b521-123456789abc
[INFO] Merkle tree built for batch a1b2c3d4-e5f6-7890-abcd-ef1234567890. Root=0xabc..., LeafCount=500, ElapsedMs=12
```

### 3.2. Gửi giao dịch Blockchain & Confirmed
```text
[INFO] Batch a1b2c3d4-e5f6-7890-abcd-ef1234567890 confirmed. TxHash=0x8f2a55949038a9610f50fb23b5883af3b4ca1366f1a09c2a114f10c14457e937, TotalElapsedMs=2340
```

### 3.3. Retry lỗi tạm thời (Transient Fault)
```text
[WARN] Transient failure in AnchorMerkleRoot for batch a1b2c3d4-e5f6-7890-abcd-ef1234567890. Retrying attempt 1. Reason=RPC timeout
```

### 3.4. Thất bại vĩnh viễn (Permanent Failure)
```text
[ERROR] Permanent failure executing AnchorMerkleRoot for batch a1b2c3d4-e5f6-7890-abcd-ef1234567890: Smart contract revert
```

### 3.5. Phục hồi batch chờ (Recovery)
```text
[INFO] Batch a1b2c3d4-e5f6-7890-abcd-ef1234567890 recovered as Confirmed. TxHash=0x8f2a55949038a9610f50fb23b5883af3b4ca1366f1a09c2a114f10c14457e937, ElapsedMs=450
```
