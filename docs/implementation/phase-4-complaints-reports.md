# Phase 4: Complaints And Reports — Implementation & Security Verification Plan (v2)

## Background & Current State Analysis

Phase 4 delivers the **Complaints and Reports** feature (US-4 / UC-4 & US-5) — a secure, audit-logged workflow allowing Students and Recruiters to report degree data errors or fraudulent issuances with evidence files, and allowing Admins to review (Approve/Reject) them while emitting domain events to the Reputation module.

### Components Delivered

| Component | Status | Notes |
|---|---|---|
| [UserRoleEnum](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Domain/SharedKernel/Enums/UserRoleEnum.cs) | ✅ Complete | Strongly-typed role enum (`Student`, `Recruiter`, `Registrar`, `Admin`, `System`) |
| [Report](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Domain/Reports/Report.cs) AggregateRoot | ✅ Complete | Refactored from `Entity` to `AggregateRoot` with `Create`, `Approve`, `Reject` methods |
| [Domain Events](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Domain/Reports/Events/) | ✅ Complete | `ReportSubmittedEvent`, `ReportApprovedEvent`, `ReportRejectedEvent`, `FraudulentDataDetectedEvent` |
| [ReportErrors](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.SharedKernel/DomainErrors/Reports/ReportErrors.cs) | ✅ Complete | Detailed domain error codes |
| [IReportRepository](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Application/Abstractions/Repositories/IReportRepository.cs) | ✅ Complete | Includes duplicate pending report check |
| [IEvidenceStorageService](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Application/Abstractions/Services/IEvidenceStorageService.cs) | ✅ Complete | Abstraction for safe file operations |
| [SubmitReportCommandHandler](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Application/Reports/Commands/SubmitReport/SubmitReportCommandHandler.cs) | ✅ Complete | Includes ownership check, duplicate prevention, and storage rollback on failure |
| [GetEvidenceQueryHandler](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Application/Reports/Queries/GetEvidence/GetEvidenceQueryHandler.cs) | ✅ Complete | Authorized evidence download (Student/Recruiter own only, Admin all) |
| [ApproveReportCommandHandler](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Application/Reports/Commands/ApproveReport/ApproveReportCommandHandler.cs) | ✅ Complete | State transition + conditional `FraudulentDataDetectedEvent` emission |
| [RejectReportCommandHandler](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Application/Reports/Commands/RejectReport/RejectReportCommandHandler.cs) | ✅ Complete | Rejection reason tracking + evidence file deletion lifecycle |
| [LocalFileSystemEvidenceStorageService](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Infrastructure/Services/LocalFileSystemEvidenceStorageService.cs) | ✅ Complete | Magic Number validation (%PDF, PNG, JPG), Guid filename generation, outside `wwwroot` |
| [ReportsController](file:///e:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.API/Controllers/ReportsController.cs) | ✅ Complete | Multipart upload, download stream, approve, reject endpoints with IP+User rate limiting |

---

## Technical & Security Decisions Summary

| Decision | Resolution |
|---|---|
| **Aggregate Root** | ✅ Refactored `Report` to inherit `AggregateRoot` so `ConvertDomainEventsToOutboxInterceptor` automatically converts events to Outbox messages. |
| **MIME & Magic Number Validation** | ✅ Validates binary file headers (`%PDF` `0x25 0x50 0x44 0x46`, `PNG` `0x89 0x50 0x4E 0x47`, `JPG` `0xFF 0xD8 0xFF`) to prevent extension renaming attacks. |
| **Path Traversal & Filename Hardening** | ✅ Generates `Guid.NewGuid() + ext` filenames, stores files in `App_Data/Evidences` outside `wwwroot`, and avoids exposing absolute server paths. |
| **Download Authorization** | ✅ Enforces RBAC + Ownership on `GET /reports/{id}/evidence`. Students and Recruiters can only download their own evidence; Admin can download all. |
| **File Lifecycle Policy** | ✅ Approved reports retain evidence files permanently for audit proof; Rejected reports delete evidence files immediately. |
| **Storage Rollback** | ✅ Wrap file save and DB insert in try-catch to delete orphaned files if DB commit fails. |
| **Anti-Spam & Rate Limiting** | ✅ Enforces duplicate pending report check per user per degree, limits multipart size to 5MB, limits description to 2000 chars, and partitions submit rate limiter by `IP + UserID`. |
| **Conditional Event Emission** | ✅ `FraudulentDataDetectedEvent` is ONLY emitted for `ReportTypeEnum.Fraudulent_Data` (not `Administrative_Error`) to prevent incorrect reputation penalties. |
| **Decoupling Bounded Context** | ✅ Module Core emits outbox events without any direct dependency on the Reputation module. |

---

## Verification Summary

### Automated Tests Execution
```powershell
dotnet test tests/ChainDegree.Domain.Tests/ChainDegree.Domain.Tests.csproj
dotnet test tests/ChainDegree.Application.Tests/ChainDegree.Application.Tests.csproj
dotnet test tests/ChainDegree.API.Tests/ChainDegree.API.Tests.csproj --filter "FullyQualifiedName~ReportsControllerTests"
```

**Results**: All unit tests and API controller tests pass 100%.
