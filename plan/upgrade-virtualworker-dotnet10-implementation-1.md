---
goal: Upgrade VirtualWorker to .NET 10 LTS with modern hosting, Dapr validation, and observability
version: 1.0
date_created: 2025-11-06
last_updated: 2025-11-06
owner: Red Dog Modernization Team
status: Planned
tags: [upgrade, dotnet10, virtualworker, dapr]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

Upgrade plan for `RedDog.VirtualWorker` (port 5500), aligning with modernization, upgrade analysis, testing, and CI strategies.

## Research References

- `docs/research/dotnet-upgrade-analysis.md` (Dependency inventory, Dapr breaking changes, Docker updates)
- `plan/testing-validation-strategy.md` (Tooling workflow, worker smoke tests, artifact expectations)
- `plan/cicd-modernization-strategy.md` (GitHub Actions tooling-audit/build/publish requirements)

## 1. Requirements & Constraints

- **REQ-001**: Target framework `net10.0`, nullable enabled, implicit usings, LangVersion 14.0.
- **REQ-002**: Dapr packages upgraded to 1.16.0.
- **REQ-003**: OpenTelemetry logging/metrics; Serilog removed.
- **REQ-004**: API documentation via Microsoft.AspNetCore.OpenApi + Scalar (if HTTP endpoints exposed).
- **SEC-001**: No vulnerable packages (`dotnet list package --vulnerable`).
- **CON-001**: ADR-0005 health endpoints required.
- **CON-002**: Docker images use .NET 10 base images.
- **GUD-001**: Minimal hosting pattern.
- **PAT-001**: Async I/O; no blocking queue operations.

### Upgrade Guardrails (2025-11-13 refresh)

- **Pin GA images:** Use `mcr.microsoft.com/dotnet/sdk:10.0.100` and `mcr.microsoft.com/dotnet/aspnet:10.0` explicitly. Keep `global.json` in place—do not delete it inside Dockerfiles.
- **Component scopes:** When renaming app IDs or labels, update Dapr component `scopes` and validation scripts in the same change.
- **Helm rollback playbook:** If `helm upgrade` reports "another operation in progress," run `helm history reddog` and `helm rollback reddog <previous rev>` before retrying.
- **Port-forward helper:** All smoke tests must use `scripts/find-open-port.sh` instead of hardcoding 52xx host ports; this prevents the "port already in use" hang.

## 2. Implementation Steps

### Implementation Phase 1

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-000** | Upgrade Assistant command → `artifacts/upgrade-assistant/virtualworker.md`. | | |
| **TASK-001** | Workload restore/update log → `artifacts/dependencies/virtualworker-workloads.txt`. | | |
| **TASK-002** | Dependency audits/graph outputs saved under `artifacts/dependencies/virtualworker-*.txt`. | | |
| **TASK-003** | API Analyzer report → `artifacts/api-analyzer/virtualworker.md`. | | |
| **TASK-004** | Update csproj to net10.0, enable nullable/implicit usings, remove legacy packages. | | |
| **TASK-005** | Upgrade packages (Dapr.AspNetCore 1.16.0, OpenTelemetry, Microsoft.AspNetCore.OpenApi, Scalar, Microsoft.Extensions.* 10.0.x). | | |
| **TASK-006** | Re-run restore + dependency audits (overwrite artifacts). | | |
| **TASK-007** | Update Dockerfile to .NET 10 base images. | | |
| **TASK-008** | `dotnet build ... -warnaserror`. | | |

### Implementation Phase 2

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-009** | Convert Program.cs to minimal hosting; configure background worker + Dapr handlers; delete Startup.cs. | | |
| **TASK-010** | Implement ADR-0005 health checks (including upstream service invocation readiness); remove ProbesController. | | |
| **TASK-011** | Configure OpenTelemetry tracing/metrics. | | |
| **TASK-012** | Configure OpenAPI + Scalar (if controllers present); otherwise document reason for exclusion. | | |
| **TASK-013** | Apply modernization features: file-scoped namespaces, remove `Task.FromResult`, resolve nullable warnings. | | |
| **TASK-014** | Validation: `dotnet test` (when available), run `scripts/run-dapr-virtualworker-smoke.sh` (port helper + Dapr invocation), run `scripts/upgrade-validate.sh VirtualWorker`, record results in `artifacts/virtualworker-validation-report.md`. | | |

## 3. Alternatives

- **ALT-001**: Keep background worker on legacy host builder — rejected.
- **ALT-002**: Delay OpenTelemetry — rejected (observability baseline required).

## 4. Dependencies

- `docs/research/dotnet-upgrade-analysis.md`
- `plan/testing-validation-strategy.md`
- `plan/cicd-modernization-strategy.md`

## 5. Files

- `RedDog.VirtualWorker/RedDog.VirtualWorker.csproj`
- `RedDog.VirtualWorker/Program.cs`
- `RedDog.VirtualWorker/Dockerfile`
- `RedDog.VirtualWorker/Controllers/ProbesController.cs`
- Tooling/validation artifacts under `artifacts/`

## 6. Testing

- `dotnet test RedDog.VirtualWorker.Tests`
- Dapr/HTTP smoke test `scripts/run-dapr-virtualworker-smoke.sh` (POST `/orders` via ingress and `/v1.0/invoke/virtualworker/method/orders` via Dapr)
- Health endpoints exercised via `scripts/upgrade-validate.sh VirtualWorker`

## 7. Risks & Assumptions

- **RISK-001**: Worker concurrency changes may affect queue processing — mitigate via smoke test.
- **RISK-002**: Nullable warnings backlog — treat as errors.
- **ASSUMPTION-001**: Worker tests will be created/enabled.

## 8. Related Specifications

- `plan/modernization-strategy.md`
- `docs/research/dotnet-upgrade-analysis.md`
- `plan/testing-validation-strategy.md`
- `plan/cicd-modernization-strategy.md`
- ADRs 0001 & 0005
