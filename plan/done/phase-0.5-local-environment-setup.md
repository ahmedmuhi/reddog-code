# Phase 0.5: Local Development Environment Setup

**Status:** 🟡 In Progress
**Created:** 2025-11-10
**Prerequisite for:** Phase 1 (Performance Baseline Establishment)
**Depends on:** Phase 0 (Prerequisites & Setup) ✅ Complete

---

## Overview

Phase 0.5 establishes a **kind + Helm-based local development environment** for running Red Dog services. This is a critical prerequisite for Phase 1 (Performance Baseline) and all subsequent modernization work.

**Context:** The original local development setup (GitHub Codespaces + docker-compose + VS Code tasks) was removed during Phase 0 cleanup on 2025-11-02. We are NOT restoring that setup. Instead, we are implementing the cloud-agnostic local development strategy defined in **ADR-0008** (kind local development environment).

**Challenge:** The original setup is 3 years old (2021-2022). Dapr, Kubernetes, and infrastructure components have evolved significantly. Expect configuration incompatibilities and breaking changes.

---

## Goals

### Primary Goal
**Establish a working kind + Helm local environment** that can:
1. Run all Red Dog services locally in Kubernetes
2. Support performance baseline testing (k6 load tests)
3. Enable iterative development and testing during modernization
4. Match production architecture (Kubernetes + Dapr) while remaining lightweight

### Non-Goals (Out of Scope)
- ❌ Docker Compose setup (explicitly rejected)
- ❌ GitHub Codespaces / dev containers (not using)
- ❌ VS Code tasks/launch configs (not using)
- ❌ Full production parity (no Flux, no GitOps, no TLS)

---

## Architecture

### Target Local Environment Stack

```
┌─────────────────────────────────────────┐
│         Developer Machine (WSL2)        │
│                                         │
│  ┌───────────────────────────────────┐ │
│  │   kind Cluster (Kubernetes)       │ │
│  │                                   │ │
│  │  ┌─────────────────────────────┐ │ │
│  │  │   Infrastructure (Helm)     │ │ │
│  │  │  - Dapr 1.16                │ │ │
│  │  │  - Redis (state + pub/sub)  │ │ │
│  │  │  - SQL Server 2022          │ │ │
│  │  │  - (optional: RabbitMQ)     │ │ │
│  │  └─────────────────────────────┘ │ │
│  │                                   │ │
│  │  ┌─────────────────────────────┐ │ │
│  │  │   Application Services      │ │ │
│  │  │  - OrderService             │ │ │
│  │  │  - MakeLineService          │ │ │
│  │  │  - AccountingService        │ │ │
│  │  │  - LoyaltyService           │ │ │
│  │  │  - (+ 4 other services)     │ │ │
│  │  └─────────────────────────────┘ │ │
│  │                                   │ │
│  └───────────────────────────────────┘ │
│                                         │
│  Tools: kubectl, helm, k6, dapr CLI     │
└─────────────────────────────────────────┘
```

### Key Design Decisions

1. **kind (Kubernetes-in-Docker)** - Per ADR-0008
   - Lightweight, fast startup
   - Production-like Kubernetes environment
   - No VM overhead (uses Docker)

2. **Helm Charts** - Per ADR-0009
   - Infrastructure: Separate chart for Dapr, Redis, SQL Server
   - Application: Chart for Red Dog services
   - Environment-specific values: `values-local.yaml`

3. **SQL Server in Kubernetes** - NOT docker-compose
   - Run as StatefulSet with PVC
   - Use SQL Server Developer Edition (free, full-featured)
   - Connection via Kubernetes service DNS

4. **Dapr 1.16** - Modernized from original 1.5.0
   - Installed via Helm (dapr/dapr chart)
   - Local component configs adapted from git history

5. **Bash Scripts** - Simple orchestration
   - `scripts/local-dev-up.sh` - Start environment
   - `scripts/local-dev-down.sh` - Stop environment
   - `scripts/local-dev-status.sh` - Check health

---

## Phase Breakdown

This setup is complex enough to warrant sub-phases:

