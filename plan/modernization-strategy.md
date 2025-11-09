# Red Dog Modernization Strategy

## Executive Summary

Transform Red Dog from a .NET-centric demo (2021) into a modern, polyglot microservices reference implementation for teaching Dapr, Kubernetes, and KEDA in 2025.

**Key Goals:**
- Polyglot architecture (5 languages: .NET, Go, Python, Node.js, Vue.js)
- Modern dependencies (latest LTS versions)
- Cloud-agnostic architecture with production-parity local development (kind + Kubernetes)

---

## Current State (As-Is)

### Tech Stack (All Outdated):
- .NET 6.0 (EOL November 2024)
- Node.js 14 (EOL April 2023)
- Vue.js 2.6 (EOL December 2023)
- Dapr 1.5.0 (Released 2022, current is 1.16)
- KEDA 2.2.0 (Released 2021, current is 2.17)
- Flux v1 (DEPRECATED)

### Services (9 .NET projects + 1 Vue UI):
1. OrderService (.NET)
2. MakeLineService (.NET)
3. LoyaltyService (.NET)
4. ReceiptGenerationService (.NET)
5. AccountingService (.NET)
6. AccountingModel (.NET - EF Core library used by AccountingService)
7. VirtualCustomers (.NET)
8. VirtualWorker (.NET)
9. Bootstrapper (.NET)
10. UI (Vue.js 2)

**Note:** AccountingModel is a class library consumed by AccountingService, not a standalone microservice.

### Infrastructure:
- GitHub workflows (deprecated syntax, hardcoded MS config)
- Single manifest directory (`manifests/branch/` only - local and corporate removed in Phase 0)

---

## Target State (To-Be)

### Modern Tech Stack:
- **.NET 10** (Latest LTS, November 2025)
- **Node.js 24** (LTS, supported until April 2028)
- **Vue.js 3.5** (Current stable)
- **Go 1.23 or 1.24** (Latest stable)
- **Python 3.12 or 3.13** (Latest stable)
- **Dapr 1.16** (Latest stable, September 2025)
- **KEDA 2.17** (Latest stable)

### Polyglot Services (7 microservices + 1 library):

**Go (2 services):**
1. **MakeLineService** - Queue management, state operations, concurrency
2. **VirtualWorker** - Worker pool, order completion

**Python (2 services):**
3. **ReceiptGenerationService** - Document generation, output bindings
4. **VirtualCustomers** - Load generation, simulation

**Node.js (1 service):**
5. **LoyaltyService** - Event-driven, pub/sub subscriber, async I/O

**.NET (2 services + 1 library):**
6. **OrderService** - Core REST API, business logic
7. **AccountingService** - SQL Server, EF Core, analytics
   - **AccountingModel** - Shared EF Core library (not a service)

**Vue.js (1 UI):**
8. **UI** - Dashboard (Vue 3)

### Removed Services:
- ❌ CorporateTransferService (Arc scenarios not needed)

### Retained Services:
- ✅ Bootstrapper (console database initializer) stays in .NET 10 until a replacement init flow is implemented.

### Infrastructure:
- Modern GitHub Actions workflows (GitHub Container Registry - GHCR)
- Single manifest directory (branch only)
- Deployment automation scripts (Bash + Helm charts)
- OpenTelemetry for observability

---

## Historical Reference: Cleanup Completed (Phase 0 - 2025-11-02)

The following items were removed during Phase 0 cleanup:

**Directories Removed:**
- ✅ `.vscode/` - Removed (broken configs referencing removed services)
- ✅ `.devcontainer/` - Removed (Phase 0)
- ✅ `manifests/local/` - Removed (Phase 0)
- ✅ `manifests/corporate/` - Removed (Phase 0)
- ✅ `RedDog.CorporateTransferService/` - Removed (Arc scenarios not needed)

**Files Removed:**
- ✅ Flux v1 configs (`.flux.yaml` files) - Deprecated technology removed

**Intentionally Retained:**
- ✅ `RedDog.Bootstrapper/` - Console database initializer stays in .NET 10
- ✅ `rest-samples/` - Useful for API testing and content creation
- ✅ GitHub workflows - Need modernization, not deletion
- ✅ `docs/` directory - Hosts active ADRs and research documents

**Cleanup Summary:**
- 58 files removed
- 4,760 lines deleted
- 2 unused services eliminated
- Session documented: `.claude/sessions/2025-11-02-0730-safe-cleanup.md`

