# Phase 0: Project Foundation, Toolchain & Complete Mock Auth UI — Implementation Plan (v2)

## Background & Goal

Phase 0 là nền tảng kỹ thuật hoàn chỉnh cho toàn bộ Frontend SPA của hệ thống ChainDegree. Sau phase này, mọi phase tiếp theo (Phase 1–7) có thể bắt tay viết feature ngay mà không lo hạ tầng.

**Git Branch:** `frontend/phase-0-foundation` (checkout từ `main`)

**Expected Outcome:**
- Dự án Vite + React + TypeScript chạy được tại `http://localhost:3000`
- Toolchain hoàn chỉnh: ESLint + Prettier + EditorConfig
- TailwindCSS v4 + shadcn/ui design system với base components
- Feature-based directory structure (chỉ tạo thư mục khi cần, không .gitkeep)
- Shared infrastructure: HTTP client (không có auth interceptor), error mapper, notification service, date utils, shared types
- Complete Mock Auth UI với Role Switcher cho 4 roles (Registrar, Student, Recruiter, Admin)
- App Shell: DashboardLayout + PublicLayout + Router + ProtectedRoute + ErrorBoundary
- Shared UI components: StatusBadge, LoadingSpinner, EmptyState, ConfirmDialog, FileUpload
- Unit tests cho tất cả shared utilities và auth guard
- 100% UI text bằng tiếng Anh
- Security Baseline documentation

---

## Technical & Business Decisions

