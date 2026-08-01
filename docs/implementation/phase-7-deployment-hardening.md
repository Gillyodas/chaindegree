# Phase 7: Deployment, Observability, And End-To-End Hardening — Implementation & Verification Plan

## Background & Current State Analysis

Phase 7 là giai đoạn đóng gói toàn bộ hệ thống (Deployment, Observability, And End-To-End Hardening) theo định hướng chạy được trên môi trường local và giả lập production. Giai đoạn này tận dụng các cơ sở hạ tầng đã có (Docker Compose cho SQL Server ở thư mục gốc, Blockchain và Monitoring ở `apps/blockchain`), đồng thời bổ sung các checklist bảo mật, tinh chỉnh cấu hình retry, health check, logging và module toggles.

### What Needs to be Built

| Component | Target Layer | Notes |
|---|---|---|
| Environment Configuration | Application/Infrastructure | Cập nhật file `.env.example` đã có, đảm bảo đủ biến môi trường. |
| Health Checks | API/Infrastructure | Cài đặt các endpoint `/health/live` và `/health/ready` để phân tách rõ liveness và readiness. |
| Structured Logging | API/Infrastructure | Kiểm tra lại middleware ở tầng API, đảm bảo đã log HTTP request đủ context bằng Serilog. |
| Module Toggles | API/DI (Dependency Injection) | Cấu hình bật/tắt Reputation và Authentication Provider (VD: `Auth.Provider=Mock` hoặc `ControlHub`). |
| Seed Data | Infrastructure | Sinh dữ liệu ban đầu chỉ chạy trên môi trường Development hoặc qua tham số dòng lệnh riêng (`dotnet run -- seed`). |
| RabbitMQ DLQ & Retry | Infrastructure | Thiết lập MassTransit Retry 3 lần, cập nhật trạng thái nghiệp vụ trước khi đẩy vào DLQ. |
| Test Suites | E2E Tests | Phân chia rõ ràng: Integration Test (dùng Mock Blockchain) để chạy thường xuyên và Full E2E (Besu thật) chạy manual. |

---

## Technical & Business Decisions Summary

| Decision | Resolution |
|---|---|
| **Worker Architecture** | ✅ Worker (Batch degree, Build Merkle, Submit tx, Update status) đang là application behavior, được giữ nguyên ở dạng `HostedService` trong API. Không tách thành service riêng trong MVP để tránh phức tạp hóa vòng đời deploy, config và auth rườm rà không đáng có. |
| **Health Checks Integration** | ✅ Phân biệt rõ liveness và readiness: `/health/live` (không check dependency) để tránh Docker restart loop, ứng phó RabbitMQ restart. `/health/ready` check các dependency (SQL Server, RabbitMQ, Besu RPC). |
| **RabbitMQ DLQ & Business State** | ✅ Thứ tự xử lý lỗi: Worker try blockchain -> fail -> retry 3 lần -> fail -> Cập nhật Degree status = `Confirmation_Error` -> publish DLQ. Không để RabbitMQ tự quyết định business status để tránh việc Degree bị kẹt vĩnh viễn ở `Pending_Confirmation`. |
| **Retry Strategy** | ✅ Giới hạn MassTransit retry: tối đa 3 lần với delay `5s`, `30s`, `2m`. Không retry vô hạn để tránh treo CPU Worker khi một giao dịch hỏng. |
| **Module Isolation (Toggles)** | ✅ Reputation module toggle: `Modules:Reputation:Enabled=true`. Auth module KHÔNG dùng `Enabled=false`, mà dùng cờ đổi provider: `Modules:Authentication:Provider="Mock"` cho Dev hoặc `"ControlHub"` cho Production. |
| **Seed Data Execution** | ✅ Seeder tuyệt đối không tự chạy khi restart ở Production. Luồng thực thi: API start -> check Development env -> chạy seeder, hoặc chạy qua command chủ động. Bỏ qua hoàn toàn trên Production. |
| **Testing Approach** | ✅ Tách CI test làm hai phần: Integration Test (sử dụng Mock Blockchain) có thể chạy mọi lúc không tốn tài nguyên; Full E2E Test (với 4 validators Besu thật) rất chậm, chỉ dành để chạy manual hoặc nằm trong release pipeline. |

---

## Security Hardening Checklist

Một phần cực kỳ quan trọng của Phase 7 là kiểm tra lại các cấu hình bảo mật trước khi release:

### Container
- [ ] Không chạy container bằng user `root` nếu không thực sự cần thiết.
- [ ] Không expose port của SQL Server ra external host trên môi trường Production.
- [ ] Không expose port của RabbitMQ Management (15672) ra ngoài ở Production.

### API
- [ ] Document rõ quy trình HTTPS termination (cấu hình trên Load Balancer hoặc Reverse Proxy).
- [ ] Thiết lập Rate Limit cho public verification endpoint (`/api/v1/institutions/degrees/verify`) để chống flood.
- [ ] Khai báo giới hạn Input size limit để phòng chống tấn công payload quá cỡ.

