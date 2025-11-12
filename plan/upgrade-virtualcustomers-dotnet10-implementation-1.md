---
goal: Upgrade VirtualCustomers console worker to .NET 10 LTS with Dapr client compliance and telemetry
version: 1.0
date_created: 2025-11-06
last_updated: 2025-11-06
owner: Red Dog Modernization Team
status: Planned
tags: [upgrade, dotnet10, virtualcustomers, console, dapr]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

Upgrade plan for the console-based `RedDog.VirtualCustomers` workload (simulates customers via Dapr clients).

## Research References

- `docs/research/dotnet-upgrade-analysis.md` (Dependency inventory, Dapr client guidance, Docker/runtime updates)
- `plan/testing-validation-strategy.md` (Tool installation requirements, console smoke tests, artifact expectations)
- `plan/cicd-modernization-strategy.md` (CI tooling-audit/build/publish requirements)

## 1. Requirements & Constraints

- **REQ-001**: Target framework `net10.0`, nullable enabled, implicit usings, LangVersion 14.0.
- **REQ-002**: Dapr client packages upgraded to 1.16.0; console app must continue driving traffic via Dapr service invocation/pub-sub.
- **REQ-003**: Logging routed through OpenTelemetry + OTLP exporter; Serilog packages removed.
- **SEC-001**: `dotnet list package --vulnerable` returns zero vulnerabilities.
- **CON-001**: Graceful cancellation/health logging required (no HTTP endpoints, but instrumentation must confirm readiness/startup/shutdown events).
- **CON-002**: Dockerfile (if containerized) uses .NET 10 runtime base image (`mcr.microsoft.com/dotnet/runtime:10.0`).
- **GUD-001**: Use `Host.CreateApplicationBuilder`/Generic Host with background services.
- **PAT-001**: Async/await only; no `.Wait()`/`.Result()`.

### Upgrade Guardrails (2025-11-13 refresh)

- **Pin GA images:** If/when containerizing, use `mcr.microsoft.com/dotnet/sdk:10.0.100` for build and `mcr.microsoft.com/dotnet/runtime:10.0` for final image. Keep `global.json` intact.
- **Helm / Rollout awareness:** If the workload is deployed via Helm in the future, document the rollback command (`helm history`, `helm rollback <release> <rev>`) to avoid being stuck in `pending-upgrade`.
- **Port-forward helper:** Smoke tests must use `scripts/find-open-port.sh` when invoking services/Dapr endpoints to avoid hanging on occupied ports.
- **Component alignment:** When this workload publishes/subscribes to Dapr topics, ensure any component scopes reference the correct `app-id` casing.

## 2. Implementation Steps

### Implementation Phase 1

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-000** | Run Upgrade Assistant: `upgrade-assistant upgrade RedDog.VirtualCustomers/RedDog.VirtualCustomers.csproj --entry-point RedDog.VirtualCustomers/RedDog.VirtualCustomers.csproj --non-interactive --skip-backup false > artifacts/upgrade-assistant/virtualcustomers.md`. | | |
| **TASK-001** | `dotnet workload restore` + `dotnet workload update`; log to `artifacts/dependencies/virtualcustomers-workloads.txt`. | | |
| **TASK-002** | Record dependency baselines (`dotnet list ... --outdated/--vulnerable/--graph`) under `artifacts/dependencies/virtualcustomers-*.txt`. | | |
| **TASK-003** | Run API Analyzer and store results in `artifacts/api-analyzer/virtualcustomers.md`. | | |
| **TASK-004** | Update csproj to net10.0, enable nullable/implicit usings, set LangVersion 14.0, remove Serilog-related packages. | | |
| **TASK-005** | Upgrade packages: `Dapr.Client` 1.16.0, `Dapr.Extensions.Configuration` 1.16.0, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `Microsoft.Extensions.*` 10.0.x. | | |
| **TASK-006** | Re-run restore + dependency audits; confirm zero outdated/vulnerable packages. | | |
| **TASK-007** | Update Dockerfile (if applicable) to use `mcr.microsoft.com/dotnet/runtime:10.0` (console) or `sdk:10.0` for build stage. | | |
| **TASK-008** | `dotnet build ... -warnaserror`. | | |

### Implementation Phase 2

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-009** | Convert Program.cs to Generic Host builders (`Host.CreateApplicationBuilder`), register background services/timers, configure Dapr clients; remove blocking `.Wait()`; delete Startup.cs if present. | | |
| **TASK-010** | Implement structured lifecycle logging + custom readiness events (log `VirtualCustomersReady=true`), ensuring integration with OpenTelemetry. | | |
| **TASK-011** | Configure OpenTelemetry tracing + metrics (no HTTP endpoints but still emit spans/metrics). | | |
| **TASK-012** | Modernize code: file-scoped namespaces, collection expressions, remove `Task.FromResult`, resolve nullable warnings. | | |
| **TASK-013** | Validation: run `scripts/run-virtualcustomers-smoke.sh` (uses `scripts/find-open-port.sh` + Dapr invocation) to ensure the console app starts, issues Dapr calls, and shuts down cleanly; log results to `artifacts/virtualcustomers-validation-report.md`. | | |

**Completion Criteria:** Tooling artifacts stored; console app builds and runs without blocking calls; OpenTelemetry instrumentation active; smoke test results recorded.

## 3. Alternatives

- **ALT-001**: Leave console app on .NET 6 — rejected (needs parity with services).
- **ALT-002**: Keep Serilog — rejected (OpenTelemetry baseline requirement).

## 4. Dependencies

- `docs/research/dotnet-upgrade-analysis.md`
- `plan/testing-validation-strategy.md`
- `plan/cicd-modernization-strategy.md`
- Dapr components referenced by VirtualCustomers

## 5. Files

- `RedDog.VirtualCustomers/RedDog.VirtualCustomers.csproj`
- `RedDog.VirtualCustomers/Program.cs`
- `RedDog.VirtualCustomers/Dockerfile` (if containerized)
- Tooling/validation artifacts under `artifacts/`

## 6. Testing

- `dotnet test RedDog.VirtualCustomers.Tests` (if/when created)
- Smoke script `scripts/run-virtualcustomers-smoke.sh`

## 7. Risks & Assumptions

- **RISK-001**: Console app may not expose HTTP health endpoints — use logging events + smoke script to validate readiness.
- **RISK-002**: Removing `.Wait()` may reveal race conditions — mitigate via integration smoke test.
- **ASSUMPTION-001**: Workload will continue running inside container/host orchestrations with environment variables for Dapr endpoints.

## 8. Related Specifications

- `plan/modernization-strategy.md`
- `docs/research/dotnet-upgrade-analysis.md`
- `plan/testing-validation-strategy.md`
- `plan/cicd-modernization-strategy.md`
- ADRs 0001 & 0005
