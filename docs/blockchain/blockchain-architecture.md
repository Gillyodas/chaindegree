# Kiến Trúc Blockchain — ChainDegree

> Tài liệu đặc tả chi tiết kiến trúc tầng blockchain của hệ thống xác thực văn bằng số ChainDegree.
> Bao gồm: lựa chọn mạng, đồng thuận, chiến lược đóng block, Merkle tree, smart contract, thư viện, mô hình triển khai và phân tích trade-off toàn diện.

---

## Mục Lục

1. [Tổng Quan Kiến Trúc](#1-tổng-quan-kiến-trúc)
2. [Lựa Chọn Mạng Blockchain](#2-lựa-chọn-mạng-blockchain)
3. [Cơ Chế Đồng Thuận — QBFT](#3-cơ-chế-đồng-thuận--qbft)
4. [Chiến Lược Đóng Block](#4-chiến-lược-đóng-block)
5. [Merkle Tree — Chiến Lược Neo Chặn Dữ Liệu](#5-merkle-tree--chiến-lược-neo-chặn-dữ-liệu)
6. [Smart Contract](#6-smart-contract)
7. [Mô Hình Lưu Trữ Hybrid](#7-mô-hình-lưu-trữ-hybrid)
8. [Thư Viện & Công Nghệ](#8-thư-viện--công-nghệ)
9. [Mô Hình Triển Khai Mạng](#9-mô-hình-triển-khai-mạng)
10. [Luồng Dữ Liệu End-to-End](#10-luồng-dữ-liệu-end-to-end)
11. [Bảo Mật & Key Management](#11-bảo-mật--key-management)
12. [Những Gì KHÔNG Sử Dụng](#12-những-gì-không-sử-dụng-anti-patterns-loại-bỏ)
13. [Thiết Kế Failure-First](#13-thiết-kế-failure-first)
14. [Tổng Hợp Trade-off & So Sánh](#14-tổng-hợp-trade-off--so-sánh)
15. [Monitoring & Observability](#15-monitoring--observability)
16. [Disaster Recovery](#16-disaster-recovery)
17. [Lộ Trình Phát Triển](#17-lộ-trình-phát-triển)

---

## 1. Tổng Quan Kiến Trúc

ChainDegree áp dụng kiến trúc **Hybrid Blockchain** — kết hợp cơ sở dữ liệu tập trung (SQL Server chạy qua Docker) cho dữ liệu chi tiết và mạng blockchain tư nhân liên minh (Private Consortium) để neo chặn bằng chứng mật mã bất biến.

```
┌─────────────────────────────────────────────────────────────────┐
│                        ChainDegree System                       │
│                                                                 │
│  ┌───────────────┐    ┌───────────────┐    ┌─────────────────┐  │
│  │  ASP.NET API  │───▶│ Background    │───▶│  Hyperledger    │  │
│  │  (Registrar)  │    │ Worker        │    │  Besu Network   │  │
│  └───────┬───────┘    │ (Batching)    │    │  (QBFT 4 nodes) │  │
│          │            └───────┬───────┘    └────────┬────────┘  │
│          ▼                    ▼                     │           │
│  ┌───────────────┐    ┌───────────────┐             │           │
│  │  SQL Server   │    │ Merkle Tree   │◀────────────┘           │
│  │  (PlainData   │    │ (Proof Store) │  Merkle Root anchored   │
│  │   + Salt)     │    └───────────────┘                         │
│  └───────────────┘                                              │
└─────────────────────────────────────────────────────────────────┘
```

### Nguyên Tắc Thiết Kế

| Nguyên tắc | Mô tả |
|---|---|
| **Tách biệt trách nhiệm** | Blockchain chỉ lưu bằng chứng (Merkle Root), không lưu dữ liệu thô |
| **Bất biến** | Dữ liệu đã neo trên chain không thể sửa đổi hay xóa |
| **Kiểm chứng độc lập** | Bất kỳ ai cũng có thể xác minh tính toàn vẹn mà không cần tin tưởng hệ thống trung tâm |
| **Hiệu suất** | Batch 500 degree → 1 transaction, giảm 500x chi phí lưu trữ trên chain |
| **Khả năng tách rời** | Blockchain service ẩn sau interface, có thể swap implementation mà không ảnh hưởng domain |

---

## 2. Lựa Chọn Mạng Blockchain

### Quyết Định: **Hyperledger Besu** — Private Consortium Network

ChainDegree sử dụng **Hyperledger Besu** làm nền tảng blockchain, vận hành ở chế độ **Private Permissioned Consortium** (mạng liên minh có cấp phép).

### Lý Do Lựa Chọn

| Tiêu chí | Hyperledger Besu | Lý do phù hợp với ChainDegree |
|---|---|---|
| **Tương thích EVM** | Hỗ trợ đầy đủ Ethereum Virtual Machine | Tận dụng hệ sinh thái Solidity, Nethereum, tooling Ethereum |
| **Enterprise-grade** | Dự án thuộc Linux Foundation, có support doanh nghiệp | Phù hợp mô hình giáo dục — cần ổn định, bảo trì dài hạn |
| **Permissioned mode** | Hỗ trợ mạng tư nhân có kiểm soát quyền truy cập | Chỉ validator được ủy quyền (Bộ GD&ĐT, đại học lớn) mới tham gia đồng thuận |
| **QBFT Consensus** | Thuật toán đồng thuận được Besu khuyến nghị cho permissioned deployment mới | Immediate finality, deterministic, không fork |
| **Zero Gas (Free Gas)** | Hỗ trợ Free Gas Network | Không tốn phí giao dịch thực tế — phù hợp mô hình phi lợi nhuận |
| **Privacy Groups** | Hỗ trợ privacy transactions (Tessera) | Hiện chưa cần — có thể cân nhắc nếu sau này xuất hiện nhu cầu lưu dữ liệu giao dịch riêng giữa các tổ chức trên blockchain |
| **Monitoring** | Tích hợp sẵn Prometheus + Grafana metrics | Giám sát health và performance của node |
| **Java-based** | Chạy trên JVM, triển khai cross-platform | Dễ containerize với Docker, phù hợp DevOps hiện đại |

### So Sánh Với Các Lựa Chọn Thay Thế

#### So sánh 1: Hyperledger Besu vs. Hyperledger Fabric

| Tiêu chí | Hyperledger Besu | Hyperledger Fabric |
|---|---|---|
| **Smart Contract Language** | Solidity (EVM) | Go / JavaScript (Chaincode) |
| **Học tập & Hệ sinh thái** | Rất lớn (Ethereum ecosystem) | Nhỏ hơn, learning curve dốc hơn |
| **Tooling .NET** | Nethereum — thư viện .NET mature | Không có thư viện .NET chính thức |
| **Kiến trúc** | Blockchain thuần (account-based) | Channel + Orderer + Endorser (phức tạp) |
| **Triển khai** | 4 nodes QBFT là đủ | Cần Orderer + Peer + CA — tối thiểu 6-8 container |
| **Finality** | Immediate (QBFT) | Eventual (Raft) hoặc BFT |
| **Privacy** | Privacy Groups (Tessera) | Channels + Private Data Collections |

> **Kết luận:** Fabric mạnh hơn về privacy channels nhưng phức tạp quá mức cho ChainDegree. Besu có Nethereum (.NET) và hệ sinh thái Solidity/EVM rộng lớn, phù hợp hơn với tech stack .NET.

#### So sánh 2: Hyperledger Besu vs. Ethereum Public (Sepolia/Mainnet)

| Tiêu chí | Hyperledger Besu (Private) | Ethereum Public |
|---|---|---|
| **Chi phí** | Miễn phí (Free Gas) | Gas fee thực tế (ETH) — rất tốn kém |
| **Tốc độ** | ~2-5 giây/block | ~12 giây/block + mempool congestion |
| **Kiểm soát** | Toàn quyền kiểm soát validators | Không kiểm soát |
| **Finality** | Immediate (QBFT) | ~13 phút (64 slots) |
| **Dung lượng** | Tối ưu, chỉ lưu cần thiết | Shared ledger toàn cầu |
| **Compliance** | CSDT kiểm soát dữ liệu nội bộ | Dữ liệu công khai, khó tuân thủ GDPR |

> **Kết luận:** Ethereum Public không phù hợp — chi phí cao, finality chậm, mất kiểm soát, và vi phạm yêu cầu về quyền kiểm soát dữ liệu.

#### So sánh 3: Hyperledger Besu vs. Quorum (ConsenSys Quorum)

| Tiêu chí | Hyperledger Besu | Quorum |
|---|---|---|
| **Bảo trì** | Linux Foundation, cộng đồng lớn | ConsenSys, đã merge vào Besu |
| **Consensus** | QBFT (mới nhất) | IBFT, Raft |
| **Tương lai** | Đang phát triển tích cực | Đã chuyển về Besu |

> **Kết luận:** Quorum đã merge vào Hyperledger Besu. Chọn Besu = chọn phiên bản hiện đại nhất.

#### So sánh 4: Blockchain vs. Không Dùng Blockchain (Hash-only DB)

| Tiêu chí | Blockchain (Besu) | Database-only (chỉ lưu hash) |
|---|---|---|
| **Bất biến** | Đảm bảo bởi đồng thuận phân tán | DBA có thể sửa bất kỳ lúc nào |
| **Kiểm chứng độc lập** | Bất kỳ validator nào cũng xác minh được | Phải tin tưởng admin hệ thống |
| **Chống chối bỏ** | Transaction đã ký bằng private key | Không có bằng chứng mật mã |
| **Minh bạch** | Mọi validator đều thấy lịch sử | Chỉ admin DB thấy |
| **Chi phí vận hành** | Thêm 4 nodes (~4 VPS) | Không thêm chi phí |
| **Độ phức tạp** | Cao hơn nhiều | Đơn giản |

> **Kết luận:** Nếu chỉ dùng DB, hệ thống mất đi **giá trị cốt lõi** — tính bất biến và kiểm chứng độc lập. Blockchain là yêu cầu bắt buộc cho mô hình xác thực văn bằng tin cậy.

---

## 3. Cơ Chế Đồng Thuận — QBFT

### Quyết Định: **QBFT (Quorum Byzantine Fault Tolerance)**

QBFT là thuật toán đồng thuận dành riêng cho mạng permissioned, thuộc họ BFT (Byzantine Fault Tolerance), được thiết kế đặc biệt cho Hyperledger Besu.

### Cách Hoạt Động

```
                    QBFT Consensus Round
                    
    ┌─────────────────────────────────────────────────┐
    │                                                 │
    │  1. PROPOSAL    Proposer gửi block đề xuất      │
    │       │                                         │
    │       ▼                                         │
    │  2. PREPARE     Validators xác nhận đề xuất     │
    │       │         (Cần ≥ ⌈2N/3⌉ phiếu)           │
    │       ▼                                         │
    │  3. COMMIT      Validators cam kết block        │
    │       │         (Cần ≥ ⌈2N/3⌉ phiếu)           │
    │       ▼                                         │
    │  4. ROUND CHANGE  Nếu proposer lỗi → bầu mới   │
    │                                                 │
    └─────────────────────────────────────────────────┘
    
    Finality: Block được chấp nhận NGAY khi đủ commit
    → Không fork, không rollback
```

### Thông Số Cấu Hình

| Tham số | Giá trị | Giải thích |
|---|---|---|
| **Block Period** | `5 seconds` | Thời gian tối đa giữa các block (khi có transaction) |
| **Request Timeout** | `10 seconds` | Timeout cho round change nếu proposer không phản hồi |
| **Số Validator tối thiểu** | `4 nodes` | Chịu được 1 node lỗi: `f = ⌊(N-1)/3⌋ = 1` |
| **Empty Blocks** | `false` | Không tạo block rỗng khi mạng idle |
| **Validator Selection** | `Round-Robin` | Luân phiên proposer theo thứ tự |

### Công Thức Chịu Lỗi BFT

$$f = \left\lfloor \frac{N - 1}{3} \right\rfloor$$

Với N = 4 validators: hệ thống chịu được **1 node Byzantine** (lỗi hoặc phá hoại).

| Số Validator (N) | Chịu lỗi (f) | Quorum cần thiết | Khuyến nghị |
|---|---|---|---|
| 4 | 1 | 3 | **Tối thiểu cho production** |
| 7 | 2 | 5 | Khuyến nghị cho scale |
| 10 | 3 | 7 | Enterprise lớn |

### Cấu Hình Genesis QBFT

```json
{
  "config": {
    "chainId": 1337,
    "berlinBlock": 0,
    "qbft": {
      "blockperiodseconds": 5,
      "epochlength": 30000,
      "requesttimeoutseconds": 10,
      "miningbeneficiary": "0x0000000000000000000000000000000000000000"
    }
  },
  "nonce": "0x0",
  "timestamp": "0x0",
  "gasLimit": "0x1fffffffffffff",
  "difficulty": "0x1",
  "mixHash": "0x63746963616c2062797a616e74696e65206661756c7420746f6c6572616e6365",
  "alloc": {
    "0xDeployerAddress": {
      "balance": "0x200000000000000000000000000000000"
    }
  },
  "extradata": "0x...(RLP encoded validator list)..."
}
```

### So Sánh QBFT vs. Các Thuật Toán Đồng Thuận Khác

| Tiêu chí | QBFT | IBFT 2.0 | Clique (PoA) | Raft |
|---|---|---|---|---|
| **BFT** | ✅ Có | ✅ Có | ❌ Chỉ CFT | ❌ Chỉ CFT |
| **Finality** | Deterministic | Deterministic | ~N/2 blocks | Deterministic |
| **Validator Rotation** | Round-Robin | Round-Robin | In-turn/No-turn | Leader Election |
| **Fork Resistance** | Không fork | Không fork | Có thể fork | Không fork |
| **Network Type** | Permissioned | Permissioned | Permissioned | Permissioned |
| **Besu Support** | ✅ Chính thức | ⚠️ Deprecated | ❌ Không | ❌ Không |
| **Maturity** | Production-ready | Stable nhưng cũ | Geth only | Fabric |

> **Lý do chọn QBFT thay vì IBFT 2.0:** QBFT là phiên bản kế nhiệm của IBFT 2.0 trên Besu, sửa các edge case trong round change, tối ưu performance, và được Besu team khuyến nghị chính thức cho mọi deployment mới. IBFT 2.0 đã bị đánh dấu deprecated.

> **Lý do không chọn Clique:** Clique không có Byzantine Fault Tolerance — nếu validator gian lận hoặc bị hack, chuỗi có thể bị fork. Điều này không chấp nhận được cho hệ thống xác thực văn bằng.

> **Lý do không chọn Raft:** Raft chỉ chịu được Crash Fault (node sập) nhưng không chịu được Byzantine Fault (node phá hoại). Trong mạng liên minh giáo dục, cần giả định rằng một node có thể bị xâm nhập.

---

## 4. Chiến Lược Đóng Block

### Quyết Định: **On-demand Block Production** (`mining-empty-blocks = false`)

Besu được cấu hình **không tạo block rỗng** khi mạng không có giao dịch.

### Cơ Chế Hoạt Động

```
Trạng thái mạng         Hành vi đóng block
──────────────────────  ──────────────────────────────────
Không có transaction  → Mạng im lặng, KHÔNG tạo block
Có transaction pending → Validator đề xuất block trong vòng 5 giây
Burst 100 transactions → Nhiều block liên tiếp, mỗi block chứa transactions
```

### Lý Do

1. **Tiết kiệm dung lượng đĩa:** Mạng idle 23/24h trong ngày (ngoài mùa tốt nghiệp). Nếu đóng block rỗng mỗi 5 giây = **17,280 block rỗng/ngày** = phí phạm hoàn toàn.
2. **Giảm tải I/O:** Mỗi block (dù rỗng) vẫn cần RLP encode, hash, consensus round, write to LevelDB.
3. **Đơn giản hóa giám sát:** Block count = Transaction count. Dễ theo dõi và audit.

### Trade-off

| Ưu điểm | Nhược điểm |
|---|---|
| Tiết kiệm 99%+ dung lượng khi idle | Latency tăng nhẹ khi transaction đầu tiên đến sau thời gian idle dài (cold start ~1-2s) |
| Giảm I/O disk trên validators | Không có heartbeat block → khó phân biệt "mạng healthy nhưng idle" vs "mạng chết" |
| Audit log sạch sẽ hơn | Một số monitoring tool giả định block rate ổn định |

### Giải Pháp Cho Nhược Điểm

- **Health check:** Sử dụng Besu JSON-RPC endpoint `eth_syncing` + `net_peerCount` thay vì dựa vào block rate.
- **Synthetic heartbeat:** Nếu cần, có thể schedule 1 transaction nhỏ mỗi giờ từ system account để tạo block tham chiếu.

---

## 5. Merkle Tree — Chiến Lược Neo Chặn Dữ Liệu

### Quyết Định: **Dual-Trigger Batch + Binary Merkle Tree + SHA-256**

Thay vì gửi từng degree hash riêng lẻ lên blockchain, hệ thống **gom batch** và xây dựng **Merkle Tree** rồi chỉ neo **Merkle Root** duy nhất lên smart contract.

### Kiến Trúc Merkle Tree

```
                    Merkle Root (neo lên blockchain)
                    ┌────────────┐
                    │  Root Hash │
                    └─────┬──────┘
                    ┌─────┴──────┐
              ┌─────┴─────┐ ┌───┴──────┐
              │  Hash(AB) │ │ Hash(CD) │
              └─────┬─────┘ └────┬─────┘
              ┌─────┴──┐     ┌───┴──┐
          ┌───┴──┐ ┌───┴──┐ ┌┴────┐ ┌┴────┐
          │ H(A) │ │ H(B) │ │H(C) │ │H(D) │
          └──────┘ └──────┘ └─────┘ └─────┘
            ▲        ▲        ▲        ▲
          Degree₁  Degree₂  Degree₃  Degree₄
          
    Leaf = DataHashLocal = SHA-256(PlainDataCanonical ∥ Salt)
```

### Thuật Toán Xây Dựng

**Input:** Danh sách `DataHashLocal` của các degree trong batch (hex string, 64 characters).

```
1. leaves = [hash₁, hash₂, ..., hashₙ]
2. Nếu n lẻ: nhân đôi leaf cuối → [hash₁, ..., hashₙ, hashₙ]
3. Lặp xây từ dưới lên:
   parent[i] = SHA-256(child[2i] ∥ child[2i+1])
   (nối raw bytes, không phải hex string)
4. Lặp cho đến khi chỉ còn 1 node → Merkle Root
5. Trích xuất Merkle Proof cho từng leaf:
   - Danh sách sibling hashes
   - Danh sách directions (left/right)
```

**Cài đặt hiện tại:** [MerkleTreeService.cs](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Infrastructure/Cryptography/Services/MerkleTreeService.cs)

### Quy Trình Xác Minh (Verify Proof)

```
Input:  leafHash, proofHashes[], proofDirections[], expectedRoot

current = leafHash
for i = 0 to proof.length - 1:
    if directions[i] == RIGHT:
        current = SHA-256(current ∥ proofHashes[i])
    else:
        current = SHA-256(proofHashes[i] ∥ current)
        
return current == expectedRoot  // true = hợp lệ
```

**Độ phức tạp xác minh:** O(log₂ N) — với batch 500 degree, chỉ cần **~9 bước hash** để xác minh 1 degree.

### Dual-Trigger Batching

Worker chạy nền theo chiến lược **hai ngưỡng kích hoạt**:

| Trigger | Điều kiện | Mục đích |
|---|---|---|
| **Size Trigger** | Batch đạt **500 degree** | Tối ưu throughput |
| **Time Trigger** | Degree cũ nhất trong queue đợi **≥ 3 phút** | Đảm bảo latency tối đa chấp nhận được |

> **Về giá trị 500:** 500 không phải hằng số tối ưu tuyệt đối mà là **giá trị khởi đầu (operational default)**. Giá trị này cần được benchmark bằng load test trên production/staging và có thể thay đổi qua configuration (`appsettings.json`). Các yếu tố ảnh hưởng: Merkle Tree build time, memory footprint, gas cost per transaction, và acceptable confirmation latency.

```
                     ┌───────────────────────────────┐
                     │   Background Worker Loop      │
                     │                               │
    Degree Queue ───▶│  Poll mỗi 15 giây             │
                     │  ┌───────────────────────┐    │
                     │  │ Count ≥ 500?          │    │
                     │  │   ✅ → Build Merkle   │    │
                     │  │   ❌ → Check time     │    │
                     │  │                       │    │
                     │  │ OldestAge ≥ 3 min?    │    │
                     │  │   ✅ → Build Merkle   │    │
                     │  │   ❌ → Sleep & retry  │    │
                     │  └───────────────────────┘    │
                     │                               │
                     │  Build Merkle Tree            │
                     │  → 1 Transaction → Blockchain │
                     │  → Update status → Confirmed  │
                     └───────────────────────────────┘
```

**Cài đặt hiện tại:** [BatchingDegreeWorker.cs](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Infrastructure/BackgroundWorkers/BatchingDegreeWorker.cs)

### Cấu Hình Worker

| Tham số | Giá trị mặc định | Có thể thay đổi |
|---|---|---|
| `MaxBatchSize` | 500 | ✅ Qua `appsettings.json` |
| `MaxWaitTimeSeconds` | 180 (3 phút) | ✅ Qua `appsettings.json` |
| `PollingIntervalSeconds` | 15 | ✅ Qua `appsettings.json` |

### So Sánh Với Các Chiến Lược Anchoring Khác

| Chiến lược | Mô tả | Ưu điểm | Nhược điểm |
|---|---|---|---|
| **1 Tx / 1 Degree** | Mỗi degree = 1 blockchain transaction | Đơn giản, realtime | Tốn 500x storage, overload validator |
| **Merkle Batch (đang dùng)** | N degree → 1 Merkle Root → 1 Tx | 500x tiết kiệm, scalable | Phải lưu proof, verify phức tạp hơn |
| **Rolling Hash Chain** | Hash chain nối tiếp | Không cần tree | Không verify được 1 degree riêng lẻ |
| **Accumulator (RSA)** | Cryptographic accumulator | Proof size cố định O(1) | Tính toán rất nặng, thư viện ít |

> **Kết luận:** Đối với batch verification, Merkle Tree là chuẩn phổ biến nhất — cân bằng tốt giữa khả năng verify riêng lẻ, proof size, và tốc độ tính toán.

### Merkle Proof Storage

> **Quan trọng:** Merkle Proof **chỉ lưu trong Database**, KHÔNG lưu trên blockchain. Lưu proof on-chain sẽ tốn gas cực lớn và hoàn toàn không cần thiết — blockchain chỉ cần lưu Merkle Root.

Proof cho mỗi degree được lưu trong bảng `BatchDegreeRecords` (SQL Server):

| Column | Kiểu | Mô tả |
|---|---|---|
| `BatchId` | `Guid` | FK → `BatchRecords` |
| `DegreeId` | `Guid` | FK → `Degrees` |
| `LeafIndex` | `int` | Vị trí leaf trong Merkle Tree |
| `ProofHashesJson` | `string` (JSON array) | Danh sách sibling hashes |

Khi verify, hệ thống:
1. Lấy `ProofHashesJson` + `LeafIndex` từ **DB** (không phải blockchain)
2. Lấy `MerkleRoot` từ `BatchRecords` (hoặc query blockchain bằng `BatchId`)
3. Chạy `VerifyProof()` — O(log₂ N) operations

---

## 6. Smart Contract

### Triết Lý: Contract Càng "Ngu" Càng Tốt

Smart contract trong ChainDegree được thiết kế theo nguyên tắc **"Thin Contract"** — contract chỉ làm đúng 3 việc:

```
Store Batch → Store Root → Emit Event
```

**Contract KHÔNG chứa:**
- ❌ Business rule
- ❌ Hash computation
- ❌ DDD logic
- ❌ Merkle Proof (proof lưu ở DB)
- ❌ Degree state machine

> **Lưu ý về Access Control:** Mặc dù mạng là permissioned, contract vẫn cần kiểm tra `msg.sender` để đảm bảo chỉ các account được ủy quyền (anchor service) mới có quyền ghi. Permissioned network ≠ mọi account đều được ghi contract. Điều này bảo vệ contract, không phải bảo vệ blockchain.

Toàn bộ business logic nằm ở Backend. Điều này giúp:
- Dễ thay đổi khi quy trình nghiệp vụ thay đổi
- Dễ kiểm thử (unit test C# thay vì Solidity test)
- Ít rủi ro khi nâng cấp smart contract
- Chi phí triển khai và gas cực thấp

### Thiết Kế Contract: **DegreeAnchor.sol**

```solidity
// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

contract DegreeAnchor {
    
    address public owner;
    mapping(address => bool) public authorizedAnchors;
    
    struct BatchMetadata {
        bytes32 MerkleRoot;
        uint256 Timestamp;
        bytes32 InstitutionId;
        string ActionType; // "Issue", "Update", "Revoke"
        bool Exists;
    }

    // Mapping: batchId → BatchMetadata
    mapping(bytes32 => BatchMetadata) public batches;
    
    modifier onlyOwner() {
        require(msg.sender == owner, "Not owner");
        _;
    }
    
    modifier onlyAnchorService() {
        require(authorizedAnchors[msg.sender], "Not authorized anchor");
        _;
    }
    
    event BatchAnchored(
        bytes32 indexed batchId,
        bytes32 merkleRoot,
        uint256 timestamp
    );
    
    constructor() {
        owner = msg.sender;
        authorizedAnchors[msg.sender] = true;
    }
    
    function addAnchorService(address _service) external onlyOwner {
        authorizedAnchors[_service] = true;
    }
    
    function removeAnchorService(address _service) external onlyOwner {
        authorizedAnchors[_service] = false;
    }
    
    function anchorMerkleRoot(
        bytes32 batchId, 
        bytes32 merkleRoot,
        bytes32 institutionId,
        string calldata actionType
    ) external onlyAnchorService {
        require(!batches[batchId].Exists, "Batch already anchored");
        
        batches[batchId] = BatchMetadata({
            MerkleRoot: merkleRoot,
            Timestamp: block.timestamp,
            InstitutionId: institutionId,
            ActionType: actionType,
            Exists: true
        });
        
        emit BatchAnchored(batchId, merkleRoot, block.timestamp);
    }
}
```

> **Reputation:** Nếu sau này Reputation cần on-chain, deploy **contract riêng** (`ReputationAnchor.sol`). Contract càng nhỏ càng tốt — không gộp nhiều concern vào 1 contract.

### Vai Trò Storage vs. Event

| Cơ chế | Mục đích | Ví dụ |
|---|---|---|
| **Storage** (`mapping`) | Query on-chain trực tiếp — đọc Merkle Root bằng `BatchId` | `batches[batchId].MerkleRoot` |
| **Event** (`emit`) | Indexing, analytics, sync off-chain — Nethereum `GetFilterChanges()` | `BatchAnchored(...)` event logs |

> Storage và Event phục vụ **hai mục đích khác nhau**. Storage để query trạng thái hiện tại on-chain. Event để tracking lịch sử và sync dữ liệu về backend/dashboard.

### Luồng Verification Decoupled

Để chứng minh tính hợp lệ của một degree, hệ thống đi theo lộ trình:

```
Degree → MerkleProof (DB) → Batch → Blockchain (Merkle Root)
```

Verification hoàn toàn không phụ thuộc vào logic trong smart contract — chỉ cần đọc Merkle Root từ chain và so khớp proof ở backend.

### So Sánh Với Contract Phức Tạp Hơn

| Approach | Mô tả | Ưu điểm | Nhược điểm |
|---|---|---|---|
| **Thin Contract (đang dùng)** | Store batch + emit event | Đơn giản, dễ audit, gas thấp, dễ upgrade | Business logic không on-chain |
| **Full Logic** | Degree state machine trên contract | Trustless hoàn toàn | Gas cao, upgrade khó, Solidity bugs |
| **Proxy Pattern** | Upgradeable contract | Có thể sửa lỗi | Phức tạp, security risk |

> **Kết luận:** Trong mạng permissioned, trust đã được đảm bảo ở tầng validator (QBFT). Blockchain chỉ cần làm notary (công chứng). Toàn bộ business logic thuộc về Backend/DDD.

---

## 7. Mô Hình Lưu Trữ Hybrid

### Phân Chia Trách Nhiệm

```
┌─────────────────────────┐     ┌──────────────────────────┐
│     SQL Server (DB)     │     │   Hyperledger Besu       │
│                         │     │   (Blockchain)           │
│  ✅ PlainData (chi tiết)│     │                          │
│  ✅ Salt (mã muối)      │     │  ✅ Merkle Root (proof)  │
│  ✅ DataHashLocal       │     │  ✅ TxHash               │
│  ✅ Status lifecycle    │     │  ✅ Block number          │
│  ✅ Merkle Proofs       │     │  ✅ Timestamp bất biến   │
│  ✅ Batch metadata      │     │  ✅ Reputation history   │
│  ✅ Jobs, Applications  │     │                          │
│  ✅ Reports             │     │  ❌ PlainData            │
│  ✅ Reputation Score    │     │  ❌ Salt                  │
│  ✅ BehaviorLogs        │     │  ❌ Business logic       │
│                         │     │                          │
│  Mutable (có thể sửa)  │     │  Immutable (bất biến)    │
│  Fast query / join      │     │  Append-only             │
└─────────────────────────┘     └──────────────────────────┘
```

### Lý Do Không Lưu Dữ Liệu Thô Trên Blockchain

1. **Chi phí storage:** Blockchain storage cực kỳ đắt (mỗi 32 bytes = 1 storage slot on EVM). 1 degree có ~500 bytes plaintext → 16 slots → rất lãng phí.
2. **Privacy:** Thông tin sinh viên (CCCD, tên, ngày sinh) là dữ liệu cá nhân. Lưu trên blockchain = mọi validator đều đọc được = vi phạm privacy.
3. **Right to be forgotten:** GDPR/PDPA yêu cầu quyền xóa dữ liệu. Blockchain bất biến = không xóa được.
4. **Hiệu quả:** Chỉ lưu 32 bytes Merkle Root đại diện cho toàn bộ batch — đủ để chứng minh tính toàn vẹn.

### Luồng Đối Chiếu Kép (Hybrid Verification)

```
                    Verification Request
                           │
              ┌────────────┴────────────┐
              ▼                         ▼
     ┌──────────────┐          ┌──────────────┐
     │  Local DB    │          │  Blockchain  │
     │              │          │              │
     │ 1. Lấy Plain │          │ 3. Lấy Root │
     │    + Salt    │          │    từ TxHash │
     │ 2. Tính lại  │          │              │
     │    SHA-256   │          │              │
     └──────┬───────┘          └──────┬───────┘
            │                         │
            └─────────┬───────────────┘
                      ▼
              ┌──────────────┐
              │ Verify Merkle│
              │ Proof        │
              │              │
              │ leaf → proof │
              │ → root match?│
              └──────┬───────┘
                     │
           ┌─────────┴──────────┐
           ▼                    ▼
      ✅ Verified         ❌ Tampered
      (Root khớp)         (Data bị sửa)
```

---

## 8. Thư Viện & Công Nghệ

### Stack Chính

| Lớp | Thư viện / Công nghệ | Phiên bản | Vai trò |
|---|---|---|---|
| **Blockchain Client** | [Nethereum](https://nethereum.com/) | 4.x | .NET SDK tương tác với EVM nodes (RPC, ABI, signing) |
| **Blockchain Node** | [Hyperledger Besu](https://besu.hyperledger.org/) | 24.x | Java-based Ethereum client hỗ trợ QBFT |
| **Smart Contract** | Solidity | 0.8.20+ | Ngôn ngữ viết smart contract |
| **Hashing** | `System.Security.Cryptography` | .NET built-in | SHA-256 cho DataHash và Merkle nodes |
| **Merkle Tree** | Custom implementation | In-house | Xây tree + generate/verify proofs |
| **Serialization** | `System.Text.Json` | .NET built-in | Canonical JSON cho PlainData |
| **Background Worker** | `Microsoft.Extensions.Hosting` | .NET built-in | `BackgroundService` cho batch worker |
| **Configuration** | `Microsoft.Extensions.Options` | .NET built-in | `IOptions<BesuOptions>` pattern |

### Nethereum — Chi Tiết Sử Dụng

**Nethereum** là thư viện .NET chính thức để tương tác với mọi blockchain tương thích EVM. Đây là lựa chọn duy nhất mature cho .NET ecosystem.

#### Các Tính Năng Sử Dụng

| Tính năng | API Nethereum | Mục đích trong ChainDegree |
|---|---|---|
| **RPC Connection** | `Web3(rpcUrl)` | Kết nối đến Besu node |
| **Account Management** | `Account(privateKey, chainId)` | Ký transaction bằng private key |
| **Contract Interaction** | `ContractHandler` | Gọi `anchorMerkleRoot()`, `getMerkleRoot()` |
| **Transaction Sending** | `SendTransactionAndWaitForReceiptAsync` | Gửi Tx và đợi receipt (finality) |
| **Event Log Query** | `GetEvent<T>().GetFilterChanges()` | Đọc `MerkleRootAnchored` events |
| **ABI Encoding** | Auto-generated từ ABI | Encode/decode contract parameters |

#### Cấu Hình Trong Hệ Thống

```json
{
  "Blockchain": {
    "Besu": {
      "RpcUrl": "http://besu-node1:8545",
      "AccountPrivateKey": "0x...(from .env)",
      "ContractAddress": "0x...(deployed address)",
      "ChainId": 1337
    }
  }
}
```

**Cài đặt hiện tại:** [BesuOptions.cs](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Infrastructure/Configurations/BesuOptions.cs)

### So Sánh Nethereum Với Các Thư Viện Khác

| Tiêu chí | Nethereum (.NET) | Web3.js (Node.js) | Ethers.js (Node.js) | Web3j (Java) |
|---|---|---|---|---|
| **Ngôn ngữ** | C# | JavaScript | JavaScript | Java |
| **Phù hợp với stack** | ✅ .NET | ❌ Cần Node runtime | ❌ Cần Node runtime | ❌ Cần JVM |
| **Maturity** | 8+ năm | 7+ năm | 5+ năm | 6+ năm |
| **ABI Codegen** | ✅ `dotnet-nethereum` | Manual | Manual | ✅ Plugin |
| **Async/Await** | ✅ Native | Callback/Promise | Promise | CompletableFuture |
| **NuGet** | ✅ | ❌ npm | ❌ npm | ❌ Maven |

> **Kết luận:** Nethereum là lựa chọn duy nhất hợp lý cho .NET backend. Nếu dùng Web3.js/Ethers.js sẽ phải chạy thêm Node.js sidecar — thêm complexity không cần thiết.

### Lý Do Dùng SHA-256 Thay Vì Keccak-256

SHA-256 được chọn **không phải vì blockchain yêu cầu**, mà vì backend canonicalization dùng SHA-256. Blockchain chỉ lưu Merkle Root (32 bytes) — không quan tâm hash function nào tạo ra nó.

| Tiêu chí | SHA-256 (đang dùng) | Keccak-256 (Ethereum native) |
|---|---|---|
| **Chuẩn hóa** | NIST standard, phổ biến | Ethereum-specific |
| **Performance .NET** | `SHA256.HashData()` — hardware-accelerated | Cần thư viện bên ngoài |
| **Tương thích** | Mọi hệ thống | Chỉ Ethereum ecosystem |
| **Audit** | QC dễ hiểu, chuẩn quốc tế | Cần giải thích cho auditor |

> **Abstraction:** Nếu sau này muốn chuyển sang Keccak-256, chỉ cần thay `IHashService` implementation. Backend hash function được ẩn sau interface — không ảnh hưởng đến phần còn lại của hệ thống.

---

## 9. Mô Hình Triển Khai Mạng

> **Nguyên Tắc Triển Khai:** Toàn bộ hệ thống (từ SQL Server, Backend API, Worker, đến các node Besu) đều được cấu hình và cài đặt **100% thông qua Docker / Docker Compose**. Việc này nhằm mục đích cô lập công nghệ, hạn chế ảnh hưởng đến môi trường máy chủ host, đảm bảo tính nhất quán giữa các môi trường (Dev/Test/Prod) và dễ dàng quản lý.

### Production Deployment Topology

```
                   VPN / Private Network
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
    Institution A      Institution B      Institution C
        │                  │                  │
  ┌──────────────┐   ┌──────────────┐   ┌──────────────┐
  │ Besu Node    │   │ Besu Node    │   │ Besu Node    │
  │ Validator    │   │ Validator    │   │ Validator    │
  │ Docker       │   │ Docker       │   │ Docker       │
  └──────────────┘   └──────────────┘   └──────────────┘
        │                  │                  │
        └──────────────────┼──────────────────┘
                           │
                 QBFT Permissioned Network
                           │
                  ┌────────┴────────┐
                  │   RPC Node      │  ← Non-validator, chỉ phục vụ RPC
                  │   (Docker)      │
                  └────────┬────────┘
                           │
                  ┌────────┴────────┐
                  │   Worker        │  ← Gọi anchorMerkleRoot() qua RPC Node
                  │   (Docker)      │
                  └────────┬────────┘
                           │
                  ┌────────┴────────┐
                  │   API           │  ← Nhận request từ Registrar
                  │   (Docker)      │
                  └─────────────────┘
```

> **Tách biệt Validator và RPC:** API/Worker **không gọi trực tiếp vào validator**. Thay vào đó, hệ thống gọi qua một **RPC Node** riêng (non-validator). RPC Node forward transaction vào mạng. Validator chỉ tập trung vào consensus.

### Yêu Cầu Cho Mỗi Validator Node

| Yêu cầu | Chi tiết |
|---|---|
| **Isolation** | Chạy trên máy/VM riêng — không chạy nhiều validator trên cùng một host |
| **Volume** | Volume riêng cho blockchain data và cấu hình |
| **Network** | Chỉ mở cổng P2P (30303) cho các validator khác |
| **RPC** | Validator KHÔNG mở RPC ra ngoài. RPC chỉ trên RPC Node riêng |
| **Monitoring** | Prometheus / Grafana, health check, log rotation |

### Vai Trò Các Bên

| Bên tham gia | Vai trò | Trách nhiệm |
|---|---|---|
| **Bộ GD&ĐT** | Validator + Bootnode | Vận hành node gốc, quản lý danh sách validator |
| **Đại học lớn (2-3 trường)** | Validator | Đồng thuận, xác nhận block |
| **CSDT thường** | Không chạy node | Tương tác qua API của ChainDegree backend |
| **ChainDegree Backend** | Transaction Sender | Gửi transaction thông qua **RPC Node** (không gọi validator trực tiếp) |

### Docker Compose Deployment

```yaml
# Simplified — full version in docker-compose.yml
services:
  # RPC Node — non-validator, phục vụ API/Worker
  besu-rpc:
    image: hyperledger/besu:latest
    command: >
      --genesis-file=/config/genesis.json
      --node-private-key-file=/config/key
      --rpc-http-enabled
      --rpc-http-api=ETH,NET,WEB3
      --rpc-http-host=0.0.0.0
      --min-gas-price=0
      --host-allowlist="*"
    ports:
      - "8545:8545"  # JSON-RPC cho Worker/API
    volumes:
      - besu-data-rpc:/opt/besu/data
      - ./besu-config/rpc-node:/config

  # Validator nodes — chỉ mở P2P, KHÔNG mở RPC
  besu-validator1:
    image: hyperledger/besu:latest
    command: >
      --genesis-file=/config/genesis.json
      --node-private-key-file=/config/key
      --min-gas-price=0
    ports:
      - "30303:30303" # P2P only
    volumes:
      - besu-data-1:/opt/besu/data
      - ./besu-config/validator1:/config

  besu-validator2:
    image: hyperledger/besu:latest
    # ... tương tự validator1 với key khác
    
  besu-validator3:
    image: hyperledger/besu:latest
    # ...
    
  besu-validator4:
    image: hyperledger/besu:latest
    # ...
```

### Yêu Cầu Phần Cứng (Mỗi Validator Node)

| Resource | Tối thiểu | Khuyến nghị |
|---|---|---|
| **CPU** | 2 cores | 4 cores |
| **RAM** | 4 GB | 8 GB |
| **Disk** | 50 GB SSD | 100 GB SSD |
| **Network** | 10 Mbps | 100 Mbps |
| **OS** | Linux (Ubuntu 22.04+) | Docker-ready |

> **Ghi chú:** Với `mining-empty-blocks = false` và traffic thấp (vài trăm Tx/ngày ngoài mùa tốt nghiệp), disk growth rất chậm (~5-10 GB/năm).

---

## 10. Luồng Dữ Liệu End-to-End

### Luồng 1: Cấp Bằng (Issuance)

```
Registrar                    API              Worker            Besu
   │                          │                 │                 │
   │── POST /degrees ────────▶│                 │                 │
   │                          │                 │                 │
   │                          │── Validate      │                 │
   │                          │── Generate Salt │                 │
   │                          │── SHA-256 hash  │                 │
   │                          │── Save to DB    │                 │
   │                          │   (Pending)     │                 │
   │                          │                 │                 │
   │◀── 202 Accepted ────────│                 │                 │
   │    (batchId, status)     │                 │                 │
   │                          │                 │                 │
   │                          │    Poll queue   │                 │
   │                          │    ────────────▶│                 │
   │                          │                 │                 │
   │                          │                 │── Build Merkle  │
   │                          │                 │   Tree (N leafs)│
   │                          │                 │                 │
   │                          │                 │── anchorRoot() ─▶
   │                          │                 │                 │
   │                          │                 │◀── TxReceipt ───│
   │                          │                 │   (TxHash,      │
   │                          │                 │    BlockNumber)  │
   │                          │                 │                 │
   │                          │                 │── Update DB     │
   │                          │                 │   Confirmed     │
   │                          │                 │   Store proofs  │
```

### Luồng 2: Xác Minh (Verification)

```
Verifier                     API              DB              Besu
   │                          │                │                │
   │── POST /verify ─────────▶│                │                │
   │   (degreeCode)           │                │                │
   │                          │── Fetch degree─▶                │
   │                          │◀── PlainData,  │                │
   │                          │    Salt, Hash  │                │
   │                          │                │                │
   │                          │── Recalculate  │                │
   │                          │   SHA-256      │                │
   │                          │── Compare hash │                │
   │                          │                │                │
   │                          │── Fetch proof ─▶                │
   │                          │◀── MerkleProof │                │
   │                          │                │                │
   │                          │── getMerkleRoot()──────────────▶│
   │                          │◀── Root from chain ────────────│
   │                          │                │                │
   │                          │── Verify proof │                │
   │                          │   against root │                │
   │                          │                │                │
   │◀── 200 OK ──────────────│                │                │
   │    verified: true        │                │                │
```

---

## 11. Bảo Mật & Key Management

### Tư Duy: Signing Capability, Không Phải Private Key

Điểm quan trọng nhất trong bảo mật blockchain: **chuyển tư duy từ "quản lý private key" sang "signing capability"**.

```
Worker
  │
  ▼
IBlockchainSigner (abstraction)
  │
  ▼
KMS.Sign()     ← Worker KHÔNG BAO GIỜ thấy key
```

Worker chỉ yêu cầu **khả năng ký** — nó không biết và không cần biết key nằm ở đâu. Đây là kiến trúc mà AWS, Azure và Google đều hướng tới.

### Key Management Roadmap

```
Demo / Local Dev       →    .env file
         │
Development / Staging  →    HashiCorp Vault / Azure Key Vault
         │
Production             →    KMS / Vault
         │
Enterprise             →    Remote Signer (Besu hỗ trợ)
         │
Compliance (FIPS, etc) →    HSM (chỉ khi bắt buộc)
```

> **Lưu ý:** KMS đã là một bước nhảy rất lớn về bảo mật. Remote Signer không chỉ dùng cho HSM — Besu native hỗ trợ remote signing qua EthSigner/Web3Signer, cho phép tách hoàn toàn signing logic ra khỏi application. HSM chỉ cần khi có yêu cầu tuân thủ đặc biệt.

### Key Lifecycle

Ngoài vị trí lưu trữ key, cần quản lý **vòng đời** (lifecycle) của signing key:

```
Generate → Activate → Rotate → Disable → Destroy
```

| Giai đoạn | Mô tả | Trách nhiệm |
|---|---|---|
| **Generate** | Tạo key pair mới trong KMS/Vault | Ops / Admin |
| **Activate** | Đưa key vào trạng thái active, có thể ký transaction | Ops / Admin |
| **Rotate** | Tạo key mới, chuyển traffic sang key mới, giữ key cũ để verify lịch sử | Scheduled / Policy |
| **Disable** | Key cũ không còn được dùng để ký, chỉ verify | Post-rotation |
| **Destroy** | Xóa vĩnh viễn key material (sau retention period) | Policy / Compliance |

> **Key Rotation Policy:** Nên rotate signing key định kỳ (ví dụ: mỗi 12 tháng) hoặc ngay khi phát hiện compromised. Contract `authorizedAnchors` mapping cho phép thêm address mới và xóa address cũ mà không cần deploy lại contract.

### Mô Hình Bảo Mật Đa Tầng

| Tầng | Cơ chế | Bảo vệ chống lại |
|---|---|---|
| **Network** | Permissioned nodes, allowlisted IPs | Unauthorized node join |
| **Consensus** | QBFT — chịu `f = ⌊(N-1)/3⌋` node Byzantine, QBFT tự xử lý | Byzantine validator |
| **Signing** | KMS / `IBlockchainSigner` abstraction | Key exposure |
| **Data** | SHA-256 + Salt | Brute-force, rainbow table |
| **Privacy** | PlainData chỉ ở DB, không on-chain | Data leak từ blockchain |
| **Contract** | Thin contract, no business logic | Smart contract exploit |
| **Application** | Interface abstraction | Coupling, dependency leak |

### Threat Model

| Threat | Xác suất | Tác động | Giải pháp |
|---|---|---|---|
| DBA sửa DB | Trung bình | Cao | Blockchain detect mismatch |
| Validator bị hack | Thấp | Rất cao | QBFT chịu 1 node lỗi — consensus tự xử lý |
| Signing key lộ | Trung bình | Cao | KMS + key rotation + audit log |
| Smart contract bug | Thấp | Thấp (thin contract) | Minimal surface, dễ audit |
| Replay attack | Thấp | Trung bình | ChainId + nonce |
| Worker crash giữa chừng | Trung bình | Trung bình | Idempotency + Lease + Retry |
| Blockchain timeout | Trung bình | Trung bình | Exponential backoff + DLQ |

---

## 12. Những Gì KHÔNG Sử Dụng (Anti-Patterns Loại Bỏ)

Để tránh over-engineering, các công nghệ sau đã được **chủ động loại bỏ** khỏi scope hiện tại:

| Công nghệ | Lý do không dùng |
|---|---|
| **Merkle Proof on-chain** | Gas cực lớn. Proof lưu ở DB là đủ — blockchain chỉ cần Merkle Root |
| **HSM (hiện tại)** | KMS đã đủ cho enterprise. HSM chỉ khi có yêu cầu tuân thủ đặc biệt |
| **Remote Signer (hiện tại)** | Chưa cần. `IBlockchainSigner` abstraction cho phép thêm sau |
| **Service Mesh** | Không cần. Docker Compose + internal network đủ |
| **Kafka** | Outbox + Worker pattern đã đủ. Kafka thêm complexity không cần thiết |
| **CQRS riêng cho Blockchain** | Không cần. Hệ thống đã có CQRS ở Application layer |
| **Event Sourcing** | Không cần. `DegreeVersion` đã giải quyết version history |
| **Byzantine Detection** | Không cần. QBFT tự xử lý Byzantine fault |
| **Sharding** | Không cần. Traffic ChainDegree không đủ lớn |
| **Zero Knowledge Proof** | Không cần. SHA-256 + Salt đã đảm bảo privacy |
| **MPC Wallet** | Không cần |
| **Threshold Signature** | Không cần |
| **DID** | Không cần |
| **IPFS** | Không cần. Hash đã đủ để chứng minh integrity |

---

## 13. Thiết Kế Failure-First

ChainDegree thiết kế theo tư duy **"Failure First"** — mọi luồng đều được thiết kế cho trường hợp lỗi trước, happy path sau.

### Các Kịch Bản Lỗi Đã Xử Lý

| Kịch bản | Cơ chế xử lý |
|---|---|
| **Worker crash giữa batch** | `DegreeProcessingRecord` với `WorkerId` + `LeaseUntil` — record hết hạn lease sẽ được worker khác pick up |
| **Blockchain timeout** | Exponential backoff: retry sau 2, 4, 8 phút. Tối đa 3 lần |
| **Duplicate transaction** | Batch `require(!batches[batchId].Exists)` trên contract — idempotent |
| **Version history** | `DegreeVersion` entity lưu toàn bộ lịch sử thay đổi trước khi overwrite |
| **Idempotency** | Worker kiểm tra `DegreeProcessingRecord.State` trước khi xử lý |
| **Lease expiry** | `LeaseUntil` timestamp — nếu worker chết, record tự giải phóng sau 10 phút |
| **Retry exhaustion** | Sau 3 lần retry → `State = Failed`, degree → `Confirmation_Error`, DLQ |
| **Outbox consistency** | Domain event → Outbox record trong cùng DB transaction |

### Failure Flow

```
Transaction thất bại
        │
        ▼
RetryCount++ (exponential backoff)
        │
        ├── RetryCount ≤ 3 → State = Failed, NextRetryAt = now + 2^n phút
        │                     Worker pick up lại ở cycle tiếp theo
        │
        └── RetryCount > 3 → State = Failed (permanent)
                              Degree → Confirmation_Error
                              Log error, chờ manual intervention
```

### Critical Case: Blockchain Thành Công, DB Commit Thất Bại

Đây là case khó nhất và cần xử lý đặc biệt:

```
Worker gửi anchor tx → Blockchain
         │
         ▼
Blockchain: ✅ Thành công (TxHash returned)
         │
         ▼
Worker: UPDATE DB (Confirmed, store proofs)
         │
         ▼
DB: ❌ Deadlock / Connection lost / Timeout
         │
         ▼
Trạng thái:
  - Blockchain đã anchor Merkle Root
  - DB vẫn ghi Pending_Confirmation
  - Worker crash hoặc restart
```

**Giải pháp: Idempotency theo TxHash**

1. Khi worker restart, nó phát hiện batch vẫn ở trạng thái `Processing`
2. Worker query blockchain bằng `batchId` → nếu `batches[batchId].Exists == true` → transaction đã được anchor
3. Worker chỉ cần **retry phần DB commit** (update status + store proofs) mà không gửi lại transaction
4. Contract `require(!batches[batchId].Exists)` chặn duplicate anchor — nếu vô tình gửi lại sẽ revert

> **Quan trọng:** Worker phải luôn kiểm tra trạng thái on-chain trước khi quyết định gửi transaction mới hay chỉ cần retry DB commit.

---

## 14. Tổng Hợp Trade-off & So Sánh

### Ma Trận Quyết Định Tổng Quan

| Quyết định | Lựa chọn | Ưu điểm chính | Nhược điểm chính | Thay thế đã loại |
|---|---|---|---|---|
| **Blockchain** | Hyperledger Besu | EVM compatible, Nethereum, QBFT | Java runtime, JVM memory | Fabric, Public Ethereum |
| **Consensus** | QBFT | Deterministic finality, BFT | Cần ≥4 nodes | IBFT 2.0, Clique, Raft |
| **Empty Blocks** | Disabled | Tiết kiệm storage 99%+ | Cold start delay nhẹ | Always-on mining |
| **Anchoring** | Merkle Tree Batch | 500x compression | Proof management ở DB | 1:1, Rolling hash |
| **Hash Function** | SHA-256 | .NET native, NIST standard | Không native EVM | Keccak-256 |
| **Contract Design** | Thin (store + emit) | Dễ audit, gas thấp, dễ upgrade | Business logic ở backend | Full state machine |
| **Storage** | Hybrid (DB + Chain) | Performance + Privacy | 2 source of truth | On-chain only |
| **Key Management** | KMS (`IBlockchainSigner`) | Worker không thấy key | Phụ thuộc cloud provider | HSM, .env plaintext |
| **Queue** | Outbox + Worker | Đơn giản, đủ dùng | Không distributed | Kafka, RabbitMQ |
| **Version History** | `DegreeVersion` entity | Đơn giản, SQL query | Không event sourcing | Event Sourcing |

### Rủi Ro Và Kế Hoạch Giảm Thiểu

| Rủi ro | Mức độ | Kế hoạch |
|---|---|---|
| Besu node không ổn định | Trung bình | Monitoring + auto-restart container |
| Batch transaction fail | Trung bình | Retry 3 lần + DLQ + exponential backoff |
| Merkle proof corruption | Thấp | Proof regeneration từ batch data |
| Worker crash | Trung bình | Lease timeout + auto-recovery |
| Signing key compromise | Thấp | KMS audit log + key rotation |
| Network partition | Thấp | QBFT round-change mechanism tự xử lý |

---

## 15. Monitoring & Observability

### Besu Node Metrics

| Metric | Mục đích | Alert khi |
|---|---|---|
| **Block height** | Đồng bộ giữa các node | Node bị lag > 10 blocks |
| **Peers** | Số kết nối P2P | Peers < N-1 (mất kết nối validator) |
| **Consensus round** | QBFT round hiện tại | Round > 1 liên tục (proposer có vấn đề) |
| **Tx pending** | Transaction chờ xử lý | Pending > 100 (congestion) |
| **Memory (JVM)** | Heap usage của Besu | > 80% allocated |
| **Disk usage** | Dung lượng blockchain data | > 80% capacity |

### Worker Metrics

| Metric | Mục đích | Alert khi |
|---|---|---|
| **Queue length** | Số degree đang chờ batch | > 2000 (backlog) |
| **Retry count** | Batch đang retry | Retry > 2 |
| **Batch latency** | Thời gian từ submit đến confirmed | > 10 phút |
| **Failed batch** | Batch thất bại vĩnh viễn | Bất kỳ (cần manual intervention) |
| **Lease orphan** | Record hết lease chưa hoàn thành | Bất kỳ (worker chết) |

---

## 16. Disaster Recovery

### Dữ Liệu Cần Backup

| Dữ liệu | Nơi lưu | Tầm quan trọng | Hậu quả nếu mất |
|---|---|---|---|
| **Genesis file** | File system | **CRITICAL** | Không khôi phục được chain |
| **Validator keys** | KMS / File | **CRITICAL** | Validator không thể tham gia consensus |
| **KMS config** | Cloud/File | **CRITICAL** | Không ký được transaction |
| **Contract ABI** | Source code | Cao | Không gọi được contract |
| **Contract address** | Config/DB | Cao | Phải deploy lại |
| **Database (SQL Server)** | Docker volume | **CRITICAL** | Mất toàn bộ degree data, proof |
| **Merkle Proofs** | Database | **CRITICAL** | Không verify được degree |
| **Besu data volume** | Docker volume | Trung bình | Có thể sync lại từ peers |

> **Quan trọng nhất:** Nếu mất **genesis file** → không khôi phục được chain. Genesis phải được backup ở ít nhất 2 location riêng biệt.

### Chiến Lược Backup

| Loại | Tần suất | Retention |
|---|---|---|
| **Database full backup** | Hàng ngày | 30 ngày |
| **Database diff backup** | Mỗi 6 giờ | 7 ngày |
| **Genesis + config** | Mỗi khi thay đổi | Vĩnh viễn |
| **KMS key backup** | Theo policy KMS provider | Theo compliance |

---

## 17. Lộ Trình Phát Triển

### Phase Hiện Tại — Core Blockchain

| Component | Trạng thái |
|---|---|
| Besu (QBFT, 4 nodes) | ✅ Thiết kế xong |
| Worker (Dual-Trigger Batching) | ✅ Đã implement |
| Outbox Pattern | ✅ Đã implement |
| Merkle Tree (build + verify) | ✅ Đã implement |
| Batch Processing | ✅ Đã implement |
| Thin Contract (DegreeAnchor.sol) | ✅ Thiết kế xong |
| Hash Service (SHA-256) | ✅ Đã implement |
| `DegreeVersion` (version history) | ✅ Đã implement |
| `IBlockchainSigner` abstraction | 🔲 Cần implement |
| KMS integration | 🔲 Cần implement |
| Real Nethereum integration | 🔲 Cần implement |
| Smart Contract deployment | 🔲 Cần implement |

### Phase Tiếp — Operations & Monitoring

- Monitoring: Prometheus + Grafana
- Retry Dashboard
- Blockchain Explorer
- Key Rotation
- KMS production setup
- Backup strategy
- Alert system

### Phase Enterprise — Scale & Compliance

- Remote Signer
- HSM (nếu yêu cầu tuân thủ)
- Cross-region deployment
- Multi-chain support
- Disaster Recovery
- Audit Portal
- Remote Validator management

---

> **Tài liệu tham chiếu:**
> - [ADR 001 — QBFT and Merkle Batching](file:///e:/codes/chaindegree/docs/adr/001.md)
> - [ADR 002 — Blockchain Transaction Signing Architecture](file:///e:/codes/chaindegree/docs/adr/002.md)
> - [Implementation Plan](file:///e:/codes/chaindegree/docs/implementation-plan.md)
> - [Business Logic Specification](file:///e:/codes/chaindegree/docs/business-logic-specification.md)
> - [IBlockchainService](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Application/Abstractions/Blockchain/IBlockchainService.cs)
> - [MerkleTreeService](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Infrastructure/Cryptography/Services/MerkleTreeService.cs)
> - [BatchingDegreeWorker](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Infrastructure/BackgroundWorkers/BatchingDegreeWorker.cs)
