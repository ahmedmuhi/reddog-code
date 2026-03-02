# Red Dog Modernization Strategy

## Overview

Transform Red Dog from a .NET-centric demo (2021) into a modern, polyglot microservices reference implementation for teaching Dapr, Kubernetes, and KEDA.

**Full history:** `plan/Archive/modernization-strategy-full-history.md`

---

## Current State (March 2026)

All foundation work is complete. The codebase runs on modern dependencies with a production-parity local dev environment.

| Layer | Current | Notes |
|---|---|---|
| .NET services (7 + 1 lib) | .NET 10 | All 9 projects upgraded Nov 2025 |
| UI | Vue 3.5 + Vite | Upgraded Nov 2025 |
| Dapr | 1.16.2 | Kubernetes mode, app-scoped components |
| Infrastructure | Redis 7.2, SQL Server 2022, Nginx v1.14 | kind + Helm |
| CI/CD | GitHub Actions + GHCR | Modernised Nov 2025 |
| Config | 4-layer architecture | Helm defaults → env overrides → Dapr Config API |
| Local dev | kind + Helm (sole workflow) | `scripts/setup-local-dev.sh` |

---

## Completed Phases

| Phase | Description | Completed |
|---|---|---|
| 0 | Foundation cleanup (58 files removed) | Nov 2025 |
| 0.5 | kind/Helm infrastructure | Nov 2025 |
| 1 baseline | Performance baseline (k6) | Nov 2025 |
| 1A | .NET 10 upgrade (9/9 projects) | Nov 2025 |
| 2 | Vue.js 3 upgrade | Nov 2025 |
| 4 | CI/CD modernisation | Nov 2025 |
| Config consolidation | 7 phases (naming, values, Kustomize elimination, infra, Dapr Config API, docs, cleanup) | Mar 2026 |

---

## Remaining Work

### Phase 1B: Polyglot Language Migration (5 services)

**Goal:** Migrate 5 services from .NET 10 to target languages, demonstrating Dapr's language-agnostic patterns.

**Go (2 services):**
1. **MakeLineService** - Queue management, state operations, concurrency
2. **VirtualWorker** - Worker pool, order completion

**Python (2 services):**
3. **ReceiptGenerationService** - Document generation, output bindings
4. **VirtualCustomers** - Load generation, simulation

**Node.js (1 service):**
5. **LoyaltyService** - Event-driven, pub/sub subscriber, async I/O

**Staying in .NET 10:**
- OrderService - Core REST API, business logic
- AccountingService + AccountingModel - SQL Server, EF Core, analytics
- Bootstrapper - Database initialiser (until replaced)

**Strategy:**
- Migrate from modern .NET 10 baseline (not outdated .NET 6)
- Dapr 1.16 compatibility already validated in Phase 1A
- Keep .NET 10 versions in git history for side-by-side comparison
- Each migration gets its own implementation plan

**Estimated effort:** 2-3 weeks

---

### Phase 3: Cloud Deployment Automation

**Goal:** One-command deployment to cloud platforms.

**Targets:**
- Azure Kubernetes Service (AKS) - `deploy-to-aks.sh`
- Azure Container Apps (ACA) - `deploy-to-aca.sh`
- AWS EKS (future)
- Google GKE (future)

**Deliverables:**
- Deployment scripts with <10 min deploy time
- Helm-based, using existing `charts/` and `values/values-{env}.yaml` pattern

**Estimated effort:** 5-7 days

---

### Phase 5: Infrastructure Enhancements (optional/deferred)

| Item | Status | Plan |
|---|---|---|
| KEDA 2.2 → 2.18 | Deferred | `plan/keda-cloud-autoscaling-implementation-1.md` |
| cert-manager → 1.19 | Planned | `plan/upgrade-certmanager-1.19-implementation-1.md` |
| RabbitMQ upgrade | Planned | `plan/upgrade-rabbitmq-4.2-implementation-1.md` |
| Cloud state stores | Planned | `plan/migrate-state-stores-cloud-native-implementation-1.md` |
| Cloud object storage | Planned | `plan/migrate-object-storage-cloud-agnostic-implementation-1.md` |
| Workload Identity | Not started | Azure/AWS/GCP — zero secrets in cluster |
| OpenTelemetry stack | Not started | Collector + Jaeger/Tempo + Grafana dashboards |

These are cloud-specific enhancements. The local dev environment is fully operational without them.

---

## Architecture Decisions (established)

- Kebab-case app IDs (e.g., `order-service`)
- `reddog` namespace for all services
- App-scoped Dapr components (explicit `scopes` lists)
- `kind + Helm` sole local workflow
- 4-layer config: chart defaults → env overrides → Dapr Config API → env vars
- `_helpers.tpl` for shared .NET env vars
- ADRs in `docs/adr/`, knowledge items in `knowledge/`

---

## Priority

1. **Phase 1B** - Polyglot migration (core teaching value)
2. **Phase 3** - Cloud deployment (enables demos)
3. **Phase 5** - Infrastructure enhancements (when targeting cloud)

---

## Future Enhancements (out of scope)

- GitOps with Flux v2 (separate learning topic)
- Infrastructure as Code / Terraform / Bicep
- Container security scanning
- Multi-cloud abstraction beyond Dapr
