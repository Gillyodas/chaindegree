# Phase 1: Degree Issuance UI & Realtime Status — Implementation Plan (v2)

## Background & Goal

Phase 1 (US-1 / UC-1) triển khai hoàn chỉnh giao diện cấp bằng cho Registrar, kết nối API Issue Degrees, hiển thị danh sách bằng cấp với trạng thái real-time, và cung cấp cơ chế retry cho các bằng cấp bị lỗi xác thực.

**Git Branch:** `frontend/phase-1-degree-issuance` (checkout từ `main`)

**Expected Outcome:**
- Ứng dụng cung cấp form động để Registrar có thể cấp một hoặc nhiều bằng cấp cùng lúc.
- Danh sách bằng cấp hiển thị rõ ràng các trạng thái thông qua các badge màu sắc.
- Trạng thái bằng cấp được cập nhật tự động qua polling (SignalR Hub chưa tồn tại ở backend — chỉ chuẩn bị extension point).
- Các yêu cầu được gửi với `Idempotency-Key` để tránh trùng lặp.
- Người dùng có thể retry các bằng cấp gặp sự cố `Confirmation_Error`.

---

## Technical & Business Decisions

| # | Decision | Resolution | Rationale |
|---|---|---|---|
| 1 | **Idempotency-Key lifecycle** | Key được sinh **1 lần cho mỗi logical submission** (khi user nhấn Submit). Key được **giữ lại** trong lifecycle của request đó, bao gồm HTTP retry do network failure. Chỉ sinh key MỚI khi **data thay đổi** (user sửa row lỗi rồi submit lại). | BE `IdempotencyFilterAttribute` cache response theo key. Cùng key + cùng data = BE trả cached response (deduplicate). Key mới = BE coi là request mới. |
| 2 | **Idempotency invariant** | **Một Idempotency-Key không bao giờ được dùng cho hai logical submissions khác nhau.** Đây là security/behavior rule, không chỉ implementation detail. | Nếu vi phạm: key cũ trả cached response cũ cho data mới -> silent data corruption. |
| 3 | **Partial failure + retry** | Response `202 Accepted` luôn trả `{ acceptedCount, degreeIds[], failures[] }`. FE dùng `failures[].studentId + failures[].major + failures[].reason` để highlight đúng row lỗi. **Rows thành công bị xóa khỏi form, rows lỗi giữ lại.** Khi user sửa data và submit lại, sinh key MỚI vì data đã thay đổi. | `IssueDegreeFailureDto(StudentId, Major, Reason)` là key để FE match row lỗi. |
| 4 | **Hook/Form responsibility boundary** | **Hook** chịu trách nhiệm: API call, cache invalidation, HTTP error mapping. **Form** chịu trách nhiệm: business result interpretation (full/partial/failure), row manipulation (remove success, retain failed), inline errors, toast messages. | Partial failure là business result, không phải UI result. Hook trả nguyên result, Form quyết định cách hiển thị. |
| 5 | **Realtime update strategy** | **Polling only (Phase 1).** SignalR Hub chưa tồn tại ở backend. Chuẩn bị extension point nhưng không kết nối thật. Polling `refetchInterval: 5000` cho `useDegreesQuery` khi user đang ở trang list. | Sau khi backend implement Hub, FE chỉ cần bật hook và tắt polling. |
| 6 | **SignalR invalidation scope** | Khi SignalR sẵn sàng (phase sau), invalidate `degreeKeys.lists()` thay vì `degreeKeys.all`. Nếu event chứa `degreeId`, invalidate thêm `degreeKeys.detail(degreeId)` cụ thể. | Tránh invalidate quá rộng. |
| 7 | **Polling scope** | Polling chỉ áp dụng cho `useDegreesQuery` (list page). Không polling `batchStatus`. | `getBatchStatus` chỉ gọi on-demand. |
| 8 | **404 handling** | API layer throw `NotFoundError`. Component quyết định UI: List page 404 -> EmptyState. Detail page 404 -> "Degree not found". API layer KHÔNG biết UI semantics, KHÔNG tự fallback. | Nhất quán toàn feature. API layer chỉ truyền tải HTTP error type. |
| 9 | **Classification validation** | Backend `Classification` là `string` tự do. FE chỉ check `required` + `non-empty`. Không hardcode enum. | FE validation là UX, không phải security control. |
| 10 | **UUID generation** | `crypto.randomUUID()` — browser native API. Project target modern browsers only. Không cần polyfill, không cần uuid library. | Supported in all modern browsers (Chrome 92+, Firefox 95+, Safari 15.4+). |
| 11 | **Retry flow (Confirmation_Error)** | FE KHÔNG tự đổi status sau click Retry. Flow: POST retry -> Backend -> database -> polling refetch -> UI phản ánh server state mới. | UI chỉ phản ánh server state. Không optimistic update cho status transitions. |
| 12 | **Pagination** | Client-side only cho MVP. Không tạo abstraction (PaginationStrategy, PaginationAdapter). Khi backend có `?page=&pageSize=` thì chuyển trực tiếp. | KISS. |
| 13 | **SignalR reconnect/race condition** | Dùng `useRef` lưu `isSignalRConnected`. Debounce 300ms khi toggle polling/SignalR. Invalidate 1 lần khi reconnect để sync missed data. | Tránh 2 source cùng invalidate gây UI flash. |

