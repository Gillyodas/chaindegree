# Phase 2: Degree Update & Revocation UI — Implementation Plan (v4 - Ultimate Survivability)

## Background & Goal

Phase 2 (US-2 / UC-2) tập trung vào việc triển khai tính năng cập nhật thông tin và thu hồi văn bằng cho Registrar. 
Mục tiêu cốt lõi của bản thiết kế này là đạt mức độ **Survivability**: Hệ thống phải sống sót qua các lỗi mạng, lỗi đồng thời (concurrency), không bao giờ tạo state sai, không làm mất dữ liệu, không vượt rào bảo mật và luôn có khả năng phục hồi/xác định lại canonical state. 

**Git Branch:** `frontend/phase-2-degree-update-revocation`

**Overall Goal:** Xây dựng luồng cập nhật/thu hồi văn bằng an toàn, chính xác và có độ tin cậy cao. ƯU TIÊN NGUYÊN TẮC: **Mọi failure của mutation phải để resource ở một trạng thái hợp lệ, không tạo side-effect trùng lặp, không làm mất canonical state, không vượt tenant boundary, và hệ thống phải có khả năng xác định lại trạng thái thật sau khi request có kết quả không xác định.**

---

## Technical & Business Decisions (Survivability Architecture)

### 1. Invariants: Idempotency & Concurrency (Backend Technical Logic)
Để đảm bảo Backend thực sự là Authority, hai cơ chế kỹ thuật sau phải hoạt động song song (không thay thế nhau):

- **Concurrency Control (Atomic State Transition):** 
  Giải quyết bài toán *different operations on same resource*. 
  Việc kiểm tra trạng thái và cập nhật phải **Atomic**. Backend sử dụng Optimistic Concurrency (thông qua `RowVersion` của EF Core). Nếu có race condition, Entity Framework ném `DbUpdateConcurrencyException`, trả về HTTP `409 Conflict`. 
- **Idempotency Guarantee:**
  Giải quyết bài toán *same operation retried*. Giúp bảo vệ khỏi việc thực thi lặp lại cùng một request logic.
  **Invariants bắt buộc:**
  - `Key + Payload Hash + Scope (Tenant)` phải unique. 
  - Cùng Key + Cùng Payload -> Trả về cached response.
  - Cùng Key + Khác Payload -> Ném `409 Conflict` (Ngăn chặn IDEMPOTENCY_KEY_REUSE).
  - Cùng Key + Khác Tenant -> Ném `404/403` (Chống Cross-tenant).
  - Cùng Key được gọi đồng thời (Concurrent) -> Đảm bảo chỉ 1 transaction được xử lý (thông qua distributed lock hoặc unique constraint record), request kia nhận cùng kết quả.

### 2. Invariants: Ambiguous Outcome & Reconciliation (Frontend Logic)
Giải quyết tình trạng "Lost Response / Timeout" (Kết quả không xác định).
- **Trạng thái Mutation `Unknown`:** `MutationResult` bao gồm `success | known_failure | unknown`. Nếu request bị timeout, 500, 503, kết quả là `unknown` (không đồng nghĩa với thất bại hay an toàn để retry).
- **Reconciliation Flow:** Khi `unknown`, FE không cho phép blind-retry. Hệ thống tự động `GET` canonical state.
  - **Trường hợp GET thất bại:** UI chuyển sang trạng thái cảnh báo đồng bộ: *"Request accepted, but the latest degree status could not be loaded. Please retry refreshing."*
  - **Trường hợp GET thành công nhưng thấy State cũ (Stale):** KHÔNG kết luận là mutation thất bại (vì transaction backend có thể đang queue). UI chuyển sang trạng thái **Unresolved/Unknown**: Disable các action buttons và hiển thị: *"Unable to determine the current operation result. The degree is being rechecked."* (có thể kết hợp polling nhẹ).

### 3. Invariants: State Machine Integrity (Domain Logic)
- Tại bất kỳ thời điểm nào, một Degree chỉ có **duy nhất một** canonical lifecycle status.
- Trạng thái của Degree không được phép lùi (regress version).
- **No Impossible State:** Không bao giờ tồn tại trạng thái xung đột (VD: Không thể vừa `Pending_Update` vừa `Pending_Revocation`).

