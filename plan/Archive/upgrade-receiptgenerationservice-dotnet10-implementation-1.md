---
goal: Upgrade ReceiptGenerationService to .NET 10 LTS with modern hosting, bindings validation, and observability
version: 1.0
date_created: 2025-11-06
last_updated: 2025-11-06
owner: Red Dog Modernization Team
status: Planned
tags: [upgrade, dotnet10, receiptgeneration, dapr]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

Deterministic plan for upgrading `RedDog.ReceiptGenerationService` (port 5300) from .NET 6.0 to .NET 10.0 LTS.

## Research References

- `docs/research/dotnet-upgrade-analysis.md` (Dependency Analysis, Dapr bindings guidance, Docker updates)
- `plan/testing-validation-strategy.md` (Tooling workflow, binding smoke tests, artifact expectations)
- `plan/cicd-modernization-strategy.md` (CI tooling-audit/build/publish jobs)

## 1. Requirements & Constraints

- **REQ-001**: Target framework `net10.0`, nullable enabled, implicit usings, LangVersion 14.0.
- **REQ-002**: Dapr bindings/pub-sub packages upgraded to 1.16.0; output bindings must continue writing receipts correctly.
- **REQ-003**: Logging & metrics via OpenTelemetry; Serilog removed.
- **REQ-004**: API documentation via Microsoft.AspNetCore.OpenApi + Scalar.
- **SEC-001**: No vulnerable packages per `dotnet list package --vulnerable`.
- **CON-001**: ADR-0005 health endpoints required.
- **CON-002**: Docker images use .NET 10 base images.
- **GUD-001**: Minimal hosting pattern.
- **PAT-001**: Async I/O for bindings (no blocking file writes).

## 2. Implementation Steps

### Implementation Phase 1

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-000** | Upgrade Assistant command → `artifacts/upgrade-assistant/receiptgenerationservice.md`. | | |
| **TASK-001** | Run workload restore/update; log to `artifacts/dependencies/receiptgenerationservice-workloads.txt`. | | |
| **TASK-002** | Capture dependency baselines and graphs; store under `artifacts/dependencies/receiptgenerationservice-*.txt`. | | |
| **TASK-003** | API Analyzer report → `artifacts/api-analyzer/receiptgenerationservice.md`. | | |
| **TASK-004** | Update csproj to net10.0, nullable, implicit usings; remove Serilog/Swashbuckle packages. | | |
| **TASK-005** | Upgrade/add packages: `Dapr.AspNetCore` 1.16.0, `OpenTelemetry.*`, `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore`, `Microsoft.Extensions.*` 10.0.x. | | |
| **TASK-006** | Re-run restore + dependency audits; confirm zero outdated/vulnerable packages. | | |
| **TASK-007** | Update Dockerfile to .NET 10 base images. | | |
| **TASK-008** | `dotnet build ... -warnaserror`. | | |

### Implementation Phase 2

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-009** | Minimal hosting Program.cs; configure controllers, binding handlers, Dapr subscribe; delete Startup.cs. | | |
| **TASK-010** | Implement ADR-0005 health checks (include storage/binding readiness) and remove ProbesController. | | |
| **TASK-011** | Configure OpenTelemetry logging/metrics with OTLP exporter. | | |
| **TASK-012** | Configure OpenAPI + Scalar endpoints. | | |
| **TASK-013** | Apply modernization features (file-scoped namespaces, collection expressions, remove `Task.FromResult`, fix nullable warnings). | | |
| **TASK-014** | Validation: `dotnet test` (when available), run `ci/scripts/run-dapr-receiptgeneration-smoke.sh` to verify binding output, run `ci/scripts/validate-health-endpoints.sh receiptgenerationservice 5300`, store results in `artifacts/receiptgenerationservice-validation-report.md`. | | |

**Completion Criteria:** Minimal hosting + OTEL + OpenAPI + ADR-0005; smoke tests & health validation pass; artifacts stored.

## 3. Alternatives

- **ALT-001**: Keep legacy logging — rejected.
- **ALT-002**: Skip binding validation — rejected (critical to ensure output binding works on .NET 10).

## 4. Dependencies

- `docs/research/dotnet-upgrade-analysis.md`
- `plan/testing-validation-strategy.md`
- `plan/cicd-modernization-strategy.md`

## 5. Files

- `RedDog.ReceiptGenerationService/RedDog.ReceiptGenerationService.csproj`
- `RedDog.ReceiptGenerationService/Program.cs`
- `RedDog.ReceiptGenerationService/Dockerfile`
- `RedDog.ReceiptGenerationService/Controllers/ProbesController.cs`
- Tooling/validation artifacts under `artifacts/`

## 6. Testing

- `dotnet test RedDog.ReceiptGenerationService.Tests`
- Dapr binding smoke test `ci/scripts/run-dapr-receiptgeneration-smoke.sh`
- Health endpoints script

## 7. Risks & Assumptions

- **RISK-001**: Output binding providers change behavior — mitigate with smoke test.
- **RISK-002**: Nullable warnings backlog — treat as errors.
- **ASSUMPTION-001**: Storage/binding credentials available for smoke tests.

## 8. Related Specifications

- `plan/modernization-strategy.md`
- `docs/research/dotnet-upgrade-analysis.md`
- `plan/testing-validation-strategy.md`
- `plan/cicd-modernization-strategy.md`
- ADRs 0001 & 0005
