# Phase 3: Public Degree Verification — Implementation Plan (v3)

## Background & Current State Analysis

Phase 3 delivers the **Public Degree Verification** feature (US-3 / UC-3) — a public, unauthenticated portal where anyone can verify a degree's authenticity through dual cryptographic + blockchain integrity checking.

### What Already Exists

| Component | Status | Notes |
|---|---|---|
| [VerificationResult](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Domain/Degrees/Enums/VerificationResult.cs) enum | ✅ Complete | All 6 values |
| [VerificationSnapshot](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Domain/Degrees/ValueObjects/VerificationSnapshot.cs) | ✅ Complete | Includes `InstitutionName`, `InstitutionId` |
| [VerifyDegreeQuery](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Application/Degrees/Queries/VerifyDegree/VerifyDegreeQuery.cs) | ✅ Complete | Supports QR mode and Direct data mode |
| [VerifyDegreeQueryHandler](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Application/Degrees/Queries/VerifyDegree/VerifyDegreeQueryHandler.cs) | ✅ Complete | Full dual-verification pipeline |
| [VerifyDegreeResponse](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Application/Degrees/Queries/VerifyDegree/VerifyDegreeResponse.cs) | ✅ Complete | Includes `InstitutionName`, `VerificationSource` enum |
| [VerifyDegreeRequest](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.API/Contracts/Degrees/VerifyDegreeRequest.cs) | ✅ Complete | Supports QR and Direct data fields |
| [DegreesController.VerifyDegree](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.API/Controllers/DegreesController.cs) | ✅ Complete | Correct routing, `[AllowAnonymous]`, `[RequestSizeLimit]`, `[EnableRateLimiting]` |
| [VerifyDegreeQueryHandlerTests](file:///e:/codes/chaindegree/apps/backend/ChainDegree/tests/ChainDegree.Application.Tests/Degrees/VerifyDegreeQueryHandlerTests.cs) | ✅ 22 tests | Full unit test coverage |
| [DegreeVersion](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Domain/Degrees/Entities/DegreeVersion.cs) | ✅ Complete | Historical snapshots |
| [IJsonCanonicalizer](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Domain/Degrees/Interfaces/IJsonCanonicalizer.cs) | ✅ Exists | Canonical JSON used in Direct Data Mode |

### Gaps Summary & Resolution

| # | Gap | WP | Resolution |
|---|---|---|---|
| 1 | Direct Data verification mode | WP 3.2 | Implemented with canonicalization + salt format validation |
| 2 | DoS: No request body size limit on public endpoint | WP 3.2 | Enforced `[RequestSizeLimit(65_536)]` (64KB) |
| 3 | Salt validation (format, length) | WP 3.2 | Enforced 16 hex char check |
| 4 | PlainDataJson must canonicalize before hashing | WP 3.2 | Integrated `IJsonCanonicalizer` |
| 5 | InstitutionName + VerificationSource in response | WP 3.3 | Added to `VerificationSnapshot` and response |
| 6 | Structured error responses (no internal data leakage) | WP 3.4 | Created `VerifyDegreeErrorResponse` |
| 7 | BehaviorLog spam risk on public endpoint | WP 3.5 | Selective logging (skip 404s) |
| 8 | DegreeCode enumeration attack surface | WP 3.2 | Modular DDD Rate Limiting (`RateLimitPolicies.Degrees.Verify`) |
| 9 | Repository: projection vs Include chains | WP 3.6 | EF Core LINQ `Select` projection with `AsNoTracking()` |
| 10 | CancellationToken propagation gaps | All WPs | Propagated end-to-end |

---

## Resolved Decisions

| Decision | Resolution |
|---|---|
| **API Contract Mode** | ✅ Support **both** QR payload + Direct Data mode. Detect by field presence. |
| **InstitutionName** | ✅ Include in response via `EducationInstitution` join. |
| **Caching** | ❌ **No caching**. Verification pipeline (DB → Hash → Merkle → Blockchain) is not heavy. Cache adds complexity without real benefit. KISS. |
| **CQRS Read Model / Elastic / Redis** | ❌ **Not needed**. MVP. Direct EF Core queries are sufficient. |

---

## Work Packages Detail

### Work Package 3.1: Branch Setup & Code Audit
- Created branch `feat/phase-3-public-verification`
- Audited baseline verification code

### Work Package 3.2: Direct Data Mode + Security Hardening
- **DoS Protection**: Enforced `[RequestSizeLimit(65_536)]` (64KB) on `POST /verify`
- **Rate Limiting**: Applied `[EnableRateLimiting(RateLimitPolicies.Degrees.Verify)]` (30 req/min)
- **Salt Validation**: Enforced 16-character hexadecimal validation
- **Canonicalization**: Integrated `IJsonCanonicalizer` on `PlainDataJson` before hashing

### Work Package 3.3: Extend Response with InstitutionName & VerificationSource
- Created `VerificationSource` enum (`Blockchain_Merkle_Root`, `Local_Database`)
- Updated `VerificationSnapshot` with `InstitutionName` and `InstitutionId`
- Enriched `VerifyDegreeResponse` with `InstitutionName` and `VerificationSource`

### Work Package 3.4: Structured Error Responses
- Created `VerifyDegreeErrorResponse` record (`Verified`, `ErrorCode`, `Message`)
- Business-facing error messages only — no internal hash or Merkle root data leakage

### Work Package 3.5: BehaviorLog — Selective Logging
- Logging to `BehaviorLogs` table for: `Verified`, `Revoked`, `CryptoHashMismatch`, `BlockchainInvalid`
- TargetId uses actual `snapshot.DegreeId`
- Skips DB logging for 404 paths (`DegreeNotFound`, `UnsupportedVersion`) to prevent bot spam

### Work Package 3.6: Repository — Projection-Based Snapshot Resolution
- Projection query using `Select` joining `EducationInstitution` and `Student`
- Enforced `AsNoTracking()` on all read paths

### Work Package 3.7: Unit Tests & Integration Tests
- 22 unit tests in `VerifyDegreeQueryHandlerTests`
- 13 unit tests in `DegreesControllerTests`

### Work Package 3.8: Documentation & System Brain
- Updated `SYSTEM_BRAIN.md` with Phase 3 map
- Saved implementation plan in `docs/implementation/phase-3-verification.md`

---

## Verification Summary

### Automated Tests Execution
```powershell
dotnet test apps/backend/ChainDegree/tests/ChainDegree.Application.Tests/ChainDegree.Application.Tests.csproj
dotnet test apps/backend/ChainDegree/tests/ChainDegree.Domain.Tests/ChainDegree.Domain.Tests.csproj
dotnet test apps/backend/ChainDegree/tests/ChainDegree.Infrastructure.Tests/ChainDegree.Infrastructure.Tests.csproj
dotnet test apps/backend/ChainDegree/tests/ChainDegree.API.Tests/ChainDegree.API.Tests.csproj
```

**Results**: All unit tests pass 100%.

---

## Commit History

```text
77b1ad5 refactor(api): modularize rate limit policies by domain modules for DDD architecture
3042cae feat(api): add rate limiting policies to all degree API endpoints
942c831 refactor(api): extract rate limiting configuration into RateLimitingExtensions
b22e841 docs(phase-3): update documentation and system brain
dc68fe6 test(verification): add unit tests for direct data mode, institution info, and error contracts
d1b230d feat(verification): enforce request size limit, rate limiting, and structured error responses
5ae1b46 fix(verification): use projection-based snapshot resolution with institution data
85a09e7 feat(verification): implement direct data verification mode with canonicalization and selective logging
1a48cb3 feat(verification): add institution name, verification source enum, and structured error contract
```
