# Kế hoạch Refactor Batching & Bulk Issue (BatchingDegreeWorker)

## 1. Đánh giá Kiến trúc Tổng thể (Architecture Overview)

Kiến trúc chuyển đổi từ mô hình xử lý tuần tự sang mô hình **Producer-Consumer** sử dụng Bounded `Channel<T>` làm in-memory dispatch buffer.

* `SQL Server` đóng vai trò là **Durable Work Store** (Source of Truth).
* Bounded `Channel<T>` cung cấp **Backpressure** chặn Producer query quá nhiều dữ liệu nếu Consumers không xử lý kịp.
* Hệ thống sẽ có một **Transaction Sender Account** trung tâm xử lý qua `INonceManager`.

**Luồng kiến trúc cuối cùng:**
```text
                         SQL Server
                   (Durable Work Store)
                           │
                           ↓
                    Batching Worker
                           │
                    ┌──────┴──────┐
                    │   Producer  │
                    │             │
                    │ Build       │
                    │ Merkle      │
                    │ Create Batch│
                    └──────┬──────┘
                           │
                           ↓
                 Bounded Channel<Batch> (Backpressure)
                           │
             ┌─────────────┼─────────────┐
             ↓             ↓             ↓
         Consumer 1    Consumer 2    Consumer N (Concurrent Async Tasks via Task.WhenAll)
             │             │             │
             └─────────────┼─────────────┘
                           ↓
                  BlockchainService
                           │
                  ┌────────┴────────┐
                  │                 │
             NonceManager      Transaction Lifecycle
                  │                 │
                  └────────┬────────┘
                           ↓
                         Besu (QBFT)
                           │
                    ┌──────┴──────┐
                    ↓             ↓
                 TxPool        Mined Block
                    │             │
                    └──────┬──────┘
                           ↓
                        Receipt
                           │
                           ↓
                  DB State Reconcile
```

---

## 2. Các Thay Đổi Cốt Lõi (Core Improvements)

### 2.1. Phân tách INonceManager vs Transaction Lifecycle
`NonceManager` không phải là một transaction state machine. Nhiệm vụ duy nhất của nó là **đảm bảo không có hai concurrent callers được cấp cùng một nonce cho cùng một sender account, đồng thời có khả năng resync local state với Besu**.

```csharp
public interface INonceManager
{
    // Lấy pending nonce hiện tại từ Besu khi startup.
    Task InitializeAsync(CancellationToken ct);
    
    // Cấp phát nonce an toàn trong RAM, increment the counter.
    Task<long> ReserveNonceAsync(CancellationToken ct);
    
    // Đồng bộ lại local nonce state với Besu khi cần thiết (vd: RPC Timeout).
    Task ResyncAsync(CancellationToken ct);
}
```
*Note: Việc theo dõi Receipt và Timeout thuộc về BlockchainService và State Machine.*

### 2.2. State Machine: Bổ sung trạng thái Unknown
Vì thao tác Blockchain qua mạng lưới luôn chứa đựng rủi ro (Timeout), ta chuẩn hóa vòng đời Transaction:

* **Pending:** Batch đang nằm chờ.
* **Processing:** Được Worker nhặt lên xử lý (Đang tạo Merkle, cấp Nonce).
* **Unknown:** `SendRawTransaction` bị RPC timeout. Application không biết chắc Tx đã vào Pool hay chưa. 
* **Submitted:** Có `TxHash` trả về. Transaction đã broadcast thành công vào mempool.
* **Completed:** Receipt báo `status = 1` (Thành công).
* **Failed:** Receipt báo `status = 0` (Thất bại).

```text
Pending
   ↓
Processing
   │
   ├──────────────→ Unknown ──→ Reconciliation (Startup / Scan)
   │
   ↓
Submitted
   │
   ├── receipt success ──→ Completed
   │
   └── receipt failure ──→ Failed
```

### 2.3. Bounded Channel (Backpressure)
Thay vì dùng Channel thông thường, khởi tạo Channel với giới hạn:
```csharp
var channel = Channel.CreateBounded<Batch>(new BoundedChannelOptions(100) {
    FullMode = BoundedChannelFullMode.Wait, // Chờ đợi nếu đầy (Backpressure cho Producer)
    SingleWriter = true,
    SingleReader = false
});
```

