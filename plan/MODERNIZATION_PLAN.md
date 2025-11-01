# Red Dog Modernization Plan

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
- Dapr 1.3.0 (Released 2021, current is 1.14+)
- KEDA 2.2.0 (Released 2021, current is 2.16+)
- Flux v1 (DEPRECATED)

### Services (10 .NET services + 1 Vue UI):
1. OrderService (.NET)
2. MakeLineService (.NET)
3. LoyaltyService (.NET)
4. ReceiptGenerationService (.NET)
5. AccountingService (.NET)
6. VirtualCustomers (.NET)
7. VirtualWorker (.NET)
8. Bootstrapper (.NET)
9. CorporateTransferService (.NET)
10. UI (Vue.js 2)

### Infrastructure:
- GitHub workflows (deprecated syntax, hardcoded MS config)
- 3 manifest directories (branch, corporate, local)
- VS Code heavy configuration
- Devcontainer setup

---

## Target State (To-Be)

### Modern Tech Stack:
- **.NET 9** (Latest stable, November 2024)
- **Node.js 20 or 22** (LTS versions)
- **Vue.js 3.5** (Current stable)
- **Go 1.23 or 1.24** (Latest stable)
- **Python 3.12 or 3.13** (Latest stable)
- **Dapr 1.15 or 1.16** (Latest stable)
- **KEDA 2.17** (Latest stable)

### Polyglot Services (8 services):

**Go (2 services):**
1. **MakeLineService** - Queue management, state operations, concurrency
2. **VirtualWorker** - Worker pool, order completion

**Python (2 services):**
3. **ReceiptGenerationService** - Document generation, output bindings
4. **VirtualCustomers** - Load generation, simulation

**Node.js (1 service):**
5. **LoyaltyService** - Event-driven, pub/sub subscriber, async I/O

**.NET (2 services):**
6. **OrderService** - Core REST API, business logic
7. **AccountingService** - SQL Server, EF Core, analytics

**Vue.js (1 service):**
8. **UI** - Dashboard (Vue 3)

### Removed Services:
- ❌ Bootstrapper (replace with init containers/SQL scripts)
- ❌ CorporateTransferService (Arc scenarios not needed)

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
- [x] `RedDog.Bootstrapper/` - ✅ Removed (Phase 2B)
- [x] `RedDog.CorporateTransferService/` - ✅ Removed (Phase 2A)

### Files:
- [ ] `.github/workflows/*` - ⚠️ SKIPPED (9 files remain - need fixing, not deletion)
- [x] Flux-related configs (`.flux.yaml` files) - ✅ Removed (Phase 5)
- [x] `docs/` directory - ✅ Removed (Phase 1)

### Kept:
- ✅ `rest-samples/` (useful for content creation)
- ✅ GitHub workflows for active services (need modernization later)

---

## Phased Modernization Approach

### Phase 0: Foundation ✅ COMPLETED (2025-11-02)
**Goal:** Clean up, remove bloat, establish baseline

**Completed:**
- [x] Remove unnecessary directories/services - ✅ Done (Phases 1, 2A, 2B, 3)
  - Removed: .devcontainer/, manifests/local/, manifests/corporate/, docs/
  - Removed: RedDog.Bootstrapper/, RedDog.CorporateTransferService/
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

### Phase 1: .NET Modernization
**Goal:** Update existing .NET services to latest LTS

**Services to update:**
- [ ] OrderService: .NET 6 → .NET 8/9
- [ ] AccountingService: .NET 6 → .NET 8/9
- [ ] Update all Dockerfiles
- [ ] Update NuGet packages (Dapr SDK, EF Core, Serilog, Swashbuckle)
- [ ] Test services still work

**Deliverables:**
- Modern .NET services
- Updated Dockerfiles
- Updated K8s manifests with new image tags

**Duration:** 2-3 days

---

### Phase 2: Vue.js Modernization
**Goal:** Upgrade UI to Vue 3

- [ ] Update package.json dependencies
- [ ] Migrate Vue 2 → Vue 3 (Composition API)
- [ ] Update Vue Router 3 → 4
- [ ] Update build tooling (Vite instead of Vue CLI?)
- [ ] Update Dockerfile (Node 14 → Node 20/22)
- [ ] Test UI functionality