---

## Constraints

- KHÔNG dùng lại Idempotency-Key cho logical submission khác (security invariant).
- KHÔNG để API layer tự fallback 404 -> empty array. API throw error, component quyết định UI.
- KHÔNG optimistic update status sau Retry. Chỉ phản ánh server state qua polling.
- KHÔNG kết nối SignalR Hub thật (chưa tồn tại ở BE). Chỉ chuẩn bị skeleton.
- KHÔNG dùng global ErrorBoundary để xử lý 404/500 HTTP errors.
- KHÔNG hardcode Classification enum values.
- KHÔNG coi frontend validation là security control.
- KHÔNG tạo pagination abstraction. Client-side simple cho MVP.
- Tất cả import từ `features/degree` phải qua file `index.ts`.
- 100% UI text và thông báo (toast) bằng tiếng Anh.
- Component UI chỉ render. Business logic trong hooks/helpers, UI logic trong components.

---

## Proposed Changes — Work Packages Chi Tiết

---

### WP-1.1: Feature `degree` — API Layer & Types

#### Prerequisite
- [ ] Verify backend response shape trước khi define types:
  - `IssueDegreeResponse`: `{ message, acceptedCount, degreeIds[], failures[] }` ([IssueDegreeResponse.cs](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Application/Degrees/Commands/IssueDegree/IssueDegreeResponse.cs))
  - `IssueDegreeFailureDto`: `{ studentId, major, reason }` ([IssueDegreeFailureDto.cs](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Application/Degrees/Commands/IssueDegree/IssueDegreeFailureDto.cs))
  - `StatusEnum` 7 values ([StatusEnum.cs](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Domain/Degrees/Enums/StatusEnum.cs))
  - `Classification` la `string` tu do
  - `IssueDegreeRequest`: `{ degrees: IssueDegreeItemRequest[] }` ([IssueDegreeRequest.cs](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.API/Contracts/Degrees/IssueDegreeRequest.cs))

#### Tasks
- [ ] Dinh nghia types tai `src/features/degree/degree.types.ts`:
  - Request types (khop backend contract):
    ```typescript
    type IssueDegreeItemRequest = {
      studentId: string; // UUID format
      major: string;
      classification: string; // free-text, NOT enum
      issuedAt: string; // ISO 8601 UTC
    };
    type IssueDegreeRequest = { degrees: IssueDegreeItemRequest[] };
    ```
  - Response types:
    ```typescript
    type IssueDegreeFailure = { studentId: string; major: string; reason: string };
    type IssueDegreeResponse = {
      message: string;
      acceptedCount: number;
      degreeIds: string[];
      failures: IssueDegreeFailure[];
    };
    ```
  - `DegreeListItem`, `DegreeDetail` cho list/detail pages.
  - `BatchStatusResponse` (mirror `BatchQueryResponse` tu BE).