### 2.4. Fencing Token & Lease Heartbeat
Để giải quyết tranh chấp lock an toàn tuyệt đối khi Lease hết hạn:
- **Fencing Token (`LeaseId`):** Khi Consumer pick batch, sinh `LeaseId = Guid.NewGuid()`. Mọi query `UPDATE` tới Database phải kèm điều kiện: `WHERE Id = @Id AND LeaseId = @LeaseId AND LeaseUntil > NOW()`.
- **Heartbeat:** Trong lúc chờ Receipt quá lâu, Consumer thực hiện Heartbeat gia hạn Lease (Vd: cứ `LeaseDuration / 3` lại gia hạn thêm).
- Nhờ có Fencing Token, Worker A sẽ lập tức bị văng lỗi/bỏ qua xử lý nếu Lease của nó hết hạn và bị Worker B chiếm dụng, tránh corrupt DB.

---

## 3. Tiêu Chí Hoàn Thành (Done Criteria)

### Concurrency
- [ ] Sử dụng Bounded `Channel<T>` để chặn Producer khi Consumer bị quá tải.
- [ ] N consumers (`Task.WhenAll`) process N batches concurrently, có hỗ trợ CancellationToken khi Graceful Shutdown.
- [ ] `ConsumerCount` cấu hình được.

### Nonce
- [ ] Tuyệt đối không sinh duplicate nonce.
- [ ] Initialize được lấy từ `pending` tx count.
- [ ] Khả năng gọi `ResyncAsync()` để sync lại state sau khi bị mất dấu RPC Timeout.

### Lease & Lock Management
- [ ] Lease có configurable duration.
- [ ] Áp dụng Fencing Token.
- [ ] Consumer tự Heartbeat gia hạn lock nếu cần.
- [ ] Báo lỗi văng luồng an toàn khi Worker cũ finalize batch mà ownership đã bị mất.

### Crash Recovery Invariants (Reconciliation)
- [ ] Các Batch `Processing` bị expired lease sẽ được nhặt lại.
- [ ] Các Batch `Submitted` (đang chờ Receipt) khi startup sẽ được check tiếp state thay vì gửi lại.
- [ ] Các Batch `Unknown` sẽ được truy vấn lại trạng thái (Có TxHash không? Có nằm trong Pool không?).

---

## 4. Bảng Kịch Bản Test (Test Matrix)

| Scenario | Expected Result |
| --- | --- |
| 10 batches × 5s delay, 4 consumers | Hoàn tất trong ~15s (Concurrency thực sự). |
| Out-of-order Submission (A: nonce 10 delay 5s; B: nonce 11 delay 0s) | Không tạo duplicate. Nonce manager không bị corrupt. Giao dịch 11/12 pending tới khi 10 xuất hiện. |
| RPC Timeout lúc Broadcast | Batch rơi vào trạng thái `Unknown`. `NonceManager.Resync()` được trigger. Không tự ý retry gửi duplicate. |
| App Restart (Recovery) | Nonce sync chuẩn xác, Batch Submitted tự động check receipt tiếp. |
| Worker Crash (Before Enqueue) | Lần chạy Recover tiếp theo ném lại vào Bounded Channel. |
| Lease Expiration & Fencing | Blockchain kẹt > Lease timeout. Worker B lấy Batch. Worker A (sống lại) finalize sẽ bị reject vì sai `LeaseId`. |
| Trì hoãn Receipt lâu hơn Lease | Heartbeat duy trì LeaseId an toàn. |

---

## 5. Kế hoạch Commit (Thứ tự ưu tiên đảm bảo an toàn)

Quá trình chuyển đổi sẽ đi từ việc củng cố cấu trúc, state trước khi nâng concurrency:
1. `refactor(domain): update batch transaction state machine (add Unknown, Submitted)`
2. `feat(worker): implement lease ownership and fencing with LeaseId`
3. `feat(worker): add INonceManager`
4. `feat(worker): add bounded channel and concurrent async consumers via Task.WhenAll`
5. `fix(worker): implement Submitted/Unknown transaction recovery reconciliation`
6. `test(worker): add concurrency and crash recovery scenarios`
