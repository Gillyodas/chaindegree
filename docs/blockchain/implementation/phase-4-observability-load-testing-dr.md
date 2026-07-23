# Kế Hoạch Triển Khai Phase 4: Observability, Load Testing & DR

Tài liệu này đặc tả chi tiết kế hoạch triển khai cho **Phase 4: Observability, Load Testing & DR** của hệ thống ChainDegree. Kế hoạch này tập trung vào ba trụ cột vận hành: **Observability** (khả năng quan sát hệ thống), **Load & Chaos Testing** (kiểm chứng hiệu năng và khả năng phục hồi), và **Disaster Recovery** (phục hồi sau sự cố).

---

## 1. Tổng Quan Kế Hoạch & Ràng Buộc (Overview & Constraints)

### Mục Tiêu Chung
*   **Observability**: Giám sát tự động Besu nodes và Backend Worker thông qua Prometheus & Grafana dashboard; chuẩn hóa log correlation.
*   **Load Testing**: Chạy thử nghiệm tải thực tế (500, 1000, 5000 degrees) với C# Console App để chứng minh các thông số cấu hình Worker (`MaxBatchSize=500`, `PollingIntervalSeconds=15s`) đạt hiệu năng tối ưu.
*   **Chaos Testing**: Thực hiện 7 kịch bản failure injection để kiểm chứng khả năng tự phục hồi (resilience) và tính idempotent on-chain.
*   **Disaster Recovery (DR)**: Tài liệu hóa SOP và viết script backup định kỳ (SQL Server DB, `genesis.json`, configs).

