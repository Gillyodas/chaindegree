# Coding Standards

This document defines the coding conventions used throughout the ChainDegree project.

---

# General Principles

Write code for humans.

Not for compilers.

Prioritize:

Correctness

↓

Readability

↓

Maintainability

↓

Performance

---

# SOLID

Follow SOLID when it improves maintainability.

Do not force SOLID everywhere.

Avoid creating interfaces "just in case".

---

# Clean Architecture

Application

- Business rules
- Interfaces
- Result pattern
- Domain language

Infrastructure

- EF Core
- Nethereum
- SQL
- HTTP
- Docker
- External services

Application must never depend on Infrastructure.

---

# Result Pattern

Use Result for:

- Validation failures
- Business rule violations
- Recoverable infrastructure failures

Examples

✓ Degree already exists

✓ RPC timeout

✓ Network unavailable

✓ Unauthorized signer

Do NOT use Result for:

- NullReferenceException
- InvalidCastException
- Programming bugs

---

# Exception Policy

Catch only exceptions that you know how to handle.

Never write

catch (Exception)

unless rethrowing or adding context.

Allowed examples

- RpcResponseException
- HttpRequestException
- SocketException
- TaskCanceledException

Unexpected exceptions should crash.

---

# Startup Validation

Application must fail fast.

Validate:

- Configuration
- ChainId
- Contract existence
- Signer authorization

Never continue startup if blockchain configuration is invalid.

---

# Startup Composition

Keep Program.cs as the composition root.

Program.cs should orchestrate the application,
not contain implementation details.

Group related registrations into dedicated extension methods.

Good

builder.Services.AddApplication();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuthenticationModule(builder.Configuration);

builder.Services.AddHealthCheckModule();

builder.Services.AddOpenApiModule();

Avoid

Hundreds of lines of service registrations directly inside Program.cs.

The goal is modular startup,
not plugin architecture.

Each module should have a single responsibility
and be independently maintainable.

Program.cs should read like a high-level overview
of how the application is composed.

---

# Dependency Injection

Always inject abstractions.

Avoid Service Locator.

Avoid static services.

Prefer constructor injection.

---

# Logging

Logs must contain enough information for production debugging.

Blockchain operations should include:

CorrelationId

BatchId

BlockchainTxHash

Elapsed Time

Never log:

Private Keys

Secrets

Passwords

Tokens

---

# Async

Prefer async all the way.

Never block async code.

Avoid:

.Result

.Wait()

---

# CancellationToken

Every async public method should accept CancellationToken.

Pass CancellationToken to downstream APIs whenever possible.

---

# Naming

Use domain language.

Good

AnchorMerkleRootAsync

CheckBatchExistsAsync

DegreeProcessingRecord

Bad

Execute()

Run()

Process()

Helper()

Manager()

Util()

---

# Blockchain

Never implement blockchain protocols manually.

Use Nethereum built-in APIs whenever available.

Use:

Account

TransactionManager

ContractHandler

FunctionMessage

Do NOT manually:

Sign RLP

Encode ABI

Build raw transaction

Unless there is a real requirement.

---

# Smart Contract

Storage is the source of truth.

Worker should read:

mapping

Worker should NOT rely on:

Event Logs

Events exist for:

Explorer

Analytics

Audit

UI

---

# Retry Policy

Retry only transient failures.

Examples

✓ Network timeout

✓ HTTP 503

✓ RPC unavailable

Never retry

Invalid input

Unauthorized

Contract revert caused by business rules

---

# Idempotency

Never send duplicate blockchain transactions.

Always check

1.

Existing TxHash

↓

2.

On-chain State

↓

3.

Send New Transaction

---

# Security

Never store secrets in source code.

Development

↓

.env

Production

↓

KMS / Remote Signer

Never expose RPC publicly.

Validator nodes should not expose RPC.

---

# Testing

Every feature should be testable.

Prefer

Unit Test

↓

Integration Test

↓

Manual Test

Every completed phase should have

Done Criteria

Deliverables

Verification Plan

---

# Documentation

Every significant architectural decision should have an ADR.

Code comments explain

HOW

ADR explains

WHY

Implementation Plan explains

WHEN

Runbook explains

HOW TO OPERATE

---

# Golden Rule

Whenever making a design decision, ask:

Is this simpler?

Is this more secure?

Is this easier to maintain?

If not,

do not introduce it.

---

# Frontend Coding Standards

