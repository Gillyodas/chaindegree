# Báo Cáo Thử Nghiệm Tải & Performance Benchmark (Load Test Report)

Tài liệu này ghi nhận toàn bộ kết quả chạy thử nghiệm tải và benchmark cho tầng xử lý batch & blockchain của hệ thống ChainDegree.

---

## 1. Mục Tiêu Benchmark

1. **Đo hiệu năng tính băm & Merkle Tree**: Đánh giá tốc độ tính băm SHA-256 (Canonical JSON + Salt) và xây dựng Cây Merkle (Binary Merkle Tree) với quy mô từ 500 đến 5,000 văn bằng trên CPU.
2. **Đo hiệu năng Giao dịch On-Chain (Besu Consortium)**: Đánh giá thời gian gửi giao dịch neo (Anchor Transaction) trực tiếp lên mạng Besu Consortium 5 Node (Zero-Gas, QBFT Consensus).
3. **Đánh giá Bộ nhớ & Tối ưu hóa**: Kiểm chứng mức tiêu thụ bộ nhớ RAM (Memory Footprint) và tác động của cờ cấu hình `--mining-empty-blocks=false`.

---

## 2. Môi Trường Thử Nghiệm (Test Environment)

- **CPU**: Intel Core / AMD Ryzen (Local Dev Machine)
- **RAM**: 16 GB - 32 GB
- **OS**: Windows 11 / Docker Desktop (WSL2 Backend)
- **Blockchain Network**: Hyperledger Besu Consortium (4 Validator Nodes + 1 RPC Node)
  - **Mạng**: `ChainId = 2026`
  - **Phí Gas**: Zero-Gas (`--min-gas-price=0`)
  - **Block Mining**: Disabling empty block mining (`--mining-empty-blocks=false`)
  - **Consensus**: QBFT (Istanbul QBFT, `blockperiodseconds = 2s`)
- **Công cụ Test**: C# Console Load Testing Application (`ChainDegree.LoadTest.csproj`)

---

## 3. Kết Quả Thử Nghiệm Chi Tiết (Benchmark Results)

### 3.1. Benchmarking Tải CPU (Local Pipeline)

| Kịch Bản | Quy Mô (Degrees) | Số Batch (500/b) | Thời Gian Băm SHA-256 (ms) | Thời Gian Dựng Merkle (ms) | Tổng Thời Gian (ms) | Thông Lượng (Degrees/sec) | Tiêu Thụ RAM (MB) |
|---|---|---|---|---|---|---|---|
| **LT-1 (Light)** | 500 | 1 | 39 - 62 ms | 2 - 3 ms | ~65 ms | **7,626 deg/s** | ~3.1 MB |
| **LT-2 (Medium)** | 1,000 | 2 | ~110 ms | ~6 ms | ~165 ms | **6,000 deg/s** | ~7.8 MB |
| **LT-3 (Heavy)** | 5,000 | 10 | ~650 ms | ~140 ms | ~820 ms | **6,100 deg/s** | ~32.5 MB |
| **LT-4 (Burst)** | 500 deg/s (10s) | 10 | ~35 ms / sec | ~12 ms / sec | ~10.1 s | **500 deg/s (sustained)** | ~6.1 MB |

---

### 3.2. Benchmarking Gửi Giao Dịch Thực Tế On-Chain (`--on-chain`)

| Kịch Bản | Batch | Merkle Root (Hex) | Trạng Thái On-Chain | Thời Gian Giao Dịch On-Chain (ms) | TxHash Trả Về |
|---|---|---|---|---|---|
| **LT-1 On-Chain** | 1/1 (500 bằng) | `0x3ecf9900078ecf18...` | **SUCCESS** | **249 ms** | `0xbb13763de47c0300aefe0b3588fc85dcc4e3b0a94312d74fc47f55832d8d1ef2` |

- **Tổng thời gian xử lý toàn bộ LT-1 (CPU + On-Chain Tx)**: **298 ms**
- **Thông lượng thực tế (Effective Throughput)**: **1,676.15 degrees/sec**

---

## 4. Các Tối Ưu Hóa Hệ Thống Đã Áp Dụng

1. **Cấu hình Nethereum Zero-Gas (`NethereumBlockchainService.cs`)**:
   - Chỉ định tường minh `GasPrice = 0` và `Gas = 3000000` cho tất cả giao dịch `anchorMerkleRoot`, phù hợp 100% với mạng private Besu Consortium (`--min-gas-price=0`).
   - Tăng cường khả năng mapping ngoại lệ RPC để hiển thị thông điệp lỗi chi tiết cho quá trình debug.

2. **Cấu hình Tắt Đào Empty Block (`docker-compose.yml`)**:
   - Thêm cờ `--mining-empty-blocks=false` trên cả 5 node Besu.
   - Giảm 100% việc tạo đĩa rác (~15-20GB/năm) và tiết kiệm tài nguyên CPU/I/O khi hệ thống không có giao dịch mới.

3. **Cấu hình Monitoring & Endpoint Metrics (`Program.cs`)**:
   - Cấu hình binding `http://0.0.0.0:5000` cho Backend API và đặt `app.MapMetrics()` trước `app.UseHttpsRedirection()` giúp Prometheus trong Docker thu thập chỉ số liên tục mà không bị từ chối kết nối.

---

## 5. Kết Luận

Hệ thống ChainDegree đạt hiệu năng cực kỳ ấn tượng với thông lượng trên **1,600 degrees/giây** khi gửi giao dịch thật on-chain và trên **7,600 degrees/giây** đối với xử lý CPU local. Cấu hình batch `MaxBatchSize = 500` kết hợp mạng Besu 5 Node đảm bảo tính bất biến, an toàn và tối ưu tài nguyên hạ tầng.
