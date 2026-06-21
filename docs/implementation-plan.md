# ChainDegree Implementation Plan

This plan breaks the system into isolated implementation phases based on the project documentation and `AGENTS.md`. The most important architectural boundaries are:

- Core degree workflows must not depend on the Reputation module.
- Auth is an independent integration boundary provided by the future `ControlHub` NuGet package.
- Blockchain behavior must stay behind interfaces so workflows can be tested without a live Besu network.
- Domain must stay independent of ASP.NET Core, EF Core, Nethereum, RabbitMQ, and other infrastructure concerns.

## Phase 1: Core Domain And Audit Foundation

Build the shared domain foundation without permanent local auth or reputation behavior.

### Scope

- Core actors as domain concepts:
  - `Registrar`
  - `Student`
  - `Recruiter`
  - `Admin`
  - `Validator/System`
- Core entities:
  - Institution
  - User reference/profile where needed by ChainDegree
  - Degree
  - DegreeType
  - BehaviorLog
- Shared degree lifecycle statuses:
  - `Pending_Confirmation`
  - `Confirmed`
  - `Confirmation_Error`
  - `Pending_Update`
  - `Pending_Revocation`
  - `Revoked`
  - `Frozen`
- Central behavior log:
  - `BehaviorLogs(Id, ActorId, ActionType, ImpactedEntityId, Description, Timestamp)`
- Domain events for state changes, such as degree created, degree revoked, report approved, and fraud detected.

### Isolation Boundary

Do not implement permanent auth infrastructure, blockchain, reports, reputation, or recruitment in this phase. Domain events may be defined, but they must not require any specific downstream module.

### Acceptance Testing Criteria

- Degree lifecycle statuses are constrained to documented values.
- Every state-changing core action can produce a behavior log entry.
- Domain objects do not depend on ASP.NET Core, EF Core, RabbitMQ, Nethereum, or ControlHub.
- Core domain events can be raised without any Reputation module installed.
- Audit fields behave consistently: `CreatedAt`, `UpdatedAt`, `DeletedAt`, `CreatedBy`, `UpdatedBy`.

## Phase 2: Independent Auth Integration Boundary

Prepare ChainDegree to use the future `ControlHub` NuGet package without treating local auth as permanent infrastructure.

### Scope

- Define application-level authorization abstractions, for example:
  - Current user accessor
  - Role/permission checker
  - Institution ownership checker
- Add thin API authorization policies for:
  - `Registrar`
  - `Student`
  - `Recruiter`
  - `Admin`
  - System/validator actors
- Use temporary test doubles or minimal adapters until `ControlHub` is available.
- Keep JWT/configuration assumptions behind replaceable interfaces.

### Isolation Boundary

Auth must be swappable. Do not hard-code a permanent local identity model, token issuer, password system, or user-management workflow inside ChainDegree.

### Acceptance Testing Criteria

- Registrar-only endpoints can be protected through an abstraction, not direct local auth logic.
- Student, Recruiter, Admin, and System policies are distinguishable.
- Ownership checks can verify that a `Registrar` belongs to the institution managing a degree.
- Tests can run using fake auth/current-user implementations.
- Replacing the temporary adapter with `ControlHub` should not require changes in Domain or core use cases.

## Phase 3: Degree Storage And Hashing Engine

Implement the local database side of degree issuance and cryptographic integrity.

### Scope

- Degree creation in the database.
- Canonical JSON generation.
- Salt generation using a fixed 16-character cryptographic hex salt.
- Hashing formula:

```text
DataHash = SHA-256(PlainDataCanonical + Salt)
```

- Duplicate prevention for the same student, degree type, and issuing institution.

### Isolation Boundary

Use a fake queue and fake blockchain adapter. This phase only proves that local degree data and hashes are correct.

### Acceptance Testing Criteria

- Creating a valid degree stores `Salt`, `DataHash`, plain degree fields, and status `Pending_Confirmation`.
- Reordering JSON fields produces the same canonical hash.
- Changing any degree field produces a different hash.
- Duplicate degree issuance is rejected and no database record is created.
- Invalid required fields return validation errors.
- `CREATE_DEGREE` behavior log is written.

## Phase 4: Async Issuance Queue And Batch Processing

Implement the application mempool described in the ADR.

### Scope

- Endpoint: `POST /api/v1/institutions/degrees`.
- Return `202 Accepted` immediately with:
  - `batchId`
  - `status`
  - `estimatedWaitTimeSeconds`
  - `checkStatusUrl`
- Push degree IDs into RabbitMQ or a database-backed queue.
- Background worker consumes queued degrees.
- Dual-trigger batching:
  - Max batch size: `500`.
  - Max wait time: `3-5 minutes`.
- Failure path sets degree status to `Confirmation_Error`.

### Isolation Boundary

Mock the blockchain transaction sender. This phase validates async queuing and worker behavior without requiring a live blockchain.

### Acceptance Testing Criteria

- API returns `202 Accepted` without waiting for blockchain processing.
- Degree remains `Pending_Confirmation` until the worker completes.
- Batch triggers when 500 queued records exist.
- Batch triggers when max wait time expires, even with fewer than 500 records.
- Successful worker run updates degrees to `Confirmed`.
- Failed worker run updates degrees to `Confirmation_Error`.
- Retry can requeue a failed confirmation.

