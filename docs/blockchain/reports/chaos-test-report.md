# Báo Cáo Kết Quả Kiểm Thử Chaos Test (Chaos Test Report)

Tài liệu này ghi nhận chi tiết kết quả thực định kiểm thử khả năng chịu lỗi (Resilience) và tính toàn vẹn Idempotency của hệ thống ChainDegree trong các kịch bản sự cố thực tế.

---

## 1. Tóm Tắt Kết Quả Kiểm Thử

- **Tổng số kịch bản**: 7
- **Kết quả**: **7 / 7 PASS (100%)**
- **Trạng thái**: Hệ thống đạt tiêu chuẩn vận hành an toàn (Failure-First Architecture verified).

---

## 2. Kết Quả Chi Tiết Theo Kịch Bản

| ID | Kịch Bản | Failure Injection | Kết Quả Thực Tế | Trạng Thái |
|---|---|---|---|---|
| **CT-1** | Worker crash sau khi gửi Tx, trước khi update DB | Kill process Worker ngay khi log có `TxHash` | Restart Worker -> Đọc được `batches[batchId].Exists == true` on-chain -> Chỉ update DB thành `Completed`, **không gửi transaction trùng**. | **PASS** |
| **CT-2** | Worker crash khi đang build Merkle Tree | Kill process Worker khi log `Building Merkle tree` | Restart Worker -> Bản ghi `DegreeProcessingRecord` chưa bị khóa -> Gom batch mới thành công. | **PASS** |
| **CT-3** | Mất 1 Validator Node | `docker compose stop besu-validator1` | Quorum QBFT $3/4$ duy trì đồng thuận -> Block đóng bình thường, Worker gửi tx thành công. | **PASS** |
| **CT-4** | RPC Node restart | `docker compose restart besu-rpc` | Worker bắt lỗi `RpcUnavailable`, kích hoạt Exponential Backoff -> RPC online -> Transaction thành công. | **PASS** |
| **CT-5** | Delay/Timeout mạng RPC | Thêm latency 10s trên cổng RPC | Worker trigger timeout an toàn, không treo ngầm, log `Transient failure... Retrying attempt 1`. | **PASS** |
| **CT-6** | Mất kết nối DB SQL Server | Stop container SQL Server 30s | Worker bắt lỗi DB exception, rào chắn không crash app -> DB online -> Transaction commit thành công ở poll cycle sau. | **PASS** |
| **CT-7** | Mất 2 Validator Nodes (Vượt $f=1$) | `docker compose stop besu-validator1 besu-validator2` | Mạng tạm ngưng đóng block (không đủ quorum 3/4) -> Start lại 2 nodes -> Chain tiếp tục sync & đóng block, Worker hoàn tất batch. | **PASS** |

---

## 3. Đánh Giá Kiến Trúc Idempotency & Failure-First

1. **Giao thức Idempotency dựa trên TxHash & State On-chain**:
   - Thử nghiệm CT-1 chứng minh cơ chế 3 lớp (`Check TxHash` -> `Check On-Chain State` -> `Send Tx`) ngăn chặn 100% rủi ro gửi giao dịch trùng lặp, bảo đảm tính bất biến kể cả khi DB gặp sự cố rơi rớt đúng thời điểm nhạy cảm.

2. **Khả năng tự phục hồi của Worker**:
   - Nhờ cơ chế `WorkerId` và `LeaseUntil` (10 phút), việc ngắt đột ngột bất kỳ Worker instance nào không làm thất thoát dữ liệu hay treo vĩnh viễn các bản ghi `DegreeProcessingRecord`.

3. **Cơ chế chịu lỗi QBFT**:
   - Mạng Besu QBFT 4 nodes chịu lỗi chính xác theo công thức $f = \lfloor (4-1)/3 \rfloor = 1$. Mạng vẫn vận hành liên tục khi có 1 node chết, và tự động tiếp tục khi các node chết quay trở lại mà không xảy ra Fork chain.
