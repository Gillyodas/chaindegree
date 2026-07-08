# ChainDegree Implementation Plan

This plan organizes the system into **feature-driven vertical slices**. Each phase delivers a complete, deployable feature from domain to API to UI, following the user stories defined in `business-domain.md`. A thin foundation phase comes first to establish shared infrastructure that every feature depends on.

## Architecture Boundaries (Apply To All Phases)

- Core degree workflows must not depend on the Reputation module.
- Auth is an independent integration boundary provided by the future `ControlHub` NuGet package.
- Blockchain behavior must stay behind interfaces so workflows can be tested without a live Besu network.
- Domain must stay independent of ASP.NET Core, EF Core, Nethereum, RabbitMQ, and other infrastructure concerns.
- Every state-changing action must write a `BehaviorLog`.
- Reputation must be an isolated module that plugs into ChainDegree Core and is removable without breaking Core behavior.

---

## Phase 0: Project Foundation, Audit, And Auth Boundary

Build the minimal shared infrastructure that every subsequent feature phase depends on.

### Scope

#### Domain Layer

- Core actors as domain concepts: `Registrar`, `Student`, `Recruiter`, `Admin`, `Validator/System`.
- Base entities: `Institution`, `User` reference/profile, `DegreeType`.
- Shared degree lifecycle statuses as a value object:
  - `Pending_Confirmation`
  - `Confirmed`
  - `Confirmation_Error`
  - `Pending_Update`
  - `Pending_Revocation`
  - `Revoked`
  - `Frozen`
- Central `BehaviorLog` entity: `BehaviorLogs(Id, ActorId, ActionType, ImpactedEntityId, Description, Timestamp)`.
- Shared audit fields: `CreatedAt`, `UpdatedAt`, `DeletedAt`, `CreatedBy`, `UpdatedBy`.
- Base domain event infrastructure (raise/dispatch mechanism).

#### Application Layer

- Authorization abstractions: current user accessor, role/permission checker, institution ownership checker.
- `BehaviorLog` service abstraction and implementation.

#### Infrastructure Layer

- EF Core `DbContext` with base entity configurations.
- Database migrations for foundation entities.
- Temporary auth adapter (test doubles) until `ControlHub` is available.

#### API Layer

- Thin API authorization policies for `Registrar`, `Student`, `Recruiter`, `Admin`, `System`.
- Health check endpoint.
- Global error handling and validation filters.

### Isolation Boundary

Do not implement permanent local auth infrastructure, blockchain, queue, or any specific feature behavior. Auth must be swappable. Domain events may be defined but must not require any specific downstream module.

### Acceptance Criteria

- Degree lifecycle statuses are constrained to documented values.
- Every state-changing core action can produce a behavior log entry.
- Domain objects do not depend on ASP.NET Core, EF Core, RabbitMQ, Nethereum, or ControlHub.
- Registrar-only endpoints can be protected through an abstraction, not direct local auth logic.
- Student, Recruiter, Admin, and System policies are distinguishable.
- Ownership checks can verify that a `Registrar` belongs to the institution managing a resource.
- Tests can run using fake auth/current-user implementations.
- Replacing the temporary adapter with `ControlHub` should not require changes in Domain or core use cases.

### Deliverable

Foundation is ready. All subsequent feature phases can build on these shared services without re-establishing audit, auth, or persistence infrastructure.

---

## Phase 1: Degree Issuance — End To End (US-1 / UC-1)

Deliver the complete degree issuance feature from API request to blockchain confirmation, including UI.

### Scope

#### Domain Layer

- `Degree` entity with all required fields: `DegreeCode`, `StudentCode`, `StudentFullName`, `StudentEmail`, `Major`, `Classification`, `IssuedAt`, `Salt`, `DataHash`, `Status`, `TxHash`, `BlockNumber`.
- Domain rules: duplicate prevention (same student + degree type + institution), status transitions.
- Domain events: `DegreeCreatedEvent`.

#### Application Layer

- Use case: `IssueDegreeCommand` / handler.
- Canonical JSON generation (alphabetical key ordering).
- Salt generation: 16-character cryptographic hex salt.
- Hashing: `DataHash = SHA-256(PlainDataCanonical + Salt)`.
- Batch tracking: `BatchId`, `BatchName`, `EstimatedWaitTime`.
- Validation: required fields, duplicate check.

#### Infrastructure Layer

- EF Core configuration for `Degree` entity.
- RabbitMQ integration: push degree IDs into queue after creation.
- Background worker (`BackgroundService`) with dual-trigger batching:
  - Trigger 1: max batch size of `500`.
  - Trigger 2: max wait time of `3–5 minutes`.
