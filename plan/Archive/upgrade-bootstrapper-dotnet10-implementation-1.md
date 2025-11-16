---
goal: Upgrade Bootstrapper console to .NET 10 LTS with EF Core 10 alignment and tooling compliance
version: 1.0
date_created: 2025-11-06
last_updated: 2025-11-06
owner: Red Dog Modernization Team
status: Planned
tags: [upgrade, dotnet10, bootstrapper, efcore, console]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

Bootstrapper initializes the Accounting database via EF Core migrations. This plan upgrades `RedDog.Bootstrapper` to .NET 10.0 LTS in lockstep with AccountingService/AccountingModel.

## Research References

- `docs/research/dotnet-upgrade-analysis.md` (Dependency Analysis, EF Core 10 breaking changes, Docker/runtime updates)
- `plan/testing-validation-strategy.md` (Tooling workflow, migration smoke tests, artifact expectations)
- `plan/cicd-modernization-strategy.md` (CI tooling-audit/build/publish requirements)

## 1. Requirements & Constraints

- **REQ-001**: Target framework `net10.0`, nullable enabled, implicit usings, LangVersion 14.0.
- **REQ-002**: EF Core packages upgraded to 10.0.x to match AccountingModel/AccountingService.
- **REQ-003**: Dapr.Client upgraded to 1.16.0 if used for config retrieval.
- **SEC-001**: `dotnet list package --vulnerable` returns zero vulnerabilities (artifact saved).
- **CON-001**: Console app must remain non-interactive; exit codes indicate success/failure (0/1).
- **CON-002**: Dockerfile/runtime image uses `mcr.microsoft.com/dotnet/runtime:10.0` (and sdk for build stage if containerized).
- **GUD-001**: Use `Host.CreateApplicationBuilder` or top-level statements with dependency injection for EF Core contexts.
- **PAT-001**: All migration operations executed asynchronously; no `.Wait()`.

## 2. Implementation Steps

### Implementation Phase 1

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-000** | Run Upgrade Assistant: `upgrade-assistant upgrade RedDog.Bootstrapper/RedDog.Bootstrapper.csproj --entry-point RedDog.Bootstrapper/RedDog.Bootstrapper.csproj --non-interactive --skip-backup false > artifacts/upgrade-assistant/bootstrapper.md`. | | |
| **TASK-001** | Run `dotnet workload restore` + `dotnet workload update`; log to `artifacts/dependencies/bootstrapper-workloads.txt`. | | |
| **TASK-002** | Capture dependency baselines using `dotnet list ... --outdated/--vulnerable/--graph`; store under `artifacts/dependencies/bootstrapper-*.txt`. | | |
| **TASK-003** | Run API Analyzer; log to `artifacts/api-analyzer/bootstrapper.md`. | | |
| **TASK-004** | Update csproj to net10.0, enable nullable/implicit usings, set LangVersion 14.0. | | |
| **TASK-005** | Upgrade packages: `Microsoft.EntityFrameworkCore.*` 10.0.x, `Dapr.Client` 1.16.0 (if used), `Microsoft.Extensions.*` 10.0.x. | | |
| **TASK-006** | Re-run restore + dependency audits to confirm zero outdated/vulnerable packages. | | |
| **TASK-007** | Update Dockerfile (if present) to .NET 10 runtime image; ensure `dotnet publish` used for build stage. | | |
| **TASK-008** | `dotnet build RedDog.Bootstrapper/RedDog.Bootstrapper.csproj -warnaserror`. | | |

### Implementation Phase 2

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-009** | Modernize Program.cs (Generic Host) to configure services, logging, and EF Core contexts; remove blocking `.Wait()` usage. | | |
| **TASK-010** | Update EF Core migration runner logic to use async calls (`context.Database.MigrateAsync()`), ensure custom logging uses ILogger (OpenTelemetry). | | |
| **TASK-011** | Align connection-string retrieval with ADRs (environment variables/Dapr secret store). | | |
| **TASK-012** | Run EF Core migration smoke test: start test SQL container, `dotnet run --project RedDog.Bootstrapper/RedDog.Bootstrapper.csproj`, verify exit code 0, log output to `artifacts/bootstrapper-migration-test.md`. | | |
| **TASK-013** | Validate downstream builds: `dotnet build RedDog.AccountingService/RedDog.AccountingService.csproj` to ensure referencing updated library works. | | |

**Completion Criteria:** Tooling artifacts stored; console build passes with zero warnings; migration smoke test log recorded; no blocking synchronous calls remain.

## 3. Alternatives

- **ALT-001**: Remove Bootstrapper entirely — rejected (still needed for DB init until new workflow defined).
- **ALT-002**: Keep EF Core 6 while AccountingService upgrades — rejected (schema mismatch).

## 4. Dependencies

- `docs/research/dotnet-upgrade-analysis.md`
- `plan/testing-validation-strategy.md`
- `plan/cicd-modernization-strategy.md`
- AccountingModel/AccountingService repositories (shared models).

## 5. Files

- `RedDog.Bootstrapper/RedDog.Bootstrapper.csproj`
- `RedDog.Bootstrapper/Program.cs`
- Dockerfile (if present)
- Artifacts under `artifacts/upgrade-assistant/`, `artifacts/dependencies/`, `artifacts/api-analyzer/`, `artifacts/bootstrapper-migration-test.md`

## 6. Testing

- Migration smoke test (test SQL container) per `plan/testing-validation-strategy.md`
- `dotnet test RedDog.Bootstrapper.Tests` (if created)
- Downstream build validation

## 7. Risks & Assumptions

- **RISK-001**: Migration script might drop data — mitigate by running smoke test against disposable DB only.
- **RISK-002**: Console lacks telemetry — ensure OpenTelemetry logs/traces captured.
- **ASSUMPTION-001**: AccountingService + AccountingModel upgrades proceed simultaneously.

## 8. Related Specifications

- `plan/modernization-strategy.md`
- `docs/research/dotnet-upgrade-analysis.md`
- `plan/testing-validation-strategy.md`
- `plan/cicd-modernization-strategy.md`
- ADRs 0001 & 0006
