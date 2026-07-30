# Phase 6: Recruitment And Application — Implementation & Verification Plan

## Background & Current State Analysis

Phase 6 delivers the **Recruitment and Application** feature (US-6, US-7 / UC-6, UC-7), bridging the gap between validated on-chain degrees and employer hiring processes. It enables `Recruiter`s to post `Job`s with specific `DegreeFilter`s, and `Student`s to apply. Applications are automatically validated against the student's degree. A mathematical `JobScore` ranking algorithm leverages the Reputation Engine to sort jobs dynamically on the job board.

### What Needs to be Built

| Component | Target Layer | Notes |
|---|---|---|
| `Job`, `DegreeFilter`, `Application` | Domain | `Job` Aggregate Root. `DegreeFilter` as Value Object. `Application` tracks match status. |
| Use Cases & Handlers | Application | `PostJobCommand`, `ApplyForJobCommand` with `forceSubmit` flag. |
| Ranking Algorithm | Application | Implementation of the `JobScore` formula for query sorting. |
| Reputation Read Abstraction | Infrastructure | `IReputationReadService` to fetch partner reputation (default 500 if missing). |
| EF Core Configurations | Infrastructure | Separate schema/tables for `Job`, `DegreeFilter`, `Application`. |
| Recruitment API Endpoints | API | `POST /api/v1/recruitment/jobs`, `POST /api/v1/recruitment/applications`, `GET /api/v1/recruitment/jobs` |

---

## Technical & Business Decisions Summary

| Decision | Resolution |
|---|---|
| **Bounded Context** | ✅ Recruitment reads degree data from Core and reputation data via a read abstraction. It does not mutate Core or Reputation state. |
| **Reputation Fallback** | ✅ If the Reputation module is offline or not installed, the `IReputationReadService` gracefully falls back to a default value of `500` to ensure the ranking algorithm still functions. |
| **Application Matching Logic** | ✅ System automatically compares `Degree` fields against `DegreeFilter`. If criteria fail, returning `422 FILTER_CRITERIA_NOT_SATISFIED`. |
| **Force Submission** | ✅ If `forceSubmit = true` is provided after a warning, the application is saved with `RankStatus` = `Under_Qualified`. Otherwise, matching applications are saved as `Highly_Qualified`. |
| **Revoked Degrees Rule** | ✅ Any degree with `Status` == `Revoked` or `Pending_Revocation` is **hard-rejected**. It cannot be submitted, even with `forceSubmit = true`. |
| **Dynamic Job Ranking** | ✅ The ranking score changes continuously over time ($\Delta t$). We will implement the formula $JobScore = (40 \times \ln(Salary_{Avg})) + (60 \times \frac{ReputationScore}{1000}) + \frac{100}{1 + \Delta t}$. This will be evaluated in a service query, potentially using database computed/translation functions for performance. |

---

## Work Packages Detail & Execution Plan

### Work Package 6.1: Recruitment Domain Model
- **Tasks**:
  - Define `Job` entity (`Id`, `Title`, `SalaryMin`, `SalaryMax`, `Description`, `CreatedAt`).
  - Define `DegreeFilter` value object (`DegreeType`, `RequiredMajor`, `MinimumClassification`).
  - Define `Application` entity (`Id`, `JobId`, `DegreeId`, `StudentId`, `ProcessStatus`, `RankStatus`).
  - Implement Domain validation rules for degree matching and revoked degree rejection.
- **Done Criteria**: Domain model encapsulates all rules. Revoked degree check prevents application creation.

### Work Package 6.2: Application Layer (Use Cases & Ranking)
- **Tasks**:
  - Implement `PostJobCommandHandler` (Auth: Recruiter).
  - Implement `ApplyForJobCommandHandler` (Auth: Student). Handles degree lookup, filter matching, and the `forceSubmit` flow.
  - Implement `JobRankingService` containing the math formula for `JobScore`.
- **Done Criteria**: Commands are implemented. Algorithm correctly calculates `JobScore` based on the specified weights ($W_{base} = 40, W_{rep} = 60, W_{time} = 100$).

### Work Package 6.3: Infrastructure & Data Access
- **Tasks**:
  - Add EF Core `IEntityTypeConfiguration` for recruitment entities.
  - Implement `IReputationReadService` adapter that calls the Reputation module (if available) or defaults to `500`.
- **Done Criteria**: Migrations for the Recruitment schema are generated. Fallback provider works seamlessly.

### Work Package 6.4: API Layer & Endpoints
- **Tasks**:
  - Create `RecruitmentController`.
  - Endpoint: `POST /api/v1/recruitment/jobs` (`201 Created`).
  - Endpoint: `POST /api/v1/recruitment/applications` (Returns `201` + `Highly_Qualified`/`Under_Qualified`, or `422`).
  - Endpoint: `GET /api/v1/recruitment/jobs` (Returns sorted list of jobs based on `JobScore`).
- **Done Criteria**: Auth correctly enforced. Correct HTTP status codes and response models returned.

### Work Package 6.5: Integration Testing & Verification
- **Tasks**:
  - Write Integration Tests for successful application (`Highly_Qualified`).
  - Write Integration Tests for non-matching application (returns `422`, then `201` with `forceSubmit`).
  - Write Integration Tests for rejected revoked degrees.
  - Write Integration Tests verifying the Job Ranking order.
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
- **Goal 3 (Revoked Rejection)**: A student attempts to apply with a `Revoked` degree. System rejects it entirely, ignoring `forceSubmit`.
- **Goal 4 (Job Ranking)**: Multiple jobs are created. Querying the jobs returns them in correct order based on Salary, Partner Reputation, and Age ($\Delta t$), verifying the formula correctness.

---

## Commit Plan (Deployable Intentions)

```text
docs(phase-6): create implementation plan for recruitment and application
feat(recruitment): define job, degreefilter, and application domain models
feat(recruitment): implement application handlers and job ranking algorithm
feat(recruitment): configure ef core and reputation read fallback adapter
feat(api): create recruitment endpoints for jobs and applications
test(recruitment): add integration tests for matching logic and job ranking
```
