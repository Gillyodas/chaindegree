# Kế hoạch Triển khai Phase 2: Production Topology (4 Validators + 1 RPC)

Tài liệu này đặc tả chi tiết kế hoạch triển khai cho **Phase 2: Production Topology** của hệ thống ChainDegree. Giai đoạn này thực hiện mở rộng mạng lưới blockchain Hyperledger Besu từ một node đơn lẻ ở môi trường phát triển (Phase 0/1) thành một mạng Consortium QBFT chịu lỗi tối thiểu đạt tiêu chuẩn vận hành thực tế (KISS & MVP oriented).

---

## 1. Danh sách các Work Package (Work Packages)

### WP 2.1: Genesis & Cryptography Configuration
*   **Mục tiêu**: Cấu hình khởi tạo mạng lưới với thuật toán QBFT, đảm bảo tính bảo mật trước các cuộc tấn công Replay Attack và tối ưu hóa phí giao dịch (Zero-Gas).
*   **Chi tiết thực hiện**:
    1.  Sử dụng công cụ sinh cấu hình hoặc hardhat tạo ra khóa riêng tư (`key`) và địa chỉ node cho 4 validator và 1 RPC node.
    2.  Tạo file `genesis.json` với cấu hình QBFT:
        *   `chainId`: Đặt giá trị custom cố định (ví dụ: `2026`) để chống Replay Attack (tuyệt đối không dùng `1337`).
        *   `blockperiodseconds`: `2` (khoảng thời gian đóng block 2 giây).
        *   `requesttimeoutseconds`: `10` (thời gian tối đa chờ đồng thuận vòng mới).
        *   `extraData`: Chứa danh sách địa chỉ ví được mã hóa hex đại diện cho 4 validator ban đầu tham gia đồng thuận.
    3.  Thiết lập thông số Zero-Gas:
        *   `zeroBaseFee`: `true` (cho phép giao dịch với `gasPrice = 0` trên mạng lưới).
        *   `gasLimit`: Đặt giới hạn đủ lớn (được cấu hình phù hợp để đáp ứng việc deploy contract mà không bị lỗi out-of-gas).
        *   `contractSizeLimit`: Giữ cấu hình mặc định của EVM (ví dụ: `24576` bytes) trừ khi việc deploy thực sự yêu cầu điều chỉnh lớn hơn.
    4.  Whitelist và cấp số dư ban đầu cho địa chỉ ví Owner/Deployer và các ví của Backend Signer (`LocalEnvSigner`).
*   **Done Criteria**: Có đầy đủ `genesis.json` và bộ khóa private/public cho 5 node sẵn sàng triển khai.

### WP 2.2: P2P Discovery & Static Peering
*   **Mục tiêu**: Thiết lập cấu hình mạng peer-to-peer (P2P) cố định, loại bỏ cơ chế tự động tìm kiếm node (Discovery) để tối giản cấu trúc mạng liên minh Consortium MVP.
*   **Chi tiết thực hiện**:
    1.  Vô hiệu hóa cơ chế tự động tìm kiếm peer thông qua tham số khởi động: `--discovery-enabled=false`.
    2.  Chỉ sử dụng cấu hình peer tĩnh thông qua `static-nodes.json` chứa danh sách chuỗi `enode://` kèm IP/Domain và port P2P (`30303`) của tất cả 4 Validator Node và 1 RPC Node.
    3.  Cấu hình các node tự động kết nối với nhau dựa vào danh sách tĩnh này khi khởi động.
    4.  Đảm bảo Validator Node hoàn toàn không mở cổng RPC ra bên ngoài.
*   **Done Criteria**: File `static-nodes.json` cấu hình chính xác enode của 5 thực thể.

### WP 2.3: Docker Compose Topology & Network Isolation
*   **Mục tiêu**: Đóng gói mạng lưới blockchain 100% bằng Docker Compose, thiết lập phân vùng mạng an toàn (Network Isolation) để cô lập Validator và whitelist cổng RPC bảo mật.
*   **Chi tiết thực hiện**:
    1.  Thiết kế file `docker-compose.yml` gồm 5 dịch vụ: `besu-validator1` đến `besu-validator4` và `besu-rpc`.
    2.  Cấu hình Docker Network tách biệt:
        *   `backend_network`: Kết nối Backend API/Worker và node `besu-rpc`.
        *   `blockchain_network`: Kết nối node `besu-rpc` và 4 node validator.
        *   *Bảo mật*: Các Validator Node chỉ hoạt động trong `blockchain_network`, không kết nối đến `backend_network` và không mở port RPC (`8545`).
    3.  Cấu hình Node RPC (`besu-rpc`):
        *   Chỉ mở port `8545:8545` kết nối đến Backend.
        *   Whitelist các module JSON-RPC cần thiết: `--rpc-http-api=ETH,NET,WEB3`. Không bật `ADMIN`, `DEBUG`, `TRACE`, `MINER`, `CLIQUE`, `PERM`. (Không bật `TXPOOL` trừ khi backend thực sự cần thiết sử dụng để giảm thiểu đặc quyền).
        *   Cấu hình `--host-allowlist` giới hạn các Host Header hợp lệ (ví dụ: `localhost,backend,besu-rpc`) thay vì dùng `*` để bảo vệ chống DNS Rebinding.
    4.  Đảm bảo tính sẵn sàng cao và bền vững dữ liệu:
        *   Khai báo volumes để mount thư mục lưu trữ data `/opt/besu/data` của từng validator node tránh mất dữ liệu khi restart.
        *   Cấu hình `restart: unless-stopped` cho toàn bộ container node.
        *   Thiết lập Docker Healthcheck *chỉ dành riêng cho RPC Node* (dùng lệnh `curl` gọi API `eth_blockNumber` local trên cổng `8545`). Validators không cấu hình healthcheck bằng RPC API vì chúng không mở cổng RPC.