| Decision | Resolution |
|---|---|
| **Framework** | React 19 + TypeScript + Vite — theo [Coding-Standards §1](file:///e:/codes/chaindegree/docs/Coding-Standards.md) |
| **Routing** | React Router v7 — mature, nested routes, lazy loading |
| **HTTP Client** | Axios singleton — Phase 0 chỉ có baseURL, timeout, error handling. **Không** có Authorization interceptor ở Phase 0. Chuẩn bị `TokenProvider` extension point để Phase auth thật plug vào sau |
| **UI Library** | TailwindCSS v4 + shadcn/ui — CSS-first config ([Coding-Standards §24](file:///e:/codes/chaindegree/docs/Coding-Standards.md)) |
| **Toast** | Sonner — lightweight, modern, shadcn/ui integrated |
| **State** | React Context API only (Auth, Theme, Sidebar) — no Redux/Zustand ([Coding-Standards §Mức 7](file:///e:/codes/chaindegree/docs/Coding-Standards.md)) |
| **Testing** | Vitest + React Testing Library — đồng bộ Vite ecosystem |
| **Path alias** | `@/` → `src/` |
| **Auth approach** | MockAuthProvider only. Không tạo RealAuthProvider placeholder — chưa biết real auth là JWT/Cookie/OAuth/IdentityServer. Khi có auth thật sẽ implement đúng contract lúc đó |
| **Token storage** | Không có ở Phase 0 — mock auth không dùng token |
| **Tailwind v4 config** | CSS-first (`@import "tailwindcss"` trong CSS, không có `tailwind.config.ts`) |
| **Font** | Inter từ Google Fonts |
| **Lazy loading** | Chỉ lazy import page-level components, layouts load eagerly |
| **ErrorBoundary scope** | Chỉ bắt React render errors, KHÔNG bắt Axios/API errors |
| **Error mapping** | HTTP layer xác định error type (`NotFoundError`, `ServerError`, etc.), **component quyết định UI state** (empty/error/retry). Không map 404 global thành empty state |
| **Date timezone** | Backend stores/returns UTC. Frontend displays user's local timezone via `Intl.DateTimeFormat` |
| **Env validation** | Required vars fail-fast (throw Error). Optional vars may fallback |
| **Barrel exports** | Chỉ ở feature level (`features/<name>/index.ts`). Sub-folders bên trong feature dùng relative import bình thường |
| **File validation** | Frontend file validation (type, size) chỉ là UX. Backend must revalidate type, size, content, authorization |

---

## Constraints

- ❌ KHÔNG implement bất kỳ nghiệp vụ feature nào ngoài Auth UI temp data
- ❌ KHÔNG kết nối API thật cho Auth — dùng temp mock data với Role Switcher
- ❌ KHÔNG tạo RealAuthProvider placeholder — chưa biết auth contract thật
- ❌ KHÔNG implement Authorization interceptor ở Phase 0
- ❌ KHÔNG dùng Redux/Zustand/XState — Context API only
- ❌ KHÔNG premature optimization (`useMemo`, `useCallback` khi chưa cần)
- ❌ KHÔNG map 404 global thành empty state — component quyết định UI
- ❌ KHÔNG đặt secrets trong VITE_* variables
- ✅ Tất cả lazy routes trỏ tới placeholder pages với text *"Coming Soon"*
- ✅ 100% UI text phải là **English**
- ✅ Feature boundary: chỉ import qua feature `index.ts`, không import nội bộ cross-feature
- ✅ Shared layer KHÔNG depend on feature/app layer
- ✅ Commit theo conventional commits: `feat:`, `chore:`, `test:`

---

## Security Baseline

Các rule bảo mật nền tảng cho Phase 0 trở đi:

- **Never store secrets in `VITE_*` variables.** Vite expose `VITE_*` vào frontend bundle. Tuyệt đối không: `VITE_JWT_SECRET`, `VITE_API_KEY`, `VITE_CLIENT_SECRET`, `VITE_PRIVATE_KEY`.
- **Never trust frontend role/permission for authorization.** Route Guard ở FE chỉ làm UX. Backend luôn enforce quyền.
- **Never use `dangerouslySetInnerHTML`** unless explicitly justified.
- **Frontend file validation is UX only.** Backend must revalidate file type, size, content, and authorization.
- **Never log access tokens, cookies, Authorization headers, or sensitive payloads.** Kể cả trong interceptors.
- **Do not persist authentication tokens** unless the real auth architecture explicitly requires it.
- **Do not expose internal server errors or stack traces to users.** Error mapper phải translate.
- **Do not put private keys/API secrets in frontend source or bundle.**

---

## Proposed Changes — Work Packages Chi Tiết

---

### WP-0.0: Branch Setup

#### Tasks
- [x] Checkout nhánh mới `frontend/phase-0-foundation` từ `main`
- [x] Commit cập nhật frontend-implementation-plan.md (branch strategy → `main`)

#### Done Criteria
- `git branch` hiển thị nhánh `frontend/phase-0-foundation` là active branch

> [!NOTE]
> Đã hoàn thành. Commit: `docs(frontend): update frontend implementation plan branch strategy to checkout from main`

---

### WP-0.1: Scaffold Vite + React + TypeScript Project

#### Tasks
- [ ] Xóa file `apps/frontend/index.html` hiện tại (boilerplate cũ)
- [ ] Khởi tạo dự án React + TypeScript: `npx -y create-vite@latest ./ --template react-ts` tại `apps/frontend/`
- [ ] Xóa boilerplate mặc định: `App.css`, `src/assets/react.svg`, `public/vite.svg`, counter demo trong `App.tsx`
- [ ] Cấu hình `tsconfig.json` với `strict: true`, path alias `@/*` → `src/*` trong `tsconfig.app.json`
- [ ] Cấu hình `vite.config.ts`: resolve alias `@` → `src`, dev server port `3000`
- [ ] Tạo `.env.example` với 5 env vars:
  ```env
  VITE_APP_NAME=ChainDegree
  VITE_API_BASE_URL=http://localhost:5000
  VITE_API_TIMEOUT=10000
  VITE_SIGNALR_URL=http://localhost:5000/hubs/degree-status
  VITE_REPUTATION_ENABLED=true
  ```
- [ ] Tạo `src/app/config/env.ts` — typed env reader với **fail-fast validation**:
  ```typescript
  // --- Required variables: fail-fast if missing ---
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL;
  if (!apiBaseUrl) {
    throw new Error('Missing required env: VITE_API_BASE_URL');
  }

  const apiTimeoutRaw = Number(import.meta.env.VITE_API_TIMEOUT);
  if (!Number.isFinite(apiTimeoutRaw) || apiTimeoutRaw <= 0) {
    throw new Error('VITE_API_TIMEOUT must be a positive number');
  }

  // --- Optional variables: may fallback ---
  export const env = {
    appName: import.meta.env.VITE_APP_NAME ?? 'ChainDegree',
    apiBaseUrl,
    apiTimeout: apiTimeoutRaw,
    signalrUrl: import.meta.env.VITE_SIGNALR_URL ?? '',
    reputationEnabled: import.meta.env.VITE_REPUTATION_ENABLED === 'true',
  } as const;
  ```
- [ ] Cập nhật `.gitignore` cho Vite project (node_modules, dist, .env.local, etc.)
- [ ] Verify `npm run dev` chạy thành công tại `http://localhost:3000`
- [ ] Verify `npm run build` thành công

#### Output
- [DELETE] `apps/frontend/index.html` (old static — replaced by Vite's index.html)
- [NEW] `apps/frontend/vite.config.ts`
- [NEW] `apps/frontend/tsconfig.json`, `tsconfig.app.json`, `tsconfig.node.json`
- [NEW] `apps/frontend/.env.example`
- [NEW] `apps/frontend/src/app/config/env.ts`
- [NEW] `apps/frontend/src/main.tsx` (clean)
- [NEW] `apps/frontend/src/App.tsx` (minimal placeholder)

#### Done Criteria
- `npm run dev` → `http://localhost:3000` hiển thị trang
- `npm run build` thành công không lỗi TypeScript
- Path alias `@/` resolve đúng trong cả IDE và build
- `.env.example` có đủ 5 env vars
- Missing `VITE_API_BASE_URL` → app crashes with clear error message
- Invalid `VITE_API_TIMEOUT` → app crashes with clear error message

#### Commit
```
feat(frontend): scaffold Vite + React + TypeScript project
```

---

### WP-0.2: Configure ESLint, Prettier & EditorConfig

#### Tasks
- [ ] Cài đặt dev dependencies:
  ```
  eslint @eslint/js typescript-eslint eslint-plugin-react-hooks eslint-plugin-react-refresh
  prettier
  ```
- [ ] Cấu hình ESLint (flat config `eslint.config.js`):
  - Extends: `eslint:recommended`, `@typescript-eslint/recommended`, `react-hooks/recommended`
  - Rules: `no-console: warn`, `@typescript-eslint/no-unused-vars: error`
  - Ignore patterns: `dist/`, `node_modules/`
- [ ] Tạo `.prettierrc`:
  ```json
  {
    "semi": true,
    "singleQuote": true,
    "trailingComma": "all",
    "printWidth": 100,
    "tabWidth": 2
  }
  ```
- [ ] Tạo `.prettierignore`: `dist/`, `node_modules/`, `*.min.js`
- [ ] Tạo `.editorconfig`:
  ```ini
  root = true
  [*]
  charset = utf-8
  end_of_line = lf
  indent_style = space
  indent_size = 2
  insert_final_newline = true
  trim_trailing_whitespace = true
  ```
- [ ] Thêm scripts vào `package.json`: `lint`, `lint:fix`, `format`
- [ ] Chạy `npm run lint` — verify 0 errors
- [ ] Chạy `npm run format` — verify formatting applied

#### Output
- [NEW] `apps/frontend/eslint.config.js`
- [NEW] `apps/frontend/.prettierrc`
- [NEW] `apps/frontend/.prettierignore`
- [NEW] `apps/frontend/.editorconfig`
- [MODIFY] `apps/frontend/package.json` — thêm scripts

#### Done Criteria
- `npm run lint` → 0 errors
- `npm run format` → formats all files without errors
- `.editorconfig` present với UTF-8, LF, indent 2

#### Commit
```
chore(frontend): configure ESLint, Prettier, EditorConfig
```

---

### WP-0.3: Setup TailwindCSS v4 + shadcn/ui Design System

#### Tasks
- [ ] Cài đặt TailwindCSS v4: `npm install tailwindcss @tailwindcss/vite`
- [ ] Cấu hình Vite plugin trong `vite.config.ts`: thêm `tailwindcss()` to plugins
- [ ] Cấu hình CSS-first Tailwind trong `src/index.css`: `@import "tailwindcss";`
- [ ] Cài đặt shadcn/ui CLI: `npx -y shadcn@latest init` (Style: New York, Base color: Neutral, CSS variables: Yes)
- [ ] Thêm Google Fonts (Inter) vào `index.html`
- [ ] Thiết lập theme tokens trong CSS variables (colors, border-radius, font-family)
- [ ] Cài đặt base shadcn/ui components (vào `src/shared/components/ui/`):
  ```
  npx shadcn@latest add button input card dialog select table badge textarea tabs dropdown-menu sonner
  ```
- [ ] Verify components render đúng

#### Output
- [MODIFY] `apps/frontend/vite.config.ts` — thêm Tailwind plugin
- [MODIFY] `apps/frontend/src/index.css` — Tailwind v4 CSS-first config + theme tokens
- [MODIFY] `apps/frontend/index.html` — thêm Google Fonts
- [NEW] `apps/frontend/components.json` — shadcn/ui config
- [NEW] `apps/frontend/src/shared/components/ui/*.tsx` — shadcn components
- [NEW] `apps/frontend/src/shared/lib/utils.ts` — cn() utility

#### Done Criteria
- TailwindCSS classes render đúng trong browser
- shadcn/ui Button, Card, Dialog render đúng với Inter font
- Theme tokens customized cho ChainDegree
- `npm run build` thành công

#### Commit
```
feat(frontend): setup TailwindCSS v4 + shadcn/ui design system
```

---

### WP-0.4: Establish Feature-Based Directory Structure

#### Tasks
- [ ] Tạo cấu trúc thư mục — **chỉ tạo thư mục có file thực tế** (không .gitkeep):
  ```
  src/
  ├── app/
  │   ├── router/
  │   ├── providers/
  │   ├── config/        (đã có env.ts)
  │   └── layouts/
  ├── features/
  │   ├── auth/
  │   │   ├── components/
  │   │   ├── pages/
  │   │   └── index.ts
  │   ├── degree/
  │   │   ├── pages/
  │   │   └── index.ts
  │   ├── verification/
  │   │   ├── pages/
  │   │   └── index.ts
  │   ├── report/
  │   │   ├── pages/
  │   │   └── index.ts
  │   ├── reputation/
  │   │   ├── pages/
  │   │   └── index.ts
  │   └── recruitment/
  │       ├── pages/
  │       └── index.ts
  ├── shared/
  │   ├── api/
  │   ├── components/
  │   │   └── ui/        (đã có ở WP-0.3)
  │   ├── lib/
  │   ├── types/
  │   └── services/
  └── assets/
  ```
- [ ] Tạo `index.ts` barrel export chỉ ở **feature level**:
  ```typescript
  // features/degree/index.ts
  // Public API — export only what other features/app need
  export { ComingSoonPage as DegreeComingSoonPage } from './pages/ComingSoonPage';
  ```
  > Sub-folders bên trong feature dùng relative import (`./components/DegreeCard`) — không cần barrel cho mỗi sub-folder
- [ ] Tạo placeholder `ComingSoonPage.tsx` cho mỗi feature:
  ```tsx
  export function ComingSoonPage() {
    return (
      <div className="flex items-center justify-center h-full">
        <h1 className="text-2xl text-muted-foreground">Coming Soon</h1>
      </div>
    );
  }
  ```

#### Output
- [NEW] Directory structure (chỉ thư mục có file)
- [NEW] `features/<name>/index.ts` barrel export per feature
- [NEW] `features/<name>/pages/ComingSoonPage.tsx` placeholder per feature

#### Done Criteria
- Cấu trúc thư mục đúng Target Directory Structure
- Mỗi feature có `index.ts` barrel export
- Không có .gitkeep files
- Placeholder pages sẵn sàng cho router

#### Commit
```
chore(frontend): establish feature-based directory structure
```

---

### WP-0.5: Implement Shared Infrastructure

#### Prerequisite Task
- [ ] **Verify frontend shared types against actual Backend DTOs** trước khi implement `api.types.ts`. Kiểm tra:
  - Backend response shape: trả `{ data: T, message: string }` hay trả trực tiếp object?
  - `DegreeStatus` 7 giá trị khớp với [DegreeStatus enum trong Domain](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Domain/Degrees/Enums)
  - Nếu backend trả trực tiếp object → không tạo `ApiResult<T>` wrapper

#### Tasks

##### 0.5.1 — HTTP Client (`src/shared/api/http.ts`)
- [ ] Cài đặt axios: `npm install axios`
- [ ] Tạo Axios singleton instance:
  - Base URL từ `env.apiBaseUrl`
  - Timeout từ `env.apiTimeout`
  - **TokenProvider extension point** (Phase 0 không đăng ký provider):
    ```typescript
    type TokenProvider = () => string | null;

    let getAccessToken: TokenProvider = () => null;

    export function configureHttpAuth(tokenProvider: TokenProvider) {
      getAccessToken = tokenProvider;
    }
    ```
  - Request interceptor:
    ```typescript
    const token = getAccessToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    // ⚠️ KHÔNG console.log(config) — tránh lộ token
    ```
    > Phase 0: `getAccessToken` luôn trả `null` → không có Authorization header. Phase auth thật sẽ gọi `configureHttpAuth(...)` để plug vào.
  - Response interceptor phân loại lỗi:
    - `401` → redirect `/login`
    - `403` → throw `ForbiddenError`
    - `404` → throw `NotFoundError`
    - `409` → throw `ConflictError`
    - `422` → throw `ValidationError`
    - `500` → throw `ServerError`
    - `timeout` → throw `TimeoutError`
    - `network error` (no response) → throw `NetworkError`
    - `cancelled` (AbortController) → silent ignore
- [ ] Export discriminated union `HttpError`:
  ```typescript
  export type HttpErrorType =
    | 'not_found'
    | 'forbidden'
    | 'conflict'
    | 'validation'
    | 'server_error'
    | 'timeout'
    | 'network'
    | 'unauthorized';
  ```

> [!IMPORTANT]
> **Boundary rule**: `shared/api/http.ts` KHÔNG import từ `app/` hoặc `features/`. Auth layer sẽ gọi `configureHttpAuth()` để đăng ký token provider — dependency đi từ app → shared, không ngược lại.

##### 0.5.2 — API Error Mapper (`src/shared/api/error-mapper.ts`)
- [ ] **Không** map 404 thành 'empty'. HTTP layer chỉ xác định error type:
  ```typescript
  // HTTP layer identifies error type
  type HttpErrorType =
    | 'not_found'    // Component decides: empty state OR "not found" message
    | 'forbidden'
    | 'conflict'
    | 'validation'
    | 'server_error'
    | 'timeout'
    | 'network'
    | 'unauthorized';
  ```
  > Component/feature quyết định UI state dựa trên context:
  > - `GET /degrees` + `NotFoundError` → DegreeListPage → EmptyState *"No degrees found"*
  > - `GET /degrees/123` + `NotFoundError` → DegreeDetailPage → NotFoundState *"Degree not found"*
  > - `POST /degrees` + `NotFoundError` → Error, không phải empty

- [ ] Business error codes → English user-friendly messages:
  ```typescript
  const errorMessages: Record<string, string> = {
    DEGREE_ALREADY_EXISTS: 'A degree with identical details has already been issued for this student.',
    CRYPTO_HASH_MISMATCH: 'Verification failed. The provided data does not match official records.',
    BLOCKCHAIN_INVALID: 'Blockchain verification failed. Data integrity cannot be confirmed.',
    DEGREE_NOT_FOUND: 'No degree found with the specified code.',
    UNSUPPORTED_VERSION: 'The specified degree version is not supported.',
    FILTER_CRITERIA_NOT_SATISFIED: 'Your degree does not meet the minimum requirements for this position.',
    'Report.EvidenceRequired': 'Evidence file is required when submitting a report.',
  };
  ```
- [ ] **Phân biệt** `500` vs `timeout` vs `network` bằng message riêng:
  - `500` → *"Something went wrong on our end. Please try again later."*
  - `timeout` → *"Request timed out. Please check your connection and try again."*
  - `network` → *"Unable to connect to the server. Please check your internet connection."*
- [ ] Export helper: `getErrorMessage(error: HttpError): string` — trả message phù hợp
- [ ] Export helper: `getBusinessErrorMessage(errorCode: string): string` — trả business message hoặc generic fallback

##### 0.5.3 — Notification Service (`src/shared/services/notification.service.ts`)
- [ ] Wrapper trên Sonner (~10 dòng, không abstract thêm):
  ```typescript
  import { toast } from 'sonner';

  export const notification = {
    success: (message: string) => toast.success(message),
    error: (message: string) => toast.error(message),
    warning: (message: string) => toast.warning(message),
    info: (message: string) => toast.info(message),
  };
  ```

##### 0.5.4 — Date Utils (`src/shared/lib/date.ts`)
- [ ] `formatDate(date: string | Date): string` — English locale (e.g., "Aug 7, 2026")
- [ ] `formatDateTime(date: string | Date): string` — English locale with time (e.g., "Aug 7, 2026, 11:10 AM")
- [ ] `formatRelativeTime(date: string | Date): string` — Relative (e.g., "3 hours ago")
- [ ] Sử dụng `Intl.DateTimeFormat` và `Intl.RelativeTimeFormat` — no external dependency
- [ ] **Timezone convention**: Backend returns UTC. Functions convert to user's local timezone for display via `Intl` API (browser handles timezone automatically).

##### 0.5.5 — Shared Types (`src/shared/types/api.types.ts`)
- [ ] Verify against actual backend DTOs (prerequisite task above)
- [ ] Define types that **reflect actual API contract** — không tạo wrapper nếu backend không dùng wrapper:
  ```typescript
  // Only define ApiResult<T> if backend actually wraps responses in { data: T, message: string }
  // Otherwise, type the response directly per endpoint

  export type ApiError = {
    errorCode?: string;
    message: string;
    details?: Record<string, string[]>;
  };

  export type PaginatedResponse<T> = {
    items: T[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
  };

  export type DegreeStatus =
    | 'Pending_Confirmation'
    | 'Confirmed'
    | 'Confirmation_Error'
    | 'Pending_Update'
    | 'Pending_Revocation'
    | 'Revoked'
    | 'Frozen';
  // ⚠️ Must verify these 7 values against Backend DegreeStatus enum

  export type ReportStatus = 'Pending_Review' | 'Approved' | 'Rejected';

  export type RankStatus = 'Highly_Qualified' | 'Under_Qualified';

  export type UserRole = 'Registrar' | 'Student' | 'Recruiter' | 'Admin';
  ```

#### Output
- [NEW] `src/shared/api/http.ts`
- [NEW] `src/shared/api/error-mapper.ts`
- [NEW] `src/shared/services/notification.service.ts`
- [NEW] `src/shared/lib/date.ts`
- [NEW] `src/shared/types/api.types.ts`

#### Done Criteria
- HTTP client có baseURL, timeout, response error interceptor — **không** có auth header (Phase 0)
- `configureHttpAuth()` exported nhưng chưa được gọi bởi ai
- Shared layer KHÔNG import từ `app/` hoặc `features/`
- Error mapper phân biệt `not_found` / `server_error` / `timeout` / `network` với message riêng
- Error mapper KHÔNG map 404 global thành empty state
- Notification service wrapper hoạt động
- Date utils format English locale, timezone = user local
- Shared types verified against Backend DTOs

#### Commit
```
feat(frontend): implement shared HTTP client, error mapper, notification service
```

---

### WP-0.6: Create Complete Mock Auth UI with Role Switcher and Router

#### Tasks

##### 0.6.1 — Auth Types & Context Interface
- [ ] Tạo `features/auth/types/auth.types.ts`:
  ```typescript
  import type { UserRole } from '@/shared/types/api.types';

  export type MockUser = {
    id: string;
    fullName: string;
    email: string;
    role: UserRole;
    institutionId?: string;
    institutionName?: string;
  };

  export type AuthContextType = {
    currentUser: MockUser | null;
    isAuthenticated: boolean;
    login: (role: UserRole) => void;
    logout: () => void;
    switchRole: (role: UserRole) => void;
  };
  ```

##### 0.6.2 — MockAuthProvider (only — no RealAuthProvider)
- [ ] Tạo `app/providers/AuthProvider.tsx`:
  - Implement `AuthContextType` interface
  - Temp mock user data cho 4 roles:
    ```typescript
    const mockUsers: Record<UserRole, MockUser> = {
      Registrar: {
        id: 'mock-registrar-001',
        fullName: 'Dr. Sarah Mitchell',
        email: 'registrar@chaindegree.edu',
        role: 'Registrar',
        institutionId: 'inst-001',
        institutionName: 'ChainDegree University',
      },
      Student: {
        id: 'mock-student-001',
        fullName: 'Alex Johnson',
        email: 'student@chaindegree.edu',
        role: 'Student',
      },
      Recruiter: {
        id: 'mock-recruiter-001',
        fullName: 'Emily Davis',
        email: 'recruiter@techcorp.com',
        role: 'Recruiter',
      },
      Admin: {
        id: 'mock-admin-001',
        fullName: 'James Wilson',
        email: 'admin@chaindegree.io',
        role: 'Admin',
      },
    };
    ```
  - `login(role)` → set currentUser to `mockUsers[role]`
  - `logout()` → clear currentUser, redirect `/login`
  - `switchRole(role)` → swap currentUser to `mockUsers[role]`
- [ ] **Không tạo RealAuthProvider** — khi auth thật cần implement, sẽ tạo implementation mới dựa trên actual auth contract (JWT/Cookie/OAuth/etc.) lúc đó
- [ ] Tạo `useAuth()` hook để consume auth context

##### 0.6.3 — Login Page
- [ ] Tạo `features/auth/pages/LoginPage.tsx`:
  - Giao diện Login đẹp với branding ChainDegree
  - **Role Switcher**: 4 cards/buttons cho 4 roles
  - Mỗi card hiển thị: Role name, mock user name, mock email
  - Click card → `login(role)` → redirect to dashboard
  - 100% English UI

##### 0.6.4 — AppRouter
- [ ] Tạo `app/router/routes.ts` — route definitions:
  ```typescript
  export const ROUTES = {
    LOGIN: '/login',
    DASHBOARD: '/',
    DEGREES: '/degrees',
    DEGREE_DETAIL: '/degrees/:id',
    VERIFY: '/verify',
    REPORTS: '/admin/reports',
    REPUTATION: '/reputation',
    JOBS: '/jobs',
    JOB_DETAIL: '/jobs/:id',
    APPLICATIONS: '/applications',
  } as const;
  ```
- [ ] Tạo `app/router/AppRouter.tsx`:
  - React Router v7 với `createBrowserRouter` và `RouterProvider`
  - Layout routes: `DashboardLayout` (eager), `PublicLayout` (eager)
  - Lazy loading cho page-level components only
  - Route mapping cho tất cả trang (trỏ tới ComingSoonPage placeholders)
- [ ] Tạo `app/router/ProtectedRoute.tsx`:
  - Kiểm tra `isAuthenticated` → redirect `/login` nếu chưa auth
  - Kiểm tra `allowedRoles` → redirect nếu role không khớp
  - Render `<Outlet />` nếu pass

##### 0.6.5 — Layouts
- [ ] Tạo `app/layouts/DashboardLayout.tsx`:
  - **Sidebar**:
    - Logo/branding "ChainDegree"
    - Navigation links theo role:
      - **Registrar**: Dashboard, Degrees, Issue Degree
      - **Student**: Dashboard, My Degrees, Jobs
      - **Recruiter**: Dashboard, Jobs, Applicants
      - **Admin**: Dashboard, Reports, Reputation
    - Active link highlighting
    - Current user info + Role badge ở bottom
    - Logout button
  - **Header**:
    - Page title (dynamic)
    - Role Switcher dropdown (dev only) — cho phép switch role nhanh
    - User avatar/name
  - **Content area**: `<Outlet />`
  - Responsive: sidebar collapsible trên mobile
- [ ] Tạo `app/layouts/PublicLayout.tsx`:
  - Simple header với logo "ChainDegree" + link "Login"
  - Content area: `<Outlet />`
  - Dùng cho `/verify` (public, no auth needed)

##### 0.6.6 — AppProviders & Main Entry
- [ ] Tạo `app/providers/AppProviders.tsx`:
  - Compose providers: `AuthProvider` → `QueryProvider` → `ThemeProvider`
- [ ] Tạo `app/providers/QueryProvider.tsx`:
  - `npm install @tanstack/react-query`
  - QueryClient với default options
- [ ] Tạo `app/providers/ThemeProvider.tsx`:
  - Light/dark mode toggle support (basic)
- [ ] Cập nhật `main.tsx` với AppProviders + AppRouter

##### 0.6.7 — ErrorBoundary
- [ ] Tạo `shared/components/ErrorBoundary.tsx`:
  - Class component (React error boundary requires class)
  - Catch React render errors (NOT axios/API errors)
  - Fallback UI: English text *"Something went wrong"* + *"Try Again"* button
  - Reset state on retry
  - Bọc xung quanh router chính

#### Output
- [NEW] `src/features/auth/types/auth.types.ts`
- [NEW] `src/features/auth/pages/LoginPage.tsx`
- [NEW] `src/features/auth/index.ts`
- [NEW] `src/app/providers/AuthProvider.tsx`
- [NEW] `src/app/providers/AppProviders.tsx`
- [NEW] `src/app/providers/QueryProvider.tsx`
- [NEW] `src/app/providers/ThemeProvider.tsx`
- [NEW] `src/app/router/routes.ts`
- [NEW] `src/app/router/AppRouter.tsx`
- [NEW] `src/app/router/ProtectedRoute.tsx`
- [NEW] `src/app/layouts/DashboardLayout.tsx`
- [NEW] `src/app/layouts/PublicLayout.tsx`
- [NEW] `src/shared/components/ErrorBoundary.tsx`
- [MODIFY] `src/main.tsx`

#### Done Criteria
- Login page hiển thị Role Switcher cho 4 roles
- Chọn role → cập nhật quyền và sidebar ngay lập tức
- Route navigation: click sidebar → đúng page placeholder
- Protected route redirect sang `/login` nếu role không khớp
- ErrorBoundary bắt React render error, hiển thị fallback UI English
- Toast notification hoạt động
- **Không** có RealAuthProvider file
- **Không** có env flag swap auth provider

#### Commit
```
feat(frontend): create complete mock auth UI with role switcher and router
```

---

### WP-0.7: Add Shared UI Components

#### Tasks
- [ ] Tạo `shared/components/StatusBadge.tsx`:
  - Props: `status: DegreeStatus`
  - Color mapping:
    - 🟡 `Pending_Confirmation` → yellow/amber
    - 🟢 `Confirmed` → green
    - 🔴 `Confirmation_Error` → red
    - 🟡 `Pending_Update` → yellow/amber
    - 🟡 `Pending_Revocation` → yellow/amber
    - 🔴 `Revoked` → red
    - ⚫ `Frozen` → gray/slate
  - Display label: human-readable text (e.g., "Pending Confirmation", "Confirmed")
  - Sử dụng shadcn Badge component làm base
- [ ] Tạo `shared/components/LoadingSpinner.tsx`:
  - Spinner animation (CSS)
  - Props: `size?: 'sm' | 'md' | 'lg'`, `className?: string`
- [ ] Tạo `shared/components/EmptyState.tsx`:
  - Props: `title?: string` (default: *"No data available"*), `description?: string`, `icon?: ReactNode`
  - Layout: centered, icon + title + description
- [ ] Tạo `shared/components/ConfirmDialog.tsx`:
  - Props: `title`, `description`, `onConfirm`, `onCancel`, `confirmLabel` (default: *"Confirm"*), `variant` (default/destructive)
  - Sử dụng shadcn Dialog component
  - Focus management correct ([Coding-Standards §27](file:///e:/codes/chaindegree/docs/Coding-Standards.md))
- [ ] Tạo `shared/components/FileUpload.tsx`:
  - Drag-and-drop zone
  - Props: `accept` (file types), `maxSize` (bytes), `onFileSelect`, `onError`
  - Default: `.pdf, .png, .jpg`, max 5MB
  - Validation: file type + file size
  - Visual states: idle, dragover, uploaded, error
  - English labels: *"Drag and drop a file here, or click to browse"*
  - > ⚠️ Frontend file validation is UX only. Backend must revalidate file type, size, content, and authorization.
- [ ] Tạo `shared/components/ErrorState.tsx`:
  - Props: `title?: string` (default: *"Something went wrong"*), `description?: string`, `onRetry?: () => void`
  - Layout: centered, error icon + title + description + Retry button

#### Output
- [NEW] `src/shared/components/StatusBadge.tsx`
- [NEW] `src/shared/components/LoadingSpinner.tsx`
- [NEW] `src/shared/components/EmptyState.tsx`
- [NEW] `src/shared/components/ConfirmDialog.tsx`
- [NEW] `src/shared/components/FileUpload.tsx`
- [NEW] `src/shared/components/ErrorState.tsx`

#### Done Criteria
- StatusBadge renders correct color & label for all 7 DegreeStatus values
- LoadingSpinner animates correctly
- EmptyState shows default English text
- ConfirmDialog opens/closes with correct focus management
- FileUpload validates file type and size, shows drag-and-drop zone
- ErrorState shows retry button

#### Commit
```
feat(frontend): add shared UI components (StatusBadge, LoadingSpinner, etc.)
```

---

### WP-0.8: Unit Tests for Phase 0

#### Tasks

##### Test Setup
- [ ] Cài đặt testing dependencies:
  ```
  npm install -D vitest @testing-library/react @testing-library/jest-dom @testing-library/user-event jsdom
  ```
- [ ] Cấu hình Vitest trong `vite.config.ts`:
  ```typescript
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
  }
  ```
- [ ] Tạo `src/test/setup.ts`:
  ```typescript
  import '@testing-library/jest-dom';
  ```
- [ ] Thêm scripts vào `package.json`: `test`, `test:run`

##### Unit Tests — All Critical Behaviors

| Test File | Test Target | Test Cases |
|---|---|---|
| `error-mapper.test.ts` | `error-mapper.ts` | Map known business error codes → correct English message |
| `error-mapper.test.ts` | `error-mapper.ts` | `server_error` / `timeout` / `network` produce 3 **distinct** messages |
| `error-mapper.test.ts` | `error-mapper.ts` | `not_found` produces `NotFoundError` type (NOT empty state) |
| `error-mapper.test.ts` | `error-mapper.ts` | Unknown error code → generic fallback message |
| `date.test.ts` | `date.ts` | `formatDate` outputs correct English format |
| `date.test.ts` | `date.ts` | `formatDateTime` outputs correct English format with time |
| `date.test.ts` | `date.ts` | `formatRelativeTime` outputs relative string |
| `notification.service.test.ts` | `notification.service.ts` | `success()`, `error()` invoke underlying toast function |
| `StatusBadge.test.tsx` | `StatusBadge` | Render correct color & label for each of 7 DegreeStatus values |
| `ProtectedRoute.test.tsx` | `ProtectedRoute` | Redirect when not authenticated |
| `ProtectedRoute.test.tsx` | `ProtectedRoute` | Redirect when role mismatches allowed roles |
| `ProtectedRoute.test.tsx` | `ProtectedRoute` | Render content when role matches |
| `ErrorBoundary.test.tsx` | `ErrorBoundary` | Catch React render error, show fallback UI English |
| `ErrorBoundary.test.tsx` | `ErrorBoundary` | Render children normally when no error |
| `MockAuthProvider.test.tsx` | `MockAuthProvider` | Provide mock user data after login |
| `MockAuthProvider.test.tsx` | `MockAuthProvider` | Switch role updates currentUser |
| `MockAuthProvider.test.tsx` | `MockAuthProvider` | Logout clears currentUser state |

> [!NOTE]
> Số lượng test cases là baseline. Nếu implementation phát hiện edge cases cần thêm test, thêm không giới hạn. Không cắt test chỉ để giữ con số cố định.

> [!TIP]
> ErrorBoundary test: React/Vitest có thể log error ra console dù test pass. Mock/suppress `console.error` trong test setup để output sạch.

#### Output
- [NEW] `src/test/setup.ts`
- [NEW] `src/shared/api/__tests__/error-mapper.test.ts`
- [NEW] `src/shared/lib/__tests__/date.test.ts`
- [NEW] `src/shared/services/__tests__/notification.service.test.ts`
- [NEW] `src/shared/components/__tests__/StatusBadge.test.tsx`
- [NEW] `src/app/router/__tests__/ProtectedRoute.test.tsx`
- [NEW] `src/shared/components/__tests__/ErrorBoundary.test.tsx`
- [NEW] `src/app/providers/__tests__/MockAuthProvider.test.tsx`
- [MODIFY] `vite.config.ts` — thêm test config
- [MODIFY] `package.json` — thêm test scripts

#### Done Criteria
- `npm run test:run` → all tests pass, 0 failures
- All Phase 0 critical behaviors covered

#### Commit
```
test(frontend): add unit tests for Phase 0 shared utilities and auth guard
```

---

### WP-0.9: Integration Verification & Documentation

#### Tasks
- [ ] **Integration smoke test** (manual verification):
  1. `npm run dev` → `http://localhost:3000` → Login page hiển thị
  2. Click "Registrar" card → redirect to Dashboard, sidebar shows Registrar links
  3. Click "Degrees" in sidebar → ComingSoonPage hiển thị
  4. Click user dropdown → Switch role to "Admin" → sidebar updates, shows "Reports" link
  5. Navigate to `/verify` → PublicLayout, no auth required
  6. Navigate to `/admin/reports` as Student → redirect to `/login`
  7. Trigger ErrorBoundary → fallback UI hiển thị
  8. Verify toast: `notification.success("Test")` → toast hiển thị
- [ ] Chạy full verification suite:
  ```
  npm run lint          # 0 errors
  npm run build         # thành công
  npm run test:run      # all pass
  ```
- [ ] Cập nhật `SYSTEM_BRAIN.md` với Phase 0 frontend map
- [ ] Lưu implementation plan vào `docs/implementation/frontend-phase-0-foundation.md`

#### Output
- [MODIFY] `SYSTEM_BRAIN.md` — thêm Frontend Phase 0 section
- [NEW] `docs/implementation/frontend-phase-0-foundation.md`

#### Done Criteria (Phase 0 Overall)

##### Functional
- [ ] `npm run dev` chạy thành công tại `http://localhost:3000`
- [ ] `npm run lint` không có error
- [ ] `npm run build` thành công không lỗi TypeScript
- [ ] `npm run test:run` — all tests pass
- [ ] Route navigation hoạt động: click sidebar → đúng page placeholder
- [ ] Login page hiển thị Role Switcher cho 4 roles
- [ ] Chọn role cập nhật quyền và sidebar ngay lập tức
- [ ] Protected route redirect sang `/login` nếu role không khớp
- [ ] ErrorBoundary bắt React render error, hiển thị fallback UI English
- [ ] Toast notification hoạt động
- [ ] Path alias `@/` resolve đúng trong cả IDE and build
- [ ] `.env.example` có đủ 5 env vars
- [ ] Missing `VITE_API_BASE_URL` → fail-fast with clear error

##### Architecture & Security
- [ ] No secrets exist in `VITE_*` variables
- [ ] No Authorization/token/cookie is logged
- [ ] Shared layer does not depend on feature/app layer
- [ ] Backend remains source of truth for authorization
- [ ] Frontend file validation is not treated as security
- [ ] 404 is not globally forced into EmptyState — component decides UI state
- [ ] No RealAuthProvider placeholder exists

#### Commit
```
docs(frontend): add Phase 0 implementation documentation and update system brain
```

---

## Commit Plan Summary (Deployable Intentions)

| # | Commit Message | WP |
|---|---|---|
| 0 | `docs(frontend): update frontend implementation plan branch strategy to checkout from main` | WP-0.0 ✅ |
| 1 | `feat(frontend): scaffold Vite + React + TypeScript project` | WP-0.1 |
| 2 | `chore(frontend): configure ESLint, Prettier, EditorConfig` | WP-0.2 |
| 3 | `feat(frontend): setup TailwindCSS v4 + shadcn/ui design system` | WP-0.3 |
| 4 | `chore(frontend): establish feature-based directory structure` | WP-0.4 |
| 5 | `feat(frontend): implement shared HTTP client, error mapper, notification service` | WP-0.5 |
| 6 | `feat(frontend): create complete mock auth UI with role switcher and router` | WP-0.6 |
| 7 | `feat(frontend): add shared UI components (StatusBadge, LoadingSpinner, etc.)` | WP-0.7 |
| 8 | `test(frontend): add unit tests for Phase 0 shared utilities and auth guard` | WP-0.8 |
| 9 | `docs(frontend): add Phase 0 implementation documentation and update system brain` | WP-0.9 |

---

## Verification Plan

### Automated Tests
```powershell
cd apps/frontend
npm run lint          # ESLint — 0 errors
npm run build         # TypeScript + Vite build — 0 errors
npm run test:run      # Vitest — all pass
```

### Manual Verification
- Login page renders 4 role cards with mock data
- Role switching updates sidebar + user info immediately
- All sidebar links navigate to correct placeholder pages
- ProtectedRoute redirects unauthorized users
- `/verify` accessible without login (PublicLayout)
- ErrorBoundary catches render errors, shows English fallback
- Toast notifications work (success, error, warning, info)
- Dark/light mode toggle works
- Responsive sidebar collapses on mobile viewport

### Integration Test Goals
- **Goal 1 (Foundation)**: Toàn bộ toolchain (Vite, TypeScript, ESLint, Prettier, Tailwind, shadcn) cấu hình chính xác, `build` thành công
- **Goal 2 (Auth UX)**: Mock auth layer hoạt động đầy đủ — login, logout, role switch, route protection — không có RealAuthProvider overhead
- **Goal 3 (Shared Infra)**: HTTP client (TokenProvider extension point, no auth header Phase 0), error mapper (component-decided UI state), notification, date utils sẵn sàng cho Phase 1+
- **Goal 4 (UI Components)**: StatusBadge, LoadingSpinner, EmptyState, ConfirmDialog, FileUpload, ErrorState render đúng
- **Goal 5 (Boundaries)**: Shared ← không biết Auth/Feature. Feature ← có thể dùng Shared. App ← compose mọi thứ
- **Goal 6 (Security Baseline)**: No secrets in VITE_*, no token logging, no global 404→empty, frontend validation = UX only

### Architecture Verification

```
                    ┌──────────────────┐
                    │       App        │
                    │  (compose all)   │
                    └────────┬─────────┘
                             │
                    ┌────────▼─────────┐
                    │      Router      │
                    └────────┬─────────┘
                             │
             ┌───────────────┼────────────────┐
             │               │                │
       ┌─────▼─────┐   ┌────▼─────┐   ┌─────▼─────┐
       │    Auth   │   │ Features │   │  Layouts  │
       └───────────┘   └────┬─────┘   └───────────┘
                            │
                      ┌─────▼─────┐
                      │   Shared  │
                      ├───────────┤
                      │ HTTP      │ ← TokenProvider extension point
                      │ Error     │ ← error type, NOT UI state
                      │ Utils     │
                      │ Services  │
                      │ UI        │
                      └───────────┘
                            │
                       ┌────▼─────┐
                       │ Backend  │
                       └──────────┘

Boundary rules:
  Shared → does NOT know Auth/Feature
  Feature → may use Shared
  App → composes everything
  Auth layer → calls configureHttpAuth() on Shared
```