---

## State Transition Matrix (Source of Truth)

Backend ĐẢM BẢO tính toàn vẹn qua ma trận trạng thái. FE chỉ dùng ma trận này để làm UX Guard (hiển thị button).

| Current Canonical State | Action: Update | Action: Revoke |
| -------------------- | ---------- | ---------- |
| `Pending_Confirmation` | ✅ shortcut | ✅ shortcut |
| `Confirmed`            | ✅ async    | ✅ async    |
| `Pending_Update`       | ❌ 409      | ❌ 409      |
| `Pending_Revocation`   | ❌ 409      | ❌ 409      |
| `Revoked`              | ❌ 409      | ❌ 409      |
| `Frozen`               | ❌ 409      | ❌ 409      |
| `Confirmation_Error`   | ❌ 409      | ❌ 409      |

---

## Proposed Changes — Work Packages Chi Tiết

### WP-2.1: Bổ sung API Layer & Domain Types (FE & BE)

#### BE Tasks (Thực hiện ở nhánh Backend)
- Sửa `DegreeErrors.InvalidStateTransition` thành `Error.Conflict` để trả về đúng `409 Conflict`.
- Tích hợp Optimistic Concurrency (`RowVersion`) trong `DegreesController.cs`.
- Xây dựng hệ thống Idempotency Record (lưu Cache/DB với Payload Hash, Scope Tenant và Expiry) cho endpoint Update/Revoke.

#### FE Tasks
- Định nghĩa Types trong `src/features/degree/degree.types.ts`:
  - `UpdateDegreeRequest`, `RevokeDegreeRequest`.
  - `UpdateDegreeResponse`: `{ message: string, degreeId: string, currentStatus: DegreeStatus }`
  - `RevokeDegreeResponse`: `{ message: string, degreeId: string, currentStatus: DegreeStatus, reputationImpact: string }`
- Thêm API calls `updateDegree` và `revokeDegree`, truyền `Idempotency-Key` headers.

### WP-2.2: Reconciliation Mutation Hooks

- **`useUpdateDegreeMutation` / `useRevokeDegreeMutation`**:
  - Gửi `Idempotency-Key` sinh ra từ form.
  - `onSuccess`: Gọi `invalidateQueries`. KHÔNG coi là hoàn thành cho đến khi query active refetch lại thành công. 
  - Xử lý Ambiguous Outcome: Bắt HTTP 500, 503, Timeout, Network Error -> Đẩy state vào `Unknown`. Kích hoạt Reconciliation (refetch GET).
  - Handle Malformed Response: FE catch lỗi nếu response thiếu `currentStatus` -> Đẩy state vào `Unknown`.

### WP-2.3: Update Modal & Revoke Dialog (Safe UI)

- Giao diện Modal form với RHF + Zod validate.
- Các wording thông báo được thắt chặt:
  - **Shortcut Success**: *"Degree revoked successfully."* (Chỉ khi status thực sự là `Revoked`).
  - **Async Accepted**: *"Revocation request accepted. Processing continues in the background."* (Chỉ khi status là `Pending_Revocation`).
  - **Conflict (409)**: Tự động đóng modal, loại bỏ stale form data, trigger refetch, toast: *"The degree state has changed. Please refresh and try again."*

### WP-2.4: Mở rộng StatusBadge & Unknown State Fallback

- Xử lý render màu: 🟡 `Pending_Update`, 🟡 `Pending_Revocation`, 🔴 `Revoked`, ⚫ `Frozen`.
- Xử lý `Unknown/Undefined`: Trả về giao diện màu trung tính (neutral styling), chống crash khi nhận runtime value không có trong TypeScript type.

---

## Kế Hoạch Kiểm Thử Nâng Cao (State Machine & Failure-Oriented)

> Hệ thống test bao phủ mọi Failure Domain. Backend test kiểm tra trạng thái DB, FE test (MSW) kiểm tra Reconciliation. Khoảng 35-50 tests cho MVP.