**Deliverables:**
- Modern Vue 3 UI
- Updated Dockerfile
- Updated K8s manifest

**Duration:** 2-3 days

---

### Phase 3: Go Service Migration
**Goal:** Rewrite services in Go

**Service 1: VirtualWorker** (Easier)
- [ ] Create new `RedDog.VirtualWorker.Go/` directory
- [ ] Implement worker logic with Dapr Go SDK
- [ ] Create Dockerfile
- [ ] Update K8s manifest
- [ ] Test against existing services

**Service 2: MakeLineService** (Medium complexity)
- [ ] Create new `RedDog.MakeLineService.Go/` directory
- [ ] Implement REST API with Gin/Echo
- [ ] Implement Dapr state management
- [ ] Create Dockerfile
- [ ] Update K8s manifest
- [ ] Test pub/sub subscription and API endpoints

**Deliverables:**
- 2 production-ready Go services
- Dockerfiles
- Updated manifests
- Example of Go + Dapr patterns

**Duration:** 3-5 days

---

### Phase 4: Python Service Migration
**Goal:** Rewrite services in Python

**Service 1: VirtualCustomers** (Easier)
- [ ] Create new `RedDog.VirtualCustomers.Python/` directory
- [ ] Implement load generation with Dapr Python SDK
- [ ] Create Dockerfile
- [ ] Update K8s manifest
- [ ] Test order creation flow

**Service 2: ReceiptGenerationService** (Easy)
- [ ] Create new `RedDog.ReceiptGenerationService.Python/` directory
- [ ] Implement receipt generation (JSON/PDF)
- [ ] Implement Dapr output binding
- [ ] Create Dockerfile
- [ ] Update K8s manifest
- [ ] Test pub/sub subscription and binding

**Deliverables:**
- 2 production-ready Python services
- Dockerfiles
- Updated manifests
- Example of Python + Dapr patterns

**Duration:** 3-4 days

---

### Phase 5: Node.js Service Migration
**Goal:** Rewrite LoyaltyService in Node.js

- [ ] Create new `RedDog.LoyaltyService.NodeJS/` directory
- [ ] Implement loyalty logic with Dapr Node.js SDK
- [ ] Implement pub/sub subscription
- [ ] Implement state management
- [ ] Create Dockerfile
- [ ] Update K8s manifest
- [ ] Test event-driven flow

**Deliverables:**
- Production-ready Node.js service
- Dockerfile
- Updated manifest
- Example of Node.js + Dapr patterns

**Duration:** 2-3 days

---

### Phase 6: Deployment Automation (CRITICAL PATH)
**Goal:** One-command deployment scripts

**Azure Kubernetes Service:**
- [ ] Create `deploy-to-aks.sh` script
  - Provision AKS cluster
  - Install Dapr
  - Install KEDA
  - Apply manifests
  - Configure ingress
  - Output access URLs

**Azure Container Apps:**
- [ ] Create `deploy-to-aca.sh` script
  - Provision Container Apps environment
  - Enable Dapr
  - Deploy services
  - Configure scaling rules
  - Output access URLs

**Elastic Kubernetes Service (Future):**
- [ ] Create `deploy-to-eks.sh` script

**Google Kubernetes Engine (Future):**
- [ ] Create `deploy-to-gke.sh` script

**Deliverables:**
- Working deployment scripts
- Minimal documentation
- Tested end-to-end deployments

**Duration:** 5-7 days

---

### Phase 7: CI/CD Modernization
**Goal:** Modern GitHub Actions workflows with GHCR

- [ ] Create matrix build workflow (builds all services)
- [ ] Push to GitHub Container Registry (GHCR) - free for public repos
- [ ] Use latest GitHub Actions versions
- [ ] Remove deprecated syntax
- [ ] Implement proper tagging strategy (git SHA + latest)
- [ ] Add automated testing (optional)

**Deliverables:**
- Modern `.github/workflows/` setup
- Automated image builds to GHCR
- Public container images available
- Proper tagging strategy

**Duration:** 2-3 days

---

### Phase 7b: Helm Charts (Alternative Deployment)
**Goal:** Production-ready Helm charts as alternative to bash scripts

