# System Brain - ChainDegree Project Map

Documenting the core architecture, classes, and logic of ChainDegree.

---

## 1. Domain Base Primitives (`ChainDegree.Core.Domain`)

### Core Entities & Base Classes

- **[Entity](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Domain/SharedKernel/Entity.cs)**: Base abstract class for all tracked entities.
  - Implements `IAuditableEntity` and `ISoftDeletable`.
  - Properties: `Id` (Guid), `CreatedAt` (DateTime), `UpdatedAt` (DateTime), `CreatedBy` (Guid), `UpdatedBy` (Guid), `DeletedAt` (DateTime?).
- **[AggregateRoot](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Domain/SharedKernel/AggregateRoot.cs)**: Inherits from `Entity`. Maintains private list of domain events `IDomainEvent` and provides `RaiseDomainEvent` / `ClearDomainEvents`.
- **[IInstitutionScoped](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Domain/SharedKernel/Interfaces/IInstitutionScoped.cs)**: Interface marker for entities scoped strictly to a single education institution. Checked dynamically in EF global query filters.

### Main Entities

- **[Degree](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Domain/Degrees/Degree.cs)**: Main entity for academic degrees. Inherits `AggregateRoot` and implements `IInstitutionScoped`.
  - Value Object: `CryptoData` (owns one `CryptoSnapshot` containing `PlainDataJson`, `Salt`, `DataHashLocal`).
- **[Student](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Domain/Students/Student.cs)**: Represents students. Inherits `AggregateRoot`.
  - Properties: `IdentityNumber` (CCCD - unique identifier), `FullName`, `Email`, `UserId`.
  - *Note*: Not institution-scoped directly as students can hold degrees from multiple institutions.
- **[InstitutionStudent](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Domain/Universities/Entities/InstitutionStudent.cs)**: Junction table managing student enrollment per institution.
  - Properties: `InstitutionId`, `StudentId`, `StudentCode` (student code at this specific institution), `EnrolledAt`.
- **[EducationInstitution](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Domain/Universities/EducationInstitution.cs)**: Represents universities. Inherits `Entity`.
- **[Registrar](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Domain/Universities/Entities/Registrar.cs)**: Represents registrars. Inherits `Entity` and implements `IInstitutionScoped`.
- **[BehaviorLog](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Domain/SharedKernel/BehaviorLog.cs)**: Append-only state logging entity. Creates instances via `CreateAudit()`.

---

## 2. Infrastructure Layer (`ChainDegree.Core.Infrastructure`)

### Persistence & Hardening

- **[ChainDegreeDbContext](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Infrastructure/Persistence/ChainDegreeDbContext.cs)**: EF Core context.
  - `OnModelCreating` delegates filter logic to `GlobalQueryFilterApplier` — only 3 lines: load configs, apply filters, call base.
  - Constructor injects `ICurrentUserAccessor` and `ILogger<ChainDegreeDbContext>`.
  - `_currentInstitutionId` field is `internal` to allow `Expression.Field` capture by `GlobalQueryFilterApplier` (same assembly).
- **[GlobalQueryFilterApplier](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Infrastructure/Persistence/QueryFilters/GlobalQueryFilterApplier.cs)**: Extracted filter logic. Scans entities for marker interfaces and applies `HasQueryFilter`:
  - `ISoftDeletable` → `e.DeletedAt == null`
  - `IInstitutionScoped` → `e.InstitutionId == dbContext._currentInstitutionId` (Expression.Field capture for live value per request)
  - Logs each entity + filter expression, plus summary count.
  - Guard clause: throws `InvalidOperationException` if `_currentInstitutionId` field not found.
  - `CombineFilters` helper gộp nhiều filter bằng `AndAlso`.
- **[AuditableEntityInterceptor](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Infrastructure/Persistence/Interceptors/AuditableEntityInterceptor.cs)**: Sets `CreatedAt`/`CreatedBy`/`UpdatedAt`/`UpdatedBy` automatically.
- **[SoftDeleteInterceptor](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Infrastructure/Persistence/Interceptors/SoftDeleteInterceptor.cs)**: Intercepts `Deleted` states of `ISoftDeletable` entities and modifies them to set `DeletedAt = DateTime.UtcNow`.
- **[UnitOfWork](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Infrastructure/Persistence/UnitOfWork.cs)**: Full transaction manager supporting implicit/explicit transactions, structured logging, safe rollbacks, and DbContext error wrapping.

### Outbox Pattern

