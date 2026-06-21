# AGENTS.md

Guidance for AI agents working in this repository.

## Project Overview

ChainDegree is a .NET backend for blockchain-backed academic degree issuance, verification, update/revocation, reputation, and recruitment workflows. Treat the documents in `docs/` as the product and architecture source of truth, especially:

- `docs/implementation-plan.md`
- `docs/business-domain.md`
- `docs/business-logic-specification.md`
- `docs/api-specification.md`
- `docs/data-specification.md`
- `docs/adr/001.md`

The backend solution lives at `apps/backend/ChainDegree/ChainDegree.slnx` and currently targets `net10.0`.

## Repository Layout

- `apps/backend/ChainDegree/src/ChainDegree.API`: ASP.NET Core entry point, controllers, filters, API configuration.
- `apps/backend/ChainDegree/src/ChainDegree.Application`: use cases, commands, queries, DTOs, validation, application abstractions.
- `apps/backend/ChainDegree/src/ChainDegree.Domain`: domain entities, value objects, domain rules, domain events.
- `apps/backend/ChainDegree/src/ChainDegree.Infrastructure`: EF Core persistence, blockchain clients, background workers, external integrations.
- `apps/backend/ChainDegree/src/ChainDegree.SharedKernel`: shared primitives used across layers.
- `docs/diagrams`: Mermaid sources and generated PNG diagrams.
- `.agents/skills`: local implementation guidance for backend, EF Core, behavior rules, and Mermaid diagrams.

## Local Skills And Rules

Before significant work, check whether a local skill applies and read its `SKILL.md`:

- `.agents/skills/dotnet-backend-patterns/SKILL.md` for .NET/API/application architecture work.
- `.agents/skills/ef-core/SKILL.md` for persistence, migrations, and data access.
- `.agents/skills/mermaid-diagram-specialist/SKILL.md` for `.mmd` diagram updates.
- `.agents/rules/BEHAVIOR.md` for workspace behavior expectations.

Prefer repository-specific patterns and documentation over generic assumptions.

## Build And Test Commands

Run commands from `apps/backend/ChainDegree` unless noted otherwise.

```powershell
dotnet restore ChainDegree.slnx
dotnet build ChainDegree.slnx
dotnet test ChainDegree.slnx
dotnet run --project src/ChainDegree.API/ChainDegree.API.csproj
```

There are no test projects yet in the solution. When adding meaningful backend behavior, prefer adding focused xUnit tests under a future `tests/` solution folder.

## Coding Conventions

- Keep nullable reference types enabled and write null-safe C#.
- Use async APIs end to end for I/O, with `CancellationToken` parameters where appropriate.
- Keep controllers thin; put business workflows in Application services/handlers.
- Keep Domain independent of ASP.NET Core, EF Core, Nethereum, RabbitMQ, and other infrastructure concerns.
- Define external contracts as abstractions in Application or Domain, then implement them in Infrastructure.
- Use `IOptions<T>` for configuration sections such as JWT, Besu, token, queue, and worker settings.
- Prefer constructor injection and explicit service registration extension methods as the project grows.
- Keep comments rare and useful; explain non-obvious business or consistency rules.

## Architecture Boundaries

- API may depend on Application, Infrastructure, and SharedKernel.
- Application may depend on Domain and SharedKernel.
- Domain should stay dependency-light and should not depend on Application, Infrastructure, or API.
- Infrastructure may depend on Application, Domain, and SharedKernel to implement interfaces and persistence.
- SharedKernel should stay small and broadly reusable.
- Reputation must be an isolated module that plugs into ChainDegree.Core. It should be removable from the Core module at any time without breaking Core behavior.
- Auth will come from a custom NuGet package named `ControlHub`. Do not implement local auth infrastructure as if it were permanent; the package will be implemented separately and installed later.

When in doubt, follow the implementation phases in `docs/implementation-plan.md`. Do not pull later-phase concerns into earlier-phase work unless the task explicitly requires it.

## Persistence Guidance

- Use EF Core with focused `DbContext` configuration.
- Put entity configurations under `ChainDegree.Infrastructure/Persistence/Configurations`.
- Prefer fluent configuration via `IEntityTypeConfiguration<T>`.
- Keep migrations small and descriptively named.
- Use `AsNoTracking()` for read-only queries and projections when full entities are unnecessary.
- Avoid raw SQL unless there is a clear reason; parameterize it when used.

## Business Rules To Preserve

- Every state-changing action should write a `BehaviorLog`.
- Role authorization must distinguish `Registrar`, `Student`, `Recruiter`, `Admin`, and system/validator actors.
- Authentication and authorization integration should be designed around the future `ControlHub` package boundary.
- Degree status values must remain within the documented lifecycle:
  - `Pending_Confirmation`
  - `Confirmed`
  - `Confirmation_Error`
  - `Pending_Update`
  - `Pending_Revocation`
  - `Revoked`
  - `Frozen`
- Degree hashing must follow the documented canonical data plus salt process.
- Blockchain behavior should be isolated behind interfaces so workflows can be tested without a live Besu network.

## Documentation And Diagrams

- Update docs when behavior, API shape, data model, or lifecycle rules change.
- For Mermaid diagrams, edit the `.mmd` source first and regenerate PNGs only when needed.
- Keep API docs and implementation aligned; do not silently introduce undocumented endpoints or status transitions.

## Environment Notes

- Do not commit real secrets from `.env`.
- Keep `.env.example` updated when new configuration keys are required.
- Development loads environment values through `DotNetEnv` in `Program.cs`.
- `docker-compose.yml` currently exists but is empty; do not assume services are available unless you add and document them.

## Agent Operating Notes

- Use `rg`/`rg --files` for searching when available.
- Use targeted edits and avoid unrelated refactors.
- Do not overwrite user changes. If the worktree is dirty, understand the touched files before editing them.
- Run the narrowest relevant build/test command after code changes and report any command that could not be run.