Frontend của ChainDegree cần đơn giản, giữ nguyên tắc KISS, dễ mở rộng và đồng bộ tốt với Backend. Khác với backend (vốn đã được thiết kế theo Clean Architecture, CQRS, DDD, module boundaries), Frontend cho MVP không cần quá phức tạp nhưng phải đặt nền móng chuẩn để dùng lâu dài, tránh những sai lầm khó sửa mà không phải refactor hàng loạt về sau.

---

## Mức 1 — Những quyết định ngay từ đầu (Khó sửa nhất)

### 1. Chọn Framework & Tech Stack
- **Stack khuyến nghị:** React + TypeScript + Vite + React Router.
- **Lý do:** Phổ biến, hệ sinh thái lớn, dễ tuyển dụng, phù hợp SPA, tránh over-engineering.
- **Lưu ý:** Không cần Next.js ở giai đoạn MVP nếu toàn bộ dữ liệu đến từ API và không yêu cầu SEO.

### 2. Bắt buộc dùng TypeScript
- Không dùng JavaScript thuần.
- Tránh lỗi runtime (ví dụ `degree.status = 5`). Định nghĩa type/interface rõ ràng (ví dụ `type DegreeStatus = "Pending_Confirmation" | "Confirmed" | "Revoked"`).
- Giúp bắt lỗi kịp thời trên IDE và đồng bộ DTO chính xác với Backend.

### 3. Cấu trúc thư mục theo Feature (Vertical Slice)
- **Không chia theo loại file:** Tránh chia kiểu `components/`, `pages/`, `hooks/`, `services/` làm phình to thư mục chứa hàng trăm component không rõ thuộc module nào.
- **Chia theo Feature:**
  ```text
  src/
  ├── app/
  │   ├── router/
  │   ├── providers/
  │   └── config/
  ├── features/
  │   ├── auth/
  │   ├── degree/
  │   ├── verification/
  │   ├── report/
  │   ├── reputation/
  │   └── recruitment/
  ├── shared/
  │   ├── api/
  │   ├── components/
  │   ├── hooks/
  │   ├── lib/
  │   ├── types/
  │   └── utils/
  ├── assets/
  └── main.tsx
  ```

### 4. Quy tắc Ranh giới Thư mục (Feature Boundary Rules)
- **Giữ nguyên ranh giới feature:** Các file bên trong một feature (ví dụ `features/degree/components/internal/...`) là nội bộ của feature đó.
- **Công khai qua Public API (`index.ts`):** Mỗi feature chỉ export các thành phần công khai (Pages, Services, Public Hooks/Types) tại file `features/<feature_name>/index.ts`.
- **Quy tắc Import:** Feature khác chỉ được import thông qua public API (ví dụ: `import { IssueDegreePage } from "@/features/degree"`), tuyệt đối **không** import trực tiếp vào thư mục nội bộ của feature khác (ví dụ: `from "@/features/degree/components/internal/Xxx"`). Shared code dùng chung cho nhiều feature phải đặt ở `@/shared`.

### 5. Phân định Rõ ràng giữa Shared và Feature Scope
- **`@/shared` chỉ chứa thành phần dùng chung, không phụ thuộc domain:** Chỉ đặt các component UI tái sử dụng thuần túy (ví dụ: `Button`, `Input`, `Modal`, `Spinner`, `DatePicker`, `Table`) hoặc utility functions đại đồng trong `@/shared`.
- **Không đưa Domain Component vào Shared:** Tuyệt đối không đưa các component mang tính nghiệp vụ (như `DegreeCard`, `JobCard`, `ReportTable`) vào `@/shared`. Các component này phải thuộc về `features/<feature_name>/components/` tương ứng để tránh biến `@/shared` thành "sọt rác" khó bảo trì.

### 6. Chuẩn hóa HTTP Client (Axios Instance Singleton)
- **Chỉ sử dụng một Axios Instance duy nhất:** Cấu hình tập trung tại `@/shared/api/http.ts` (hoặc `api-client.ts`).
- **Quy định chung:**
  - Thiết lập timeout mặc định (ví dụ: 10s - 15s).
  - Sử dụng **Request Interceptor** để tự động gắn `Authorization: Bearer <token>` vào header.
  - Sử dụng **Response Interceptor** để xử lý tập trung lỗi HTTP (401 Unauthorized, 403 Forbidden, 500 Internal Error) và luồng refresh token (nếu có).
- Tuyệt đối **không** tự tạo instance axios riêng hoặc gọi `axios.create()` rải rác ở từng service.

---

## Mức 2 — Kiến trúc cốt lõi & Robustness

