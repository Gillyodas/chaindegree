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