---

## Phased Modernization Approach

### ✅ HISTORICAL: Phase 0 Cleanup (Completed 2025-11-02)

**This phase is complete.** The following cleanup was performed to establish a clean baseline:

**What Was Removed:**
- Removed `.devcontainer/`, `manifests/local/`, `manifests/corporate/`, `.vscode/`
- Removed `RedDog.CorporateTransferService/` (Arc scenarios not needed)
- Removed Flux v1 configuration files

**What Was Retained:**
- `manifests/branch/` - Single manifest directory for Kubernetes deployments
- `docs/` - Hosts ADRs and research documents
- GitHub workflows - Need modernization, not deletion
- `RedDog.Bootstrapper/` - Database initializer

**Results:**
- 58 files removed, 4,760 lines deleted
- 2 unused services eliminated
- Session: `.claude/sessions/2025-11-02-0730-safe-cleanup.md`

**Follow-Up Documentation (2025-11-09):**
- Created ADR-0008 (kind local development)
- Created ADR-0009 (Helm multi-environment deployment)
- Created ADR-0010 (Nginx Ingress Controller)
- Updated CLAUDE.md with documentation structure

---

## Active Modernization Phases

The phases below represent the **planned modernization work**:

**Phase Roadmap:**
1. **Infrastructure Prerequisites** - Upgrade Dapr, KEDA, infrastructure (3-4 weeks)
2. **Phase 1A** - .NET 10 upgrade for all services (1-2 weeks) ← **NEXT PHASE**
3. **Phase 1B** - Polyglot migration (Go, Python, Node.js) (2-3 weeks)
4. **Phase 2** - Vue.js 3 upgrade (2-3 days)
5. **Phase 3** - Deployment automation (varies)
6. **Phase 4** - CI/CD modernization (varies)
7. **Phase 5** - Infrastructure and observability enhancements (varies)

**Current Blocker:** Phase 1A requires testing strategy implementation (see `plan/testing-validation-strategy.md`)

---

### Phase Dependency Matrix (Global Prerequisites)

The following prerequisites must be completed before starting Phase 1A:

1. **Tooling Readiness**
   - Install and verify Upgrade Assistant, API Analyzer, `dotnet workload update`, `dotnet list` scripts, and Dapr CLI per *Tool Installation Requirements* in `plan/testing-validation-strategy.md`.
   - Ensure artifact directories (`artifacts/upgrade-assistant/`, `artifacts/api-analyzer/`, `artifacts/dependencies/`) exist and are writable.
2. **Testing & Validation Baseline**
   - Implement the scripts/checklists in `plan/testing-validation-strategy.md` (health endpoint validation, Dapr smoke tests, coverage collection) before touching service code.
3. **CI/CD Modernization**
   - Execute `plan/cicd-modernization-strategy.md` together with `plan/upgrade-github-workflows-implementation-1.md` so every `.github/workflows/*.yaml` file runs the tooling-audit, build/test, and publish jobs with .NET 10 SDKs/Node 24.

---

## Infrastructure Prerequisites (Platform Foundation)

**Note:** This infrastructure upgrade is **optional** and can be performed later. Phase 1A (.NET 10 upgrade) can proceed with current Dapr 1.5.0 infrastructure.

**Status:** Planned (Optional - can be deferred)
**Duration:** 3-4 weeks
**Objective:** Upgrade infrastructure platform to support modern Dapr features and polyglot services

### Why Infrastructure Upgrade is Beneficial

- **Dapr 1.5.0 is 3+ years outdated** - Missing modern features and security patches
- **KEDA 2.2.0 predates Kubernetes 1.30** - May have compatibility issues
- **State stores need cloud-native migration** - Dapr 1.16 does NOT support Redis 7/8 (only 6.x)
- **Object storage should use cloud-agnostic strategy** per ADR-0007

However, **Phase 1A (.NET 10 upgrade) can proceed with current infrastructure** (Dapr 1.5.0). This infrastructure upgrade can be performed later.

### Scope

**Platform Infrastructure:**
- Dapr 1.5.0 → 1.16.2 (runtime platform for all services)
- KEDA 2.2.0 → 2.18.1 (autoscaling framework)
- cert-manager 1.3.1 → 1.19 (TLS certificate management)