### 7. Xử lý Lỗi Toàn cục (Error Boundary)
- Mặc định trong React, nếu một component throw exception thì có thể làm hỏng toàn bộ cây ứng dụng.
- **Quy tắc:** Mỗi route chính hoặc khu vực UI quan trọng phải được bọc bởi `ErrorBoundary`.
- **Luồng kiến trúc:** `App` -> `ErrorBoundary` -> `Router` -> `Page`.
- **Yêu cầu:** Log lỗi, hiển thị màn hình thông báo lỗi thân thiện (Fallback UI) và cung cấp nút "Thử lại" (Retry).

### 8. Phân quyền và Bảo vệ Route (Route Protection & Guard)
- Sử dụng `ProtectedRoute` wrapper cho các trang yêu cầu quyền hạn (Registrar, Admin, Student...).
- **Nguyên tắc cốt lõi:** Route Guard ở Frontend **chỉ phục vụ mục đích trải nghiệm người dùng (UX)** (chuyển hướng trang, ẩn menu). Backend luôn là nơi thực thực thi kiểm tra quyền (Authorization) và enforce bảo mật cuối cùng.

### 9. Đồng bộ & Ánh xạ Mã lỗi API (API Error Mapping)
- Backend sử dụng Result Pattern trả về các error code chuẩn (ví dụ `DEGREE_ALREADY_EXISTS`, `INVALID_SIGNATURE`, `NOT_FOUND`).
- Frontend **không** hiển thị trực tiếp `error.message` thô lên UI.
- Sử dụng module `ApiErrorMapper` tập trung để ánh xạ từ backend error code sang thông điệp tiếng Việt (hoặc ngôn ngữ UI) thân thiện với người dùng trước khi hiển thị Toast/Alert.

### 10. Quản lý Server State & Chuẩn hóa Query Keys (TanStack Query)
- Giải quyết tự động các bài toán: loading, retry, caching, refetching, cache invalidation, background refresh.
- **Chuẩn hóa Query Keys Factory:** Định nghĩa hằng số/factory cho Query Keys (ví dụ `degreeKeys.all`, `degreeKeys.detail(id)`, `jobKeys.lists()`) tại từng feature để việc invalidate cache (`queryClient.invalidateQueries(...)`) luôn chính xác và nhất quán.
- **Bắt buộc Immutability (Không Mutate Server State Directly):** Tuyệt đối không thay đổi trực tiếp thuộc tính của dữ liệu trả về từ React Query (ví dụ: cấm `data.name = 'new'`). Mọi thay đổi dữ liệu phải thông qua Mutation -> Invalidate Query.

### 11. Chiến lược Xử lý Form (React Hook Form + Zod)
- Sử dụng kết hợp **React Hook Form** + **Zod Schema Validation**.
- **Lợi ích:** Đảm bảo hiệu năng tốt, validate dữ liệu ở FE đồng bộ, hỗ trợ tự động infer TypeScript types từ schema, dễ tái sử dụng các validation rule.

### 12. Tách Business Logic khỏi Component
- Component chỉ làm nhiệm vụ render giao diện và bắt sự kiện.
- Business logic, validation rule phức tạp, tính toán trạng thái phải được đưa vào: `hooks`, `services`, `helpers`, `mappers`.

---

## Mức 3 — Standards & Quy chuẩn phát triển

### 13. Phân biệt API DTO và UI ViewModel (Mapper Rule)
- **Tên DTO đồng bộ Backend:** Tên thuộc tính trong DTO giữ nguyên giống Backend (`studentFullName`, `issuedAt`).
- **Nguyên tắc Linh hoạt (KISS Mapper):**
  - Nếu UI hiển thị y nguyên dữ liệu từ API: Sử dụng trực tiếp DTO, không cần tạo mapper thừa thãi.
  - Nếu UI cần định dạng, biến đổi phức tạp hoặc kết hợp dữ liệu: Sử dụng Mapper helper (`API DTO` -> `Mapper` -> `ViewModel`) để tránh truyền DTO thô qua nhiều lớp component UI.

### 14. Quy ước Đặt tên (Naming Conventions)
Đảm bảo tính nhất quán tuyệt đối trong toàn bộ dự án:
- **Components:** PascalCase (ví dụ `DegreeCard.tsx`, `StudentTable.tsx`).
- **Custom Hooks:** camelCase với tiền tố `use` (ví dụ `useIssueDegree.ts`).
- **Queries & Mutations:** `use<Feature>Query.ts`, `use<Feature>Mutation.ts`.
- **API Services:** `<feature>.api.ts`.
- **Types / Interfaces:** `<feature>.types.ts`.
- **Mappers & Helpers:** `<feature>.mapper.ts`, `<feature>.helper.ts`.

