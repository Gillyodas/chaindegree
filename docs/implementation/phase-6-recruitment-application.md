# Phase 6: Recruitment And Application — Implementation & Verification Plan

## Background & Current State Analysis

Phase 6 delivers the **Recruitment and Application** feature (US-6, US-7 / UC-6, UC-7), bridging the gap between validated on-chain degrees and employer hiring processes. It enables `Recruiter`s to post `Job`s with specific `DegreeFilter`s, and `Student`s to apply. Applications are automatically validated against the student's degree. A mathematical `JobScore` ranking algorithm leverages the Reputation Engine to sort jobs dynamically on the job board.

### What Needs to be Built

| Component | Target Layer | Notes |
|---|---|---|
| `Job` | Domain | Aggregate Root for job postings. Tracks `RecruiterId`, `Status` (Open/Closed), `ExpiresAt`, and salary bounds. |
| `Application` | Domain | **Aggregate Root** (Independent lifecycle). Tracks `StudentId`, `JobId`, `ProcessStatus`, `RankStatus`. Unique `StudentId` + `JobId` constraint. |
| `DegreeFilter` | Domain | Value Object representing minimum matching criteria for a `Job`. |
| Use Cases & Handlers | Application | `PostJobCommand`, `ApplyForJobCommand`. Application logic verifies `Degree.StudentId == CurrentUserId` (IDOR prevention). |
| Ranking Algorithm | Application | Calculates `JobScore` using `IOptions<RankingOptions>` to avoid magic numbers. Avoids N+1 queries by batch-loading Reputations. |
| Reputation Read Abstraction | Infrastructure | `IReputationReadService` to fetch partner reputations in batch (default 500 if missing). |
| EF Core Configurations | Infrastructure | Separate schema/tables for `Job`, `Application`. |
| Recruitment API | API | Endpoints with explicit `[Authorize(Roles="...")]`. Simple SQL LIKE search, no Elastic/AI for MVP. |

---

## Technical & Business Decisions Summary

| Decision | Resolution |
|---|---|
| **Bounded Context & Aggregates** | ✅ Recruitment reads degree data from Core and reputation data via abstraction. `Application` is its own Aggregate Root since it has an independent lifecycle (`Submitted -> Reviewed -> Accepted -> Rejected`) separate from `Job`. |
| **Student Ownership (Security)** | ✅ `ApplyForJobCommand` MUST verify `Degree.StudentId == CurrentUserId`. This prevents an IDOR vulnerability where User A uses User B's degree ID. |
| **Duplicate Applications** | ✅ Enforce an invariant (and DB Unique Constraint) on `StudentId` + `JobId`. A student can only apply once per job. |
| **Job Lifecycle & Deadline** | ✅ `Job` has `Status` (`Open`, `Closed`) and `ExpiresAt`. Applications are rejected if `Now > ExpiresAt` or `Status == Closed`. (Checked during Application, no scheduler needed). |
| **Data Validation Rules** | ✅ `SalaryMin <= SalaryMax`. Both must be `> 0` to prevent `ln(0)` error in ranking. `Description` is limited to max 4000 chars. |
| **Reputation Fallback** | ✅ If the Reputation module is offline, `IReputationReadService` defaults to `500`. |
| **Application Matching Logic** | ✅ Server validates `Degree` against `DegreeFilter`. Comparisons are hierarchical (e.g., `Excellent >= Good`), not strict equality. |
| **Force Submission** | ✅ Server computes match independently. If `forceSubmit = true` but the student is under-qualified, the server correctly sets `RankStatus = Under_Qualified`. The client's `forceSubmit` cannot trick the server into assigning `Highly_Qualified`. |
| **Revoked Degrees Rule** | ✅ Any degree with `Status` == `Revoked` or `Pending_Revocation` is **hard-rejected**. |
| **Dynamic Job Ranking** | ✅ Uses formula $JobScore = (W_{base} \times \ln(Salary_{Avg})) + (W_{rep} \times \frac{ReputationScore}{1000}) + \frac{W_{time}}{1 + \Delta t}$. Weights are read from `IOptions<RankingOptions>` ($W_{base} = 40, W_{rep} = 60, W_{time} = 100$). |
| **Query Optimization** | ✅ For sorting by `JobScore`, fetch Jobs, then batch-load `ReputationScore` for the unique `PartnerIds` via dictionary to prevent N+1 query issues. No Redis or advanced caching for MVP. |
| **Logging** | ✅ Behavior logs for `POST_JOB`, `APPLY_JOB` must include `JobId`, `RecruiterId`, `StudentId`, `DegreeId`. Never log PII or JWT. |

