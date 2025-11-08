# Red Dog Modernization Strategy

## Executive Summary

Transform Red Dog from a .NET-centric demo (2021) into a modern, polyglot microservices reference implementation for teaching Dapr, Kubernetes, and KEDA in 2025.

**Key Goals:**
- Polyglot architecture (5 languages: .NET, Go, Python, Node.js, Vue.js)
- One-command deployment to cloud platforms (AKS, Container Apps, EKS, GKE)
- Modern dependencies (latest LTS versions)
- Cloud-first approach (remove local dev complexity)

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
- 3 manifest directories (branch, corporate, local)
- VS Code heavy configuration
- Devcontainer setup

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

**Secret Management Decision:** All services (including init containers for database setup) will use **Dapr secret store** with Azure Key Vault backend. CSI Secrets Store driver will NOT be used to maintain consistency and avoid dual secret management solutions.

### Infrastructure:
- Modern GitHub Actions workflows (GitHub Container Registry - GHCR)
- Single manifest directory (branch only)
- Deployment automation scripts (Bash + Helm charts)
- OpenTelemetry for observability
- Minimal VS Code config (optional)

---

## What to Remove

### Directories:
- [x] `.vscode/` - ✅ Removed (Phase 3)
- [x] `.devcontainer/` - ✅ Removed (Phase 1)
- [x] `manifests/local/` - ✅ Removed (Phase 1)
- [x] `manifests/corporate/` - ✅ Removed (Phase 1)
- [x] `RedDog.CorporateTransferService/` - ✅ Removed (Phase 2A)
- [ ] `RedDog.Bootstrapper/` - ⚠️ Retained intentionally (console initializer stays on .NET 10)

### Files:
- [ ] `.github/workflows/*` - ⚠️ SKIPPED (9 files remain - need fixing, not deletion)
- [x] Flux-related configs (`.flux.yaml` files) - ✅ Removed (Phase 5)
- [ ] `docs/` directory - ⚠️ Retained (hosts active research such as the .NET upgrade analysis)

### Kept:
- ✅ `rest-samples/` (useful for content creation)
- ✅ GitHub workflows for active services (need modernization later)
- ✅ `docs/` research content (upgrade analysis, CI/CD plan, etc.)

---

## Phased Modernization Approach

### Phase 0: Foundation ✅ COMPLETED (2025-11-02)
**Goal:** Clean up, remove bloat, establish baseline

**Completed:**
- [x] Remove unnecessary directories/services - ✅ Done (Phases 1, 2A, 2B, 3)
  - Removed: .devcontainer/, manifests/local/, manifests/corporate/
  - Retained: `docs/` (hosts current research deliverables)
  - Removed: RedDog.CorporateTransferService/
  - Removed: .vscode/ (broken configs referencing removed services)
- [x] Simplify manifest structure (keep only branch/) - ✅ Done (Phase 1)
- [x] Remove Flux v1 configs - ✅ Done (Phase 5)

**Skipped:**
- [ ] Remove old GitHub workflows - ⚠️ SKIPPED (workflows need fixing, not deletion)
- [ ] Update CLAUDE.md with new architecture - Deferred (CLAUDE.md already current)
- [ ] Document polyglot migration decisions - Deferred to implementation phases

**Results:**
- 58 files removed (including cleanup guide)
- 4,760 lines deleted
- 2 unused services eliminated
- 5 cleanup commits + 1 guide removal
- Session documented: `.claude/sessions/2025-11-02-0730-safe-cleanup.md`

**Actual Duration:** 1 day (2025-11-02)

---

### Phase 1A: .NET 10 Upgrade (All Services)
**Goal:** Upgrade ALL 9 .NET projects to .NET 10 LTS before language migrations

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

**Deliverables:**
- All 9 .NET projects running on .NET 10 + Dapr 1.16
- Production-ready deployment (no EOL frameworks)
- Validated Dapr 1.16 compatibility across all patterns (pub/sub, state, bindings, service invocation)
- Modern .NET baseline for Phase 1B migrations

**Estimated Effort (per research):** 33-44 engineering hours spanning targeting updates, hosting/logging refactors, async fixes, and feature adoption.

**Duration:** 1-2 weeks

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

**Reference:** `docs/research/cicd-modernization.md` contains the full pipeline assessment and modernization steps.

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

**Reference:** Use the CI/CD research doc for deployment automation details and `docs/research/testing-validation-strategy.md` for validation tie-ins.

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

**Reference:** `docs/research/testing-validation-strategy.md` documents the observability validation steps.

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