### Phase 0.5.1: Foundation (kind + Dapr)
**Goal:** Get kind cluster running with Dapr installed

**Tasks:**
1. Create `kind-config.yaml` (cluster configuration)
2. Create kind cluster: `kind create cluster --config kind-config.yaml`
3. Install Dapr via Helm: `helm install dapr dapr/dapr -n dapr-system`
4. Verify Dapr installation: `dapr status -k`

**Success Criteria:**
- ✅ kind cluster running
- ✅ Dapr control plane healthy (3 pods: operator, sidecar-injector, sentry)
- ✅ `kubectl` can connect to cluster

---

### Phase 0.5.2: Infrastructure (SQL Server + Redis)
**Goal:** Deploy infrastructure services required by Red Dog

**Tasks:**
1. Recover Dapr component configs from git history (`manifests/local/branch/`)
2. Modernize Dapr components (update to Dapr 1.16 spec)
3. Create Helm chart for infrastructure:
   - Redis (state stores + pub/sub)
   - SQL Server 2022 (StatefulSet + PVC)
   - Dapr components (as ConfigMaps/Secrets)
4. Deploy infrastructure: `helm install reddog-infra ./charts/infrastructure -f values/values-local.yaml`
5. Run Bootstrapper (EF migrations) to initialize SQL database

**Success Criteria:**
- ✅ Redis running and accessible
- ✅ SQL Server running with RedDog database initialized
- ✅ Dapr components configured (state stores, pub/sub, secret store)

**Potential Issues:**
- ⚠️ Dapr component specs may have changed (v1alpha1 → v1alpha2?)
- ⚠️ SQL Server connection strings need Kubernetes service DNS
- ⚠️ Redis connection may need authentication in newer versions

---

### Phase 0.5.3: Application Deployment (Red Dog Services)
**Goal:** Deploy Red Dog services to kind cluster

**Tasks:**
1. Build Docker images locally (all 8 services)
2. Load images into kind cluster: `kind load docker-image <image-name>`
3. Create Helm chart for Red Dog application
4. Adapt Kubernetes manifests from `manifests/branch/base/deployments/`
5. Deploy services: `helm install reddog ./charts/reddog -f values/values-local.yaml`
6. Verify all services are running with Dapr sidecars

**Success Criteria:**
- ✅ All 8 services deployed and running
- ✅ Dapr sidecars injected (2 containers per pod)
- ✅ Services can communicate via Dapr service invocation
- ✅ OrderService API accessible (port-forward or ingress)

**Potential Issues:**
- ⚠️ Image pull policies (need `imagePullPolicy: IfNotPresent`)
- ⚠️ Dapr annotations may have changed syntax
- ⚠️ Service discovery issues (Dapr app-id mapping)
- ⚠️ Resource limits may be too high for local cluster

---

### Phase 0.5.4: Validation & Testing
**Goal:** Verify end-to-end functionality before baseline testing

**Tasks:**
1. Port-forward OrderService: `kubectl port-forward svc/orderservice 5100:80`
2. Test REST API endpoints:
   - GET /product (list products)
   - POST /order (create order)
   - GET /order/{id} (retrieve order)
3. Verify pub/sub flow:
   - OrderService publishes to `orders` topic
   - MakeLineService, LoyaltyService, AccountingService receive messages
4. Verify state management:
   - MakeLineService stores orders in Redis state
   - LoyaltyService updates points in Redis state
5. Verify database:
   - AccountingService stores orders in SQL Server
   - Query SQL database to confirm data

**Success Criteria:**
- ✅ Can create orders via REST API
- ✅ Orders appear in MakeLineService queue
- ✅ Orders stored in AccountingService database
- ✅ Loyalty points updated
- ✅ No errors in service logs

---

### Phase 0.5.5: Load Testing Setup (k6)
**Goal:** Install k6 and create baseline test scripts

**Tasks:**
1. Install k6: `sudo apt install k6` (WSL2) or download binary
2. Create `load-tests/` directory
3. Create k6 test scripts:
   - `load-tests/order-service.js` - Test OrderService endpoints
   - `load-tests/makeline-service.js` - Test MakeLineService queue
   - `load-tests/accounting-service.js` - Test analytics API