- Merkle tree construction from `DataHash` values in the batch.
- Nethereum transaction sender: one transaction per batch containing the Merkle root.
- Store transaction metadata: `TxHash`, `BlockNumber`, Merkle root/proof data.
- Hyperledger Besu QBFT configuration with `mining-empty-blocks = false`.
- Failure path: set degree status to `Confirmation_Error`, route to retry/DLQ.

#### API Layer

- Endpoint: `POST /api/v1/institutions/degrees`.
- Return `202 Accepted` immediately with `batchId`, `status`, `estimatedWaitTimeSeconds`, `checkStatusUrl`.
- Batch status check endpoint.

#### UI Layer

- Registrar degree issuance form with multi-degree add button (`[+]`).
- Toast notification on successful submission.
- Degree list with status badges: 🟡 `Pending_Confirmation`, 🟢 `Confirmed`, 🔴 `Confirmation_Error`.
- Retry button for `Confirmation_Error` status.
- Duplicate degree rows remain in form with inline red errors.

### Isolation Boundary

Blockchain behavior is behind interfaces. Core degree workflows should not know whether the anchor is Besu, a mock, or another implementation. No reputation, reports, or recruitment logic.

### Acceptance Criteria

- Creating a valid degree stores `Salt`, `DataHash`, plain degree fields, and status `Pending_Confirmation`.
- Reordering JSON fields produces the same canonical hash.
- Changing any degree field produces a different hash.
- Duplicate degree issuance is rejected and no database record is created.
- Invalid required fields return validation errors.
- `CREATE_DEGREE` behavior log is written.
- API returns `202 Accepted` without waiting for blockchain processing.
- Degree remains `Pending_Confirmation` until the worker completes.
- Batch triggers when 500 queued records exist.
- Batch triggers when max wait time expires, even with fewer than 500 records.
- Successful worker run updates degrees to `Confirmed`.
- Failed worker run updates degrees to `Confirmation_Error`.
- Retry can requeue a failed confirmation.
- Worker sends one blockchain transaction per batch, not one per degree.
- One Merkle root represents all degree hashes in the batch.
- After receipt confirmation, local records store `TxHash` and `BlockNumber`.
- Merkle proof can validate an individual degree hash against the anchored root.
- Blockchain transaction failure routes the batch to retry or DLQ and does not mark degrees `Confirmed`.
- No empty blocks are generated when the local network is idle.
- Registrar sees pending, confirmed, and error states with correct visual badges.
- Successful batch issuance shows a non-blocking toast.
- Duplicate degree rows remain in the form with inline errors.

### Deliverable

A registrar can issue degrees through the UI, the system queues and batches them asynchronously, anchors them on Besu via Merkle tree, and the registrar sees real-time status updates.

---

## Phase 2: Degree Update And Revocation — End To End (US-2 / UC-2)

Deliver the complete degree update and revocation feature.

### Scope

#### Domain Layer

- Status transitions: `Confirmed` → `Pending_Revocation` / `Pending_Update`, and finalization to `Revoked` / `Confirmed`.
- Shortcut logic for `Pending_Confirmation` degrees: immediate revocation to `Revoked`, direct update with hash recalculation.
- Domain events: `DegreeRevokedEvent`, `DegreeUpdatedEvent`, `DegreeRevokedWithoutConfirmationEvent`, `DegreeUpdatedWithoutConfirmationEvent` (signals reputation exemption without requiring Reputation module).
- Required reason/comment handling with predefined reason categories.
- The system must preserve previous hash/version information for every confirmed degree update. Previous blockchain-anchored hashes must remain auditable and immutable.

#### Application Layer

- Use case: `RevokeDegreeCommand` / handler.
- Use case: `UpdateDegreeCommand` / handler.
- Logic for distinguishing `Confirmed` vs `Pending_Confirmation` flow.
- Hash recalculation on update.

#### Infrastructure Layer

- Queue confirmed degree revocations/updates for blockchain state transaction.
- Background worker processes revocation/update blockchain transactions.

#### API Layer

- Endpoint: `POST /api/v1/institutions/degrees/{id}/revoke`.
  - `Confirmed` degree: returns `202 Accepted`, status → `Pending_Revocation`.
  - `Pending_Confirmation` degree: returns `200 OK`, status → `Revoked` immediately.
- Endpoint: `PUT /api/v1/institutions/degrees/{id}`.
  - Returns `202 Accepted`, status → `Pending_Update`.

