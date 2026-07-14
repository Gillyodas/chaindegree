# Blockchain Implementation Plan

Tài liệu này xác định lộ trình triển khai chi tiết cho các thành phần blockchain trong hệ thống ChainDegree. Kế hoạch được chia thành 5 giai đoạn (từ Phase 0 đến Phase 4) tập trung vào nguyên tắc **"Đúng và Đủ"**, loại bỏ over-engineering và ưu tiên hai giá trị cốt lõi: Tính đúng đắn (Correctness) và Bảo mật (Security).

---

## Phase 0: Development Environment & "Hello World"

**Mục tiêu:** Dựng môi trường Besu tối thiểu, triển khai smart contract và xác nhận toàn bộ toolchain hoạt động trơn tru để khoanh vùng lỗi nếu có sau này.

- **0.1. Local Besu Node:** Chạy 1 node Besu duy nhất qua Docker (chế độ dev) để làm môi trường test nhanh.
- **0.2. Contract Development & Tooling:** 
  - Viết `DegreeAnchor.sol`.
  - Thiết lập **Hardhat** làm toolchain chính.
  - Viết script deploy bằng Hardhat (tuyệt đối không deploy thủ công bằng Remix).
    - *Lưu ý (Fail Fast):* Script deploy phải gọi `eth_chainId` để đảm bảo đang deploy đúng mạng lưới (VD: Expected 1337) trước khi thực hiện deploy.
  - Viết Unit Tests bao phủ các case: Happy Path, Duplicate Batch, Invalid BatchId, Unauthorized Caller, Non-existing Batch.
- **0.3. "Hello World" Transaction:** Chạy script Hardhat để deploy contract lên node Besu local và gọi thử hàm `anchorMerkleRoot`.

**✅ Done Criteria (Smoke Test):**
```
Deploy contract → Verify contract exists (eth_getCode != 0x) → Anchor test transaction → Read mapping `batches[batchId]` → Verify state → PASS
```
**📦 Deliverables:**
- Besu Docker Environment
- `DegreeAnchor.sol`
- Hardhat Config
- Deployment Script
- Contract Test Suite
- Deployment Verification Guide

---

## Phase 1: Nethereum Integration & Core Logic

**Mục tiêu:** Hoàn thiện luồng cấp bằng và đóng gói giao dịch từ Backend xuống Blockchain trên môi trường Node đơn. Quá trình làm sẽ theo thứ tự: *Contract → Nethereum → Worker → Signer*.

- **1.1. Smart Contract Unit Tests:** Viết test coverage cho contract trực tiếp bằng Hardhat.
- **1.2. Nethereum Service & Confirmation Strategy:** 
  - Tích hợp thư viện Nethereum vào Backend.
  - Viết logic gọi hàm `anchorMerkleRoot`.
  - **Transaction Confirmation Strategy:** 
    - *Development / Besu QBFT:* Receipt thành công được xem là final (do đặc thù QBFT không có chain reorg). Worker cập nhật Database ngay lập tức.
    - *Public chain (Ethereum...):* Thiết kế abstraction cho phép cấu hình số confirmations (ví dụ 3–12 confirmations) để sẵn sàng nếu sau này chuyển sang public blockchain.
  - *Lưu ý:* Lược bỏ việc đọc event logs (`GetFilterChanges()`), Worker chỉ quan tâm transaction có success hay không thông qua Receipt.
- **1.3. Cập nhật Worker & Idempotency Flow:**
  - Tích hợp `NethereumBlockchainService` vào `BatchingDegreeWorker`.
  - **Xử lý Idempotency (ưu tiên TxHash):**
    - Kiểm tra `DegreeProcessingRecord` có `BlockchainTxHash` chưa?
    - Nếu CÓ: Query receipt của TxHash → Nếu Success → Cập nhật DB (Confirmed) → Return. Nếu Pending → **Retry sau**.
      - *Lưu ý (Timeout Strategy):* Nếu transaction ở trạng thái Pending quá ngưỡng cấu hình, chuyển sang trạng thái Failed hoặc Retry theo chính sách của Worker, tránh chờ vô thời hạn.
    - Nếu KHÔNG CÓ TxHash: Đọc on-chain state `contract.batches(batchId).Exists`.
    - Nếu ĐÃ TỒN TẠI trên chain → Cập nhật DB (Confirmed).
    - Nếu CHƯA TỒN TẠI → Tiến hành gửi Tx mới.
  - **Retry Policy:** Worker sử dụng Exponential Backoff cho các lỗi tạm thời (RPC timeout, network error...), tránh retry liên tục gây quá tải RPC Node.
