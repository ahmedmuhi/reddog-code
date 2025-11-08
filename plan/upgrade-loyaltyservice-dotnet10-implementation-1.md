---
goal: Upgrade LoyaltyService to .NET 10 LTS with modern hosting, pub/sub validation, and observability
version: 1.0
date_created: 2025-11-06
last_updated: 2025-11-06
owner: Red Dog Modernization Team
status: Planned
tags: [upgrade, dotnet10, loyaltyservice, dapr]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

Deterministic plan for upgrading `RedDog.LoyaltyService` (port 5400) from .NET 6.0 to .NET 10.0 LTS, aligned with the modernization roadmap, upgrade analysis, testing, and CI strategies.

## Research References

- `docs/research/dotnet-upgrade-analysis.md` (Dependency Analysis, Dapr pub/sub breaking changes, Docker base image guidance)
- `plan/testing-validation-strategy.md` (Tool installation requirements, validation scripts, artifact expectations)
- `plan/cicd-modernization-strategy.md` (GitHub Actions tooling-audit and status checks)

## 1. Requirements & Constraints

- **REQ-001**: Target framework `net10.0`, nullable enabled, implicit usings, LangVersion 14.0.
- **REQ-002**: Dapr packages upgraded to 1.16.0; service must continue subscribing to `orders` topic and updating loyalty state in Redis.
- **REQ-003**: Replace Serilog with OpenTelemetry logging/metrics; adopt Microsoft.AspNetCore.OpenApi + Scalar UI.
- **SEC-001**: `dotnet list package --vulnerable` must return zero vulnerabilities; artifacts stored.
- **CON-001**: ADR-0005 health endpoints required.
- **CON-002**: Dockerfile uses .NET 10 base images.
- **GUD-001**: Minimal hosting replaces Startup.
- **PAT-001**: Async-only patterns (no synchronous state store calls).

## 2. Implementation Steps

### Implementation Phase 1

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-000** | Upgrade Assistant command (`upgrade-assistant upgrade RedDog.LoyaltyService/RedDog.LoyaltyService.csproj ...`) → `artifacts/upgrade-assistant/loyaltyservice.md`. | | |
| **TASK-001** | Run `dotnet workload restore` + `dotnet workload update`; save log to `artifacts/dependencies/loyaltyservice-workloads.txt`. | | |
| **TASK-002** | Capture dependency baselines using `dotnet list ... package --outdated/--vulnerable` and `--graph`; outputs to `artifacts/dependencies/loyaltyservice-*.txt`. | | |
| **TASK-003** | Run API Analyzer and store results in `artifacts/api-analyzer/loyaltyservice.md`. | | |
| **TASK-004** | Update csproj to net10.0 + nullable + implicit usings; remove legacy packages. | | |
| **TASK-005** | Add/upgrade packages: `Dapr.AspNetCore` 1.16.0, `OpenTelemetry.*`, `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore`, `Microsoft.Extensions.*` 10.0.x. | | |
| **TASK-006** | Re-run restore + dependency audits; confirm zero outdated/vulnerable packages. | | |
| **TASK-007** | Update Dockerfile to .NET 10 images; ensure publish step uses `dotnet publish`. | | |
| **TASK-008** | `dotnet build ... -warnaserror` to verify zero warnings. | | |

**Completion Criteria:** Tooling artifacts present; Dockerfile uses .NET 10 images; build warning-free.

### Implementation Phase 2

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-009** | Switch Program.cs to minimal hosting; configure controllers, Dapr subscribe handler, CloudEvents; delete Startup.cs. | | |
| **TASK-010** | Implement ADR-0005 health checks (including Redis readiness); remove ProbesController. | | |
| **TASK-011** | Configure OpenTelemetry tracing + metrics with OTLP exporter; remove Serilog. | | |
| **TASK-012** | Configure OpenAPI + Scalar endpoints; verify via `dotnet run`. | | |
| **TASK-013** | Apply modernization features: file-scoped namespaces, remove `Task.FromResult`, resolve nullable warnings. | | |
| **TASK-014** | Validation per testing strategy: `dotnet test` (when available), run `ci/scripts/run-dapr-loyalty-smoke.sh`, run `ci/scripts/validate-health-endpoints.sh loyaltyservice 5400`, log results to `artifacts/loyaltyservice-validation-report.md`. | | |

**Completion Criteria:** Minimal hosting + OTEL + OpenAPI + ADR-0005 in Program.cs; validation report stored; no legacy files remain.

## 3. Alternatives

- **ALT-001**: Keep Swagger UI & Serilog — rejected.
- **ALT-002**: Delay health endpoint migration — rejected (Phase 1A requirement).

## 4. Dependencies

- `docs/research/dotnet-upgrade-analysis.md`
- `plan/testing-validation-strategy.md`
- `plan/cicd-modernization-strategy.md`

## 5. Files

- `RedDog.LoyaltyService/RedDog.LoyaltyService.csproj`
- `RedDog.LoyaltyService/Program.cs`
- `RedDog.LoyaltyService/Dockerfile`
- `RedDog.LoyaltyService/Controllers/ProbesController.cs`
- Tooling/validation artifacts under `artifacts/`

## 6. Testing

- `dotnet test RedDog.LoyaltyService.Tests` (coverage ≥80%)
- Dapr smoke test `ci/scripts/run-dapr-loyalty-smoke.sh`
- Health endpoints script

## 7. Risks & Assumptions

- **RISK-001**: Redis state schema regressions — mitigate via smoke test.
- **RISK-002**: Nullable backlog — treat as errors.
- **ASSUMPTION-001**: Test project will exist before validation phase.

## 8. Related Specifications

- `plan/modernization-strategy.md`
- `docs/research/dotnet-upgrade-analysis.md`
- `plan/testing-validation-strategy.md`
- `plan/cicd-modernization-strategy.md`
- ADRs 0001 & 0005
