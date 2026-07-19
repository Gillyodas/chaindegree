# Môi trường Triển khai Blockchain Consortium (Phase 2)

Môi trường chạy mạng lưới Consortium Hyperledger Besu phục vụ dự án ChainDegree, cấu hình gồm 4 validator nodes và 1 RPC gateway node sử dụng cơ chế đồng thuận QBFT.

---

## 1. Cấu trúc thư mục blockchain
```
apps/blockchain/
├── README.md                  # Hướng dẫn chi tiết cách chạy mạng, deploy & test và mô tả cấu trúc dự án
├── docker-compose.yml         # File định nghĩa 5 node Besu và Network Isolation
├── genesis/
│   └── genesis.json           # File cấu hình khởi tạo mạng QBFT (ChainId 2026, Zero-Gas)
├── configs/
│   ├── static-nodes.json      # File peer tĩnh chứa enode urls của 5 node
│   ├── validator1..4/
│   │   └── key                # Node private key của validator tương ứng
│   └── rpc/
│       └── key                # Node private key của RPC node
└── contracts/
    └── scripts/
        └── generate-consortium-config.ts # Script sinh keys và config genesis
```

---

## 2. Hướng dẫn thiết lập và khởi chạy

### Bước 1: Sinh cấu hình mạng và Validator Keys (Nếu cần thiết lập lại)
Nếu muốn khởi tạo lại toàn bộ cặp khóa và cấu hình genesis mới, chạy lệnh dưới đây từ thư mục Hardhat project (`apps/blockchain/contracts`):
```bash
npx hardhat run scripts/generate-consortium-config.ts
```
*Lưu ý:* Việc chạy lại script này sẽ tạo mới toàn bộ private keys của validator/RPC, đồng thời tính toán lại và cập nhật RLP-encoded `extraData` trong `genesis/genesis.json` và `static-nodes.json`.

### Bước 2: Khởi chạy mạng blockchain Consortium bằng Docker Compose
Từ thư mục blockchain gốc (`apps/blockchain`), thực hiện khởi động mạng:
```bash
docker compose up -d
```
Mạng lưới sẽ chạy dưới nền với 5 containers:
- `besu-validator1` đến `besu-validator4` (Chỉ kết nối P2P nội bộ qua cổng `30303`, không mở cổng RPC).
- `besu-rpc` (Mở cổng RPC `8545:8545` để kết nối với Backend, whitelist APIs `ETH, NET, WEB3`).

### Bước 3: Kiểm tra tính sẵn sàng của mạng RPC
Gọi JSON-RPC đến node RPC để kiểm tra Chain ID và Block Number:
```bash
# Kiểm tra Chain ID (Kỳ vọng trả về 0x7ea = 2026 thập phân)
# Dành cho Linux / Git Bash / macOS:
curl -X POST -H "Content-Type: application/json" --data '{"jsonrpc":"2.0","method":"eth_chainId","params":[],"id":1}' http://localhost:8545

# Dành cho Windows Command Prompt (CMD):
curl -X POST -H "Content-Type: application/json" -d "{\"jsonrpc\":\"2.0\",\"method\":\"eth_chainId\",\"params\":[],\"id\":1}" http://localhost:8545

# Dành cho Windows PowerShell:
Invoke-RestMethod -Uri http://localhost:8545 -Method Post -ContentType "application/json" -Body '{"jsonrpc":"2.0","method":"eth_chainId","params":[],"id":1}'

# Kiểm tra Block Number hiện tại (Phải tăng dần nếu có block mới đóng)
# Dành cho Windows PowerShell:
Invoke-RestMethod -Uri http://localhost:8545 -Method Post -ContentType "application/json" -Body '{"jsonrpc":"2.0","method":"eth_blockNumber","params":[],"id":1}'
```

### Bước 4: Deploy Smart Contract lên mạng Consortium
Di chuyển vào thư mục Hardhat project (`apps/blockchain/contracts`) và chạy lệnh deploy:
```bash
npx hardhat run scripts/deploy.ts --network besuConsortium
```
Sau khi deploy thành công, cập nhật địa chỉ contract mới từ file `deployed-address.json` vào cấu hình Backend `.env` (`Blockchain__ContractAddress`).

---

## 3. Các kịch bản kiểm thử (Verification & Fault Tolerance)

Để xác thực hệ thống vận hành đúng chuẩn sản xuất và chịu lỗi tốt, thực hiện các kịch bản kiểm thử sau:

1.  **Kịch bản 1: Khả năng chịu lỗi đồng thuận (Consensus Tolerance)**
    - Tắt 1 node validator bất kỳ: `docker compose stop besu-validator1`.
    - Gửi request cấp bằng/neo chặn mới từ Backend.
    - **Kỳ vọng:** Mạng vẫn hoạt động và block vẫn được đóng bình thường (do QBFT $3f+1$ với $N=4, f=1$ cho phép tối đa 1 node validator gặp sự cố).
2.  **Kịch bản 2: Tự động đồng bộ lại node (Validator Sync)**
    - Bật lại node validator đã tắt: `docker compose start besu-validator1`.
    - Kiểm tra log node: `docker compose logs -f besu-validator1`.
    - **Kỳ vọng:** Node tự động kết nối lại các validator khác qua `static-nodes.json` và nhanh chóng đồng bộ các block bị thiếu về node cục bộ.
3.  **Kịch bản 3: Cơ chế Fail-Fast lúc khởi động backend**
    - Sửa đổi tham số cấu hình `Blockchain__ChainId` trong `.env` backend thành một mã chain sai (ví dụ: `1234` thay vì `2026`).
    - Khởi chạy Backend API/Worker.
    - **Kỳ vọng:** Ứng dụng ném lỗi và dừng ngay lập tức tại `BlockchainStartupValidatorService` thay vì chạy ngầm với cấu hình lỗi.