---

## Work Packages Detail & Execution Plan

### Work Package 6.1: Recruitment Domain Models
- **Tasks**:
  - Define `Job` Aggregate Root (`Id`, `RecruiterId`, `Title`, `SalaryMin` > 0, `SalaryMax` >= `SalaryMin`, `Description` (max 4000), `Status` (Open/Closed), `ExpiresAt`, `CreatedAt`).
  - Define `DegreeFilter` value object (`DegreeType`, `RequiredMajor`, `MinimumClassification`).
  - Define `Application` Aggregate Root (`Id`, `JobId`, `DegreeId`, `StudentId`, `ProcessStatus` (Submitted, Reviewed, Accepted, Rejected), `RankStatus`).
  - Implement Domain validation rules for degree matching (hierarchical).
- **Done Criteria**: Domain model encapsulates all rules (salary checks, description length).

### Work Package 6.2: Application Layer (Use Cases & Ranking)
- **Tasks**:
  - Implement `PostJobCommandHandler`.
  - Implement `ApplyForJobCommandHandler`. Handles ownership check (IDOR), deadline check, duplicate check, degree matching, and `forceSubmit` flow.
  - Implement `RankingOptions` (appsettings).
  - Implement `JobRankingService` containing the math formula using options.
- **Done Criteria**: Handlers cover all edge cases (duplicate apply, IDOR, deadline). Ranking algorithm uses configured weights.

### Work Package 6.3: Infrastructure & Data Access
- **Tasks**:
  - Add EF Core configurations. Enforce `UniqueIndex(StudentId, JobId)` on `Application`.
  - Implement `IReputationReadService` to support batch fetching `GetReputationsAsync(IEnumerable<Guid> partnerIds)`.
- **Done Criteria**: Migrations generated. Batch reputation loader prevents N+1.

### Work Package 6.4: API Layer & Endpoints
- **Tasks**:
  - Create `RecruitmentController`.
  - `POST /api/v1/recruitment/jobs` (Explicit `[Authorize(Roles="Recruiter")]`).
  - `POST /api/v1/recruitment/applications` (Explicit `[Authorize(Roles="Student")]`).
  - `GET /api/v1/recruitment/jobs` (Returns sorted list, standard SQL query logic).
- **Done Criteria**: Role-based auth strictly enforced. Correct HTTP status codes.

### Work Package 6.5: Integration Testing & Verification
- **Tasks**:
  - E2E tests for IDOR prevention (User A cannot use User B's degree).
  - E2E tests for duplicate application rejection.
  - E2E tests for deadline/closed job rejection.
  - E2E tests for hierarchical matching (`Excellent >= Good`).
- **Done Criteria**: All matching, fallback, and sorting logic proven through end-to-end simulation.

---

## Verification & Integration Test Plan

### Automated Tests Execution Expected Commands
```powershell
dotnet test tests/ChainDegree.Domain.Tests/ChainDegree.Domain.Tests.csproj --filter "FullyQualifiedName~Recruitment"
dotnet test tests/ChainDegree.Application.Tests/ChainDegree.Application.Tests.csproj --filter "FullyQualifiedName~Recruitment"
dotnet test tests/ChainDegree.API.Tests/ChainDegree.API.Tests.csproj --filter "FullyQualifiedName~RecruitmentControllerTests"
```

### End-To-End Integration Goals
- **Goal 1 (Matching Application)**: A student applies with a valid degree that matches the job's `DegreeFilter`. System saves application as `Highly_Qualified`.
- **Goal 2 (Force Submit)**: A student applies with a non-matching degree. System returns `422`. Student re-submits with `forceSubmit = true`. System saves application as `Under_Qualified`.
- **Goal 3 (Security & Invariants)**: System rejects application if `Degree` belongs to someone else, if job is past `ExpiresAt`, or if `StudentId` + `JobId` already exists.
- **Goal 4 (Job Ranking)**: Multiple jobs are created. Querying the jobs batches reputations and returns them in correct order based on Salary, Partner Reputation, and Age ($\Delta t$), using options configuration.

---

## Commit Plan (Deployable Intentions)

```text
docs(phase-6): update implementation plan with comprehensive rules and safeguards
feat(recruitment): define job and application aggregate roots with invariants
feat(recruitment): implement configuration-driven ranking algorithm and queries
feat(recruitment): implement application handlers with IDOR prevention and deadline checks
feat(recruitment): configure ef core constraints and batch reputation reader
feat(api): create recruitment endpoints with explicit role-based auth
test(recruitment): add comprehensive integration tests for matching, security, and ranking
```