- [ ] Khoi tao API Service tai `src/features/degree/degree.api.ts`:
  - `issueDegrees(data: IssueDegreeRequest, idempotencyKey: string)`:
    - POST `/api/v1/institutions/degrees`
    - Header: `Idempotency-Key: {idempotencyKey}`
    - 1 key per batch submit, khong per item
  - `getBatchStatus(batchId: string)` -> GET `.../batches/{batchId}` (on-demand).
  - `retryDegreeConfirmation(id: string)` -> POST `.../degrees/{id}/retry`.
  - `getDegrees()` -> GET `/api/v1/institutions/degrees`. Khong fallback. Throw error neu that bai.
  - `getDegree(id: string)` -> GET `.../degrees/{id}`. Khong fallback. Throw error neu that bai.
- [ ] Khoi tao Query Keys Factory tai `src/features/degree/degree.keys.ts`:
  ```typescript
  export const degreeKeys = {
    all: ['degrees'] as const,
    lists: () => [...degreeKeys.all, 'list'] as const,
    detail: (id: string) => [...degreeKeys.all, 'detail', id] as const,
    batchStatus: (batchId: string) => [...degreeKeys.all, 'batch', batchId] as const,
  };
  ```

#### Output
- [NEW] `src/features/degree/degree.types.ts`
- [NEW] `src/features/degree/degree.api.ts`
- [NEW] `src/features/degree/degree.keys.ts`

#### Done Criteria
- Types khop chinh xac voi backend DTO shapes.
- API methods goi dung URI, method, headers.
- API layer KHONG co fallback logic. Throw error khi that bai.
- `issueDegrees` gui `Idempotency-Key` header per batch request.

#### Commit
```text
feat(degree): define types, API service, and query key factory
```

---

### WP-1.2: Custom Hooks — Mutations & Queries

#### Tasks
- [ ] Tao hooks tai `src/features/degree/hooks/`:
  - **`useIssueDegreesMutation`**:
    - Nhan `idempotencyKey` tu caller (Form quyet dinh key, khong phai hook).
    - Goi `issueDegrees(data, idempotencyKey)`.
    - `onSuccess`: invalidate `degreeKeys.lists()`. **Tra nguyen `IssueDegreeResponse`** cho caller. KHONG toast, KHONG phan loai full/partial/failure trong hook.
    - `onError`: KHONG toast. Throw/return error cho caller xu ly.
    - Boundary: Hook = API call + cache invalidation + error mapping. Form = business result + UI.
  - **`useDegreesQuery`**:
    - Fetch list, kem `refetchInterval: 5000` (polling) chi khi o trang list VA SignalR chua connected.
    - Error: `NotFoundError` -> throw cho component. Component quyet dinh render EmptyState.
    - `ServerError` -> throw cho component. Component render ErrorState.
  - **`useDegreeDetailQuery(id)`**:
    - Fetch chi tiet degree, khong polling.
    - Error: throw cho component. Component quyet dinh UI (404 -> "Degree not found", 500 -> ErrorState).
  - **`useBatchStatusQuery(batchId)`**:
    - On-demand query, khong polling.
  - **`useRetryDegreeMutation`**:
    - Goi `retryDegreeConfirmation(id)`.
    - `onSuccess`: invalidate `degreeKeys.lists()` + `degreeKeys.detail(id)`. KHONG tu doi status. Polling se fetch server state moi.

#### Output
- [NEW] `src/features/degree/hooks/useIssueDegrees.ts`
- [NEW] `src/features/degree/hooks/useDegreeQueries.ts`

#### Done Criteria
- Hook tra nguyen business result, khong chua UI logic (toast, row manipulation).
- Retry khong optimistic update status. Chi invalidate cache, doi polling.
- Invalidate scope: `degreeKeys.lists()` (khong `degreeKeys.all`).

