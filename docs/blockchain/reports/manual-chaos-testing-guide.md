# Hướng Dẫn Thực Hiện Chaos Testing Thủ Công (Manual Chaos Testing Guide)

Tài liệu này cung cấp hướng dẫn từng bước chi tiết (Step-by-step Guide) kèm theo các lệnh PowerShell/Docker, câu truy vấn SQL và nhật ký log kỳ vọng để bạn tự thực hiện **Kiểm Thử Sự Cố (Chaos Testing)** đầy đủ **7 Kịch Bản (CT-1 đến CT-7)** cho hệ thống **ChainDegree**.

---

## 📋 Chuẩn Bị Môi Trường Trước Khi Test

1. **Khởi động mạng Blockchain 5 Node**:
   ```powershell
   cd apps/blockchain
   docker-compose up -d
   ```
2. **Kiểm tra trạng thái Grafana & Prometheus**:
   - Truy cập Grafana: `http://localhost:3000` (Kiểm tra Dashboard **Besu Network Health** & **Worker Health**).
   - Truy cập Prometheus Target: `http://localhost:9090/targets` (Đảm bảo target `chaindegree-backend` và `besu-nodes` màu xanh `UP`).

3. **Khởi động Backend API / Worker**:
   ```powershell
   cd apps/backend/ChainDegree
   dotnet run --project src/ChainDegree.API/ChainDegree.API.csproj --urls "http://0.0.0.0:5000"
   ```

---

## 🧪 Kịch Bản CT-1: Worker Crash Sau Khi Gửi Transaction (Idempotency Core)

### 🎯 Mục đích:
Kiểm tra hiện tượng giao dịch đã gửi lên Blockchain thành công nhưng Worker bị crash ngắt đột ngột trước khi kịp cập nhật trạng thái `Confirmed` vào SQL Database.

### 🐾 Các bước thực hiện:
1. Gửi kịch bản On-Chain Load Test hoặc tạo 1 Batch văn bằng mới:
   ```powershell
   dotnet run --project apps/blockchain/tests/load-test/ChainDegree.LoadTest.csproj LT-1 --on-chain
   ```
2. Ngay khi giao dịch thành công (nhận được `TxHash`), giả lập Worker bị sập ngắt chừng (hoặc tắt bằng `Ctrl + C` / `taskkill /F /IM ChainDegree.API.exe`).
3. **Kiểm tra trạng thái trên Blockchain**:
   Mở PowerShell và dùng `curl` gọi RPC kiểm tra trực tiếp Smart Contract:
   ```powershell
   curl http://localhost:8545 -H "Content-Type: application/json" -Data '{"jsonrpc":"2.0","method":"eth_getTransactionReceipt","params":["0x<DE-TX-HASH-CUA-BAN>"],"id":1}'
   ```
   *Kỳ vọng*: Trả về JSON có `"status": "0x1"` (Giao dịch đã đóng block thành công trên chain).
4. Khởi chạy lại Backend Worker:
   ```powershell
   dotnet run --project src/ChainDegree.API/ChainDegree.API.csproj --urls "http://0.0.0.0:5000"
   ```

### ✅ Kết quả Kỳ vọng:
- Worker sau khi khởi động lại sẽ kiểm tra lại Smart Contract (`GetBatchAsync`).
- Nhận thấy `Exists == true` trên chain, Worker sẽ **tự động chuyển trạng thái Batch sang `Completed` và Văn bằng sang `Confirmed`** mà **KHÔNG** gửi lại giao dịch trùng lặp lên Blockchain (Idempotence).

---

## 🧪 Kịch Bản CT-2: Worker Crash Trong Khi Build Merkle Tree (Worker Recovery)

### 🎯 Mục đích:
Kiểm tra tính an toàn khi Worker bị sập trong giai đoạn đang băm dữ liệu và dựng cây Merkle Tree (trước khi gửi giao dịch).