**Data Infrastructure:**
- State stores: Migrate from Redis to cloud-native databases
  - Local dev: Redis 6.2.14 (Dapr 1.16 compatible)
  - Azure: Cosmos DB (NoSQL API) via `state.azure.cosmosdb`
  - AWS: DynamoDB via `state.aws.dynamodb`
  - GCP: Cloud Firestore via `state.gcp.firestore`

**Object Storage:**
- Migrate from Azure Blob (cloud-specific) to cloud-agnostic strategy
  - Local dev: MinIO (S3-compatible) via `bindings.aws.s3`
  - AWS: S3 via `bindings.aws.s3` with IRSA
  - Azure: Blob Storage via `bindings.azure.blobstorage` with Workload Identity
  - GCP: Cloud Storage via `bindings.gcp.bucket` with Workload Identity

**Supporting Infrastructure:**
- SQL Server 2019 → 2022 (or PostgreSQL 17 alternative)
- RabbitMQ Helm 8.20.2 → Docker 4.2.0-management
- Nginx Helm 3.31.0 → Docker 1.28.0-bookworm

### Success Criteria

- ✅ Dapr 1.16.2 operational on all clusters (AKS, EKS, GKE, Container Apps)
- ✅ KEDA 2.18.1 installed and healthy
- ✅ cert-manager 1.19 issuing Let's Encrypt certificates
- ✅ State stores migrated to cloud-native databases (no data loss)
- ✅ Object storage using cloud-agnostic Dapr bindings
- ✅ All infrastructure containers upgraded to latest stable versions
- ✅ Kubernetes 1.30+ verified on all target platforms
- ✅ Workload Identity configured for Azure, AWS, GCP
- ✅ All services running Dapr 1.16 sidecars successfully
- ✅ End-to-end order flow validated (VirtualCustomers → OrderService → MakeLineService → VirtualWorker)

### Critical Constraints

- **Dapr .NET SDK 1.16.2 does NOT support .NET 10** - Services must use Dapr HTTP/gRPC APIs directly until upstream fix
- **Dapr 1.16 does NOT support Redis 7/8** - Cloud-native state stores (Cosmos DB, DynamoDB, Firestore) required
- **Azure Blob Storage has NO S3 API** - Must use cloud-specific Dapr bindings per environment
- **Redis 6.2.14 is EOL (July 2024)** - Acceptable for local dev only, NOT production

### Implementation Plans

1. [Infrastructure Prerequisites (Master Plan)](./upgrade-phase0-platform-foundation-implementation-1.md)
2. [Dapr 1.5.0 → 1.16.2 Upgrade](./upgrade-dapr-1.16-implementation-1.md)
3. [KEDA 2.2.0 → 2.18.1 Upgrade](./upgrade-keda-2.18-implementation-1.md)
4. [cert-manager 1.3.1 → 1.19 Upgrade](./upgrade-certmanager-1.19-implementation-1.md)
5. [Infrastructure Containers Upgrade](./upgrade-infrastructure-containers-implementation-1.md)
6. [Cloud-Native State Stores Migration](./migrate-state-stores-cloud-native-implementation-1.md)
7. [Cloud-Agnostic Object Storage Migration](./migrate-object-storage-cloud-agnostic-implementation-1.md)

### Risks and Mitigation

- **RISK-001: State Data Loss** - Export state before migration, verify import integrity
- **RISK-002: Dapr Service Invocation Breaking Change** - Add explicit `Content-Type: application/json` headers (Dapr 1.9+ requirement)
- **RISK-003: KEDA Helm CRD Conflicts** - Patch CRDs with Helm ownership metadata before upgrade
- **RISK-004: Workload Identity Misconfiguration** - Test in staging, document configuration steps
- **RISK-005: Production Downtime** - 2-hour maintenance window for production deployment

### Deliverables

- Modern infrastructure platform ready for .NET 10 and polyglot services
- Cloud-native state stores and object storage
- Workload Identity Federation configured (no long-lived secrets)
- Dapr 1.16 HTTP/gRPC API patterns documented
- All infrastructure upgrades validated in staging before production

**Duration:** 3-4 weeks
**Risk Level:** Medium (7 Dapr breaking changes, state migration complexity)

---

### Phase 1A: .NET 10 Upgrade (All Services)
**Goal:** Upgrade ALL 9 .NET projects to .NET 10 LTS before language migrations
**Prerequisite:** Phase 0 must complete successfully

