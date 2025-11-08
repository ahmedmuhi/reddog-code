---
goal: Upgrade OrderService from .NET 6.0 to .NET 10.0 LTS with modern hosting, observability, and validation
version: 3.0
last_updated: 2025-11-06
owner: Red Dog Modernization Team
status: Planned
references:
  - docs/research/dotnet-upgrade-analysis.md
  - docs/research/testing-validation-strategy.md
  - docs/research/cicd-modernization.md
  - plan/modernization-strategy.md
  - docs/adr/adr-0001-dotnet10-lts-adoption.md
  - docs/adr/adr-0005-kubernetes-health-probe-standardization.md
---

# OrderService .NET 10 LTS Upgrade Guide

> Use this playbook together with the research assets listed above. The modernization strategy provides the roadmap; this document captures the OrderService-specific work.

## 1. Service Snapshot

| Item | Details |
|------|---------|
| Current framework | .NET 6.0 |
| Target framework | .NET 10.0 LTS |
| Project path | `RedDog.OrderService/RedDog.OrderService.csproj` |
| Deployment targets | AKS, ACA, KEDA-enabled clusters |
| Dapr patterns used | Pub/Sub (`orders` topic), service invocation (`makeline-service`), state store (Redis) |
| Source status | Retained in .NET (no language migration) |

### Prerequisite Findings (from research)
- Legacy `IHostBuilder + Startup.cs` pattern must be replaced with minimal hosting.
- `ProbesController` custom endpoints must be replaced by ADR-0005 (`/healthz`, `/livez`, `/readyz`).
- Swashbuckle must be removed in favor of Microsoft.AspNetCore.OpenApi + Scalar UI.
- Serilog must be replaced with native OpenTelemetry logging (OTLP exporter).
- Nullable reference types, implicit usings, file-scoped namespaces, and modern C# features must be enabled.
- Dapr.AspNetCore 1.5.0 → 1.16.0, EF Core packages → 10.0.x, Docker base images → `mcr.microsoft.com/dotnet/*:10.0`.

## 2. High-Level Plan

| Phase | Goal | Key Outputs |
|-------|------|-------------|
| Phase A | Project & package upgrades | `net10.0` target, Dapr 1.16, EF Core 10, nullable reference types, implicit usings |
| Phase B | Hosting & middleware refactor | Minimal hosting Program.cs, delete Startup.cs, configure ADR-0005 health checks, OpenTelemetry, OpenAPI + Scalar |
| Phase C | Code modernization | Async audit, primary constructors (if applicable), collection expressions, logging scopes |
| Phase D | Validation & pipelines | Unit/integration tests, health-check smoke tests, CI workflow updates per research |

Estimated effort: ~33-44 engineering hours (per research doc) for the platform upgrade work applicable to OrderService.

## 3. Detailed Tasks

### Phase A – Project & Package Upgrades

| Task | Description | Acceptance |
|------|-------------|------------|
| A1 | Update `TargetFramework` to `net10.0`, enable `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<LangVersion>14.0</LangVersion>` in csproj. | `dotnet build` succeeds targeting net10.0 |
| A2 | Bump package references: `Dapr.AspNetCore` 1.16.0, `Microsoft.Extensions.*` 10.x, `Microsoft.EntityFrameworkCore.*` 10.0.x, remove Swashbuckle, remove Serilog packages. | `dotnet list package` shows desired versions only |
| A3 | Add OpenTelemetry packages: `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Instrumentation.AspNetCore`. | Packages restore, no unused references |
| A4 | Update Dockerfile base images to `mcr.microsoft.com/dotnet/sdk:10.0` and `mcr.microsoft.com/dotnet/aspnet:10.0`; ensure `WORKDIR`, `COPY`, and publish steps reflect new build output. | Docker image builds locally |

### Phase B – Hosting, Middleware, and Observability

