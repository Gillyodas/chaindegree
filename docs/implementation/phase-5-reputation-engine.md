# Phase 5: Reputation Engine — Implementation & Verification Plan (v2)

## Background & Current State Analysis

Phase 5 delivers the **Reputation Engine** feature (US-5 / UC-5) — an independent, plug-and-play module that automatically calculates and tracks institution reputation scores based on system events (like `ReportApprovedEvent`, `DegreeRevokedEvent`), with full blockchain anchoring and a transparent history.

### What Needs to be Built

| Component | Target Layer | Notes |
|---|---|---|
| `ReputationScore` AggregateRoot | Domain | Tracks current score (default: 1000) and `IsFrozen` state. Implements `RowVersion` for Optimistic Concurrency. |
| `ReputationHistory` Entity | Domain | Append-only history of score changes. Tracks `EventId` (for idempotency), `ReasonCode`, `ScoreChange`, and anchor status. |
| `PenaltyPolicy` | Domain | Maps `ReasonCode` $\rightarrow$ `ScoreImpact` $\rightarrow$ `Freeze` action. |
| Event Consumers | Application | Consumes Core events via RabbitMQ (Idempotent, Transactional). |
| Reputation Queries | Application | Queries for fetching current score and history logs (Uses `AsNoTracking` and Caching). |
| EF Core Configurations | Infrastructure | Separate tables (`reputation` schema) with Concurrency Tokens. |
| Blockchain Worker | Infrastructure | Background worker that anchors `HistoryHash` asynchronously. |
| Reputation API Endpoints | API | `GET /api/v1/reputation/institutions/{id}` and `/history`. |

---

## Technical & Business Decisions Summary

| Decision | Resolution |
|---|---|
| **Bounded Context & Decoupling** | ✅ The Reputation module MUST be completely decoupled from Core. It listens to Core events via RabbitMQ. If Reputation is offline, Core degree workflows must still succeed. |
| **Consumer Idempotency (Critical)** | ✅ RabbitMQ provides *At-Least-Once* delivery. Consumers MUST be idempotent using a Unique Constraint on `ReputationHistory.EventId` (or a `ProcessedMessages` table) to prevent double penalties on Ack failures. |
| **Concurrency & Race Conditions (Critical)** | ✅ `ReputationScore` must use Optimistic Concurrency (`RowVersion`). Multiple events for the same institution arriving simultaneously must not overwrite each other (e.g., $1000 - 20$ and $1000 - 150$ must result in $830$, not $850$). |
| **Rich Domain Aggregate** | ✅ State mutations MUST happen inside the Domain. `ReputationScore` will expose methods like `ApplyPenalty(penaltyPolicy, reasonCode)`, `ApplyExemption()`, `Freeze()`. Do not perform math (`score -= 20`) directly in Application/Repo layers. |
| **History Immutability** | ✅ `ReputationHistory` is **Append-Only** (Never Update, Never Delete). If an adjustment is needed, insert a new compensating row. Business logic must rely on `ReasonCode`, using `ReasonDescription` only for display. |
| **Transactional Processing** | ✅ Consumer workflow: Begin Transaction $\rightarrow$ Check Idempotency $\rightarrow$ Update Score $\rightarrow$ Insert History $\rightarrow$ Commit $\rightarrow$ ACK. |
| **Blockchain Anchoring via Worker** | ✅ Consumers must NEVER call the blockchain directly to prevent timeout/retry blockages. Workflow: Consumer saves `PendingAnchor` $\rightarrow$ Worker processes pending rows $\rightarrow$ Anchored to Besu. |
| **Data Privacy on Blockchain** | ✅ To save gas and protect internal data, only the `HistoryHash` (Hash of the history record) will be anchored, rather than full reason descriptions and scores. |

---

## Work Packages Detail & Execution Plan

### Work Package 5.1: Reputation Domain Model & Penalty Policy
- **Tasks**:
  - Define `ReputationScore` Aggregate Root (`Id`, `InstitutionId`, `CurrentScore`, `IsFrozen`, `RowVersion`).
  - Add domain methods: `ApplyPenalty(PenaltyPolicy policy)`, `ApplyExemption()`, `Freeze()`.
  - Define `ReputationHistory` Entity (`Id`, `EventId`, `ScoreChange`, `ReasonCode`, `Timestamp`, `AnchorStatus`, `TxHash`).
  - Define `PenaltyPolicy` mappings (e.g., `S-01` $\rightarrow$ `-20`, `R-01` $\rightarrow$ `-400` + Freeze).