### Database
- [ ] Kiểm soát quyền chạy Database Migration. KHÔNG tự động migrate trên môi trường Production khi app startup.

### RabbitMQ
- [ ] Tạo và sử dụng User riêng biệt cho hệ thống, KHÔNG dùng user mặc định `guest/guest`.

### Blockchain
- [ ] Các node Besu Validator đóng hoàn toàn cổng RPC đối với public.
- [ ] Đảm bảo các Private keys không bị đóng gói cứng (hardcode) nằm bên trong Docker image.

---

## Work Packages Detail & Execution Plan

### Work Package 7.1: Base Configurations & Infrastructure Adjustments
- **Tasks**:
  - Rà soát các cấu hình Docker hiện tại (`E:\codes\chaindegree\docker-compose.yml`, thư mục `apps/blockchain`).
  - Kiểm tra và cập nhật file `.env.example` cung cấp đủ biến môi trường (SQL Server, RabbitMQ, MassTransit, Besu).
- **Done Criteria**: Có thể chạy được ứng dụng (kết nối DB, RabbitMQ, Blockchain) với `.env` được copy từ `.env.example`.

### Work Package 7.2: Observability (Health Checks & Structured Logging)
- **Tasks**:
  - Kiểm tra lại middleware log HTTP request ở tầng API, đảm bảo có tích hợp context cho Serilog.
  - Cài đặt endpoint `/health/live` (luôn trả về Healthy nếu API vẫn response được).
  - Cài đặt endpoint `/health/ready` check SQL Server, RabbitMQ, và Besu RPC.
- **Done Criteria**: Health check endpoints được phân tách logic hoàn toàn. Restart RabbitMQ sẽ làm `/health/ready` fail, nhưng `/health/live` vẫn sống.

### Work Package 7.3: Resilience (Retry, DLQ & Business Status)
- **Tasks**:
  - Cấu hình MassTransit retry policy (3 retries: 5s, 30s, 2m).
  - Worker catch exception khi giao dịch thất bại lần cuối: gọi DB Update state `Degree.Status` thành `Confirmation_Error`.
  - Push message vào RabbitMQ DLQ sau khi cập nhật db state.
- **Done Criteria**: degree bị lỗi Blockchain sẽ chuyển sang Error. Message gốc bị giữ trong DLQ để admin investigate thủ công, vòng đời xử lý Worker không bị nghẽn (CPU không bị treo loop).

### Work Package 7.4: Module Toggles, Seed Data & Validation
- **Tasks**:
  - Áp dụng logic Provider pattern cho Authentication (`Mock` vs `ControlHub`).
  - Đảm bảo logic DataSeeder chỉ invoke nếu `Environment == Development` hoặc khởi động bằng cờ `--seed`.
  - Chia test: thiết lập MockBlockchain service ở project Integration Test hiện tại.
- **Done Criteria**: Không chạy lộn Seeder lên Prod, test Integration chạy siêu tốc độ, Auth module linh động.

---

## Verification & Integration Test Plan

### Automated Tests Execution Expected Commands
```powershell
# Chạy Unit & Integration tests hiện tại (sử dụng Mock Blockchain, rất nhanh)
dotnet test ChainDegree.slnx

# Chạy Full E2E Test với Besu Blockchain thực
# (Cần dựng sẵn cluster Besu trước khi test)
# Lệnh này dùng cho local release hoặc manual pipeline
dotnet test tests/ChainDegree.E2E.Tests/ChainDegree.E2E.Tests.csproj
```

### End-To-End Integration Goals
- **Goal 1 (Resilience)**: Giao dịch blockchain thất bại 3 lần sẽ chuyển degree sang `Confirmation_Error` trước, sau đó đẩy sang DLQ.
- **Goal 2 (Modularity)**: Toggle Auth provider sang Mock cho phép local test mà không cần service ngoài; đổi thành ControlHub cho Production fail-fast nếu config sai.
- **Goal 3 (Observability)**: Docker tự restart app nếu `/health/live` fail, và LB tự drop traffic ra khỏi instance nếu `/health/ready` fail.
- **Goal 4 (Security Guardrails)**: Đạt 100% các tiêu chí trong Security Hardening Checklist thông qua cấu hình môi trường chuẩn.

---

## Commit Plan (Deployable Intentions)

```text
docs(phase-7): plan deployment, observability and hardening phase with refined architecture rules
feat(observability): separate liveness and readiness health checks to prevent restart loops
feat(infrastructure): configure masstransit retry limits, business state fallback and dlq
feat(core): implement flexible auth provider and feature toggles for reputation
feat(infrastructure): restrict data seeder execution exclusively to development
test(integration): decouple real blockchain from fast ci integration tests
docs(security): formalize security hardening checklist in configurations
```
