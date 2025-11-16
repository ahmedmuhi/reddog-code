---
goal: Upgrade OrderService to .NET 10 LTS with modern hosting, observability, and validation
version: 1.0
date_created: 2025-11-06
last_updated: 2025-11-11
owner: Red Dog Modernization Team
status: ✅ COMPLETED
completion_date: 2025-11-11
session: .claude/sessions/2025-11-11-1541-phase1a-orderservice-dotnet10-upgrade.md
tags: [upgrade, dotnet10, orderservice, dapr, observability]
---

# Introduction

![Status: Completed](https://img.shields.io/badge/status-Completed-green)

This implementation plan defines the deterministic steps for upgrading `RedDog.OrderService` from .NET 6.0 to .NET 10.0 LTS while adopting the modernization requirements identified in `docs/research/dotnet-upgrade-analysis.md`. The plan is fully executable by AI agents or humans without additional interpretation.

## Research References

- `docs/research/dotnet-upgrade-analysis.md` (Dependency Analysis, Breaking Change Analysis, Docker base image guidance)
- `plan/testing-validation-strategy.md` (Tool installation, validation scripts, artifact expectations)
- `plan/cicd-modernization-strategy.md` (GitHub Actions workflow modernization and tooling audit requirements)

## 1. Requirements & Constraints

- **REQ-001**: Service must target `.NET 10.0` and use SDK features (`Nullable`, `ImplicitUsings`, `LangVersion 14.0`).
- **REQ-002**: Dapr SDK dependencies must be updated to `1.16.0` (pub/sub, state, service invocation compatibility).
- **REQ-003**: API documentation must use `Microsoft.AspNetCore.OpenApi` and `Scalar.AspNetCore` per web API standards.
- **REQ-004**: Logging and metrics must use OpenTelemetry with OTLP exporters; Serilog packages must be removed.
- **SEC-001**: `dotnet list package --vulnerable` must report zero vulnerabilities after upgrades.
- **CON-001**: Health endpoints must comply with ADR-0005 (`/healthz`, `/livez`, `/readyz`).
- **CON-002**: Docker images must be based on `mcr.microsoft.com/dotnet/sdk:10.0` and `aspnet:10.0` (Ubuntu 24.04 per ADR-0003).
- **GUD-001**: Minimal hosting (`WebApplication.CreateBuilder`) must replace `Startup.cs`.
- **PAT-001**: Async/await only; remove `Task.FromResult` placeholders.

## 2. Implementation Steps

### Implementation Phase 1

- **GOAL-001**: Update project configuration, packages, and Docker assets to .NET 10 standards.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-000** | Run `.NET Upgrade Assistant`: `upgrade-assistant upgrade RedDog.OrderService/RedDog.OrderService.csproj --entry-point RedDog.OrderService/RedDog.OrderService.csproj --non-interactive --skip-backup false` and store the generated report at `artifacts/upgrade-assistant/orderservice.md`. | | |
| **TASK-001** | Execute `dotnet workload restore` and `dotnet workload update` within the repo root; append console output to `artifacts/dependencies/orderservice-workloads.txt`. | | |
| **TASK-002** | Capture dependency baselines: `dotnet list RedDog.OrderService/RedDog.OrderService.csproj package --outdated --include-transitive`, `dotnet list ... package --vulnerable`, and `dotnet list ... reference --graph`, saving outputs to `artifacts/dependencies/orderservice-{outdated|vulnerable|graph}.txt`. | | |
| **TASK-003** | Run API Analyzer: `dotnet tool run api-analyzer -f net10.0 -p RedDog.OrderService/RedDog.OrderService.csproj > artifacts/api-analyzer/orderservice.md`. | | |
| **TASK-004** | Edit `RedDog.OrderService/RedDog.OrderService.csproj`: set `<TargetFramework>net10.0</TargetFramework>`, add `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<LangVersion>14.0</LangVersion>`, and remove Serilog/Swashbuckle `<PackageReference>` entries. | | |
| **TASK-005** | Update package references to target versions: `Dapr.AspNetCore` 1.16.0, `Microsoft.Extensions.*` 10.0.x, `Microsoft.EntityFrameworkCore.*` 10.0.x, add `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Instrumentation.AspNetCore`, `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore`. | | |
| **TASK-006** | Re-run dependency audits post-package update: `dotnet restore` followed by the same `dotnet list package` commands, overwriting the artifacts in `artifacts/dependencies/` to confirm zero outdated/vulnerable packages. | | |
| **TASK-007** | Update `RedDog.OrderService/Dockerfile` to use `mcr.microsoft.com/dotnet/sdk:10.0` and `mcr.microsoft.com/dotnet/aspnet:10.0` images; ensure publish step uses `dotnet publish -c Release -o /app/publish`. | | |
| **TASK-008** | Execute `dotnet build RedDog.OrderService/RedDog.OrderService.csproj -warnaserror` to confirm zero warnings after csproj/Docker changes; fix any nullable or analyzer issues surfaced. | | |

**Completion Criteria (Phase 1):** Artifact files exist under `artifacts/upgrade-assistant/`, `artifacts/dependencies/`, and `artifacts/api-analyzer/`; `dotnet build` completes without warnings; Dockerfile references only .NET 10 base images.

### Implementation Phase 2

- **GOAL-002**: Refactor hosting, health checks, logging, and API surface to modern patterns; add validation artifacts.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-009** | Replace `RedDog.OrderService/Program.cs` contents with minimal hosting pattern from research doc: instantiate `WebApplication.CreateBuilder`, configure services, map controllers, Dapr subscribe handler, CloudEvents, and run the app. Delete `RedDog.OrderService/Startup.cs`. | | |
| **TASK-010** | Implement ADR-0005 health checks in Program.cs: add `builder.Services.AddHealthChecks()` with tags `live` and `ready`, map endpoints `/healthz`, `/livez`, `/readyz`. Delete `RedDog.OrderService/Controllers/ProbesController.cs`. | | |
| **TASK-011** | Configure OpenTelemetry: in Program.cs add `builder.Services.AddOpenTelemetry().WithTracing(...)` and `.WithMetrics(...)` using OTLP exporter endpoint `OTEL_EXPORTER_OTLP_ENDPOINT` from environment variables. Remove all Serilog-specific code references. | | |
| **TASK-012** | Configure OpenAPI + Scalar: `builder.Services.AddOpenApi()`, `app.MapOpenApi()`, `app.MapScalarApiReference()`. Verify `/openapi/v1.json` and `/scalar/v1` serve successfully via `dotnet run`. | | |
| **TASK-013** | Apply modernization features: convert namespaces to file-scoped, replace any `Task.FromResult` usages, ensure DTOs/controllers resolve nullable warnings, adopt collection expressions where applicable. Use `dotnet format` to enforce style. | | |
| **TASK-014** | Create or update validation scripts per `plan/testing-validation-strategy.md`: run `dotnet test` with coverage, execute Dapr integration smoke test, run `ci/scripts/validate-health-endpoints.sh`, and append results to `artifacts/orderservice-validation-report.md`. | | |

**Completion Criteria (Phase 2):** `Program.cs` contains minimal hosting + OTEL + OpenAPI; health endpoints respond 200 locally; `artifacts/orderservice-validation-report.md` records passing unit/integration tests and health-check results; repository has no Serilog or Startup files.

## 3. Alternatives

- **ALT-001**: Retain Serilog and Swashbuckle with incremental upgrades — rejected due to modernization standard requiring OpenTelemetry and Scalar UI.
- **ALT-002**: Delay health endpoint migration until language migration phase — rejected because ADR-0005 compliance is mandatory for the .NET 10 upgrade baseline.

## 4. Dependencies

- **DEP-001**: `docs/research/dotnet-upgrade-analysis.md` for minimal hosting samples and package versions.
- **DEP-002**: `plan/testing-validation-strategy.md` for required validation scripts and coverage thresholds.
- **DEP-003**: `plan/cicd-modernization-strategy.md` for GitHub Actions modifications (ensuring workflows use SDK 10.0 and run tests).

## 5. Files

- **FILE-001**: `RedDog.OrderService/RedDog.OrderService.csproj` — framework target, package references, nullable settings.
- **FILE-002**: `RedDog.OrderService/Program.cs` — minimal hosting, OTEL, health checks, API mapping.
- **FILE-003**: `RedDog.OrderService/Dockerfile` — runtime and SDK base images.
- **FILE-004**: `RedDog.OrderService/Controllers/ProbesController.cs` — to be deleted after health middleware adoption.
- **FILE-005**: `artifacts/orderservice-package-report.txt` and `artifacts/orderservice-validation-report.md` — generated outputs documenting compliance.

## 6. Testing

- **TEST-001**: `dotnet test RedDog.OrderService.Tests/RedDog.OrderService.Tests.csproj --collect:"XPlat Code Coverage"` — must meet ≥80% coverage.
- **TEST-002**: Dapr integration smoke test script (`ci/scripts/run-dapr-orderservice-smoke.sh`) verifying pub/sub flow for `orders` topic.
- **TEST-003**: Health endpoint validation (`ci/scripts/validate-health-endpoints.sh orderservice 5100`) ensuring `/healthz`, `/livez`, `/readyz` return HTTP 200.
- **TEST-004**: CI workflow run `ci-build-orderservice.yml` executing build, test, Trivy scan using SDK 10.0 images.

## 7. Risks & Assumptions

- **RISK-001**: Dapr SDK 1.16 introduces runtime behavior changes. *Mitigation*: execute smoke tests against local Dapr sidecar before merging.
- **RISK-002**: Nullable enablement may surface numerous warnings. *Mitigation*: treat warnings as errors during Phase 1 to force resolution.
- **ASSUMPTION-001**: OrderService has or will have a corresponding test project (`RedDog.OrderService.Tests`). If absent, create minimal tests prior to executing TEST-001.

## 8. Related Specifications / Further Reading

- `docs/research/dotnet-upgrade-analysis.md`
- `plan/testing-validation-strategy.md`
- `plan/cicd-modernization-strategy.md`
- `plan/modernization-strategy.md`
- `docs/adr/adr-0001-dotnet10-lts-adoption.md`
- `docs/adr/adr-0005-kubernetes-health-probe-standardization.md`