#### UI Layer

- Degree detail screen with `[Cập nhật]` and `[Thu hồi bằng]` buttons.
- Contextual toast messages based on original degree status.
- Status badge transitions: 🟡 `Pending_Revocation` → 🔴 `Revoked`.

### Isolation Boundary

Core may publish events, but it must not call Reputation services directly. The workflow must still complete if the Reputation module is absent.

### Acceptance Criteria

- Only the owning institution's `Registrar` can update or revoke a degree.
- `Confirmed` degree revocation returns `202 Accepted`.
- `Pending_Confirmation` degree revocation returns immediate success and status `Revoked`.
- Pending shortcut emits an exemption signal but does not require Reputation to exist.
- Updating a pending degree recalculates `DataHash`.
- Updating or revoking a confirmed degree creates a new blockchain state transaction.
- `UPDATE_DEGREE` or `REVOKE_DEGREE` log is written with original status and reason.

### Deliverable

A registrar can update or revoke degrees through the UI with appropriate async/shortcut behavior, blockchain anchoring, and audit logging.

---

## Phase 3: Public Degree Verification — End To End (US-3 / UC-3)

Deliver the public verification portal.

### Scope

#### Domain Layer

- Verification result value objects: `Verified`, `Revoked`, `CryptoHashMismatch`, `BlockchainInvalid`.

#### Application Layer

- Use case: `VerifyDegreeQuery` / handler.
- Dual-verification pipeline:
  1. Fetch local degree data, salt, and hash from database.
  2. Recalculate `Hash(PlainData + Salt)` and compare against local stored hash.
  3. Validate against blockchain Merkle root using Merkle proof.
- Return verified, revoked, or fraud/mismatch result.

#### Infrastructure Layer

- Blockchain query: fetch Merkle root from smart contract.
- Merkle proof validation.

#### API Layer

- Public endpoint: `POST /api/v1/institutions/degrees/verify`.
- No authentication required.
- Success: `200 OK` with `verified: true`, blockchain details.
- Hash mismatch: `422 CRYPTO_HASH_MISMATCH`.
- Blockchain invalid: `422 BLOCKCHAIN_INVALID`.
- Revoked degree: clear revoked result.

#### UI Layer

- Public `Verification Portal` page with degree code input field.
- Result display with visual states:
  - Green border/badge: valid confirmed degree.
  - Red border/badge: revoked degree.
  - Orange flashing border with danger warning: data integrity compromised (hash mismatch).

### Isolation Boundary

Verification only detects status and integrity. It does not file complaints, mutate reputation, or trigger penalties.

### Acceptance Criteria

- Public users can verify without login.
- Valid confirmed degree returns `200 OK` with `verified: true`.
- Revoked degree returns a clear revoked result.
- Modified database/plain data causes `422 CRYPTO_HASH_MISMATCH`.
- Invalid blockchain/Merkle proof causes `422 BLOCKCHAIN_INVALID`.
- Every lookup writes `VERIFY_DEGREE` behavior log, anonymous or identified.

### Deliverable

Anyone can verify a degree's authenticity through the public portal, with clear visual results for valid, revoked, and tampered degrees.

---

## Phase 4: Complaints And Reports — End To End (US-4 / UC-4)

Deliver the report submission and admin review workflow.

### Scope

#### Domain Layer

- `Report` entity with fields: `ReportId`, `DegreeId`, `ReporterId`, `ReporterRole`, `ReportType` (`Administrative_Error`, `Fraudulent_Data`), `Description`, `EvidenceUrl`, `Status` (`Pending_Review`, `Approved`, `Rejected`).
- Domain events: `ReportSubmittedEvent`, `ReportApprovedEvent`, `FraudulentDataDetectedEvent`.
- Reporting permission rules: student can report only own degrees, recruiter can report any degree.

#### Application Layer

- Use case: `SubmitReportCommand` / handler.
- Use case: `ApproveReportCommand` / handler.
- Evidence file upload handling.
- Validation: required report type, description, evidence file.

#### Infrastructure Layer

- EF Core configuration for `Report` entity.
- File storage for evidence uploads.
- Event publishing (RabbitMQ) for `ReportApprovedEvent` / `FraudulentDataDetectedEvent`.

#### API Layer

- Endpoint: `POST /api/v1/institutions/degrees/reports` (multipart/form-data).
  - Auth: `Student` or `Recruiter`.
  - Returns `201 Created` with `reportId`, `status: Pending_Review`.