### 15. Sử dụng Absolute Imports (`@/`) & Thứ tự Import (Import Order)
- **Path Alias:** Cấu hình path alias trong TypeScript & Vite (`@/shared`, `@/features/...`).
- **Quy tắc sắp xếp Import:**
  1. Thư viện cốt lõi (React, React Router...)
  2. Thư viện bên thứ ba (TanStack Query, Lucide icons...)
  3. Shared Modules (`@/shared/...`)
  4. Feature Modules (`@/features/...`)
  5. Relative Imports trong cùng thư mục (`./...`)

### 16. Công cụ Chuẩn hóa Codebase (ESLint, Prettier & EditorConfig)
- Bắt buộc tích hợp bộ ba **ESLint + Prettier + .editorconfig** ngay từ đầu dự án để tự động phát hiện lỗi cú pháp và giữ phong cách code nhất quán giữa các lập trình viên.

### 17. Đồng bộ Environment & Quản lý Gitignore (.env)
- Luôn có `.env.example` chuẩn hóa trong repository (ví dụ `VITE_API_BASE_URL=`). Không hardcode URL (`localhost:5000`) hay cấu hình hệ thống trực tiếp trong mã nguồn.
- **Quy tắc Gitignore:** Tuyệt đối không commit các file môi trường cá nhân hoặc secret (`.env.local`, `.env.development.local`, `.env.production.local`) vào Git repository.

### 18. Xử lý Thời gian (Date Handling)
- **Không format Date trực tiếp trong component** bằng `new Date()` hay toán tử thủ công.
- Sử dụng module tập trung `@/shared/lib/date.ts` cung cấp các hàm trợ giúp như `formatDate()`, `formatDateTime()`, `formatRelativeTime()`. Khi cần đổi định dạng ngày tháng toàn hệ thống, chỉ cần sửa tại một nơi duy nhất.

### 19. Quản lý Thông báo (Notification Strategy)
- Không gọi trực tiếp thư viện toast thô (như `toast.success(...)`) rải rác trong các UI components.
- Bọc qua một `NotificationService` hoặc wrapper helper ở `@/shared/services/notification.service.ts` để dễ dàng nâng cấp/thay thế thư viện toast về sau mà không phải sửa lại toàn bộ codebase.

---

## Mức 4 — Bảo mật (Security Baseline)

### 20. Đừng tin Frontend (Zero Trust UI)
- Giao diện và việc ẩn/hiện nút chỉ phục vụ UX.
- Backend luôn là nơi thực hiện kiểm tra quyền (Authorization) và enforce business rules cuối cùng.

### 21. Không lưu Secret trong Frontend
- Mã nguồn build Frontend là công khai.
- Tuyệt đối không nhúng API secret keys, JWT secret hay private key vào mã nguồn Frontend.

### 22. Cơ chế Quản lý Token (JWT)
- **Production (Bảo mật cao):** Access Token thời gian sống ngắn, Refresh Token lưu trong `HttpOnly Secure Cookie` để chống XSS.
- **MVP nội bộ / Đồ án:** Có thể chấp nhận lưu Access Token trong memory hoặc `localStorage` để giảm độ phức tạp, nhưng cần nhận thức rõ đây là sự đánh đổi (trade-off) về mặt bảo mật.

### 23. Phòng chống XSS & Re-validation File Upload
- Tận dụng cơ chế escape mặc định của React, tránh `dangerouslySetInnerHTML`.
- Kiểm tra định dạng file ở FE để tăng trải nghiệm người dùng, nhưng Backend bắt buộc phải re-validate MIME type, extension và nội dung file upload.

---

## Mức 5 — UI/UX, Accessibility & Performance

### 24. Thư viện UI/UX có sẵn (Design System & Reusable Components)
- Không tự triển khai UI hoàn toàn từ đầu (reinventing the wheel).
- Sử dụng **TailwindCSS** kết hợp với **shadcn/ui** (hoặc Radix UI primitives, Lucide icons) để phát triển nhanh, đẹp, tái sử dụng tốt mà vẫn linh hoạt và an toàn bảo mật.

### 25. Quản lý Trạng thái UI (Loading / Error / Empty)
- Tuyệt đối không để xảy ra "trang trắng" khi tương tác API.
- Mỗi màn hình/thao tác API phải xử lý đủ 3 trạng thái: Loading, Empty data, và Error handling.

### 26. Tối ưu Hiệu năng theo Nguyên tắc KISS (Performance Rule)
- **Không tối ưu hóa sớm (No Premature Optimization):** Không sử dụng `useMemo`, `useCallback`, hoặc `React.memo` nếu chưa có bằng chứng hoặc đo đạc thực tế về vấn đề hiệu năng.
- Giữ mã nguồn đơn giản và dễ đọc là ưu tiên hàng đầu.

