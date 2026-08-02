# Refactor EF Core Logging and Outbox Pattern Noise

The current setup logs every SQL statement executed by Entity Framework Core, which creates excessive noise in the console (especially due to polling from the `OutboxProcessor` and `BatchingDegreeWorker`). This implementation plan details the steps to suppress these SQL logs and enhance the business logs to provide more operational value.

## 🎯 Principle

> **Business logs should describe business progress, not implementation details.**

Logs about framework details (like EF Core executing SQL) should only appear when there are warnings or errors. At the `Information` level, we prioritize business milestones that operators care about, keeping the console clean, readable, and conducive to the observability goals of Phase 7.

## Proposed Changes

### 1. Configuration Changes

We will suppress `Microsoft.EntityFrameworkCore.Database.Command` logs by setting their minimum level to `Warning`. We will retain `Microsoft.EntityFrameworkCore: Information` so we don't lose other helpful EF Core lifecycle warnings or context. 
To adhere to DRY, this will primarily be set in `appsettings.json`. `appsettings.Development.json` will inherit it without duplication, keeping it clean for future overrides if needed.

#### [MODIFY] [appsettings.json](file:///E:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.API/appsettings.json)
- Add `"Microsoft.EntityFrameworkCore.Database.Command": "Warning"` under `Logging:LogLevel`.
- Add `"Microsoft.EntityFrameworkCore": "Information"` if not already present.

#### [MODIFY] [appsettings.Development.json](file:///E:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.API/appsettings.Development.json)
- Ensure it does not duplicate the `Database.Command` setting. Remove explicit log levels that are redundant.

### 2. Business Logic Changes (OutboxProcessor)

We will refine the logging in `OutboxProcessor.cs` to ensure that it emits clear, actionable business logs when it processes events, rather than flooding the console on every empty poll.

#### [MODIFY] [OutboxProcessor.cs](file:///E:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Infrastructure/Persistence/Outbox/OutboxProcessor.cs)
- **Keep early return**: Do not log anything if there are no messages (normal polling behavior).
- **Processing Batch**: Start with `Processing {Count} outbox messages...` and end with `Completed. Processed={Count}, Elapsed={ElapsedMs} ms`.
- **Success Log**: Log integration event publication clearly: `Outbox message published. MessageId={Id}, EventType={Type}`. Include `CorrelationId` and `BatchId/DegreeId` if available.
- **Retry Log**: If a failure occurs, provide explicit retry logs: `Publish failed. MessageId={Id}, EventType={Type}, Retry={RetryCount}/{MaxRetry}, NextRetry={Time}, Reason={Error}`.
- **Traceability**: Avoid generating fake random CorrelationIds in background polling so that request-driven trace IDs remain authentic and searchable. Log `MessageId` and `EventType` directly.

### 3. Business Logic Changes (BatchingDegreeWorker)

The `BatchingDegreeWorker` also contributes to console spam due to its own polling mechanism. We will apply the same principles here.

#### [MODIFY] [BatchingDegreeWorker.cs](file:///E:/codes/chaindegree/apps/backend/ChainDegree/src/ChainDegree.Infrastructure/BackgroundWorkers/BatchingDegreeWorker.cs)
- **No empty polling logs**: Remove logs like `Checking queue...` if they exist or occur on empty cycles.
- **Trigger Log**: Log when a batch actually triggers: `Batch triggered. BatchSize={Size}, Reason={Reason}`.
- **Milestone Logs**: Add clear milestone logs:
  - `Merkle tree built. Leaves={Count}, Root={Root}`
  - `Blockchain transaction submitted. TxHash={TxHash}`
  - `Batch completed. Elapsed={ElapsedMs} ms`

## Verification Plan

### Automated Tests
- `dotnet build ChainDegree.slnx` to ensure the project still builds properly after the changes in the workers.

### Manual Verification
- Run the API via `dotnet run --project src/ChainDegree.API/ChainDegree.API.csproj`.
- Observe the console output.
  - **Case 1 (Empty Queue)**: Console should be quiet, with no polling or SQL spam.
  - **Case 2 (1 Message)**: Console should show `Processing...`, `Published...`, and `Completed.` with Elapsed time.
  - **Case 3 (Publish Fail)**: Console should explicitly show `Retry X/Y` and eventually `Failed.`.
  - **Case 4 (Retry Success)**: Console should show `Retry X` followed by `Published.`.