- Endpoint: `POST /api/v1/institutions/reports/{id}/approve`.
  - Auth: `Admin`.
  - Returns `202 Accepted`.

#### UI Layer

- Report button alongside degree detail view: `[Báo cáo sai sót/Gian lận]`.
- Report form modal: report type dropdown, description textarea, drag-and-drop file upload.
- Toast notification on successful submission.

### Isolation Boundary

Reports may read degree data and publish events, but must not calculate or persist reputation scores directly. Report approval must work even when the Reputation module is absent.

### Acceptance Criteria

- Student can report only their own degree.
- Recruiter can report any degree they encountered.
- Missing report type, description, or evidence file is rejected.
- Report creation returns `201 Created` and status `Pending_Review`.
- Admin approval returns `202 Accepted`.
- Approval emits the documented event without requiring the Reputation module to be installed.
- `SUBMIT_REPORT` behavior log is written.
- Report form requires type, description, and evidence.

### Deliverable

Students and recruiters can submit reports with evidence, and admins can review and approve them, triggering downstream events.

---

## Phase 5: Reputation Engine — End To End (US-5 / UC-5)

Deliver the independent reputation module as a plug-in.

### Scope

#### Domain Layer (Reputation Module)

- `ReputationScore` entity per institution. Initial score: `1000`.
- `ReputationHistory` entity for tracking score changes over time.
- Penalty constants:
  - Minor (`S-01`, `S-02`): `-20`.
  - Major (`R-02`): `-150`.
  - Critical (`R-01`): `-400` + freeze institution.
  - Hack/system compromise (`H-01`): `0` points but freeze institution.
- Institution freeze logic for critical or hack scenarios.
- Special exemption rule: `Pending_Confirmation` shortcut events never deduct reputation.

#### Application Layer

- Event consumers for Core and Report events:
  - `ReportApprovedEvent` → scenario-based penalty calculation.
  - `DegreeRevokedEvent` / `DegreeUpdatedEvent` (confirmed degrees, school-fault reasons) → penalty.
  - `PendingDegreeShortcutEvent` → no penalty (exemption).
- Read model exposure for other modules (especially Recruitment).

#### Infrastructure Layer

- Reputation module's own EF Core configurations/tables.
- Event subscription (RabbitMQ consumer).
- Blockchain anchoring: push reputation history changes on-chain through blockchain abstractions.

#### API Layer

- Reputation query endpoints (institution reputation score, history).

#### UI Layer

- Reputation dashboard with line chart showing score history over time.
- List of penalty/change reasons for transparency.
- Dashboard shown only when the Reputation module is enabled.

### Isolation Boundary

Reputation must plug into ChainDegree Core and be removable without breaking Core degree issuance, verification, update, revocation, or reports. Core must never depend on Reputation interfaces or data models.

### Acceptance Criteria

- Reputation module can be disabled without breaking Core tests.
- Approved `S-01` or `S-02` deducts `20`.
- Approved `R-02` deducts `150`.
- Approved `R-01` deducts `400` and freezes institution.
- `H-01` freezes institution without point deduction.
- Pending-degree shortcut events never deduct reputation.
- Reputation history is persisted and can be queried.
- Reputation anchoring uses blockchain abstractions, not direct Core coupling.
- Reputation chart reflects score history when the module is enabled.
- UI remains usable when the Reputation module is disabled.

### Deliverable

The reputation engine automatically calculates and tracks institution reputation scores based on system events, with full blockchain anchoring and a transparent dashboard.

---

## Phase 6: Recruitment And Application — End To End (US-6, US-7 / UC-6, UC-7)

Deliver the complete recruitment and application workflow.

### Scope

#### Domain Layer

- `Job` entity with fields: title, salary range, description, degree filters.
- `DegreeFilter` value object: degree type, required major, minimum classification.
- `Application` entity with fields: `ApplicationId`, `JobId`, `DegreeId`, `StudentId`, `ProcessStatus`, `RankStatus` (`Highly_Qualified`, `Under_Qualified`).
- Matching logic: compare student degree against job filters.
- Rejection rule: revoked or `Pending_Revocation` degrees are always rejected.
- Force submission with `forceSubmit = true`.

#### Application Layer

- Use case: `PostJobCommand` / handler.
- Use case: `ApplyForJobCommand` / handler.
- Job ranking algorithm:

```text
JobScore =
  (W_base × ln(SalaryAvg))
  + (W_rep × ReputationScore_partner / 1000)
  + (W_time / (1 + daysSinceCreated))
```