- **[OutboxMessage](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Domain/SharedKernel/OutboxMessage.cs)**: Persistable domain event container.
- **[ConvertDomainEventsToOutboxInterceptor](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Infrastructure/Persistence/Interceptors/ConvertDomainEventsToOutboxInterceptor.cs)**: Serializes raised domain events into `OutboxMessage` entities within the save changes transaction.
- **[OutboxProcessor](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Infrastructure/Persistence/Outbox/OutboxProcessor.cs)**: Periodic background processor that polls `OUTBOX_MESSAGES` table, publishes events using MediatR, and records progress/errors.

---

## 3. Application Services & Abstractions (`ChainDegree.Core.Application`)

### Auth Abstractions

- **[ICurrentUserAccessor](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Application/Abstractions/Auth/ICurrentUserAccessor.cs)**: Exposes current user details (UserId, Role, InstitutionId, IpAddress). Implemented temporarily via `FakeCurrentUserAccessor`.
- **[IInstitutionOwnershipChecker](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Application/Abstractions/Auth/IInstitutionOwnershipChecker.cs)**: Checks registrar institution bounds.
- **[IRoleChecker](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Application/Abstractions/Auth/IRoleChecker.cs)**: Checks role permissions.

### Pipeline Behaviors

- **[ValidationBehavior](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Application/Common/Behaviors/ValidationBehavior.cs)**: MediatR open pipeline behavior that executes FluentValidation validators and returns generic error `Result` collections instead of throwing exceptions.
- **[IProblemException](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Application/Common/Exceptions/IProblemException.cs)**: Exception interface specifying mapped `StatusCode`, `ErrorCode`, and descriptive `Detail` for unified error handling.

---

## 4. API Layer (`ChainDegree.API`)

### Custom Filters & Exception Handling

- **[ChainDegreeProblemDetailsFactory](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.API/Filters/ChainDegreeProblemDetailsFactory.cs)**: Custom factory extending the default ASP.NET Core `ProblemDetailsFactory`. 
  - Standardizes all API error responses with a unified JSON structure.
  - Automatically enriches responses with transport metadata: `traceId` (Activity tracing or HTTP TraceIdentifier), `correlationId` (from headers `X-Request-Id` or `X-Correlation-Id`), and `timestamp` (UTC ISO 8601 format).
  - Normalizes the `type` field to point to internal documentation URIs: `https://chaindegree.io/errors/{slug}`.
- **[ErrorTypeMap](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.API/Filters/ErrorTypeMap.cs)**: Static helper mapping between HTTP status codes and specific documentation slugs.
- **[GlobalExceptionFilterAttribute](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.API/GlobalExceptionFilterAttribute.cs)**: Handles unhandled exceptions by pattern matching on `IProblemException` and using `ChainDegreeProblemDetailsFactory` to create the unified error response.
- **[ApiControllerBase](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.API/ApiControllerBase.cs)**: Base API controller providing utility methods (`ProcessResult`, `HandleFailure`) that map domain errors to HTTP results which are then processed by the factory.

---

## 5. Degree Verification & Public Portal (`Phase 3`)

- **[VerificationSource](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Domain/Degrees/Enums/VerificationSource.cs)**: Domain enum for verification source (`Blockchain_Merkle_Root`, `Local_Database`).
- **[VerificationSnapshot](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Domain/Degrees/ValueObjects/VerificationSnapshot.cs)**: Snapshot containing degree data, crypto hashes, Merkle proof, version, status, and institution details (`InstitutionName`, `InstitutionId`).
- **[VerifyDegreeQuery](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Application/Degrees/Queries/VerifyDegree/VerifyDegreeQuery.cs)**: Query supporting dual verification modes:
  - **QR Payload Mode**: `DegreeCode` + `Version?` + `IssuedAt?`
  - **Direct Data Mode**: `DegreeCode` + `PlainDataJson` + `Salt` (with canonicalization)
- **[VerifyDegreeQueryHandler](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Application/Degrees/Queries/VerifyDegree/VerifyDegreeQueryHandler.cs)**: Dual-verification handler executing snapshot resolution, status check, local integrity check (canonicalization + hashing), blockchain Merkle proof validation, and selective behavior logging.
- **[VerifyDegreeResponse](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Application/Degrees/Queries/VerifyDegree/VerifyDegreeResponse.cs)**: Public verification response object with `Verified`, `Status`, `VerificationSource`, `InstitutionName`, degree details, and `BlockchainDetails`.
- **[VerifyDegreeErrorResponse](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.API/Contracts/Degrees/VerifyDegreeErrorResponse.cs)**: Structured error response contract preventing internal data leakage.
- **[DegreesController.VerifyDegree](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.API/Controllers/DegreesController.cs#L155-L190)**: Public endpoint (`POST /api/v1/institutions/degrees/verify`) hardened with `[AllowAnonymous]`, `[RequestSizeLimit(65_536)]` (64KB DoS protection), and `[EnableRateLimiting("verify-degree")]` (30 req/min enumeration protection).