### 1. State Transition Matrix (12+ tests - BE)
Kiểm thử mọi tổ hợp từ ma trận trạng thái.
- **Hậu điều kiện (Post-conditions):** Test không chỉ check mã HTTP 200/409, mà phải assert trạng thái trong DB (`Degree.Status`) và các action hợp lệ tiếp theo.
- VD: `Confirmed` + Update -> `202 Accepted` -> DB là `Pending_Update` -> Hành động Revoke kế tiếp trả 409.

### 2. State Integrity & No Impossible State (5+ tests - BE)
- Đảm bảo DB Constraint / Domain Logic không cho phép tồn tại văn bằng vừa `Pending_Update` vừa `Pending_Revocation`, không cho phép `Revoked -> Confirmed`. Version không lùi.

### 3. Idempotency & Concurrency Tests (8+ tests - BE)
- Same key + same payload -> Same result (Idempotent).
- Same key + different payload -> `409 Conflict`.
- Same key + cross-tenant -> `403/404`.
- Concurrent double-click (Race condition) -> 1 success, 1 rejected/cached.
- Atomic Concurrency: Cố tình cập nhật 1 record từ 2 thread cùng lúc -> `DbUpdateConcurrencyException` -> `409 Conflict`.

### 4. Authorization / Tenant / IDOR (6+ tests - BE)
- Registrar A gọi Update/Revoke trên bằng của Registrar B bằng HTTP call trực tiếp -> `404 Not Found`.

### 5. Ambiguous Outcomes & Reconciliation (6+ tests - FE/MSW & BE)
- **FE MSW Test:** Timeout after server success -> Trạng thái mutation = `unknown` -> Trigger GET -> Cập nhật UI an toàn.
- **FE MSW Test:** Mutation success nhưng Refetch thất bại -> UI báo *"Request accepted, but status could not be loaded"*.
- **FE MSW Test:** Timeout -> GET thấy vẫn là state cũ -> UI báo *"Unable to determine operation result. Rechecking..."* (Unresolved state).

### 6. Failure & Malformed Response Resilience (6+ tests - FE)
- API trả về JSON thiếu `currentStatus` -> FE không crash, StatusBadge hiển thị Unknown.
- 500/503 -> Không optimistic mutate, UI hiển thị Error Alert, giữ nguyên canonical data. Không cho phép blind-retry vì chưa biết server đã commit hay chưa.

---

## E2E Scenarios (8 Scenarios)

- **E2E-01:** Confirmed → Update → Pending_Update.
- **E2E-02:** Pending_Confirmation → Update shortcut.
- **E2E-03:** Confirmed → Revoke → Pending_Revocation.
- **E2E-04:** Pending_Confirmation → Revoke shortcut → Revoked.
- **E2E-05 (Conflict):** 409 state conflict → modal closes → refetch → buttons update.
- **E2E-06 (Reconciliation):** Timeout after server success → reconcile by GET → correct final UI.
- **E2E-07 (Unresolved/Stale GET):** Timeout after mutation → reconcile by GET (trả về old state) → UI hiển thị Unresolved Alert, disable buttons.
- **E2E-08 (Security/IDOR):** Unauthorized/cross-tenant direct mutation → denied, no data leak.

---

## 5 Câu hỏi bảo chứng Mutation (Survival Checklist)
Tất cả Work Packages đã được thiết kế thỏa mãn 5 câu hỏi:
1. Có thể chạy 2 lần không? *(Không, Idempotency-Key + Payload Hash block việc trùng lặp).*
2. Request timeout sau khi commit thì sao? *(FE kích hoạt Reconcile. Nếu GET lỗi/trả về state cũ -> Đưa vào Unresolved State).*
3. Actor khác thay đổi resource lúc đang ghi? *(BE chặn bằng Atomic RowVersion Concurrency + 409 State Transition).*
4. Server trả lỗi giữa chừng/malformed? *(UI catch error, fallback `Unknown`, không tạo state sai).*
5. Sau failure, hệ thống có xác định được canonical state? *(Có, refetch. Nếu refetch thất bại, UI báo rõ ràng tình trạng mất đồng bộ).*