### 🐾 Các bước thực hiện:
1. Đưa các văn bằng vào hàng chờ `Pending_Confirmation`.
2. Khởi chạy Worker. Khi màn hình console/log vừa xuất hiện dòng: `Building Merkle tree for batch...`, lập tức tắt đột ngột Worker (`Ctrl + C` hoặc `taskkill`).
3. Khởi chạy lại Worker:
   ```powershell
   dotnet run --project src/ChainDegree.API/ChainDegree.API.csproj --urls "http://0.0.0.0:5000"
   ```

### ✅ Kết quả Kỳ vọng:
- Bản ghi `DegreeProcessingRecord` chưa bị khóa vĩnh viễn nhờ cơ chế `LeaseUntil` (10 phút).
- Khi restart, Worker tự động giải phóng lock cũ, gom lại các văn bằng `Pending_Confirmation` thành batch mới và gửi giao dịch thành công.

---

## 🧪 Kịch Bản CT-3: Mất 1 Node Validator (Fault Tolerance $f=1$)

### 🎯 Mục đích:
Kiểm chứng khả năng chịu lỗi Byzantine của thuật toán QBFT khi có 1 trong 4 Validator bị sập ($N=4, f=1$). Mạng vẫn phải đạt Quorum $\lceil 2N/3 \rceil = 3$ nodes active.

### 🐾 Các bước thực hiện:
1. Ngắt 1 node Validator trong Docker:
   ```powershell
   docker stop besu-validator1
   ```
2. Quan sát trên Grafana Dashboard (`http://localhost:3000`):
   - Màn hình `Connected Peers` của `besu-validator1` sẽ mất kết nối.
   - Các node còn lại (`validator2`, `validator3`, `validator4`, `besu-rpc`) vẫn báo 3 peers active.
3. Chạy lệnh gửi giao dịch On-Chain:
   ```powershell
   dotnet run --project apps/blockchain/tests/load-test/ChainDegree.LoadTest.csproj LT-1 --on-chain
   ```
4. Khôi phục lại Validator 1:
   ```powershell
   docker start besu-validator1
   ```

### ✅ Kết quả Kỳ vọng:
- Lệnh `--on-chain` vẫn báo **`[On-Chain SUCCESS]`** với thời gian đóng block bình thường (~250ms).
- Mạng lưới vẫn tạo block và xác nhận văn bằng mượt mà do 3/4 node Validator vẫn hoạt động.

---

## 🧪 Kịch Bản CT-4: Khởi Động Lại Node RPC Tạm Thời (RPC Resilience)

### 🎯 Mục đích:
Kiểm tra Backend Worker có tự phục hồi (Retry with Exponential Backoff) khi nút cổng kết nối RPC (`besu-rpc`) bị ngắt tạm thời hay không.

### 🐾 Các bước thực hiện:
1. Chạy kịch bản Load Test On-Chain.
2. Ngay khi lệnh đang chạy, lập tức khởi động lại RPC Node:
   ```powershell
   docker restart besu-rpc
   ```
3. Quan sát log của Worker / Console Tool.

### ✅ Kết quả Kỳ vọng:
- Worker nhận lỗi `Blockchain.RpcUnavailable` hoặc `HttpRequestException`.
- Worker kích hoạt chính sách Retry (Retry Policy), chờ RPC node khởi động lại xong (~3-5 giây) và tự động hoàn tất giao dịch mà không làm crash ứng dụng.

---

## 🧪 Kịch Bản CT-5: Mạng RPC Bị Thắt Cổ Chai / Latency (Network Delay 10s)

### 🎯 Mục đích:
Kiểm tra rào chắn xử lý Timeout và Exponential Backoff khi kết nối mạng RPC bị trễ 10s.

### 🐾 Các bước thực hiện:
1. Tạm thời ngắt card mạng hoặc dùng công cụ giả lập latency/delay 10 giây đối với port 8545:
   ```powershell
   # Hoặc stop tạm thời container besu-rpc trong 10 giây
   docker pause besu-rpc
   Start-Sleep -Seconds 10
   docker unpause besu-rpc
   ```
2. Quan sát nhật ký log của Worker.