4. Test k6 scripts with low load (smoke test)
5. Document k6 usage in README

**Success Criteria:**
- ✅ k6 installed and verified
- ✅ k6 scripts created for 3 services
- ✅ Smoke tests pass (10 VUs, 30s duration)
- ✅ Scripts output JSON results to `artifacts/performance/`

---

## Risk Assessment

### High Risk Areas

1. **Dapr Component Compatibility** ⚠️ HIGH
   - Original configs are Dapr 1.5.0 (2021)
   - Current target is Dapr 1.16 (2024)
   - API versions may have changed
   - Component schema may have breaking changes
   - **Mitigation:** Review Dapr 1.16 docs, test incrementally

2. **SQL Server Kubernetes Deployment** ⚠️ MEDIUM
   - SQL Server in Kubernetes is non-trivial
   - Requires persistent storage (PVC)
   - Connection string format different from docker-compose
   - EF migrations may fail on first run
   - **Mitigation:** Use official SQL Server Helm chart or StatefulSet examples

3. **Service Discovery** ⚠️ MEDIUM
   - Dapr service invocation relies on app-id matching
   - Kubernetes service names may not match original setup
   - **Mitigation:** Review manifests/branch/base/deployments/ for app-id patterns

4. **Image Build & Loading** ⚠️ LOW
   - Building 8 Docker images locally may be slow
   - kind requires manual image loading
   - **Mitigation:** Build only changed services, cache images

---

## Success Criteria (Overall Phase 0.5)

**Phase 0.5 is complete when:**

1. ✅ kind cluster running with Dapr 1.16 installed
2. ✅ SQL Server 2022 running in cluster with RedDog database
3. ✅ Redis running for state stores and pub/sub
4. ✅ All 8 Red Dog services deployed and healthy
5. ✅ End-to-end order flow works (create → queue → process → store)
6. ✅ k6 installed with test scripts ready
7. ✅ Documentation updated (CLAUDE.md, README.md)
8. ✅ Bash scripts created for easy start/stop

**When complete, we can proceed to Phase 1: Performance Baseline Establishment.**

---

## Estimated Effort

**Optimistic:** 8-12 hours (if configs work with minor updates)
**Realistic:** 16-24 hours (troubleshooting Dapr compatibility, SQL setup)
**Pessimistic:** 32-40 hours (major breaking changes, need to rewrite components)

**Recommendation:** Time-box each sub-phase. If a sub-phase takes >2x estimated time, escalate for decision (simplify? defer? research?).

---

## Next Steps

1. **Immediate:** Recover 6 Dapr component YAMLs from git history
2. **Inspect:** Review recovered configs for Dapr 1.16 compatibility
3. **Decision Point:** Can we modernize configs, or do we need to rewrite from scratch?
4. **Proceed:** Start Phase 0.5.1 (kind + Dapr foundation)

---

## References

- **ADR-0008:** kind Local Development Environment
- **ADR-0009:** Helm Multi-Environment Deployment
- **Testing Strategy:** `plan/testing-validation-strategy.md` (Phase 1 prerequisites)
- **Original Setup:** Git commit `ecca0f5^` (before manifests/local deletion)
- **Dapr Docs:** https://docs.dapr.io/operations/hosting/kubernetes/
- **kind Docs:** https://kind.sigs.k8s.io/docs/user/quick-start/

---

## Notes

**Why not docker-compose?**
- Doesn't match production (Kubernetes)
- Doesn't support Dapr well (requires custom networking)
- Doesn't align with ADR-0008 (kind-based local dev)

**Why not GitHub Codespaces?**
- Limits flexibility (cloud-only)
- Doesn't work in WSL2 local environment
- Removed in Phase 0 cleanup (intentional decision)

**Why kind vs minikube/k3d?**
- kind is fastest for CI/CD (used by Kubernetes itself)
- kind is Docker-native (no VM overhead)
- kind is recommended in ADR-0008

---

**Last Updated:** 2025-11-10 (Session: 2025-11-10-1503-phase1-performance-baseline)