**Reference:** See `docs/research/dotnet-upgrade-analysis.md` for the detailed Platform Upgrade Implementation Guide and Breaking Change Analysis.

**Strategic Rationale:**
- **Risk Isolation:** Separate framework upgrade from language migration (one variable at a time)
- **Dapr Validation:** Test Dapr 1.16 compatibility once in .NET, then reuse for all language migrations
- **Production Safety:** Deploy .NET 10 services immediately, removing EOL .NET 6 risk
- **Modern Baseline:** Migrate to Go/Python/Node.js from modern .NET 10 codebase, not outdated .NET 6

**Services to Upgrade (9 projects):**
1. RedDog.OrderService (.NET 6 → .NET 10)
2. RedDog.AccountingService (.NET 6 → .NET 10)
3. RedDog.AccountingModel (.NET 6 → .NET 10)
4. RedDog.MakeLineService (.NET 6 → .NET 10)
5. RedDog.LoyaltyService (.NET 6 → .NET 10)
6. RedDog.ReceiptGenerationService (.NET 6 → .NET 10)
7. RedDog.VirtualWorker (.NET 6 → .NET 10)
8. RedDog.VirtualCustomers (.NET 6 → .NET 10)
9. RedDog.Bootstrapper (.NET 6 → .NET 10)

**Key Updates:**
- .NET 10.0 SDK and runtime
- Dapr SDK 1.16+ (11 minor versions jump)
- OpenTelemetry native logging (replace Serilog)
- Scalar UI for OpenAPI documentation (replace Swashbuckle UI)
- Entity Framework Core 10.0.x (for AccountingService/AccountingModel/Bootstrapper)
- Remove deprecated package: `Microsoft.AspNetCore 2.2.0` (VirtualCustomers)

**Key Findings from Research:**
- All 7 web services still use `IHostBuilder + Startup.cs`; each needs a Program.cs refactor to `WebApplicationBuilder`.
- Health probes use `/probes/*`; migrate to ADR-0005 standard endpoints (`/healthz`, `/livez`, `/readyz`).
- VirtualCustomers must drop `Microsoft.AspNetCore 2.2.0` before retargeting.
- EF Core upgrades: AccountingService/AccountingModel/Bootstrapper must move to EF Core 10.0.x.
- Swashbuckle → Microsoft.AspNetCore.OpenApi + Scalar; Serilog → OpenTelemetry.

**Tooling & Automation Requirements:**
- Run `.NET Upgrade Assistant` for each project before manual edits: `upgrade-assistant upgrade <Project>.csproj --entry-point <Project>.csproj --non-interactive --skip-backup false`, storing logs in `artifacts/upgrade-assistant/<project>.md`.
- Execute `.NET API Analyzer` (`dotnet tool run api-analyzer -f net10.0 -p <Project>.csproj`) and capture reports in `artifacts/api-analyzer/<project>.md`.
- Capture dependency status with `dotnet list <Project>.csproj package --outdated --include-transitive`, `dotnet list <Project>.csproj package --vulnerable`, and `dotnet list <Project>.csproj reference --graph`, saving outputs to `artifacts/dependencies/<project>-outdated.txt`, `...-vulnerable.txt`, and `...-graph.txt`.
- Run `dotnet workload restore` and `dotnet workload update` prior to every `dotnet build` to keep workloads aligned with the .NET 10 SDK.

**Deliverables:**
- All 9 .NET projects running on .NET 10 + Dapr 1.16
- Production-ready deployment (no EOL frameworks)
- Validated Dapr 1.16 compatibility across all patterns (pub/sub, state, bindings, service invocation)
- Modern .NET baseline for Phase 1B migrations

**Estimated Effort (per research):** 33-44 engineering hours spanning targeting updates, hosting/logging refactors, async fixes, and feature adoption.

**Duration:** 1-2 weeks

**Implementation Guides:**
- `plan/upgrade-accountingservice-dotnet10-implementation-1.md`
- `plan/upgrade-accountingmodel-dotnet10-implementation-1.md`
- `plan/upgrade-bootstrapper-dotnet10-implementation-1.md`
- `plan/upgrade-makelineservice-dotnet10-implementation-1.md`
- `plan/upgrade-loyaltyservice-dotnet10-implementation-1.md`
- `plan/upgrade-receiptgenerationservice-dotnet10-implementation-1.md`
- `plan/upgrade-orderservice-dotnet10-implementation-1.md`
- `plan/upgrade-virtualworker-dotnet10-implementation-1.md`
- `plan/upgrade-virtualcustomers-dotnet10-implementation-1.md`