### ✅ Kết quả Kỳ vọng:
- Worker không bị rò rỉ bộ nhớ hay treo tiến trình vĩnh viễn.
- Worker log thông điệp `Transient failure... Retrying attempt 1`, thực hiện retry an toàn theo Exponential Backoff và hoàn tất khi RPC khôi phục.

---

## 🧪 Kịch Bản CT-6: Mất Kết Nối SQL Server Database Tạm Thời (DB Failure 30s)

### 🎯 Mục đích:
Kiểm tra tính toàn vẹn giao dịch (ACID) khi Database bị ngắt kết nối trong lúc Worker xử lý batch.

### 🐾 Các bước thực hiện:
1. Tắt SQL Server Service / Container trong 30 giây:
   ```powershell
   docker stop sqlserver # Hoặc ngắt dịch vụ MSSQLSERVER
   Start-Sleep -Seconds 30
   docker start sqlserver
   ```
2. Chạy Backend Worker và quan sát log.

### ✅ Kết quả Kỳ vọng:
- Worker bắt được lỗi kết nối DB (`DbUpdateException` / `SqlException`), ghi log `Warning/Error` và rào chắn không làm crash ứng dụng Backend.
- Khi SQL Server online trở lại, chu kỳ Polling tiếp theo sẽ tự động ghi nhận dữ liệu thành công.

---

## 🧪 Kịch Bản CT-7: Mất 2 Node Validator (Consensus Pause & Recovery $f+1=2$)

### 🎯 Mục đích:
Kiểm tra hành vi mạng Blockchain khi vượt quá ngưỡng chịu lỗi (mất 2/4 Validator $\rightarrow$ chỉ còn 2 nodes, không đủ 3/4 quorum để đóng block).

### 🐾 Các bước thực hiện:
1. Ngắt 2 node Validator cùng lúc:
   ```powershell
   docker stop besu-validator1 besu-validator2
   ```
2. Thực thi gửi giao dịch On-Chain:
   ```powershell
   dotnet run --project apps/blockchain/tests/load-test/ChainDegree.LoadTest.csproj LT-1 --on-chain
   ```
3. Quan sát hiện tượng:
   - Giao dịch được gửi vào Mempool nhưng **không có block mới nào được đào** (mạng tạm dừng tạo block để chờ đủ quorum).
4. Bật lại 2 Validator:
   ```powershell
   docker start besu-validator1 besu-validator2
   ```

### ✅ Kết quả Kỳ vọng:
- Khi 2 Validator bật lại, mạng QBFT tự động tái lập đồng thuận, block mới lập tức được đào và giao dịch đang treo trong Mempool được đóng gói thành công.

---

## 📊 Bảng Tổng Kết Đầy Đủ 7 Kịch Bản Chaos Test

| Mã Kịch Bản | Tên Kịch Bản | Thao Tác Failure Injection | Kỳ Vọng Trạng Thái Mạng | Kỳ Vọng Xử Lý Của Backend |
|---|---|---|---|---|
| **CT-1** | Worker Crash (Tx Sent) | Kill Worker ngay sau khi gửi Tx | Mạng bình thường | Auto-recovery `Confirmed` qua Idempotency |
| **CT-2** | Worker Crash (Build Tree) | Kill Worker khi đang build Merkle | Mạng bình thường | Giải phóng `LeaseUntil` & gom lại batch mới |
| **CT-3** | Fault Tolerance ($f=1$) | `docker stop besu-validator1` | 3/4 Nodes Quorum ok | Giao dịch đào thành công (<300ms) |
| **CT-4** | RPC Resilience | `docker restart besu-rpc` | RPC ngắt 3-5s | Worker Retry & kết nối lại mượt mà |
| **CT-5** | Network Latency | `docker pause/unpause besu-rpc` 10s | Trễ kết nối RPC | Trigger Timeout & Exponential Backoff retry |
| **CT-6** | DB Failure | Tắt SQL Server 30s | Mạng blockchain bình thường | Worker rào lỗi DB & retry commit khi DB online |
| **CT-7** | Consensus Pause | `docker stop besu-validator1 besu-validator2` | Mất Quorum, tạm dừng đóng block | Chờ 2 node khôi phục và đóng block tự động |