### Ràng Buộc (Constraints & Technical Decisions)
*   **Phù hợp các chuẩn repo**: Tuân thủ [AI_CONTEXT.md](file:///e:/codes/chaindegree/docs/AI_CONTEXT.md), [Coding-Standards.md](file:///e:/codes/chaindegree/docs/Coding-Standards.md) và các tài liệu kiến trúc trong `docs/blockchain/`.
*   **Tách biệt Monitoring Topology**: File `docker-compose.monitoring.yml` được tách riêng hoàn toàn khỏi `docker-compose.yml` chính của blockchain để đảm bảo tính độc lập (monitoring là optional add-on).
*   **Monitoring scope**: Chỉ tạo Prometheus Metrics & Grafana Dashboards, **KHÔNG setup Alerting** (Email/Slack/Webhook) phức tạp để tránh over-engineering.
*   **Log Correlation Scope**: Tầng API đã có `CorrelationIdMiddleware` cấp `X-Correlation-ID` theo request. Tầng Worker chạy ngầm (Background Service) sẽ tự tạo `BatchCorrelationId` đại diện cho mỗi vòng xử lý batch độc lập để trace toàn bộ thao tác trong vòng đời của batch.
*   **Load Test Tool**: Sử dụng **C# Console App** kết nối trực tiếp tầng Domain/Infrastructure để tái sử dụng toàn bộ logic có sẵn, đo lường chính xác end-to-end performance từ tạo record đến on-chain confirmation.

---

## 2. Danh Sách Các Work Package (Work Packages)

### WP 4.1: Besu Prometheus Metrics & Grafana Dashboards
*   **Mục tiêu**: Bật endpoint Prometheus native trên 5 nodes Besu và dựng Grafana Dashboard giám sát sức khỏe mạng blockchain.
*   **Chi tiết thực hiện**:
    1.  Cập nhật tham số khởi chạy của các container Besu trong `docker-compose.yml` (`--metrics-enabled=true`, `--metrics-host=0.0.0.0`, `--metrics-port=9545`). Cổng 9545 chỉ mở nội bộ trong `blockchain_network`.
    2.  Tạo `docker-compose.monitoring.yml` chứa dịch vụ `prometheus` (image: `prom/prometheus`) và `grafana` (image: `grafana/grafana`).
    3.  Tạo cấu hình `monitoring/prometheus/prometheus.yml` scrape metrics từ 5 nodes Besu theo chu kỳ 15s.
    4.  Cấu hình Grafana auto-provisioning (`datasources.yml` và `dashboards.yml`).
    5.  Thiết kế Grafana Dashboard **Besu Network Health** với 6 panels: Block Height (đối chiếu sync giữa nodes), Connected Peers, QBFT Consensus Round, Pending Transactions, JVM Memory Usage, Block Production Rate.
*   **Kế hoạch kiểm thử Manual**:
    1.  Khởi chạy stack bằng lệnh: `docker compose -f docker-compose.yml -f docker-compose.monitoring.yml up -d`.
    2.  Kiểm tra Prometheus UI tại `http://localhost:9090/targets` đảm bảo 5/5 Besu targets ở trạng thái `UP`.
    3.  Kiểm tra Grafana tại `http://localhost:3000` xem dashboard cập nhật dữ liệu live.
*   **Done Criteria**: Grafana Dashboard hiển thị đầy đủ 6 panels với dữ liệu thời gian thực từ 5 Besu nodes.

### WP 4.2: Worker Structured Logging & Log Correlation
*   **Mục tiêu**: Chuẩn hóa cấu trúc log output của `BatchingDegreeWorker` bổ sung thông tin truy vết chuyên sâu.
*   **Chi tiết thực hiện**:
    1.  Tạo `BatchCorrelationId` (Guid) mới cho mỗi chu kỳ xử lý batch của Worker. Đính kèm vào context thông qua `_logger.BeginScope()`.
    2.  Bổ sung `Stopwatch` đo chính xác `ElapsedMs` cho các công đoạn: Build Merkle Tree, Send Blockchain Transaction, Wait Receipt.
    3.  Chuẩn hóa thông điệp log của Worker để luôn chứa các thông tin: `BatchId`, `BlockchainTxHash`, `BatchCorrelationId`, `ElapsedMs`.
    4.  Đảm bảo tuân thủ tuyệt đối quy tắc bảo mật: Không log Private Keys, Mật khẩu, hoặc Thông tin nhạy cảm.
    5.  Soạn thảo tài liệu chuẩn hóa log tại `docs/blockchain/observability/log-structure.md`.
*   **Kế hoạch kiểm thử Unit Test**:
    *   Viết Unit Test với `FakeLogger`/`TestLogger` kiểm tra khi Worker thực thi batch, log output tạo ra có đúng định dạng chứa `BatchId`, `BlockchainTxHash`, và `ElapsedMs`.
    *   Test đảm bảo `BatchCorrelationId` thay đổi duy nhất sau mỗi chu kỳ processing.
*   **Done Criteria**: Toàn bộ log của Worker xuất ra dưới dạng Structured Log có chứa đủ 4 trường correlation fields.

### WP 4.3: Worker Metrics Instrumentation (Prometheus Endpoint)
*   **Mục tiêu**: Tích hợp thư viện `prometheus-net` vào .NET Backend để xuất metrics của Worker cho Prometheus scrape.
*   **Chi tiết thực hiện**:
    1.  Cài đặt package `prometheus-net.AspNetCore` vào dự án API.
    2.  Tạo class `WorkerMetrics` khai báo các chỉ số:
        *   `QueueLength` (Gauge): Số bằng đang chờ đóng batch.
        *   `BatchesProcessed` (Counter): Tổng số batch xử lý thành công.
        *   `BatchesFailed` (Counter): Tổng số batch thất bại.
        *   `BatchLatency` (Histogram): Thời gian hoàn tất batch từ lúc gom đến khi confirmed on-chain.
        *   `MerkleBuildTime` (Histogram): Thời gian dựng cây Merkle.
        *   `BlockchainTxTime` (Histogram): Thời gian tương tác blockchain.
    3.  Cấu hình `app.MapMetrics()` trong `Program.cs` mở endpoint `/metrics`.
    4.  Cập nhật `prometheus.yml` bổ sung job scrape backend API.
    5.  Thiết kế Grafana Dashboard **Worker Health** hiển thị realtime các chỉ số trên.
*   **Kế hoạch kiểm thử Unit Test**:
    *   Viết Unit Test kiểm tra class `WorkerMetrics` ghi nhận đúng tăng/giảm của `QueueLength` và `BatchesProcessed` khi gọi mock service.
    *   Integration test kiểm tra endpoint HTTP `/metrics` trả về response status 200 kèm nội dung Prometheus format.
*   **Done Criteria**: Endpoint `/metrics` hoạt động và Grafana hiển thị realtime thông số hoạt động của Worker.

### WP 4.4: Load Testing & Benchmark
*   **Mục tiêu**: Kiểm chứng giới hạn chịu tải và tối ưu các tham số Worker (`MaxBatchSize=500`, `PollingIntervalSeconds=15s`).
*   **Chi tiết thực hiện**:
    1.  Xây dựng dự án **C# Console App Load Test** tại `apps/blockchain/tests/load-test/`.
    2.  Thực hiện 4 kịch bản benchmark:
        *   **LT-1 (Light)**: 500 degrees (1 batch đầy).
        *   **LT-2 (Medium)**: 1,000 degrees (2 batches nối tiếp).
        *   **LT-3 (Heavy)**: 5,000 degrees (10 batches liên tục).
        *   **LT-4 (Burst)**: Ghi nhận 500 degrees/giây trong 10 giây vào queue.
    3.  Thu thập các thông số: TPS on-chain, Batch Latency, Merkle Build Time, Block Confirmation Time, Throughput (degrees/phút), Memory consumption.
    4.  Tổng hợp và viết báo cáo benchmark tại `docs/blockchain/reports/load-test-report.md`.
*   **Done Criteria**: Báo cáo Benchmark hoàn thành chứng minh thông số `MaxBatchSize=500` và `PollingIntervalSeconds=15s` hoạt động ổn định trên 5,000 degrees mà không gây gánh nặng bộ nhớ hay sập RPC node.

### WP 4.5: Chaos Testing
*   **Mục tiêu**: Kiểm thử tính bền vững và khả năng tự phục hồi của hệ thống khi gặp sự cố bất ngờ.
*   **Chi tiết thực hiện**:
    1.  Tài liệu hóa kịch bản kiểm thử tại `docs/blockchain/reports/chaos-test-procedure.md`.
    2.  Thực hiện 7 kịch bản Failure Injection thủ công:
        *   **CT-1 (Idempotency Core)**: Kill Worker ngay sau khi gửi transaction on-chain nhưng chưa kịp commit DB -> Restart Worker -> Kiểm tra Worker đọc được state on-chain và chỉ update DB (không gửi transaction trùng).
        *   **CT-2 (Worker Recovery)**: Kill Worker khi đang build Merkle Tree -> Restart -> gom lại batch an toàn.
        *   **CT-3 (Fault Tolerance)**: Stop 1 Validator node (`besu-validator1`) -> Gửi batch mới -> Mạng QBFT vẫn đóng block bình thường.
        *   **CT-4 (RPC Resilience)**: Restart node `besu-rpc` khi Worker đang gửi giao dịch -> Worker retry thành công sau khi RPC online.
        *   **CT-5 (Network Latency)**: Thêm delay mạng 10s cho RPC node -> Worker trigger timeout và retry theo Exponential Backoff.
        *   **CT-6 (DB Failure)**: Stop SQL Server 30s -> Worker log error -> DB khôi phục -> Worker tự retry ghi nhận DB thành công.
        *   **CT-7 (Consensus Pause)**: Stop 2 Validator nodes (vượt quá ngưỡng $f=1$) -> Mạng ngừng đóng block -> Start lại nodes -> Mạng phục hồi và Worker hoàn tất batch.
    3.  Tổng hợp kết quả kiểm thử vào `docs/blockchain/reports/chaos-test-report.md`.
*   **Done Criteria**: Tất cả 7 kịch bản Chaos Test đều PASS với kết quả hệ thống giữ vững tính toàn vẹn dữ liệu, không tạo transaction rác/trùng lắp.

### WP 4.6: Disaster Recovery Procedures
*   **Mục tiêu**: Thiết lập quy trình chuẩn (SOP) và công cụ phục hồi hệ thống khi xảy ra thảm họa mất mát dữ liệu.
*   **Chi tiết thực hiện**:
    1.  Viết tài liệu `docs/blockchain/disaster-recovery/dr-procedure.md` phân loại tầm quan trọng dữ liệu (Critical: `genesis.json`, DB SQL Server, Key Pair; High: Contract ABI/Address; Medium: Besu data volume).
    2.  Tạo script backup tự động `apps/blockchain/scripts/backup-db.sh` chạy `docker exec` trích xuất file backup SQL Server định kỳ với chính sách lưu trữ 30 ngày.
    3.  Tạo file `docs/blockchain/disaster-recovery/backup-checklist.md` hướng dẫn backup thủ công các file cấu hình immutable (`genesis.json`, `static-nodes.json`).
*   **Done Criteria**: Có đầy đủ tài liệu hướng dẫn khôi phục và script backup DB hoạt động ổn định.

### WP 4.7: Integration Verification (End-to-End Phase 4)
*   **Mục tiêu**: Kiểm thử tích hợp toàn bộ các thành phần Phase 4 trên môi trường thực tế.
*   **Kịch bản Integration Test**:
    1.  Khởi chạy toàn bộ stack: Besu Consensus (4V + 1RPC) + Monitoring (Prometheus + Grafana) + Backend API/Worker.
    2.  Thực thi script Load Test bơm 500 bằng.
    3.  Quan sát Grafana Dashboard kiểm tra metrics của Besu và Worker biến động đồng bộ.
    4.  Kiểm tra logs của Worker đảm bảo có đủ `BatchCorrelationId`, `BatchId`, `BlockchainTxHash`, `ElapsedMs`.
    5.  Chạy script backup DB kiểm tra file `.bak` được khởi tạo thành công.
*   **Done Criteria**: Đạt tất cả các tiêu chí nghiệm thu của Phase 4.

---

## 3. Cấu Trúc Thư Mục Bàn Giao (Deliverables Structure)

```
apps/blockchain/
├── docker-compose.monitoring.yml      # Cấu hình stack Prometheus & Grafana
├── monitoring/
│   ├── prometheus/
│   │   └── prometheus.yml             # Cấu hình target scrape Prometheus
│   └── grafana/
│       ├── provisioning/
│       │   ├── datasources/
│       │   │   └── datasource.yml     # Auto datasource Prometheus
│       │   └── dashboards/
│       │       └── dashboards.yml     # Auto import dashboards
│       └── dashboards/
│           ├── besu-network-health.json   # Dashboard giám sát 5 Besu nodes
│           └── worker-health.json         # Dashboard giám sát Worker Backend
├── scripts/
│   └── backup-db.sh                   # Script backup SQL Server DB
└── tests/
    └── load-test/                     # C# Console App Load Testing

apps/backend/ChainDegree/src/
├── ChainDegree.Infrastructure/
│   ├── Monitoring/
│   │   └── WorkerMetrics.cs           # Khai báo Prometheus Metrics
│   └── BackgroundWorkers/
│       └── BatchingDegreeWorker.cs     # Cập nhật Structured Logging & Metrics

docs/blockchain/
├── observability/
│   └── log-structure.md               # Tài liệu cấu trúc Log
├── reports/
│   ├── load-test-report.md            # Báo cáo kết quả Load Test
│   ├── chaos-test-procedure.md        # Kịch bản kiểm thử Chaos Test
│   └── chaos-test-report.md           # Báo cáo kết quả Chaos Test
├── disaster-recovery/
│   ├── dr-procedure.md                # Quy trình Disaster Recovery (SOP)
│   └── backup-checklist.md            # Checklist backup cấu hình
└── implementation/
    └── phase-4-observability-load-testing-dr.md  # File kế hoạch triển khai này
```

---

## 4. Kiểm Thử & Verification Plan

### Automated Unit & Integration Tests
```powershell
# Chạy Unit Tests kiểm tra Logging & Metrics
dotnet test ChainDegree.slnx --filter "Category=WorkerLogging|Category=WorkerMetrics"
```

### Manual Verification Checklist
1. **Prometheus Targets**: Truy cập `http://localhost:9090/targets` -> Tất cả 5 Besu nodes và Backend API ở trạng thái `UP`.
2. **Grafana Dashboards**: Truy cập `http://localhost:3000` -> Hiển thị trực quan dữ liệu từ Besu và Worker.
3. **Log Traceability**: Kiểm tra file log Worker có chứa `BatchCorrelationId`, `BatchId`, `BlockchainTxHash`, `ElapsedMs`.
4. **Benchmark Verification**: Load test 5,000 degrees hoàn tất 100% không mất mát dữ liệu.
5. **Chaos Test Verification**: PASS 7/7 kịch bản Failure Injection.
6. **DR Backup**: Execute `backup-db.sh` sinh file `.bak` hợp lệ.