---

### Phase 1B: Language Migration (5 Services)
**Goal:** Migrate 5 services from .NET 10 to target languages

**Migration Strategy:**
- Start from modern .NET 10 baseline (not EOL .NET 6)
- Dapr 1.16 compatibility already validated in Phase 1A
- Compare modern .NET 10 vs Go/Python/Node.js (better teaching value)
- Keep .NET 10 versions in git history for reference

**Go Migrations (2 services):**
1. **MakeLineService** (.NET 10 → Go) - Queue management, concurrency patterns
2. **VirtualWorker** (.NET 10 → Go) - Worker pool, performance optimization

**Python Migrations (2 services):**
3. **ReceiptGenerationService** (.NET 10 → Python) - Document generation, scripting
4. **VirtualCustomers** (.NET 10 → Python) - Load testing, simulation

**Node.js Migration (1 service):**
5. **LoyaltyService** (.NET 10 → Node.js) - Event-driven, pub/sub, async I/O

**Keeping in .NET 10 (2 services + 1 library):**
- OrderService - Core business logic, REST API patterns
- AccountingService - SQL Server, EF Core, relational data
- AccountingModel - Shared library

**Deliverables:**
- 5 production-ready services in Go, Python, Node.js
- Polyglot architecture demonstrating Dapr language-agnostic patterns
- Side-by-side comparison of same service in .NET 10 vs target language (git history)

**Duration:** 2-3 weeks

---

### Phase 2: Vue.js Modernization
**Goal:** Upgrade UI from Vue 2 to Vue 3

**Key Changes:**
- Vue 2.6 → Vue 3.5
- Vue Router 3 → 4
- Node.js 14 → Node.js 24 LTS
- Composition API adoption
- Vite build tooling (replace Vue CLI)

**Deliverables:**
- Modern Vue 3 UI with TypeScript
- Production-ready Dockerfile
- Updated K8s manifest

**Duration:** 2-3 days

**Implementation Guide:** `plan/upgrade-ui-vue3-implementation-1.md`

---

### Phase 3: Deployment Automation (CRITICAL PATH)
**Goal:** One-command deployment to cloud platforms

**Azure Kubernetes Service (AKS):**
- Provision cluster + install Dapr/KEDA
- Deploy all services via kubectl/Helm
- Configure ingress and output URLs

**Azure Container Apps (ACA):**
- Provision environment with Dapr enabled
- Deploy services with KEDA scaling rules
- Output application URLs

**Future Platforms:**
- AWS EKS deployment script
- Google GKE deployment script

**Deliverables:**
- `deploy-to-aks.sh` and `deploy-to-aca.sh` scripts
- Tested end-to-end deployments (<10 min deployment time)
- Minimal documentation (prerequisites, usage)

**Duration:** 5-7 days

---

### Phase 4: CI/CD Modernization
**Goal:** Automate builds and publish to GitHub Container Registry

**Implementation Guides:** `plan/cicd-modernization-strategy.md`, `plan/upgrade-github-workflows-implementation-1.md`

**Reference:** `plan/cicd-modernization-strategy.md` contains the full pipeline assessment and modernization steps.

**Key Changes:**
- Matrix build workflow (all services, all languages)
- Push to GHCR (free for public repos)
- Modern tagging strategy (git SHA + latest)
- Remove deprecated GitHub Actions syntax
- Optional: Automated testing integration

**Deliverables:**
- Modern `.github/workflows/` configuration
- Public container images on GHCR
- Automated builds on every push/PR

**Duration:** 2-3 days

---

### Phase 4b: Helm Charts (Optional)
**Goal:** Production-ready Helm chart as alternative to bash scripts

**Deliverables:**
- Complete Helm chart with templated manifests
- Parameterized values.yaml (image tags, replicas, resources)
- Documentation for Helm-based deployment

**Duration:** 2-3 days

**Note:** Recommended for teaching production deployment patterns alongside bash scripts

---

### Phase 5: Infrastructure Modernization
**Goal:** Update Dapr/KEDA components and implement modern cloud patterns

**Reference:** `plan/testing-validation-strategy.md` (validation flows) and `plan/cicd-modernization-strategy.md` (deployment automation)

