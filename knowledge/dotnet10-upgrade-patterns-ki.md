---
id: KI-DOTNET10_UPGRADE_PATTERNS-001
title: .NET 10 Upgrade Patterns & Failure Prevention
tags:
  - red-dog
  - dotnet
  - upgrade
  - health-checks
  - dapr
  - deployment
last_updated: 2025-11-24
source_sessions: []
source_plans:
  - plan/upgrade-phase0-platform-foundation-implementation-1.md
confidence: high
status: Active
owner: Platform Engineering
notes: Derived from successful upgrades of OrderService, ReceiptGenerationService, and AccountingService
---

# Summary

This Knowledge Item captures the repeatable patterns, constraints, and failure modes discovered during .NET 10 upgrades of Red Dog services. It defines the canonical upgrade checklist, automation guardrails, and validation criteria to prevent recurring issues (health probe drift, missing Dapr sidecars, configuration drift, stale images).

## Key Facts

- **FACT-001**: Red Dog services upgraded to .NET 10 LTS target framework `net10.0` with `LangVersion` 14.0 and nullable reference types enabled.
- **FACT-002**: Health check endpoints follow ADR-0005: `/healthz` (startup/liveness), `/livez` (liveness), `/readyz` (readiness with Dapr sidecar check).
- **FACT-003**: Dapr sidecar injection is confirmed by verifying pod shows `2/2` Running, NOT `1/1`.
- **FACT-004**: All images must be built with three tags: `:net10`, `:net10-test`, and `ghcr.io/...:latest`, then pushed to GHCR and optionally loaded into kind.
- **FACT-005**: Standard OpenTelemetry packages (v1.12.0) replace Serilog for observability.
- **FACT-006**: REST APIs use `Microsoft.AspNetCore.OpenApi` and `Scalar.AspNetCore` instead of Swashbuckle.
- **FACT-007**: Production-ready `DaprSidecarHealthCheck` implementation exists in `RedDog.AccountingService/HealthChecks/` and should be copied to other services.
- **FACT-008**: Automation scripts (`upgrade-preflight.sh`, `upgrade-build-images.sh`, `upgrade-validate.sh`, `upgrade-dotnet10.sh`) enforce the upgrade workflow.

## Constraints

- **CON-001**: Code changes (health endpoints) and infrastructure changes (Helm probe paths) MUST be committed together in the same change set to prevent drift.
- **CON-002**: Validation MUST confirm `2/2` container count before declaring upgrade success; `1/1` indicates missing Dapr sidecar.
- **CON-003**: Configuration keys in code MUST align with Helm chart environment variable names (accounting for ASP.NET Core `__` → `:` translation).
- **CON-004**: All environment variables required by the service (`ASPNETCORE_URLS`, `ConnectionStrings__RedDog`, `SA_PASSWORD`) MUST be defined in the Helm chart.
- **CON-005**: Image tags MUST be pushed to GHCR; building locally without pushing causes deployments to use stale registry images.
- **CON-006**: Build and deployment automation MUST rely on command exit codes, NOT log text parsing, to determine success/failure.
- **CON-007**: Dapr environment variable helpers (e.g., `EnsureDaprEnvironmentVariables`) MUST run AFTER `WebApplication.CreateBuilder` has loaded configuration.
- **CON-008**: Helm release names, Kubernetes labels, deployment names, and Dapr `app-id` values MUST stay synchronized to avoid "component not configured" errors.

## Patterns & Recommendations

- **PAT-001**: Run `upgrade-preflight.sh` before starting any upgrade to verify Dapr injector health, identify stuck rollouts, and document current state.
- **PAT-002**: Copy the production-ready `DaprSidecarHealthCheck.cs` from `RedDog.AccountingService/HealthChecks/` to the target service and update the namespace.
- **PAT-003**: Update `.csproj` with `net10.0`, `LangVersion 14.0`, Dapr 1.16.0, OpenTelemetry 1.12.0, and remove all Serilog/Swashbuckle packages.
- **PAT-004**: Register health checks with tagged predicates: `"live"` for `/healthz` and `/livez`, `"ready"` (including `DaprSidecarHealthCheck`) for `/readyz`.
- **PAT-005**: Update Helm chart probes to use `/healthz` (startupProbe), `/livez` (livenessProbe), and `/readyz` (readinessProbe) with appropriate timeouts.
- **PAT-006**: Add `ASPNETCORE_URLS=http://+:80` environment variable to all services; add database-specific env vars (`SA_PASSWORD`, `ConnectionStrings__RedDog`) where applicable.
- **PAT-007**: Use `upgrade-build-images.sh` to build ALL image tags in one operation, push to GHCR, and optionally load into kind with `LOAD_INTO_KIND=true`.
- **PAT-008**: Deploy with `helm upgrade reddog charts/reddog -f values/values-local.yaml --wait --timeout 5m`, then wait 5 seconds for stabilization.
- **PAT-009**: Run `upgrade-validate.sh` to verify `2/2` pod status, all health endpoints return HTTP 200, zero probe failures, and Dapr subscriptions are registered.
- **PAT-010**: Perform functional smoke tests (e.g., `curl` against forwarded service port) before updating session logs and modernization strategy.
- **PAT-011**: For database services, verify `SA_PASSWORD` is substituted (not literal `${SA_PASSWORD}`) and configuration keys match between code and Helm.

## Risks & Open Questions

### Risks

- **RISK-001**: Upgrading code without updating Helm probes causes HTTP 404 on health endpoints, leading to CrashLoopBackOff.
- **RISK-002**: Declaring success when pod shows `1/1` instead of `2/2` results in missing Dapr sidecar and non-functional pub/sub.
- **RISK-003**: Building images locally without pushing to GHCR causes cluster to pull stale images lacking health endpoints.
- **RISK-004**: Configuration key mismatches between code and Helm (e.g., `ConnectionStrings:RedDog` vs `ConnectionStrings__RedDog`) cause database connection failures.
- **RISK-005**: Missing `ASPNETCORE_URLS` environment variable causes services to bind to unexpected ports or fail to start.
- **RISK-006**: Relying on log text parsing instead of exit codes for build/deploy validation leads to false positives.

### Open Questions

- **OPEN-001**: Should automation scripts support rollback scenarios when validation fails?
- **OPEN-002**: Should preflight checks enforce minimum Helm/kubectl/Docker versions?

## Source & Provenance

- Derived from: `docs/guides/dotnet10-upgrade-procedure.md` (migrated to KI)
- Related implementation plans:
  - `plan/upgrade-phase0-platform-foundation-implementation-1.md`
- Related ADRs:
  - `docs/adr/adr-0001-dotnet10-lts-adoption.md` (decision rationale)
  - `docs/adr/adr-0005-kubernetes-health-probe-standardization.md` (health endpoint patterns)
- Validated with:
  - OrderService (complete)
  - ReceiptGenerationService (complete)
  - AccountingService (complete)