Using: `W_base = 40`, `W_rep = 60`, `W_time = 100`.

- Fallback/default reputation value (`500`) when Reputation module is absent.

#### Infrastructure Layer

- EF Core configurations for `Job`, `DegreeFilter`, `Application`.
- Reputation read abstraction (reads from Reputation module or uses default).

#### API Layer

- Endpoint: `POST /api/v1/recruitment/jobs`.
  - Auth: `Recruiter`.
  - Returns `201 Created`.
- Endpoint: `POST /api/v1/recruitment/applications`.
  - Auth: `Student`.
  - Matching: `201 Created` with `Highly_Qualified`.
  - Non-matching + `forceSubmit: false`: `422 FILTER_CRITERIA_NOT_SATISFIED`.
  - Non-matching + `forceSubmit: true`: `201 Created` with `Under_Qualified`.
  - Revoked/pending-revocation degree: rejected.

#### UI Layer

- Recruiter job posting form with degree filter configuration section (`[+ Thêm điều kiện bằng]`).
- Student application flow with yellow warning modal for non-matching degrees.
- Force submit option: `[Vẫn nộp]`.
- Recruiter applicant list grouped by qualification status (`Highly_Qualified` above, `Under_Qualified` below).
- Job listings sorted by calculated `JobScore`.

### Isolation Boundary

Recruitment reads degree data from Core and reputation data through a read abstraction. It must support a fallback/default reputation value when the Reputation module is absent.

### Acceptance Criteria

- Only verified `Recruiter` can post jobs.
- Job creation returns `201 Created`.
- Job filters are stored in the traditional database only.
- Matching application returns `201 Created` and `Highly_Qualified`.
- Non-matching application with `forceSubmit = false` returns `422 FILTER_CRITERIA_NOT_SATISFIED`.
- Non-matching application with `forceSubmit = true` returns `201 Created` and `Under_Qualified`.
- Revoked or pending-revocation degree is always rejected.
- Job listings are sorted by calculated `JobScore`.
- Missing Reputation module uses the documented default/floor reputation value instead of failing.
- `POST_JOB` and `APPLY_JOB` logs are written.
- Student can force-submit after seeing the warning modal.
- Recruiter can distinguish `Highly_Qualified` and `Under_Qualified` applicants.

### Deliverable

Recruiters can post jobs with degree filters, students can apply with automatic matching and force submission, and job listings are ranked by the reputation-weighted algorithm.

---

## Phase 7: Deployment, Observability, And End-To-End Hardening

Package the whole system for local and production-like operation.

### Scope

- Docker Compose for:
  - API
  - Database
  - RabbitMQ
  - Worker
  - Besu nodes
- Environment configuration.
- Health checks for all services.
- Worker retry/DLQ visibility.
- Structured logging.
- Seed data for: roles, institutions, degree types, sample jobs.
- Full end-to-end test suite.
- Module toggles/service registration boundaries for:
  - ControlHub auth adapter.
  - Reputation module.

### Acceptance Criteria

- `docker compose up` starts all required services documented in the repository.
- API, worker, database, queue, and Besu nodes expose healthy status.
- Full issuance flow works end to end: issue → queue → Merkle anchor → confirmed → public verify.
- Full revocation flow works end to end: confirmed → pending revocation → blockchain transaction → revoked.
- Report approval triggers reputation penalty when the Reputation module is enabled.
- Core report approval still succeeds when the Reputation module is disabled.
- Recruitment flow works with job filters, forced submission, and ranking.
- Failed blockchain transaction is retried or appears in DLQ.
- Logs allow tracing a degree from API request to queue message to blockchain transaction.

### Deliverable

The complete system is containerized, observable, and verified end-to-end with all features integrated and toggleable modules.

---

## Recommended Build Order

1. **Phase 0**: Project foundation, audit, and auth boundary.
2. **Phase 1**: Degree issuance (US-1) — full vertical slice.
3. **Phase 2**: Degree update and revocation (US-2) — full vertical slice.
4. **Phase 3**: Public degree verification (US-3) — full vertical slice.
5. **Phase 4**: Complaints and reports (US-4) — full vertical slice.
6. **Phase 5**: Reputation engine (US-5) — full vertical slice.
7. **Phase 6**: Recruitment and application (US-6, US-7) — full vertical slice.
8. **Phase 7**: Deployment, observability, and end-to-end hardening.

Each phase is independently deployable after completion. This order follows business priority: core degree workflows first, then trust mechanisms, then recruitment, and finally production hardening.