*   **Done Criteria**: Chạy lệnh `docker compose up -d` khởi động toàn bộ mạng lưới thành công, RPC node trả về block tăng dần qua healthcheck.

### WP 2.4: Environment Transition & Deployment
*   **Mục tiêu**: Định tuyến kết nối Backend và Hardhat vào node RPC mới và triển khai Smart Contract lên mạng Consortium.
*   **Chi tiết thực hiện**:
    1.  Cấu hình lại `appsettings.json` và biến môi trường của Backend API/Worker sang `RPC_URL` trỏ tới `besu-rpc:8545`. Đảm bảo Backend không cần biết và không thể truy cập trực tiếp các validator node.
    2.  Cấu hình Hardhat `hardhat.config.ts` để thêm network mới (ví dụ: `besuConsortium`) kết nối thông qua URL của RPC node và chainId đã cấu hình (ví dụ: `2026`).
    3.  Chạy script deploy của Hardhat để deploy `DegreeAnchor.sol` lên mạng Consortium mới.
    4.  Ghi nhận địa chỉ smart contract vào `deployed-address.json`.
*   **Done Criteria**: Contract `DegreeAnchor` được deploy thành công trên mạng Consortium, địa chỉ được cập nhật tự động vào backend cấu hình.

### WP 2.5: Verification & Fault Tolerance Procedures
*   **Mục tiêu**: Kiểm thử toàn diện khả năng tự phục hồi, chống chịu lỗi của mạng đồng thuận QBFT và tính đúng đắn của ứng dụng.
*   **Kịch bản kiểm thử bắt buộc**:
    1.  **Test 1 (Consensus Resilience)**: Stop 1 validator node (`docker compose stop besu-validator1`). Backend thực hiện gửi transaction neo chặn mới. Xác nhận block vẫn được sinh ra bình thường và transaction được xác thực (Consensus QBFT $3f+1$ với $N=4, f=1$ vẫn hoạt động khi có tối đa 1 node chết).
    2.  **Test 2 (Validator Sync)**: Khởi động lại validator đã tắt. Kiểm tra log của node đó xem có tự động kết nối qua `static-nodes.json` và sync kịp block mới nhất từ các validator khác hay không.
    3.  **Test 3 (RPC Recovery)**: Restart node RPC. Đảm bảo backend tự động kết nối lại thành công sau khi RPC phục hồi và tiếp tục xử lý công việc.
    4.  **Test 4 (Deploy Contract)**: Biên dịch và deploy lại contract lên mạng Consortium thành công.
    5.  **Test 5 (Read Contract)**: Gọi query on-chain mapping `batches[batchId]` thông qua RPC node để đọc Merkle Root và verify tính nhất quán dữ liệu.
    6.  **Test 6 (Write Contract)**: Cấp phát bằng chứng từ backend worker, gửi transaction thông qua RPC, ghi nhận TxHash và Receipt.
    7.  **Test 7 (Receipt Verification)**: Kiểm tra thông tin block number, gas used từ Receipt thông qua `eth_getTransactionReceipt`.
    8.  **Test 8 (Fail-Fast Startup Check)**: Cấu hình sai `ChainId` (ví dụ `1234` thay vì `2026`) ở backend. Khởi chạy backend và kiểm tra xem `BlockchainStartupValidatorService` có crash ứng dụng lập tức để ngăn chặn chạy sai cấu hình hay không.

---

## 2. Cấu trúc thư mục bàn giao (Deliverables Structure)
Khi hoàn thành Phase 2, repository blockchain sẽ có cấu trúc như sau:
```
apps/blockchain/
├── README.md                  # Hướng dẫn chi tiết cách chạy mạng, deploy & test và mô tả cấu trúc dự án
├── docker-compose.yml         # File định nghĩa 5 node Besu và Network Isolation
├── genesis/
│   └── genesis.json           # File cấu hình khởi tạo mạng QBFT (ChainId 2026, Zero-Gas)
├── configs/
│   ├── static-nodes.json      # File peer tĩnh chứa enode urls của 5 node
│   ├── validator1/
│   │   └── key                # Node private key của validator 1
│   ├── validator2/
│   │   └── key
│   ├── validator3/
│   │   └── key
│   ├── validator4/
│   │   └── key
│   └── rpc/
│       └── key                # Node private key của RPC node
└── scripts/
    └── deploy.ts              # Script deploy contract của Hardhat
```