## Phase 5: Blockchain Anchor And Merkle Proof Layer

Implement the real blockchain-facing layer.

### Scope

- Hyperledger Besu QBFT integration.
- Nethereum transaction sender.
- Smart contract storage for:
  - Merkle roots
  - Degree status anchors
  - Generic event/history anchors needed by independent modules
- Merkle tree generation from `data_hash_local`.
- Store transaction metadata:
  - `TxHash`
  - `BlockNumber`
  - Merkle root/proof data
- Configure Besu with:

```text
mining-empty-blocks = false
```

### Isolation Boundary

Expose blockchain behavior through interfaces consumed by Application and Infrastructure. Core degree workflows should not know whether the anchor is Besu, a mock, or another implementation.

### Acceptance Testing Criteria

- Worker sends one blockchain transaction per batch, not one per degree.
- One Merkle root represents all degree hashes in the batch.
- After receipt confirmation, local records store `TxHash` and `BlockNumber`.
- Merkle proof can validate an individual degree hash against the anchored root.
- Blockchain transaction failure routes the batch to retry or DLQ and does not mark degrees `Confirmed`.
- No empty blocks are generated when the local network is idle.

## Phase 6: Degree Update And Revocation Lifecycle

Implement US-2 in the Core module.

### Scope

- Endpoint: `POST /api/v1/institutions/degrees/{id}/revoke`.
- Endpoint: `PUT /api/v1/institutions/degrees/{id}`.
- Confirmed degree path:
  - Move to `Pending_Revocation` or `Pending_Update`.
  - Queue blockchain state transaction.
  - Finalize to `Revoked` or back to `Confirmed`.
- Pending degree shortcut:
  - Revoke immediately to `Revoked`.
  - Update directly and recalculate hash.
  - Publish a domain event indicating the shortcut was exempt from reputation impact.
- Required reason/comment handling.

### Isolation Boundary

Core may publish events, but it must not call Reputation services directly. The workflow must still complete if the Reputation module is absent.

### Acceptance Testing Criteria

- Only the owning institution's `Registrar` can update or revoke a degree.
- `Confirmed` degree revocation returns `202 Accepted`.
- `Pending_Confirmation` degree revocation returns immediate success and status `Revoked`.
- Pending shortcut emits an exemption signal but does not require Reputation to exist.
- Updating a pending degree recalculates `DataHash`.
- Updating or revoking a confirmed degree creates a new blockchain state transaction.
- `UPDATE_DEGREE` or `REVOKE_DEGREE` log is written with original status and reason.

## Phase 7: Public Degree Verification

Implement US-3 in the Core module.

### Scope

- Public endpoint: `POST /api/v1/institutions/degrees/verify`.
- Fetch local degree data, salt, and hash.
- Recalculate hash from database/plain data.
- Compare against local stored hash.
- Validate against blockchain/Merkle root.
- Return verified, revoked, or fraud/mismatch result.

### Isolation Boundary

Verification only detects status and integrity. It does not file complaints, mutate reputation, or trigger penalties.

### Acceptance Testing Criteria

- Public users can verify without login.
- Valid confirmed degree returns `200 OK` with `verified: true`.
- Revoked degree returns a clear revoked result.
- Modified database/plain data causes `422 CRYPTO_HASH_MISMATCH`.
- Invalid blockchain/Merkle proof causes `422 BLOCKCHAIN_INVALID`.
- Every lookup writes `VERIFY_DEGREE` behavior log, anonymous or identified.

## Phase 8: Reports Module

Implement US-4 as an independent reporting workflow that can emit events for other modules.

### Scope

- Endpoint: `POST /api/v1/institutions/degrees/reports`.
- Evidence file upload.
- Report statuses:
  - `Pending_Review`
  - Approved/rejected states
- Endpoint: `POST /api/v1/institutions/reports/{id}/approve`.
- Emit events such as:
  - `ReportSubmittedEvent`
  - `ReportApprovedEvent`
  - `FraudulentDataDetectedEvent`

### Isolation Boundary

Reports may read degree data and publish events, but it must not calculate or persist reputation scores directly.

### Acceptance Testing Criteria

- Student can report only their own degree.
- Recruiter can report any degree they encountered.
- Missing report type, description, or evidence file is rejected.
- Report creation returns `201 Created` and status `Pending_Review`.
- Admin approval returns `202 Accepted`.
- Approval emits the documented event without requiring the Reputation module to be installed.
- `SUBMIT_REPORT` behavior log is written.

## Phase 9: Independent Reputation Module

Implement US-5 as a plug-in module that consumes Core and Report events.

### Scope

- Reputation score starts at `1000`.
- Consume events from Core and Reports:
  - confirmed school-side revocation/update events
  - approved report events
  - fraud/hack events
  - pending-degree exemption events
- Penalties:
  - Minor: `-20`
  - Major: `-150`
  - Critical: `-400`
  - Hack/system compromise: `0` but freeze institution