| Task | Description | Acceptance |
|------|-------------|------------|
| B1 | Convert to minimal hosting: single `Program.cs` using `var builder = WebApplication.CreateBuilder(args);`; move all Startup.cs logic; delete `Startup.cs`. | `Program.cs` compiles, Startup removed |
| B2 | Configure OpenTelemetry logging + metrics: `builder.Services.AddOpenTelemetry().WithTracing(...).WithMetrics(...);` configure OTLP exporter endpoint from env vars following standards doc. | Logs emitted via ILogger show OTLP pipeline enabled |
| B3 | Configure OpenAPI + Scalar: `builder.Services.AddOpenApi(); app.MapOpenApi(); app.MapScalarApiReference();`. | `/openapi/v1.json` and `/scalar/v1` available |
| B4 | Implement ADR-0005 health checks: add `builder.Services.AddHealthChecks()` with tags, map `/healthz`, `/livez`, `/readyz`; remove `ProbesController`. | Curling endpoints returns 200 with expected content |
| B5 | Ensure Dapr integration uses minimal hosting (MapSubscribeHandler, CloudEvents, controllers). | `dapr run` local scenario works |

### Phase C – Code Modernization

| Task | Description | Acceptance |
|------|-------------|------------|
| C1 | Apply file-scoped namespaces, collection expressions, primary constructors where applicable. | StyleCop/formatting check passes |
| C2 | Resolve nullable warnings (`dotnet build /warnaserror`). Focus on DTOs, controllers, service clients. | Build emits zero warnings |
| C3 | Audit async usage (based on research async review). Ensure no `Task.FromResult` wrappers, no blocking I/O. | Async analyzer passes |
| C4 | Update configuration sources per ADR-0004 (Dapr Configuration API) if applicable. | Config loads via Dapr when running with component |

### Phase D – Validation & Pipeline Integration

| Task | Description | Acceptance |
|------|-------------|------------|
| D1 | Implement/extend unit tests for controllers/services; run `dotnet test` with coverage. | Coverage ≥ 80% (per testing strategy) |
| D2 | Create integration test using Dapr CLI (publish to `orders`, verify response). | Integration test passes locally |
| D3 | Update GitHub Actions workflow to use SDK 10.0, run `dotnet build/test`, push image, run Trivy, per `docs/research/cicd-modernization.md`. | Workflow run succeeds in dry run |
| D4 | Execute health-check smoke test script (`ci/scripts/validate-health-endpoints.sh`). | All endpoints respond 200 |

## 4. Validation Checklist

- `dotnet build` (Debug/Release) succeeds with warnings treated as errors.
- `dotnet test` passes with coverage report uploaded.
- Dapr pub/sub flow tested via integration test harness.
- `/healthz`, `/livez`, `/readyz` respond 200 when running locally and in dev cluster.
- `/openapi/v1.json` and `/scalar/v1` accessible.
- OTLP logs visible in configured collector (Jaeger/Grafana, per observability plan).
- Docker image builds (both `docker build` and GH Actions workflow) using net10.0 base images.
- All references removed to Serilog, Swashbuckle, Startup.cs, ProbesController.

## 5. Dependencies & Risks

| Risk | Mitigation |
|------|------------|
| Breaking change in Dapr SDK 1.16 | Follow upgrade notes in research doc; test pub/sub + service invocation |
| Nullable warnings backlog | Prioritize DTOs/controllers first; treat warnings as errors to prevent regressions |
| CI pipeline gaps | Adopt CI/CD modernization guide; run workflows before merge |
| Health checks misconfigured | Use ADR sample code and validation script to ensure readiness/liveness correctness |

## 6. References
- `docs/research/dotnet-upgrade-analysis.md` — Platform upgrade & breaking change guidance.
- `docs/research/testing-validation-strategy.md` — Build, integration, performance, and observability validation steps.
- `docs/research/cicd-modernization.md` — CI/CD pipeline modernization playbook.
- `plan/modernization-strategy.md` — Overall program roadmap.
- ADRs 0001, 0002, 0003, 0004, 0005, 0006 — Technical standards enforced by this upgrade.