- **1.4. Signer Abstraction:**
  - Tạo `IBlockchainSigner`.
  - Implement `LocalEnvSigner` bằng cách wrap Nethereum's Account abstraction (sử dụng private key từ `.env`). Không tự chế lại protocol ký.
- **1.5. Configuration Validation (Fail Fast):**
  - Kiểm tra các cấu hình thiết yếu khi ứng dụng khởi động (RPC URL, ChainId, Contract Address, Signer).
  - Gọi `eth_getCode(ContractAddress)` để đảm bảo contract thực sự tồn tại trên mạng lưới.
  - Nếu cấu hình không hợp lệ hoặc contract trả về `0x`, ứng dụng phải Fail Fast (dừng ngay lập tức) thay vì để chạy lâu mới phát hiện lỗi.


**✅ Done Criteria (Integration Test):**
```
Backend Worker → Gửi Anchor Tx → Lấy TxHash → Đợi Receipt → Update Database (Confirmed)
```
**📦 Deliverables:**
- Unit test cho contract (Hardhat)
- `NethereumBlockchainService`
- `LocalEnvSigner`
- Tích hợp thành công vào Worker
- README hướng dẫn chạy local

---

## Phase 2: Production Topology (4 Validators + 1 RPC)

**Mục tiêu:** Mở rộng từ node dev đơn lẻ lên kiến trúc mạng consortium QBFT chịu lỗi đúng chuẩn production.

- **2.1. Cấu hình QBFT Genesis:** Tạo `genesis.json` với danh sách 4 validators ban đầu.
- **2.2. Docker Compose Topology:**
  - **4 Validator Nodes:** Chỉ tham gia consensus, chỉ mở port P2P (30303), KHÔNG mở RPC.
  - **1 RPC Node:** Non-validator, chỉ phục vụ mở RPC (8545) cho Backend gọi vào mạng.
  - *Lưu ý bảo mật RPC:* RPC Node chỉ cho phép Backend hoặc Internal Network truy cập. Tuyệt đối **không expose RPC trực tiếp ra Internet**. Chỉ enable các JSON-RPC APIs cần thiết cho Backend (ví dụ `eth_sendRawTransaction`, `eth_call`, `eth_getTransactionReceipt`), vô hiệu hóa hoàn toàn các API quản trị và debug (`admin_*`, `debug_*`, `personal_*`) trên Production.
  - **Bootnode / Static Nodes:** Cấu hình để các node tự động tìm thấy nhau (Discovery).
  - *Lưu ý về RPC HA (Production Scaling):* Kiến trúc hiện tại dùng 1 RPC. Khi lên production thật, khuyến nghị mô hình: `4 Validators + 2 RPC Nodes + Load Balancer (HAProxy/Nginx) → Backend`.
- **2.3. Chuyển đổi môi trường:** 
  - Cập nhật RPC URL của Backend sang RPC Node mới. 
  - Chạy lại script Hardhat deploy lên mạng mới (Không viết lại deploy logic).

**✅ Done Criteria (Fault Tolerance Test):**
```
Mạng 4 Validators đang chạy → Tắt/Kill 1 Validator (down) → Backend gửi Tx mới → Chain vẫn hoạt động và block vẫn được đóng (PASS).
```
**📦 Deliverables (Infrastructure as Code):**
- `docker-compose.yml` cho mạng 4 Validator + 1 RPC + Bootnode
- `.env.example`
- `config/` (chứa các file cấu hình node)
- `genesis.json`
- `static-nodes.json`
- Deployment script chạy trên mạng lưới mới

