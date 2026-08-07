# Phase 1: Degree Issuance UI & Realtime Status — Implementation Plan

## Background & Goal

Phase 1 (US-1 / UC-1) triển khai hoàn chỉnh giao diện cấp bằng cho Registrar, kết nối API Issue Degrees, hiển thị danh sách bằng cấp với trạng thái real-time qua SignalR (kèm polling fallback), và cung cấp cơ chế retry cho các bằng cấp bị lỗi xác thực.

**Git Branch:** `frontend/phase-1-degree-issuance` (checkout từ `main`)

**Expected Outcome:**
- Ứng dụng cung cấp form động để Registrar có thể cấp một hoặc nhiều bằng cấp cùng lúc.
- Danh sách bằng cấp hiển thị rõ ràng các trạng thái thông qua các badge màu sắc.
- Trạng thái bằng cấp được cập nhật tự động (real-time) qua kết nối SignalR, mà không cần làm mới trang.
- Đảm bảo cơ chế tự động phục hồi qua polling nếu SignalR gặp sự cố.
- Các yêu cầu được gửi với `Idempotency-Key` để tránh trùng lặp.
- Người dùng có thể retry các bằng cấp gặp sự cố `Confirmation_Error`.

---

## Technical & Business Decisions

| Decision | Resolution |
|---|---|
| **Data Fetching/Mutation** | **TanStack Query v5** — Sử dụng Query Keys Factory (`degree.keys.ts`) để quản lý cache hiệu quả, thực hiện invalidate cache sau các mutation (issue/retry). |
| **Realtime Update** | **SignalR Client (`@microsoft/signalr`)** — Lắng nghe sự kiện `DegreeStatusUpdated` hoặc `BatchCompleted` từ Backend. |
| **Fallback Cơ Chế Realtime** | **Polling Fallback** — Khi SignalR kết nối, polling bị vô hiệu hóa. Khi SignalR mất kết nối, TanStack Query sẽ tự động bật polling `refetchInterval` mỗi 5s để đảm bảo không lỡ cập nhật. |
| **Form Validation** | **React Hook Form + Zod** — Quản lý dynamic field array cho việc cấp nhiều bằng. Validate các yêu cầu như StudentId (UUID) và các trường bắt buộc tại frontend trước khi gửi request. |
| **Idempotency** | Sinh `UUID` trên frontend mỗi lần submit và gửi qua header `Idempotency-Key` cho yêu cầu `POST /degrees`. |
| **Partial Failure** | Xử lý lỗi một phần khi backend trả về danh sách `failures[]`. Các row lỗi giữ lại form và hiển thị cảnh báo đỏ ngay trên row; các row thành công bị xóa. |
| **State Visualization** | Sử dụng component `StatusBadge` với quy ước màu: 🟡 `Pending_Confirmation`, 🟢 `Confirmed`, 🔴 `Confirmation_Error`. |

---

## Constraints

- ❌ KHÔNG gửi request trùng lặp (phải sử dụng `Idempotency-Key`).
- ❌ KHÔNG chạy Polling và SignalR cùng lúc để tránh lãng phí bandwidth.
- ❌ KHÔNG gộp các lỗi HTTP 500 với 404 (xử lý message hiển thị riêng biệt theo Phase 0).
- ✅ Tất cả import từ `features/degree` phải qua file `index.ts`.
- ✅ 100% UI text và thông báo (toast) bằng tiếng Anh.
- ✅ Component UI chỉ render. Validation và error mapping nằm trong hooks/helpers.
- ✅ Phải có Unit Tests và Integration Test E2E ở mức cơ bản cho luồng cấp bằng.

---

## Proposed Changes — Work Packages Chi Tiết

---

### WP-1.1: Feature `degree` — API Layer & Types

#### Tasks
- [ ] Định nghĩa các Interface/Type tại `src/features/degree/degree.types.ts`:
  - Request: `IssueDegreeItemRequest`, `IssueDegreeRequest`, `BatchStatusResponse`.
  - Response: `DegreeListItem`, `DegreeDetail`, `IssueDegreeResponse`.
  - Type: `DegreeStatus`.