**Reference:** Use the CI/CD research doc for deployment automation details and `plan/testing-validation-strategy.md` for validation tie-ins.

**Dapr & KEDA Updates:**
- Dapr components → v1.16 API
- KEDA manifests → v2.17
- Test autoscaling (CPU, queue-based)
- Update component configs (Redis, RabbitMQ, SQL)

**Dapr Configuration API (Cloud-Agnostic Config):**
- Implement `reddog.config` component per environment:
  - Local: Redis
  - Azure: Azure App Configuration
  - AWS/GCP: PostgreSQL
- Migrate services to `DaprClient.GetConfiguration()`
- Enable dynamic config updates (feature flags, settings)
- Remove hardcoded environment variables from application config

**Workload Identity (AKS Security):**
- Enable Workload Identity on AKS
- Managed identities for each service
- Dapr secret store with Azure Key Vault via Workload Identity
- Remove deprecated Service Principal certificates
- Zero secrets stored in cluster (zero-trust model)

**Deliverables:**
- Modern Dapr 1.16 + KEDA 2.17 components
- Cloud-agnostic configuration via Dapr API
- Production-grade secret management (Workload Identity)
- Reference documentation and ADRs

**Duration:** 1.5 weeks

---

### Phase 5b: OpenTelemetry Integration (Optional)
**Goal:** Enhanced observability with distributed tracing

**Reference:** `plan/testing-validation-strategy.md` documents the observability validation steps.

**Key Components:**
- OpenTelemetry Collector deployment
- Jaeger/Tempo for trace visualization
- Grafana dashboards (service calls, pub/sub flow, metrics)
- Dapr 1.16 native OTEL support

**Teaching Value:**
- Industry-standard observability stack
- Distributed tracing across polyglot services
- Demonstrates Dapr telemetry integration

**Duration:** 2-3 days

**Note:** Highly recommended for teaching observability patterns

---

## Priority Matrix

### Critical Path (Must Have):
1. **Phase 0** - Foundation cleanup ✅ COMPLETED
2. **Phase 1A** - .NET 10 upgrade (all 8 projects) - **REMOVES EOL RISK**
3. **Phase 3** - Deployment automation (teaching goal)

### High Priority (Should Have):
4. **Phase 1B** - Language migrations (polyglot architecture)
   - Go services (MakeLineService, VirtualWorker)
   - Python services (ReceiptGenerationService, VirtualCustomers)
   - Node.js service (LoyaltyService)
5. **Phase 2** - Vue 3 UI modernization

### Medium Priority (Nice to Have):
6. **Phase 4** - CI/CD with GHCR (automated builds)
7. **Phase 5** - Infrastructure modernization (Dapr 1.16, KEDA 2.17, Workload Identity)

### Enhanced Features (Recommended):
8. **Phase 4b** - Helm Charts (production deployment patterns)
9. **Phase 5b** - OpenTelemetry (observability & tracing)

---

## Success Criteria

### Technical:
- ✅ All .NET services upgraded to .NET 10 LTS (Phase 1A) - **Production-safe baseline**
- ✅ Dapr 1.16 compatibility validated in .NET 10 before language migrations
- ✅ 5 programming languages represented (.NET, Go, Python, Node.js, Vue.js)
- ✅ Language migrations from modern .NET 10 baseline (not EOL .NET 6)
- ✅ One-command deployment to AKS and Container Apps (<10 min)
- ✅ KEDA autoscaling demonstrated
- ✅ All Dapr patterns showcased (pub/sub, state, bindings, service invocation)
- ✅ Container images published to GHCR (free for public repos)
- ✅ OpenTelemetry for distributed tracing (optional but recommended)

### Teaching:
- ✅ Students can deploy in <10 minutes
- ✅ Easy to demonstrate polyglot architecture
- ✅ Clear service boundaries and responsibilities
- ✅ Modern, production-like patterns (2025 best practices)
- ✅ Side-by-side comparison: .NET 10 vs Go/Python/Node.js (git history)
- ✅ Extensible for adding new patterns (circuit breakers, observability, etc.)
- ✅ Both simple (bash) and production (Helm) deployment options

### Migration Strategy:
- ✅ **Two-phase approach de-risks modernization:**
  - Phase 1A: Framework upgrade (all .NET 6 → .NET 10)
  - Phase 1B: Language migration (5 services .NET 10 → Go/Python/Node.js)