#### Commit
```text
feat(degree): implement issuance mutations and queries hooks
```

---

### WP-1.3: Degree Issuance Form (Core UI)

#### Tasks
- [ ] Tao Zod schema:
  ```typescript
  const issueDegreeItemSchema = z.object({
    studentId: z.string().uuid('Must be a valid UUID'),
    major: z.string().min(1, 'Major is required'),
    classification: z.string().min(1, 'Classification is required'), // free-text, NOT enum
    issuedAt: z.string().min(1, 'Issue date is required'),
  });
  const issueDegreeFormSchema = z.object({
    degrees: z.array(issueDegreeItemSchema).min(1, 'At least one degree is required'),
  });
  ```
- [ ] Xay dung Form Component:
  - React Hook Form `useFieldArray` cho dynamic rows.
  - Moi row: StudentId, Major, Classification, IssuedAt.
  - Nut `[+ Add Degree]`, nut `[Remove]` per row.
  - Submit button disabled khi mutation `isPending`.
- [ ] **Idempotency-Key lifecycle** (managed by Form, not hook):
  ```typescript
  // Key duoc sinh 1 lan khi bat dau logical submission
  const idempotencyKeyRef = useRef<string | null>(null);

  function handleSubmit(data) {
    // Sinh key MOI cho moi logical submission moi
    idempotencyKeyRef.current = crypto.randomUUID();
    mutation.mutate({ data, key: idempotencyKeyRef.current });
  }

  // Neu mutation fail do network va TanStack Query retry:
  // mutation.mutate() duoc goi lai voi CUNG key (tu ref)
  // -> Backend deduplicate

  // Neu user sua data roi submit lai:
  // handleSubmit() duoc goi lai -> sinh key MOI
  // -> Backend coi la request moi
  ```
- [ ] **Business result handling** (Form chiu trach nhiem):
  - Nhan `IssueDegreeResponse` tu hook.
  - `failures.length === 0`: toast success, clear form.
  - `failures.length > 0 && acceptedCount > 0`: toast warning partial. Match `failures[i]` voi form rows bang `studentId + major`. Remove rows thanh cong, giu rows loi voi inline error do.
  - `acceptedCount === 0`: toast error "All degree issuance requests were rejected."
- [ ] **Double-submit protection**: Disable submit button khi `isPending`. Disabled button la UX. Idempotency-Key la safety net (cung key -> cung response).

#### Output
- [NEW] `src/features/degree/components/IssueDegreeForm.tsx`

#### Done Criteria
- Idempotency-Key sinh 1 lan per logical submission, giu lai cho HTTP retry.
- Key moi chi khi user thay doi data va submit lai.
- Form xu ly business result (toast, row manipulation), khong phai hook.
- Double submit: disabled button (UX) + same key (safety).
- Classification chi validate required, khong enum.

#### Commit
```text
feat(degree): build dynamic degree issuance form with Zod validation
```

---

### WP-1.4: Degree List Page

#### Tasks
- [ ] Xay dung Page tai `src/features/degree/pages/DegreeListPage.tsx`:
  - Fetch qua `useDegreesQuery` (polling `refetchInterval: 5000`).
  - Table: DegreeCode, StudentName, Major, Classification, Status, IssuedAt, Actions.
  - `StatusBadge` cho moi row.
  - Link row -> `/degrees/:id`.
- [ ] Nut `[Retry]`:
  - Chi render khi `status === 'Confirmation_Error'`.
  - Goi `useRetryDegreeMutation(id)`.
  - Disabled khi `isPending`.
  - Sau click: KHONG tu doi status. Doi polling refetch server state.
- [ ] States (component quyet dinh UI tu error type):
  - Loading: skeleton table.
  - `NotFoundError` -> `EmptyState title="No degrees found"`.
  - `ServerError` / `TimeoutError` / `NetworkError` -> `ErrorState onRetry={refetch}`.
  - KHONG lan 500 thanh empty.
- [ ] Pagination: client-side don gian. Khong abstraction.

