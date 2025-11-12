---
goal: Upgrade MakeLineService to .NET 10 LTS with modern hosting, Dapr 1.16, and observability compliance
version: 1.0
date_created: 2025-11-06
last_updated: 2025-11-06
owner: Red Dog Modernization Team
status: Planned
tags: [upgrade, dotnet10, makelineservice, dapr]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

Deterministic implementation plan for upgrading `RedDog.MakeLineService` (port 5200) from .NET 6.0 to .NET 10.0 LTS in alignment with `plan/modernization-strategy.md`, `docs/research/dotnet-upgrade-analysis.md`, `plan/testing-validation-strategy.md`, and `plan/cicd-modernization-strategy.md`.

## Research References

- `docs/research/dotnet-upgrade-analysis.md` (Dependency inventory, Dapr breaking changes, Docker updates)
- `plan/testing-validation-strategy.md` (Tooling workflow, health/Dapr smoke scripts, artifact requirements)
- `plan/cicd-modernization-strategy.md` (CI tooling-audit/build/publish structure)

## 1. Requirements & Constraints

- **REQ-001**: Target framework `net10.0`; enable `<Nullable>`, `<ImplicitUsings>`, `<LangVersion>14.0`.
- **REQ-002**: Dapr packages upgraded to 1.16.0; service must continue to use pub/sub + Redis state store.
- **REQ-003**: Logging & metrics migrated to OpenTelemetry; Serilog removed.
- **REQ-004**: API documentation exposed via Microsoft.AspNetCore.OpenApi + Scalar.
- **SEC-001**: `dotnet list package --vulnerable` reports zero vulnerabilities (artifacts stored).
- **CON-001**: ADR-0005 health endpoints `/healthz`, `/livez`, `/readyz` required.
- **CON-002**: Dockerfile must use `mcr.microsoft.com/dotnet/sdk:10.0` and `aspnet:10.0`.
- **GUD-001**: Minimal hosting replaces Startup; ADR-0005 + Dapr handlers configured in Program.cs.
- **PAT-001**: Async/await only (no blocking queue operations).

## 2. Implementation Steps

### Implementation Phase 1

- **GOAL-001**: Establish .NET 10 baseline with tooling compliance.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-000** | Run Upgrade Assistant: `upgrade-assistant upgrade RedDog.MakeLineService/RedDog.MakeLineService.csproj --entry-point RedDog.MakeLineService/RedDog.MakeLineService.csproj --non-interactive --skip-backup false > artifacts/upgrade-assistant/makelineservice.md`. | | |
| **TASK-001** | `dotnet workload restore` + `dotnet workload update`; log output to `artifacts/dependencies/makelineservice-workloads.txt`. | | |
| **TASK-002** | `dotnet list ... package --outdated --include-transitive`, `--vulnerable`, and `dotnet list ... reference --graph`; store in `artifacts/dependencies/makelineservice-{outdated|vulnerable|graph}.txt`. | | |
| **TASK-003** | `dotnet tool run api-analyzer -f net10.0 -p RedDog.MakeLineService/RedDog.MakeLineService.csproj > artifacts/api-analyzer/makelineservice.md`; resolve warnings ≥ Medium. | | |
| **TASK-004** | Update csproj with net10.0, nullable, implicit usings, LangVersion, remove Serilog/Swashbuckle packages. | | |
| **TASK-005** | Add/upgrade packages: `Dapr.AspNetCore` 1.16.0, `OpenTelemetry.*`, `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore`, `Microsoft.Extensions.*` 10.0.x. | | |
| **TASK-006** | Re-run `dotnet restore` and dependency audits to confirm zero outdated/vulnerable packages. | | |
| **TASK-007** | Update Dockerfile to .NET 10 base images; ensure publish step uses `dotnet publish -c Release -o /app/publish`. | | |
| **TASK-008** | `dotnet build RedDog.MakeLineService/RedDog.MakeLineService.csproj -warnaserror` to confirm zero warnings. | | |

**Completion Criteria (Phase 1):** Tooling artifacts exist; Dockerfile uses .NET 10 images; `dotnet build` passes with zero warnings.

### Implementation Phase 2

- **GOAL-002**: Modernize hosting, health, telemetry, Dapr handlers, and validation.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-009** | Replace Program.cs with minimal hosting (WebApplication builder), configure controllers, Dapr subscribe handler, CloudEvents; delete Startup.cs. | | |
| **TASK-010** | Implement ADR-0005 health checks (include Redis connectivity checks); remove `ProbesController`. | | |
| **TASK-011** | Configure OpenTelemetry tracing + metrics with OTLP exporter env var `OTEL_EXPORTER_OTLP_ENDPOINT`. | | |
| **TASK-012** | Configure OpenAPI + Scalar endpoints and verify via `dotnet run`. | | |
| **TASK-013** | Apply modernization features: file-scoped namespaces, remove `Task.FromResult`, resolve nullable warnings, adopt collection expressions. | | |
| **TASK-014** | Execute validation per `plan/testing-validation-strategy.md`: `dotnet test` (when tests exist), run Dapr smoke test `scripts/run-dapr-makeline-smoke.sh`, run `scripts/upgrade-validate.sh MakeLineService`, store results in `artifacts/makelineservice-validation-report.md`. | | |

**Completion Criteria (Phase 2):** Program.cs uses minimal hosting + OTEL + OpenAPI + ADR-0005; validation report captures passing tests/smoke checks; repository contains no legacy Startup/Serilog files.

## 3. Alternatives

- **ALT-001**: Retain legacy Startup + Swagger UI — rejected (modernization standards).
- **ALT-002**: Skip OpenTelemetry until language migration — rejected (observability baseline required in Phase 1A).

## 4. Dependencies

- **DEP-001**: `docs/research/dotnet-upgrade-analysis.md`.
- **DEP-002**: `plan/testing-validation-strategy.md`.
- **DEP-003**: `plan/cicd-modernization-strategy.md` (CI workflow updates).

## 5. Files

- `RedDog.MakeLineService/RedDog.MakeLineService.csproj`
- `RedDog.MakeLineService/Program.cs`
- `RedDog.MakeLineService/Dockerfile`
- `RedDog.MakeLineService/Controllers/ProbesController.cs` (delete)
- Artifacts under `artifacts/upgrade-assistant/`, `artifacts/dependencies/`, `artifacts/api-analyzer/`, `artifacts/makelineservice-validation-report.md`

## 6. Testing

- **TEST-001**: `dotnet test RedDog.MakeLineService.Tests` (when available) with coverage ≥80%.
- **TEST-002**: Dapr integration smoke test `scripts/run-dapr-makeline-smoke.sh` verifying queue operations.
- **TEST-003**: Health endpoints validation script.

## 7. Risks & Assumptions

- **RISK-001**: Redis schema/ETag changes — mitigate with smoke tests.
- **RISK-002**: Nullable enablement backlog — treat warnings as errors.
- **ASSUMPTION-001**: MakeLineService tests will be created/enabled.

## 8. Related Specifications

- `plan/modernization-strategy.md`
- `docs/research/dotnet-upgrade-analysis.md`
- `plan/testing-validation-strategy.md`
- `plan/cicd-modernization-strategy.md`
- `docs/adr/adr-0001-dotnet10-lts-adoption.md`
- `docs/adr/adr-0005-kubernetes-health-probe-standardization.md`