- ✅ **Validates Dapr 1.16 once in .NET, reuses for all language migrations**
- ✅ **Production runs .NET 10 (supported) while migrations proceed at own pace**

---

## Risk Mitigation

### Risk: Language migrations introduce bugs
**Mitigation:**
- Upgrade to .NET 10 first (Phase 1A) - creates known-good baseline
- Migrate from .NET 10 baseline (Phase 1B) - not outdated .NET 6
- Keep .NET 10 versions in git history for comparison
- Test Dapr 1.16 compatibility once in .NET, reuse for all languages

### Risk: Mixing framework upgrade + language migration introduces too many variables
**Mitigation:**
- **Two-phase strategy separates concerns:**
  - Phase 1A: Framework upgrade only (.NET 6 → .NET 10)
  - Phase 1B: Language migration only (.NET 10 → Go/Python/Node.js)
- If issues arise in Phase 1B, we know it's language-specific (not framework)

### Risk: Production runs EOL .NET 6 during lengthy migrations
**Mitigation:**
- Phase 1A deploys .NET 10 to production immediately
- All services on supported framework before starting language migrations
- Phase 1B can proceed at own pace (production already safe)

### Risk: Deployment scripts fail on different environments
**Mitigation:** Test on clean subscriptions/accounts, document prerequisites

### Risk: Too much scope, takes too long
**Mitigation:** Follow phased approach, prioritize critical path first (Phase 1A → Phase 3 → Phase 1B)

### Risk: Automated tooling skipped causes undetected regressions
**Mitigation:**
- Enforce execution of Upgrade Assistant, API Analyzer, and dotnet list commands for every project (artifacts saved under `artifacts/upgrade-assistant/`, `artifacts/api-analyzer/`, `artifacts/dependencies/`) before reviews.
- Block merges unless CI tooling checks pass (API Analyzer reports no critical warnings, `dotnet list package --vulnerable` returns none, dependency reports uploaded).

---

## Next Steps

1. Review and approve this plan
2. Start Phase 0 (foundation cleanup)
3. Create feature branch for modernization work
4. Regular progress updates via sessions
5. Test deployments early and often

---

## Future Enhancements (Out of MVP Scope)

These are valuable technologies but intentionally excluded from the initial modernization to keep focus on core microservices patterns:

### GitOps with Flux v2
**Why valuable:** Automated deployment from Git, declarative infrastructure
**Why not now:**
- Separate learning topic (GitOps vs. Dapr/microservices)
- Adds complexity to basic deployment
- Students need K8s fundamentals first

**When to add:** After MVP is stable and if teaching GitOps becomes a goal

### Infrastructure as Code (Terraform/Bicep)
**Why valuable:** Repeatable infrastructure provisioning, version control
**Why not now:**
- Bash scripts with `az cli` sufficient for teaching
- IaC is a separate specialized topic
- Adds abstraction layer students must learn

**When to add:** Document as alternative approach, create separate branch/example

### Container Security Scanning
**Why valuable:** Supply chain security, vulnerability detection
**Why not now:**
- Advanced DevSecOps topic
- Not core to microservices architecture teaching
- Requires additional tooling and complexity

**When to add:** Advanced security module (separate from core curriculum)

### Advanced Patching (Copacetic, etc.)
**Why valuable:** Automated container updates, security patching
**Why not now:**
- Very advanced topic
- Requires deep container understanding
- Not relevant for learning microservices patterns

**When to add:** Advanced operations topic (if ever)

### Multi-Cloud Abstraction
**Why valuable:** Portable across clouds
**Why not now:**
- Focus is Azure first (AKS, Container Apps)
- Already supporting K8s (portable to EKS/GKE with minimal changes)

**When to add:** Phase 6 already includes EKS/GKE deployment scripts (future)

---

## Notes

- Repository is for reference implementation, not self-guided tutorial
- Learning happens via blog/YouTube content
- Focus on deployment automation over documentation
- Keep REST samples for instructor testing convenience
- Target audience: Intermediate developers learning cloud-native patterns
- **MVP Philosophy:** Deploy and learn in <10 minutes, extend from there
- **Container Registry:** Using GHCR (free for OSS) instead of private registries
- **Deployment Options:** Bash scripts for simplicity, Helm for production patterns
- **Observability:** OpenTelemetry recommended but optional (easy with Dapr 1.16)