---

## Phase 3: Key Management & Security (Design Only)

**Mục tiêu:** Chuẩn bị sẵn kiến trúc bảo mật cho việc scale lên Cloud/Enterprise, nhưng **chỉ dừng ở mức tài liệu (Document)** để tránh sa đà vào over-engineering hệ thống lúc này.

- **3.1. Thiết kế KMS Signer:** 
  - Tài liệu hóa kiến trúc nâng cấp `IBlockchainSigner` dùng `KMSSigner` (Azure Key Vault / HashiCorp Vault) hoặc `RemoteSigner`.
  - **Lưu ý:** KHÔNG code implementation lúc này. Chỉ code thật sự khi dự án yêu cầu deploy lên Cloud Production.
- **3.2. Key Lifecycle Documentation:** 
  - Tài liệu hóa quy trình quản lý vòng đời của key (Generate → Activate → Rotate → Disable → Destroy). 
  - KHÔNG cần viết CLI automation scripts hay tool chuyên dụng cho việc này.

**✅ Done Criteria:** 
```
Có bản thiết kế tài liệu chuẩn xác về lộ trình nâng cấp Security và Key Management. Codebase giữ nguyên LocalEnvSigner.
```
**📦 Deliverables:**
- Tài liệu kiến trúc nâng cấp KMS
- Tài liệu Key Lifecycle Procedure

---

## Phase 4: Observability, Load Testing & DR

**Mục tiêu:** Đảm bảo khả năng vận hành, giám sát (Observability) và chứng minh hiệu năng hệ thống chịu được ngưỡng load thực tế.

- **4.1. Monitoring Dashboards & Logging:** 
  - Cài đặt Prometheus và Grafana.
  - Dashboard theo dõi Besu (Block height, Peers, Tx pending, JVM Memory) và Worker (Queue length, Batch latency, Retry count).
  - *Log Correlation:* Worker log bắt buộc phải chứa `BatchId`, `BlockchainTxHash` và `CorrelationId` để phục vụ truy vết sự cố nhanh chóng.
  - *Lưu ý:* Chỉ làm Dashboard, KHÔNG setup Alerting (Email/Slack/Webhook) phức tạp.
- **4.2. Load Testing & Chaos Testing (Benchmark):** 
  - Chạy giả lập cấp bằng số lượng lớn để kiểm chứng các thông số config của worker.
  - **Load Test:** 500 degrees → 1000 degrees → 5000 degrees (Đo lường: TPS, Latency, Memory, Merkle Build Time, Worker Throughput, **Block Confirmation Time**).
  - **Chaos Test (Failure Injection manual):** 
    - Worker crash sau khi gửi transaction nhưng trước khi cập nhật DB → Khởi động lại → Worker phát hiện tx đã thành công và chỉ cập nhật DB (không gửi tx mới).
    - Kill Worker đang build Merkle → Restart → Đảm bảo không duplicate anchor.
    - Tắt 1 Validator hoặc restart RPC node → Worker retry → Cuối cùng thành công.
    - Network timeout (VD: RPC bị delay 10s) → Worker timeout → Retry an toàn.
- **4.3. Disaster Recovery (DR):** 
  - *Thực hiện sau khi đã hiểu hành vi hệ thống qua Load Test.*
  - Chạy Cron job backup Database định kỳ đơn giản.
  - Tài liệu hóa hướng dẫn backup các file `genesis.json` và configs (Không viết script automation rườm rà).

**✅ Done Criteria (Performance & Resilience Test):**
```
Hoàn thành báo cáo Benchmark chứng minh thông số Batch=500, Polling=15s là hợp lý và hệ thống tự phục hồi đúng sau các kịch bản Chaos Test.
```
**📦 Deliverables:**
- Grafana dashboard config
- Log structure documentation
- Benchmark & Load Test Report
- Chaos Test Report
- Disaster Recovery procedure document
