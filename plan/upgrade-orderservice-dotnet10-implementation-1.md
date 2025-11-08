---
goal: Upgrade OrderService to .NET 10 LTS with modern hosting, observability, and validation
version: 1.0
date_created: 2025-11-06
last_updated: 2025-11-06
owner: Red Dog Modernization Team
status: Planned
tags: [upgrade, dotnet10, orderservice, dapr, observability]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

This implementation plan defines the deterministic steps for upgrading `RedDog.OrderService` from .NET 6.0 to .NET 10.0 LTS while adopting the modernization requirements identified in `docs/research/dotnet-upgrade-analysis.md`. The plan is fully executable by AI agents or humans without additional interpretation.

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
| **TASK-001** | Edit `RedDog.OrderService/RedDog.OrderService.csproj`: set `<TargetFramework>net10.0</TargetFramework>`, add `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<LangVersion>14.0</LangVersion>`. Remove `<PackageReference>` entries for Serilog and Swashbuckle. | | |
| **TASK-002** | In same csproj, update package versions: `Dapr.AspNetCore` to `1.16.0`, `Microsoft.Extensions.*` to `10.0.x`, `Microsoft.EntityFrameworkCore.*` to `10.0.x`, add `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Instrumentation.AspNetCore`, `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore`. | | |
| **TASK-003** | Run `dotnet restore RedDog.OrderService/RedDog.OrderService.csproj` and `dotnet list package --outdated --include-transitive` to confirm only target versions remain. Capture output in `artifacts/orderservice-package-report.txt`. | | |
| **TASK-004** | Update `RedDog.OrderService/Dockerfile`: change base images to `mcr.microsoft.com/dotnet/sdk:10.0` and `mcr.microsoft.com/dotnet/aspnet:10.0`; ensure publish step uses `dotnet publish -c Release -o /app/publish`. | | |
| **TASK-005** | Execute `dotnet build RedDog.OrderService/RedDog.OrderService.csproj -warnaserror` and resolve all compilation or nullable warnings introduced by phase tasks. | | |

**Completion Criteria (Phase 1):** `dotnet build` succeeds with zero warnings; `artifacts/orderservice-package-report.txt` shows no outdated packages; Dockerfile references only .NET 10 images.

### Implementation Phase 2

- **GOAL-002**: Refactor hosting, health checks, logging, and API surface to modern patterns; add validation artifacts.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-006** | Replace `RedDog.OrderService/Program.cs` contents with minimal hosting pattern from research doc: instantiate `WebApplication.CreateBuilder`, configure services, map controllers, Dapr subscribe handler, CloudEvents, and run the app. Delete `RedDog.OrderService/Startup.cs`. | | |
| **TASK-007** | Implement ADR-0005 health checks in Program.cs: add `builder.Services.AddHealthChecks()` with tags `live` and `ready`, map endpoints `/healthz`, `/livez`, `/readyz`. Delete `RedDog.OrderService/Controllers/ProbesController.cs`. | | |
| **TASK-008** | Configure OpenTelemetry: in Program.cs add `builder.Services.AddOpenTelemetry().WithTracing(...)` and `.WithMetrics(...)` using OTLP exporter endpoint `OTEL_EXPORTER_OTLP_ENDPOINT` from environment variables. Remove all Serilog-specific code references. | | |
| **TASK-009** | Configure OpenAPI + Scalar: `builder.Services.AddOpenApi()`, `app.MapOpenApi()`, `app.MapScalarApiReference()`. Verify `/openapi/v1.json` and `/scalar/v1` serve successfully via `dotnet run`. | | |
| **TASK-010** | Apply modernization features: convert namespaces to file-scoped, replace any `Task.FromResult` usages, ensure DTOs/controllers resolve nullable warnings, adopt collection expressions where applicable. Use `dotnet format` to enforce style. | | |
| **TASK-011** | Create or update validation scripts per `docs/research/testing-validation-strategy.md`: run `dotnet test` with coverage, execute Dapr integration smoke test, and run `ci/scripts/validate-health-endpoints.sh`. Store results in `artifacts/orderservice-validation-report.md`. | | |

**Completion Criteria (Phase 2):** `Program.cs` contains minimal hosting + OTEL + OpenAPI; health endpoints respond 200 locally; `artifacts/orderservice-validation-report.md` records passing unit/integration tests and health-check results; repository has no Serilog or Startup files.

## 3. Alternatives

- **ALT-001**: Retain Serilog and Swashbuckle with incremental upgrades — rejected due to modernization standard requiring OpenTelemetry and Scalar UI.
- **ALT-002**: Delay health endpoint migration until language migration phase — rejected because ADR-0005 compliance is mandatory for the .NET 10 upgrade baseline.

## 4. Dependencies

- **DEP-001**: `docs/research/dotnet-upgrade-analysis.md` for minimal hosting samples and package versions.
- **DEP-002**: `docs/research/testing-validation-strategy.md` for required validation scripts and coverage thresholds.
- **DEP-003**: `docs/research/cicd-modernization.md` for GitHub Actions modifications (ensuring workflows use SDK 10.0 and run tests).

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
- `docs/research/testing-validation-strategy.md`
- `docs/research/cicd-modernization.md`
- `plan/modernization-strategy.md`
- `docs/adr/adr-0001-dotnet10-lts-adoption.md`
- `docs/adr/adr-0005-kubernetes-health-probe-standardization.md`