### 27. Chuẩn hóa Khả năng Truy cập cơ bản (Accessibility - a11y)
- Mọi nút bấm (button) đều phải có label rõ ràng (hoặc `aria-label` nếu chỉ dùng icon).
- Form input phải đi kèm `<label>` tương ứng.
- Modal/Dialog phải xử lý quản lý focus đúng cách.

---

## Mức 6 — Chiến lược Kiểm thử (Testing Strategy)

- **Business Logic / Helper / Mapper:** Viết **Unit Test** để đảm bảo tính chính xác của thuật toán và chuyển đổi dữ liệu.
- **UI Components & Pages:** Kiểm thử thủ công (**Manual Testing**) hoặc integration test tối giản cho các luồng chính. Không ép buộc phủ test 100% cho toàn bộ các component giao diện thuần túy ở giai đoạn MVP.

---

## Mức 7 — Quyết định Kiến trúc & Những thứ KHÔNG nên làm ở MVP (KISS Baseline)

Để giữ Frontend đơn giản, tinh gọn và tập trung vào mục tiêu MVP chính, dự án thống nhất các quyết định sau:

1. **Quản lý Global State (Không dùng Redux / Zustand):**
   - **React Query** + **React Hook Form** đã quản lý 90–95% state của hệ thống (Server State và Form State).
   - Global State còn lại (Theme, Auth User, Sidebar) sử dụng **React Context API** là hoàn toàn đủ. **Không sử dụng Redux Toolkit, Zustand hay XState** cho MVP để tránh phức tạp hóa mã nguồn.
2. **Không áp dụng Feature-Sliced Design (FSD) đầy đủ:**
   - Sử dụng cấu trúc gọn nhẹ 3 tầng đơn giản: `app/`, `features/`, `shared/`. Không rập khuôn áp dụng FSD đầy đủ (Entities, Features, Widgets, Pages...) gây rườm rà trái với tinh thần KISS.
3. **Danh sách các công nghệ/mô hình loại trừ khỏi MVP:**
   - Micro Frontend.
   - Clean Architecture / Hexagonal Architecture phức tạp ở Frontend.
   - Atomic Design, Storybook.
   - Event Bus phức tạp, Domain-Driven Frontend.
   - GraphQL, WebSocket (nếu chưa có nhu cầu realtime rõ ràng).
   - CSS Modules, Offline-first, Multi-language (i18n) nếu dự án chỉ có một ngôn ngữ.

---

## 10 Nguyên tắc vàng cho Frontend ChainDegree

1. **React + TypeScript:** Bắt buộc dùng TypeScript, giữ stack đơn giản và hiện đại (Vite + React Router).
2. **Cấu trúc theo Feature & Public API:** Tổ chức thư mục theo feature (`features/`), phân định rõ `@/shared` (generic UI) và chỉ import thông qua file `index.ts` của feature.
3. **Error Boundary & Graceful Fallback:** Bọc route chính bằng Error Boundary, không để sập giao diện toàn trang.
4. **Tách API Client & Error Mapping:** Sử dụng duy nhất một Axios Singleton Instance (`shared/api/http.ts`), dùng TanStack Query (với Query Key Constants & Immutability) và ánh xạ mã lỗi backend (`ApiErrorMapper`) sang thông điệp tiếng Việt.
5. **Form Validation (React Hook Form + Zod):** Dùng React Hook Form kết hợp với Zod Schema để validate và infer TypeScript type.
6. **Tách Business Logic & Linh hoạt Mapper:** Giữ component gọn nhẹ (chỉ render UX), đưa logic vào hooks/services; dùng Mapper khi UI khác DTO backend.
7. **Bảo mật ở Backend & Route Guard cho UX:** Route Guard ở FE chỉ làm UX; Backend chịu trách nhiệm kiểm tra quyền và an toàn bảo mật.
8. **Dùng UI Libraries có sẵn:** Tận dụng TailwindCSS + shadcn/ui để tái sử dụng, tăng tốc độ và đảm bảo thiết kế chuẩn.
9. **Chuẩn hóa Tooling, Naming & Imports:** Tích hợp ESLint/Prettier/.editorconfig, dùng alias `@/`, tuân thủ đặt tên file và sắp xếp thứ tự import nhất quán.
10. **Giữ Frontend đơn giản (KISS):** Dùng React Context cho global state, không tối ưu sớm (no premature optimization), không đưa các kiến trúc/thư viện phức tạp thừa thãi (Redux, FSD, Micro FE) vào MVP.