#### Output
- [NEW] `src/features/degree/pages/DegreeListPage.tsx`

#### Done Criteria
- Status badges dung mau cho 3 statuses Phase 1.
- Retry button khong optimistic update. Chi invalidate + doi polling.
- 500 -> ErrorState, 404 -> EmptyState. Khong lan.
- Client-side pagination don gian.

#### Commit
```text
feat(degree): create degree list page with status badges
```

---

### WP-1.5: Degree Detail Page (Skeleton)

#### Tasks
- [ ] Tao Page tai `src/features/degree/pages/DegreeDetailPage.tsx`:
  - `useParams().id` -> `useDegreeDetailQuery(id)`.
  - 404 handling tai component level:
    ```tsx
    if (error instanceof NotFoundError) {
      return <EmptyState title="Degree not found" description="This degree may have been removed." />;
    }
    if (error) {
      return <ErrorState title="Failed to load degree" onRetry={refetch} />;
    }
    ```
  - Render: DegreeCode, StudentId, Major, Classification, Status, IssuedAt, CreatedAt.
  - Skeleton loading.
- [ ] Placeholder buttons (disabled):
  - `[Update]` -> "Available in Phase 2"
  - `[Revoke]` -> "Available in Phase 2"
  - `[Report Issue]` -> "Available in Phase 4"
- [ ] Link "Back to Degrees".

#### Output
- [NEW] `src/features/degree/pages/DegreeDetailPage.tsx`

#### Done Criteria
- 404 -> "Degree not found" tai page level. Khong crash, khong ErrorBoundary.
- 500 -> ErrorState voi retry.

#### Commit
```text
feat(degree): add degree detail page skeleton
```

---

### WP-1.6: SignalR Extension Point & Polling Integration

> **Backend chua co SignalR Hub.** WP nay chi chuan bi extension point. Polling la co che realtime chinh cho Phase 1.

#### Tasks
- [ ] Tao SignalR connection helper tai `src/shared/lib/signalr.ts`:
  - Wrapper cho `@microsoft/signalr` HubConnectionBuilder.
  - Config: URL tu `env.signalrUrl`, auto-reconnect.
  - Export `createSignalRConnection(hubUrl)`.
  - Khong ket noi tu dong.
- [ ] Tao hook skeleton tai `src/features/degree/hooks/useSignalRDegreeStatus.ts`:
  ```typescript
  /**
   * Extension point for SignalR realtime degree status updates.
   * Currently a no-op. Will be activated when backend implements SignalR Hub.
   *
   * When activated:
   * 1. Connect to Hub at env.signalrUrl
   * 2. Listen for 'DegreeStatusUpdated' events
   * 3. On event: invalidate degreeKeys.lists() + degreeKeys.detail(degreeId)
   * 4. Set isConnected -> true, disables polling
   * 5. On disconnect: isConnected -> false, polling resumes
   * 6. On reconnect: debounce 300ms, invalidate, isConnected -> true
   */
  export function useSignalRDegreeStatus() {
    return { isConnected: false };
  }
  ```
- [ ] Tich hop polling trong `useDegreesQuery`:
  - `refetchInterval: isSignalRConnected ? false : 5000`
  - Phase 1: luon polling.

#### Output
- [NEW] `src/shared/lib/signalr.ts`
- [NEW] `src/features/degree/hooks/useSignalRDegreeStatus.ts`
- [MODIFY] `src/features/degree/hooks/useDegreeQueries.ts`

#### Commit
```text
feat(degree): add SignalR extension point and polling-based realtime updates
```

---

### WP-1.7: Update feature barrel export & router

#### Tasks
- [ ] Cap nhat `src/features/degree/index.ts` barrel export.
- [ ] Cap nhat `src/app/router/AppRouter.tsx`:
  - `/degrees` -> `DegreeListPage` (lazy, Registrar).
  - `/degrees/issue` -> `IssueDegreeForm` page (lazy, Registrar).
  - `/degrees/:id` -> `DegreeDetailPage` (lazy, Registrar).