- Freeze institution for critical or hack scenarios.
- Persist reputation history in the module's own model/tables.
- Anchor reputation state/history on-chain through blockchain abstractions.
- Expose a read model for other modules, especially Recruitment.

### Isolation Boundary

Reputation must plug into ChainDegree Core and be removable without breaking Core degree issuance, verification, update, revocation, or reports. Core must never depend on Reputation interfaces or data models.

### Acceptance Testing Criteria

- Reputation module can be disabled without breaking Core tests.
- Approved `S-01` or `S-02` deducts `20`.
- Approved `R-02` deducts `150`.
- Approved `R-01` deducts `400` and freezes institution.
- `H-01` freezes institution without point deduction.
- Pending-degree shortcut events never deduct reputation.
- Reputation history is persisted and can be queried.
- Reputation anchoring uses blockchain abstractions, not direct Core coupling.

## Phase 10: Recruitment And Application Workflow

Implement US-6 and US-7.

### Scope

- Endpoint: `POST /api/v1/recruitment/jobs`.
- Degree filters:
  - Degree type
  - Required major
  - Minimum classification
- Endpoint: `POST /api/v1/recruitment/applications`.
- Match student degree against job filters.
- Rank application:
  - `Highly_Qualified`
  - `Under_Qualified`
- Reject revoked or pending-revocation degrees.
- Forced submission with `forceSubmit = true`.
- Job ranking algorithm:

```text
JobScore =
  (W_base * ln(SalaryAvg))
  + (W_rep * ReputationScore_partner / 1000)
  + (W_time / (1 + daysSinceCreated))
```

Using:

```text
W_base = 40
W_rep = 60
W_time = 100
```

### Isolation Boundary

Recruitment reads degree data from Core and reputation data through a read abstraction. It should still support a fallback/default reputation value when the Reputation module is absent.

### Acceptance Testing Criteria

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

## Phase 11: UI/UX Implementation

Build the user-facing screens after the backend contracts are stable.

### Scope

- Registrar degree issuance form with multi-degree add button.
- Degree list with status badges.
- Retry button for `Confirmation_Error`.
- Degree detail with update/revoke actions.
- Public verification portal.
- Report modal with file upload.
- Reputation dashboard with line chart, shown only when the Reputation module is enabled.
- Recruiter job posting form.
- Student application flow with yellow warning modal.
- Recruiter applicant list grouped by qualification status.

### Isolation Boundary

Use stable API contracts from earlier phases. UI should not contain business rules that belong in the backend. Auth UI should assume `ControlHub` ownership for identity concerns.

### Acceptance Testing Criteria

- Registrar sees pending, confirmed, revoked, and error states with correct visual badges.
- Successful batch issuance shows a non-blocking toast.
- Duplicate degree rows remain in the form with inline errors.
- Verification result shows green, red, or orange/fraud state correctly.
- Report form requires type, description, and evidence.
- Reputation chart reflects score history when the module is enabled.
- UI remains usable when the Reputation module is disabled.
- Student can force-submit after seeing the warning modal.
- Recruiter can distinguish `Highly_Qualified` and `Under_Qualified` applicants.

## Phase 12: Deployment, Observability, And End-To-End Hardening

Package the whole system for local and production-like operation.

### Scope

- Docker Compose for:
  - API
  - Database
  - RabbitMQ
  - Worker
  - Besu nodes
- Environment configuration.
- Health checks.
- Worker retry/DLQ visibility.
- Structured logs.
- Seed data for:
  - Roles
  - Institutions
  - Degree types
  - Sample jobs
- Full end-to-end test suite.
- Module toggles or service registration boundaries for:
  - ControlHub auth adapter
  - Reputation module

### Isolation Boundary

This phase integrates the completed modules. Avoid introducing new business behavior here unless needed to close deployment or observability gaps.

### Acceptance Testing Criteria

- `docker compose up` starts all required services documented in the repository.
- API, worker, database, queue, and Besu nodes expose healthy status.
- Full issuance flow works end to end:
  - Issue degree
  - Queue
  - Merkle anchor
  - Confirmed
  - Public verify
- Full revocation flow works end to end:
  - Confirmed
  - Pending revocation
  - Blockchain transaction
  - Revoked
- Report approval triggers reputation penalty when the Reputation module is enabled.
- Core report approval still succeeds when the Reputation module is disabled.
- Recruitment flow works with job filters, forced submission, and ranking.
- Failed blockchain transaction is retried or appears in DLQ.
- Logs allow tracing a degree from API request to queue message to blockchain transaction.

## Recommended Build Order

1. Core domain and audit foundation.
2. Independent auth integration boundary.
3. Degree storage and hashing engine.
4. Async queue and batch worker.
5. Blockchain anchor and Merkle proof layer.
6. Degree update and revocation lifecycle.
7. Public verification.
8. Reports module.
9. Independent reputation module.
10. Recruitment and application workflow.
11. UI/UX.
12. Deployment and end-to-end hardening.

This order keeps identity integration replaceable, keeps Reputation removable from Core, and builds the most important system guarantees early: auditability, cryptographic integrity, async processing, and blockchain anchoring.
