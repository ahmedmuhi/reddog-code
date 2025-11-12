---
goal: Upgrade AccountingService to .NET 10 LTS with modern hosting, EF Core 10, and observability compliance
version: 1.0
date_created: 2025-11-06
last_updated: 2025-11-06
owner: Red Dog Modernization Team
status: Planned
tags: [upgrade, dotnet10, accountingservice, efcore, dapr]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

This implementation plan provides deterministic steps to upgrade `RedDog.AccountingService` from .NET 6.0 to .NET 10.0 LTS. The plan aligns with `plan/modernization-strategy.md`, `docs/research/dotnet-upgrade-analysis.md`, `plan/testing-validation-strategy.md`, and `plan/cicd-modernization-strategy.md`.

## Research References

- `docs/research/dotnet-upgrade-analysis.md` (Dependency Analysis, Breaking Change Analysis, Docker base image updates)
- `plan/testing-validation-strategy.md` (Tool installation requirements, validation scripts, artifact expectations)
- `plan/cicd-modernization-strategy.md` (GitHub Actions workflow structure and tooling-audit requirements)

## 1. Requirements & Constraints

- **REQ-001**: Target framework must be `net10.0` with `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, and `<LangVersion>14.0`.
- **REQ-002**: Dapr SDK packages upgraded to `1.16.0` for pub/sub, state, and bindings.
- **REQ-003**: API surface must use `Microsoft.AspNetCore.OpenApi` + `Scalar.AspNetCore` for documentation.
- **REQ-004**: Logging/metrics migrated to OpenTelemetry with OTLP exporter; Serilog packages removed.
- **REQ-005**: EF Core packages updated to `10.0.x`, including design-time tools and SQL Server provider shared with `RedDog.AccountingModel`.
- **SEC-001**: `dotnet list package --vulnerable` must report zero vulnerabilities; dependency reports stored under `artifacts/dependencies/`.
- **CON-001**: Health endpoints must comply with ADR-0005 (`/healthz`, `/livez`, `/readyz`).
- **CON-002**: Docker images must be based on `mcr.microsoft.com/dotnet/sdk:10.0` and `mcr.microsoft.com/dotnet/aspnet:10.0`.
- **GUD-001**: Minimal hosting (`WebApplication.CreateBuilder`) must replace `Startup.cs`.
- **PAT-001**: Async/await only; remove `Task.FromResult` and sync-over-async patterns.

## 2. Implementation Steps

### Implementation Phase 1

- **GOAL-001**: Update project configuration, dependencies, and Docker assets to the .NET 10 baseline with tooling artifacts.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-000** | Run `.NET Upgrade Assistant`: `upgrade-assistant upgrade RedDog.AccountingService/RedDog.AccountingService.csproj --entry-point RedDog.AccountingService/RedDog.AccountingService.csproj --non-interactive --skip-backup false` → `artifacts/upgrade-assistant/accountingservice.md`. | | |
| **TASK-001** | Execute `dotnet workload restore` and `dotnet workload update`; append console output to `artifacts/dependencies/accountingservice-workloads.txt`. | | |
| **TASK-002** | Capture dependency baselines: `dotnet list RedDog.AccountingService/RedDog.AccountingService.csproj package --outdated --include-transitive`, `--vulnerable`, and `dotnet list ... reference --graph`, saving files to `artifacts/dependencies/accountingservice-{outdated|vulnerable|graph}.txt`. | | |
| **TASK-003** | Run API Analyzer: `dotnet tool run api-analyzer -f net10.0 -p RedDog.AccountingService/RedDog.AccountingService.csproj > artifacts/api-analyzer/accountingservice.md`. Resolve warnings ≥ Medium before proceeding. | | |
| **TASK-004** | Update `RedDog.AccountingService/RedDog.AccountingService.csproj`: set `<TargetFramework>net10.0</TargetFramework>`, enable `<Nullable>`/`<ImplicitUsings>`, set `<LangVersion>14.0`, remove Serilog/Swashbuckle packages. | | |
| **TASK-005** | Add/upgrade package references: `Dapr.AspNetCore` 1.16.0, `Dapr.Extensions.Configuration` 1.16.0, `Microsoft.EntityFrameworkCore.*` 10.0.x, `Microsoft.Extensions.*` 10.0.x, `OpenTelemetry.*`, `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore`. Ensure `RedDog.AccountingModel` reference remains intact. | | |
| **TASK-006** | Re-run `dotnet restore` and dependency audits, overwriting artifacts to confirm zero outdated/vulnerable packages. | | |
| **TASK-007** | Update `RedDog.AccountingService/Dockerfile` to `mcr.microsoft.com/dotnet/sdk:10.0` and `mcr.microsoft.com/dotnet/aspnet:10.0`; ensure publish step uses `dotnet publish -c Release -o /app/publish`. | | |
| **TASK-008** | Execute `dotnet build RedDog.AccountingService/RedDog.AccountingService.csproj -warnaserror` to confirm zero warnings after csproj/Docker edits. | | |

**Completion Criteria (Phase 1):** `artifacts/upgrade-assistant/`, `artifacts/dependencies/`, and `artifacts/api-analyzer/` contain up-to-date logs; `dotnet build` completes without warnings; Dockerfile references .NET 10 base images only.

### Implementation Phase 2

- **GOAL-002**: Modernize hosting, health checks, telemetry, EF Core migrations, and validation.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-009** | Replace `Program.cs` contents with minimal hosting model, configure services, Dapr controllers, CloudEvents, subscribe handler, and delete `RedDog.AccountingService/Startup.cs`. | | |
| **TASK-010** | Implement ADR-0005 health checks in Program.cs (including database checks via `AddDbContextCheck<AccountingContext>`), map `/healthz`, `/livez`, `/readyz`, and delete `Controllers/ProbesController.cs`. | | |
| **TASK-011** | Configure OpenTelemetry tracing + metrics with OTLP exporter using env var `OTEL_EXPORTER_OTLP_ENDPOINT`; remove Serilog usage. | | |
| **TASK-012** | Configure OpenAPI + Scalar: `builder.Services.AddOpenApi()`, `app.MapOpenApi()`, `app.MapScalarApiReference()`. Verify endpoints via `dotnet run`. | | |
| **TASK-013** | Apply modernization features: file-scoped namespaces, remove `Task.FromResult`, resolve nullable warnings in controllers/models, adopt collection expressions/primary constructors where applicable. Run `dotnet format`. | | |
| **TASK-014** | Validate EF Core migrations: run `dotnet ef database update --project RedDog.AccountingService/RedDog.AccountingService.csproj --context AccountingContext` against test SQL container, then `dotnet ef database update <previous>` to confirm rollback. Store log in `artifacts/accountingservice-efmigrations.md`. | | |
| **TASK-015** | Execute validation scripts per `plan/testing-validation-strategy.md`: `dotnet test RedDog.AccountingService.Tests` (when available) with coverage, run Dapr integration smoke test, run `ci/scripts/validate-health-endpoints.sh accountingservice 5700`, append results to `artifacts/accountingservice-validation-report.md`. | | |

**Completion Criteria (Phase 2):** `Program.cs` uses minimal hosting with OTEL + OpenAPI + ADR-0005 health checks; EF migrations succeed/rollback in test environment; validation report documents passing tests and smoke checks; repository contains no Serilog or Startup files.

## 3. Alternatives

- **ALT-001**: Retain Serilog + Swashbuckle with incremental upgrades — rejected (modernization standards require OpenTelemetry + Scalar).
- **ALT-002**: Defer EF Core 10 migration — rejected (AccountingService depends on AccountingModel; both must align with EF Core 10 before language migrations).

## 4. Dependencies

- **DEP-001**: `docs/research/dotnet-upgrade-analysis.md` for minimal hosting/OTEL guidance.
- **DEP-002**: `plan/testing-validation-strategy.md` for validation scripts and coverage thresholds.
- **DEP-003**: `plan/cicd-modernization-strategy.md` for GitHub Actions workflow updates (tooling audit, build/test jobs).
- **DEP-004**: `RedDog.AccountingModel/RedDog.AccountingModel.csproj` (shared EF models); ensure both projects upgrade in lockstep.

## 5. Files

- **FILE-001**: `RedDog.AccountingService/RedDog.AccountingService.csproj`
- **FILE-002**: `RedDog.AccountingService/Program.cs`
- **FILE-003**: `RedDog.AccountingService/Dockerfile`
- **FILE-004**: `RedDog.AccountingService/Controllers/ProbesController.cs` (to be deleted)
- **FILE-005**: `artifacts/upgrade-assistant/accountingservice.md`, `artifacts/dependencies/accountingservice-*.txt`, `artifacts/api-analyzer/accountingservice.md`, `artifacts/accountingservice-efmigrations.md`, `artifacts/accountingservice-validation-report.md`

## 6. Testing

- **TEST-001**: `dotnet test RedDog.AccountingService.Tests/RedDog.AccountingService.Tests.csproj --collect:"XPlat Code Coverage"` (≥75% coverage once tests exist).
- **TEST-002**: Dapr integration smoke test script (`ci/scripts/run-dapr-accounting-smoke.sh`) validating pub/sub + service invocation scenarios.
- **TEST-003**: Health endpoint validation (`ci/scripts/validate-health-endpoints.sh accountingservice 5700`).
- **TEST-004**: EF Core migration pipeline test (apply + rollback using test SQL container).
- **TEST-005**: CI workflow `ci-build-accountingservice.yml` running tooling audit, build, test, Trivy scan.

## 7. Risks & Assumptions

- **RISK-001**: EF Core 10 migrations introduce schema changes. *Mitigation*: execute migration/rollback tests before merging; coordinate with AccountingModel upgrades.
- **RISK-002**: Dapr SDK changes affect state/pub-sub handlers. *Mitigation*: run Dapr smoke tests from validation strategy.
- **ASSUMPTION-001**: AccountingService test project will be created or expanded to enable unit/integration tests.

## 8. Related Specifications / Further Reading

- `plan/modernization-strategy.md`
- `docs/research/dotnet-upgrade-analysis.md`
- `plan/testing-validation-strategy.md`
- `plan/cicd-modernization-strategy.md`
- `docs/adr/adr-0001-dotnet10-lts-adoption.md`
- `docs/adr/adr-0005-kubernetes-health-probe-standardization.md`
 EOF