- [ ] Khởi tạo API Service tại `src/features/degree/degree.api.ts`:
  - Hàm `issueDegrees(data, idempotencyKey)` thực hiện POST tới `/api/v1/institutions/degrees`.
  - Hàm `getBatchStatus(batchId)` thực hiện GET tới `/api/v1/institutions/degrees/batches/{batchId}`.
  - Hàm `retryDegreeConfirmation(id)` thực hiện POST tới `/api/v1/institutions/degrees/{id}/retry`.
  - Hàm `getDegrees()` fallback fetching list of degrees.
  - Hàm `getDegree(id)` fallback fetching degree details.
- [ ] Khởi tạo Query Keys Factory tại `src/features/degree/degree.keys.ts`:
  - `degreeKeys.all`, `degreeKeys.lists()`, `degreeKeys.detail(id)`, `degreeKeys.batchStatus(batchId)`.

#### Output
- [NEW] `src/features/degree/degree.types.ts`
- [NEW] `src/features/degree/degree.api.ts`
- [NEW] `src/features/degree/degree.keys.ts`

#### Done Criteria
- Khai báo type đầy đủ và khớp với thiết kế API Specification.
- API Methods được gọi đúng URI và phương thức POST/GET.
- Đủ query keys cho các luồng danh sách, chi tiết, và batch status.

#### Commit
```text
feat(degree): define types, API service, and query key factory
```

---

### WP-1.2: Custom Hooks — Mutations & Queries

#### Tasks
- [ ] Tạo hooks fetch data và mutation tại `src/features/degree/hooks`:
  - `useIssueDegreesMutation`: gọi API issue, handle thành công (invalidate list query) + báo lỗi.
  - `useDegreesQuery`: query list.
  - `useDegreeDetailQuery(id)`: query chi tiết.
  - `useBatchStatusQuery(batchId)`: query trạng thái batch với Polling 5 giây (chỉ bật khi SignalR disconnect).
  - `useRetryDegreeMutation`: gọi retry.
- [ ] Xử lý logic hiển thị Toast Notifications trong hàm `onSuccess`/`onError` của mutation hooks.

#### Output
- [NEW] `src/features/degree/hooks/useIssueDegrees.ts`
- [NEW] `src/features/degree/hooks/useDegreeQueries.ts`

#### Done Criteria
- Invalidate list data khi issue thành công.
- Hook trả về loading, error states, và data tương ứng cho UI.

#### Commit
```text
feat(degree): implement issuance mutations and queries hooks
```

---

### WP-1.3: Degree Issuance Form (Core UI)

#### Tasks
- [ ] Tạo Schema validation với Zod tại `src/features/degree/components/IssueDegreeForm.tsx`:
  - Zod validate cho: StudentId phải đúng định dạng UUID, Major, Classification phải đúng enum.
- [ ] Xây dựng Component Form có khả năng thêm xóa các trường:
  - Form field array hiển thị dạng bảng: Student Id, Major, Classification, Issued At.
  - Nút `[+ Add Degree]` cho row mới, nút `[Remove]` cho row.
- [ ] Xử lý logic nộp đơn (Submit):
  - Sinh UUID phía FE làm `Idempotency-Key`.
  - Catch lỗi partial failures (`failures[]`) từ BE.
  - Nếu thành công tất cả, clear form và toast báo. Nếu thất bại, highlight dòng lỗi và toast báo lỗi.

#### Output
- [NEW] `src/features/degree/components/IssueDegreeForm.tsx`

#### Done Criteria
- Validate được Client-side (required, format) chặn spam submit khi lỗi.
- Nhập liệu nhiều row linh hoạt và gửi đủ request kèm idempotency key.
- Xử lý được partial failure từ backend theo quy định AC3 US-1.
- Toast thông báo thành công hiển thị tiếng Anh chính xác: *"Successfully submitted X degree(s). The system is processing verification in the background."*

#### Commit
```text
feat(degree): build dynamic degree issuance form with Zod validation
```

---