- [ ] Cap nhat Sidebar cho role Registrar.

#### Output
- [MODIFY] `src/features/degree/index.ts`
- [MODIFY] `src/app/router/AppRouter.tsx`
- [MODIFY] `src/app/layouts/DashboardLayout.tsx`

#### Commit
```text
feat(degree): wire degree pages into router and sidebar navigation
```

---

## Testing Plan

### Unit Tests

| # | Test File | Test Target | Test Cases | Tool |
|---|---|---|---|---|
| 1 | `degree.api.test.ts` | `degree.api.ts` | Mock Axios, verify URL, method, `Idempotency-Key` header gui per batch | Vitest |
| 2 | `degree.api.test.ts` | `getDegrees` | 404 -> throw `NotFoundError` (KHONG fallback empty array) | Vitest |
| 3 | `degree.keys.test.ts` | `degree.keys.ts` | `lists()` khac `detail(id)`, cau truc array nhat quan | Vitest |
| 4 | `useIssueDegrees.test.ts` | `useIssueDegreesMutation` | Success: invalidate `lists()`, tra nguyen `IssueDegreeResponse` | Vitest |
| 5 | `useIssueDegrees.test.ts` | `useIssueDegreesMutation` | Hook KHONG toast. KHONG phan loai partial/full. Chi tra result | Vitest |
| 6 | `useIssueDegrees.test.ts` | `useIssueDegreesMutation` | HTTP error -> throw, KHONG toast trong hook | Vitest |
| 7 | `IssueDegreeForm.test.tsx` | Idempotency-Key | **Test A**: new submission -> key A. **Test B**: same submission HTTP retry -> same key A. **Test C**: data changed + submit -> key B (khac A) | Vitest + RTL |
| 8 | `IssueDegreeForm.test.tsx` | Business result | Full success (failures=[]) -> toast success, form cleared | Vitest + RTL |
| 9 | `IssueDegreeForm.test.tsx` | Business result | Partial failure -> toast warning, match failures to rows, remove success, retain failed with inline error | Vitest + RTL |
| 10 | `IssueDegreeForm.test.tsx` | Business result | Full failure (acceptedCount=0) -> toast error "All rejected" | Vitest + RTL |
| 11 | `IssueDegreeForm.test.tsx` | Double submit | Submit button disabled khi `isPending` | Vitest + RTL |
| 12 | `IssueDegreeForm.test.tsx` | Zod schema | Valid UUID accepted, invalid UUID rejected | Vitest + RTL |
| 13 | `IssueDegreeForm.test.tsx` | Zod schema | `classification` chi check required, khong check enum | Vitest + RTL |
| 14 | `useDegreeQueries.test.ts` | `useDegreeDetailQuery` | 404 -> `NotFoundError` (component render "Degree not found") | Vitest |
| 15 | `useDegreeQueries.test.ts` | `useDegreeDetailQuery` | 500 -> `ServerError` (component render ErrorState) | Vitest |
| 16 | `useDegreeQueries.test.ts` | `useDegreesQuery` | Polling: `refetchInterval` = 5000 khi `isSignalRConnected = false` | Vitest |
| 17 | `useDegreeQueries.test.ts` | `useDegreesQuery` | Stale cache: sau invalidate, data moi duoc fetch | Vitest |
| 18 | `DegreeListPage.test.tsx` | Retry button | Chi hien khi `Confirmation_Error`. KHONG optimistic update status. Goi API + invalidate + doi polling | Vitest + RTL |
| 19 | `DegreeListPage.test.tsx` | Error states | 500 -> ErrorState, empty -> EmptyState. Khong lan | Vitest + RTL |
| 20 | `DegreeDetailPage.test.tsx` | 404 | Render "Degree not found". Khong crash | Vitest + RTL |

### Integration Test (E2E)

> Su dung MSW (Mock Service Worker). Dung **fake timers** (`vi.useFakeTimers()` / `vi.advanceTimersByTime(5000)`) thay vi doi that 5 giay.