- [ ] Create `helm-chart/` directory structure
- [ ] Create Chart.yaml and values.yaml
- [ ] Template K8s manifests (deployments, services, ingress)
- [ ] Parameterize common values (image tags, replicas, resources)
- [ ] Document Helm installation process
- [ ] Test deployment via Helm

**Deliverables:**
- Complete Helm chart for Red Dog
- values.yaml with sensible defaults
- Documentation for Helm-based deployment
- Alternative to bash scripts for production-like deployments

**Duration:** 2-3 days

**Note:** This is optional but recommended for teaching production deployment patterns

---

### Phase 8: Dapr & KEDA Updates
**Goal:** Update infrastructure components to latest versions

- [ ] Update Dapr components to v1.15/1.16 API
- [ ] Update KEDA manifests to v2.17
- [ ] Test KEDA autoscaling behavior (CPU, RabbitMQ queue length)
- [ ] Update component configs (Redis, RabbitMQ, SQL)
- [ ] Verify Dapr 1.16 features work correctly

**Deliverables:**
- Modern Dapr components (1.15/1.16)
- Modern KEDA scaled objects (2.17)
- Tested autoscaling demos
- Updated component manifests

**Duration:** 2-3 days

---

### Phase 8b: OpenTelemetry Integration (Enhanced Observability)
**Goal:** Add distributed tracing and observability with OpenTelemetry

**Why:** Dapr 1.16 has built-in OpenTelemetry support, making this easy to implement and highly valuable for teaching distributed systems observability.

- [ ] Deploy OpenTelemetry Collector to cluster
- [ ] Enable Dapr OpenTelemetry configuration
- [ ] Deploy Jaeger or Tempo for trace visualization
- [ ] Configure trace sampling rates
- [ ] Add Grafana dashboards for metrics
- [ ] Document how to view traces across services

**Deliverables:**
- OpenTelemetry Collector deployment
- Jaeger/Tempo for distributed tracing
- Grafana dashboards showing:
  - Service-to-service calls
  - Pub/sub message flow
  - State operations
  - Performance metrics
- Documentation on using observability tools

**Teaching Value:**
- Shows distributed tracing in microservices
- Demonstrates Dapr telemetry integration
- Industry-standard observability stack
- Easy to correlate requests across services

**Duration:** 2-3 days

**Note:** This is highly recommended for teaching observability patterns

---

## Priority Matrix

### Critical Path (Must Have):
1. **Phase 0** - Foundation cleanup
2. **Phase 1** - .NET modernization (keeps existing services working)
3. **Phase 6** - Deployment automation (teaching goal)

### High Priority (Should Have):
4. **Phase 3** - Go services (polyglot demo)
5. **Phase 4** - Python services (polyglot demo)
6. **Phase 5** - Node.js service (polyglot demo)

### Medium Priority (Nice to Have):
7. **Phase 2** - Vue 3 (UI modernization)
8. **Phase 7** - CI/CD with GHCR (development workflow)
9. **Phase 8** - Dapr/KEDA updates (infrastructure)

### Enhanced Features (Recommended):
10. **Phase 7b** - Helm Charts (production deployment patterns)
11. **Phase 8b** - OpenTelemetry (observability & tracing)

---

## Success Criteria

### Technical:
- ✅ All services run on latest LTS versions
- ✅ 5 programming languages represented
- ✅ One-command deployment to AKS and Container Apps
- ✅ KEDA autoscaling demonstrated
- ✅ All Dapr patterns showcased (pub/sub, state, bindings, service invocation)
- ✅ Container images published to GHCR (free for public repos)
- ✅ OpenTelemetry for distributed tracing (optional but recommended)

### Teaching:
- ✅ Students can deploy in <10 minutes
- ✅ Easy to demonstrate polyglot architecture
- ✅ Clear service boundaries and responsibilities
- ✅ Modern, production-like patterns
- ✅ Extensible for adding new patterns (circuit breakers, observability, etc.)
- ✅ Both simple (bash) and production (Helm) deployment options

---

## Risk Mitigation

### Risk: Language migrations introduce bugs
**Mitigation:** Keep original .NET services in git history, test thoroughly

### Risk: Deployment scripts fail on different environments
**Mitigation:** Test on clean subscriptions/accounts, document prerequisites

### Risk: Too much scope, takes too long
**Mitigation:** Follow phased approach, prioritize critical path first

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