- **Done Criteria**: Domain encapsulates all logic. `ReputationHistory` enforces append-only rules. 100% Unit test coverage on `ReputationScore` mutations.

### Work Package 5.2: Application Layer - Consumers & Queries
- **Tasks**:
  - Implement `ReportApprovedEventConsumer`, `DegreeRevokedEventConsumer`, etc.
  - Implement Idempotency check via `EventId` (skip if already processed).
  - Wrap processing in explicit DB Transactions.
  - Implement Queries (`GetInstitutionReputationQuery`, `GetReputationHistoryQuery`) enforcing `AsNoTracking()`.
  - Include `EventVersion` or `SchemaVersion` awareness in consumers (for future-proofing).
- **Done Criteria**: Concurrent events trigger `DbUpdateConcurrencyException`. Duplicates are skipped.

### Work Package 5.3: Infrastructure - Persistence & Blockchain Worker
- **Tasks**:
  - Add EF Core `IEntityTypeConfiguration` for `reputation` schema. Map `RowVersion` as `IsRowVersion()`. Add Unique Index on `ReputationHistory.EventId`.
  - Implement `ReputationAnchoringBackgroundWorker` that polls/listens for `PendingAnchor` histories.
  - Implement `IReputationBlockchainService` to anchor `HistoryHash` to Besu.
- **Done Criteria**: Blockchain failures do not impact RabbitMQ event processing. History is safely anchored asynchronously.

### Work Package 5.4: API Layer & Endpoints
- **Tasks**:
  - Create `ReputationsController`.
  - Add `GET /api/v1/reputation/institutions/{id}`. Apply `Cache-Control` headers (e.g., `max-age=30`) for browser caching without Redis.
  - Add `GET /api/v1/reputation/institutions/{id}/history`.
  - Handle Domain Errors and Concurrency exceptions gracefully.
- **Done Criteria**: Endpoints return cached data correctly.

### Work Package 5.5: Integration Testing & Verification
- **Tasks**:
  - Write Integration Tests simulating full flow (Core Event $\rightarrow$ Consumer $\rightarrow$ DB $\rightarrow$ Worker $\rightarrow$ Blockchain Mock).
  - Simulate Concurrency: Fire 2 simultaneous events for the same institution and verify Optimistic Concurrency handles it correctly.
  - Simulate Idempotency: Send the exact same `EventId` twice and verify it only processes once.
- **Done Criteria**: Race conditions prevented. Idempotency confirmed.

### Work Package 5.6: Documentation
- **Tasks**:
  - Update `SYSTEM_BRAIN.md` or API specs if necessary.
- **Done Criteria**: Docs are in sync with the implementation.

---

## Verification & Integration Test Plan

### Automated Tests Execution Expected Commands
```powershell
dotnet test tests/ChainDegree.Domain.Tests/ChainDegree.Domain.Tests.csproj --filter "FullyQualifiedName~Reputation"
dotnet test tests/ChainDegree.Application.Tests/ChainDegree.Application.Tests.csproj --filter "FullyQualifiedName~Reputation"
dotnet test tests/ChainDegree.API.Tests/ChainDegree.API.Tests.csproj --filter "FullyQualifiedName~ReputationsControllerTests"
```

### End-To-End Integration Goals
- **Goal 1 (Concurrency)**: Two consumers processing `-20` and `-150` simultaneously result in a final score of `830` using `RowVersion` retries.
- **Goal 2 (Idempotency)**: A delayed RabbitMQ ACK causes a duplicate delivery. Consumer detects `EventId` in `ReputationHistory` and safely ACKs without deducting points twice.
- **Goal 3 (Worker Anchoring)**: Consumer commits DB transaction rapidly. Worker picks up `PendingAnchor` state, calculates `HistoryHash`, submits to Besu, and updates DB with `TxHash`.

---

## Commit Plan (Deployable Intentions)

```text
docs(phase-5): create v2 implementation plan for reputation engine
feat(reputation): define aggregate root, penalty policy, and domain invariants
feat(reputation): implement optimistic concurrency and idempotency in consumers
feat(reputation): add ef core configurations for reputation schema with rowversion
feat(reputation): implement async background worker for history hash anchoring
feat(api): create reputation endpoints with cache-control and asnotracking queries
test(reputation): add concurrency, idempotency, and workflow integration tests
```