#### Scenario 1: Cap bang thanh cong (happy path)
1. Mo trang `/degrees/issue` (role Registrar).
2. Them 2 rows, nhap data hop le.
3. Submit -> intercept `POST /degrees`, verify body shape va `Idempotency-Key` header.
4. Mock response: `{ acceptedCount: 2, degreeIds: [...], failures: [] }`.
5. Verify: Toast success, form cleared.
6. Navigate `/degrees` -> verify rows moi voi badge Pending.

#### Scenario 2: Partial failure + retry
1. Submit 3 degrees. Mock: 2 accepted, 1 failure.
2. Verify: Toast warning, 2 rows removed, 1 row giu lai voi inline error.
3. User sua data row loi, submit lai.
4. Verify: Request thu 2 co **Idempotency-Key KHAC** request thu 1 (data da thay doi).
5. Mock response 2: all accepted.
6. Verify: Toast success, form cleared.

#### Scenario 3: Polling updates status
1. Mo `/degrees`, mock initial data voi 2 degrees `Pending_Confirmation`.
2. `vi.advanceTimersByTime(5000)` -> mock response moi: 1 `Confirmed`, 1 `Confirmation_Error`.
3. Verify: Badge doi mau. Khong F5.
4. Click `[Retry]` tren `Confirmation_Error`.
5. Verify: API call `POST .../retry`. KHONG optimistic update. Sau polling refetch -> badge chuyen lai `Pending_Confirmation`.

#### Scenario 4: Detail page 404
1. Navigate `/degrees/non-existent-id`.
2. Mock 404.
3. Verify: "Degree not found". Khong crash, khong ErrorBoundary fallback.

#### Scenario 5: Idempotency duplicate submission
1. Submit batch, capture `Idempotency-Key = X`.
2. Simulate network retry (TanStack Query retry voi cung mutation args).
3. Verify: request thu 2 gui cung key X.
4. Mock backend tra cached response (cung `degreeIds`, cung `failures`).
5. Verify: FE khong tao duplicate degree rows. UI state giong het lan 1.

#### Commit
```text
test(degree): add unit and integration tests for degree issuance flow
```

---

## Done Criteria Tong Hop Phase 1

- [ ] `npm run dev` chay thanh cong, tat ca pages render dung.
- [ ] `npm run build` thanh cong khong loi TypeScript.
- [ ] `npm run lint` khong error.
- [ ] `npm run test` — tat ca tests pass.
- [ ] Registrar co the mo form, them nhieu bang, submit thanh cong.
- [ ] Form validate client-side: UUID format, required fields (classification khong validate enum).
- [ ] Submit gui dung API voi `Idempotency-Key` per logical submission (khong per item, khong random moi retry).
- [ ] Partial failure: rows loi giu lai + inline error, rows thanh cong bi xoa. Submit lai = key moi.
- [ ] Toast messages xu ly boi Form component, khong phai hook.
- [ ] Danh sach bang cap hien thi dung status badge colors.
- [ ] Retry khong optimistic update. Chi invalidate + doi polling.
- [ ] Polling 5s cap nhat list tu dong (SignalR extension point san sang nhung no-op).
- [ ] Detail page xu ly 404 tai component level.
- [ ] Loading/Empty/Error states xu ly rieng biet. Khong gop 500 thanh empty.
- [ ] API layer khong co fallback logic. Throw error, component quyet dinh UI.
- [ ] Double-submit: disabled button (UX) + same idempotency key (safety).
- [ ] E2E tests dung fake timers, khong doi that.

---

## Commits Summary

```text
feat(degree): define types, API service, and query key factory
feat(degree): implement issuance mutations and queries hooks
feat(degree): build dynamic degree issuance form with Zod validation
feat(degree): create degree list page with status badges
feat(degree): add degree detail page skeleton
feat(degree): add SignalR extension point and polling-based realtime updates
feat(degree): wire degree pages into router and sidebar navigation
test(degree): add unit and integration tests for degree issuance flow
```
