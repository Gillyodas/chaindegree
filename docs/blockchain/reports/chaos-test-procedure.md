# Quy Trình Kiểm Thử Chaos Test (Failure Injection Procedure)

Tài liệu này hướng dẫn chi tiết các bước thực thi kiểm thử sự cố (Failure Injection) cho hệ thống blockchain ChainDegree để chứng minh tính tự phục hồi (resilience) và tính bất biến (idempotency).

---

## 1. Kịch Bản CT-1: Worker Crash Sau Khi Gửi Transaction (Idempotency Core)

- **Mục đích**: Kiểm tra hiện tượng transaction đã được đóng trên blockchain nhưng Worker sập trước khi kịp cập nhật trạng thái Database (`Confirmed`).
- **Các bước thực hiện**:
  1. Gửi 1 batch bằng mới.
  2. Theo dõi log Worker, ngay khi xuất hiện thông điệp `Blockchain transaction sent... TxHash=0x...`, lập tức ngắt đột ngột tiến trình Worker (`Kill process` / `Ctrl+C`).
  3. Kiểm tra trạng thái DB: Batch ở trạng thái `Processing`.
  4. Kiểm tra trạng thái Blockchain: Gọi RPC query `batches[batchId]` xác nhận `Exists == true`.
  5. Khởi chạy lại Worker.
- **Kết quả kỳ vọng**: Worker phát hiện batch đã tồn tại trên chain (`Exists == true`), tự động chuyển batch sang `Completed`, cập nhật bằng sang `Confirmed` mà **KHÔNG** gửi lại giao dịch mới (Idempotency).

---

## 2. Kịch Bản CT-2: Worker Crash Trong Khi Build Merkle Tree

- **Mục đích**: Kiểm tra khi Worker sập trong giai đoạn chuẩn bị dữ liệu.
- **Các bước thực hiện**:
  1. Đưa 500 bằng vào hàng chờ `Pending_Confirmation`.
  2. Kill Worker khi log vừa ghi nhận `Building Merkle tree...`.
  3. Khởi chạy lại Worker.
- **Kết quả kỳ vọng**: Bản ghi `DegreeProcessingRecord` vẫn ở trạng thái `Processing`. Sau khi hết hạn `LeaseUntil` (10 phút) hoặc restart, Worker tự gom lại batch mới và gửi giao dịch thành công.

---

## 3. Kịch Bản CT-3: Mất 1 Validator Node (Fault Tolerance $f=1$)

- **Mục đích**: Kiểm thử khả năng chịu lỗi Byzantine của thuật toán đồng thuận QBFT với 4 nodes.
- **Các bước thực hiện**:
  1. Tắt 1 node validator: `docker compose stop besu-validator1`.
  2. Gửi batch bằng mới từ Backend.
  3. Quan sát log Worker và Grafana Dashboard.
- **Kết quả kỳ vọng**: Mạng lưới vẫn đạt quorum đồng thuận ($\lceil 2N/3 \rceil = 3$ nodes active), block tiếp tục được tạo và batch được xác nhận thành công.

---

## 4. Kịch Bản CT-4: Phục Hồi Node RPC (RPC Resilience)

- **Mục đích**: Đảm bảo Backend tự động kết nối lại khi nút RPC bị khởi động lại.
- **Các bước thực hiện**:
  1. Gửi batch mới.
  2. Khi Worker đang gọi RPC, khởi động lại container RPC: `docker compose restart besu-rpc`.
- **Kết quả kỳ vọng**: Worker nhận lỗi `RpcUnavailable`, kích hoạt Exponential Backoff retry, sau khi RPC online trở lại transaction được gửi và xác nhận thành công.

---

## 5. Kịch Bản CT-5: Mạng RPC Bị Thắt Cổ Chai / Timeout (Network Latency)

- **Mục đích**: Kiểm tra xử lý Timeout của Worker.
- **Các bước thực hiện**:
  1. Giả lập delay mạng cho RPC node hoặc ngắt tạm thời mạng `backend_network`.
  2. Worker thực thi `AnchorMerkleRootAsync`.
- **Kết quả kỳ vọng**: Worker log thông điệp `Transient failure... Retrying attempt X`, áp dụng Retry Policy an toàn.

---

## 6. Kịch Bản CT-6: Mất Kết Nối SQL Server Database Tạm Thời

- **Mục đích**: Kiểm tra tính nhất quán giao dịch (Transaction Consistency) của DB.
- **Các bước thực hiện**:
  1. Tắt container SQL Server trong 30 giây khi Worker đang chạy.
  2. Bật lại SQL Server.
- **Kết quả kỳ vọng**: Worker bắt được `DbUpdateException`/`SocketException`, không crash tiến trình ngầm, retry thành công ở chu kỳ sau.

---

## 7. Kịch Bản CT-7: Mất 2 Validator Nodes (Ngừng Đóng Block & Phục Hồi)

- **Mục đích**: Kiểm tra hành vi khi mạng vượt quá ngưỡng chịu lỗi ($f+1 = 2$ nodes down).
- **Các bước thực hiện**:
  1. Tắt 2 nodes validator: `docker compose stop besu-validator1 besu-validator2`.
  2. Gửi batch bằng mới.
  3. Bật lại 2 nodes validator: `docker compose start besu-validator1 besu-validator2`.
- **Kết quả kỳ vọng**: Khi 2 nodes down, mạng dừng tạo block mới (do không đủ 3/4 votes). Worker log `Transient Timeout`. Ngay khi 2 nodes khôi phục, mạng đồng thuận lại và batch được đóng thành công.
