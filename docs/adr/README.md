# Architectural Decision Records (ADRs)

## Introduction

This document serves as the **central navigation hub** for all architectural decisions in the Red Dog microservices application. It provides:

- **Implementation status tracking** for each decision
- **Configuration decision tree** to guide where settings should be placed
- **Category-based index** to find decisions by topic
- **Role-based reading guides** tailored to developers, operators, and decision makers
- **Cross-references** to related documentation

### About ADRs

We use [Architectural Decision Records (ADRs)](https://adr.github.io/) to document significant architectural choices. Each ADR follows the template defined in `adr-template.md` and captures:

- **Context:** What forces are at play?
- **Decision:** What did we decide?
- **Consequences:** What are the trade-offs?
- **Status:** Proposed → Accepted → Implemented (or Superseded/Deprecated)

### How to Read This Document

1. **New to the project?** Start with [Role-Based Reading Guides](#role-based-reading-guides) below
2. **Looking for specific decision?** Use [ADR Index by Category](#adr-index-by-category)
3. **Need to configure something?** See [Configuration Decision Tree](#configuration-decision-tree)
4. **Want to know what's implemented?** Check the status icons (🟢🟡🔵⚪) in the index

---

## Implementation Status Legend

Each ADR is marked with a status icon showing its implementation state:

| Icon | Status | Meaning |
|------|--------|---------|
| 🟢 | **Implemented** | Fully working in current codebase with evidence in code |
| 🟡 | **In Progress** | Partially implemented, active work ongoing |
| 🔵 | **Accepted** | Decision made and documented, implementation planned |
| ⚪ | **Planned** | Under consideration, not yet implemented |

---

## ADR Index by Category

### Core Platform Decisions

Foundational technology choices that affect the entire application:

- 🔵 [ADR-0001: .NET 10 LTS Adoption](adr-0001-dotnet10-lts-adoption.md)
  - **Decision:** Upgrade all .NET services from 6.0 to 10.0 LTS
  - **Status:** Accepted but not implemented (services still .NET 6.0)
  - **Why it matters:** .NET 6.0 reached EOL, .NET 10 provides long-term support until 2028

- 🟢 [ADR-0002: Cloud-Agnostic Configuration via Dapr](adr-0002-cloud-agnostic-configuration-via-dapr.md)
  - **Decision:** Use Dapr Secret Store to abstract cloud-specific secret management
  - **Status:** Implemented (secrets.yaml component exists, DaprClient.GetSecretAsync() in use)
  - **Why it matters:** Enables deployment to Azure, AWS, GCP without code changes

- 🔵 [ADR-0003: Ubuntu 24.04 Base Image Standardization](adr-0003-ubuntu-2404-base-image-standardization.md)
  - **Decision:** Use ubuntu:24.04 as base for all application containers
  - **Status:** Accepted but not implemented (Dockerfiles not created yet)
  - **Why it matters:** Security patches, consistent platform across all services

### Configuration & Secrets Management

How settings, secrets, and runtime behavior are configured:

- 🟢 [ADR-0002: Cloud-Agnostic Configuration via Dapr (Secret Store)](adr-0002-cloud-agnostic-configuration-via-dapr.md)
  - **Covers:** Secrets like connection strings, API keys, passwords
  - **Implementation:** `manifests/branch/base/components/secrets.yaml`

- ⚪ [ADR-0004: Dapr Configuration API Standardization](adr-0004-dapr-configuration-api-standardization.md)
  - **Decision:** Use Dapr Configuration API for business rules and feature flags
  - **Status:** Planned but **NOT IMPLEMENTED** (zero GetConfiguration() calls in codebase)
  - **Why it matters:** Would enable runtime configuration updates without redeployment

- 🔵 [ADR-0006: Infrastructure Configuration via Environment Variables](adr-0006-infrastructure-configuration-via-environment-variables.md)
  - **Decision:** Use environment variables for ports, URLs, runtime modes
  - **Status:** Accepted (pattern used in current services)
  - **Why it matters:** Standard Kubernetes/container pattern for infrastructure settings

**See also:** [Configuration Decision Tree](#configuration-decision-tree) below for guidance on where to put settings

### Deployment & Infrastructure

How services are packaged, deployed, and run across environments:

- ⚪ [ADR-0008: kind Local Development Environment](adr-0008-kind-local-development-environment.md)
  - **Decision:** Use kind (Kubernetes in Docker) for local development
  - **Status:** Planned but not implemented (kind-config.yaml doesn't exist)
  - **Why it matters:** Provides production-like environment on developer machines
  - **Blocker:** Requires Helm charts (ADR-0009) to be created first

- ⚪ [ADR-0009: Helm Multi-Environment Deployment](adr-0009-helm-multi-environment-deployment.md)
  - **Decision:** Use Helm charts with environment-specific values files
  - **Status:** Planned but not implemented (charts/ directory doesn't exist)
  - **Why it matters:** Enables deployment to AKS, EKS, GKE with single chart

- ⚪ [ADR-0010: Nginx Ingress Controller (Cloud-Agnostic)](adr-0010-nginx-ingress-controller-cloud-agnostic.md)
  - **Decision:** Use Nginx Ingress Controller instead of cloud-specific ingress
  - **Status:** Planned but not implemented
  - **Why it matters:** Portable HTTP routing across Azure, AWS, GCP

### Operational Standards

Runtime behavior, monitoring, and service health:

- 🔵 [ADR-0005: Kubernetes Health Probe Standardization](adr-0005-kubernetes-health-probe-standardization.md)
  - **Decision:** Implement `/healthz`, `/livez`, `/readyz` endpoints in all services
  - **Status:** Accepted but not fully implemented (current services use `/health`)
  - **Why it matters:** Standard Kubernetes health check pattern for liveness/readiness

- ⚪ [ADR-0011: OpenTelemetry Observability Standard](adr-0011-opentelemetry-observability-standard.md)
  - **Decision:** Use native OpenTelemetry OTLP exporters for logging, tracing, metrics
  - **Status:** Planned (services currently use Serilog 4.1.0)
  - **Why it matters:** Vendor-neutral observability with cloud-agnostic export targets
  - **Blocker:** Requires .NET 10 upgrade (ADR-0001) first

### Multi-Cloud Strategy

High-level architectural approach to cloud portability:

- 🔵 [ADR-0007: Cloud-Agnostic Deployment Strategy](adr-0007-cloud-agnostic-deployment-strategy.md)
  - **Decision:** Use containerized infrastructure to enable deployment to any cloud
  - **Status:** Accepted (architectural principle established)
  - **Why it matters:** Showcases that Dapr abstracts infrastructure, allowing deployment to AKS, Container Apps, EKS, GKE
  - **Related ADRs:** Implemented by ADR-0002, 0009, 0010

---

## Implementation Dashboard

Track progress across all architectural decisions:

| Category | Total ADRs | Implemented | In Progress | Planned |
|----------|-----------|-------------|-------------|---------|
| Core Platform | 3 | 0 | 0 | 3 |
| Configuration | 3 | 1 (ADR-0002) | 0 | 2 |
| Deployment | 3 | 0 | 1 (ADR-0010) | 2 |
| Operational | 2 | 0 | 0 | 2 |
| **TOTAL** | **11** | **1 (9%)** | **1 (9%)** | **9 (82%)** |

### Completion Milestones

- ✅ **Phase 0 (Cleanup):** Completed 2025-11-02
  - Removed .devcontainer, manifests/local, manifests/corporate, CorporateTransferService

- ⚠️ **Phase 1A (.NET 10 Upgrade):** Blocked
  - Blocker: Testing strategy implementation required
  - ADRs affected: ADR-0001, ADR-0003, ADR-0011

- ⚪ **Phase 1B (Polyglot Migration):** Not Started
  - Prerequisites: Phase 1A completion
  - ADRs affected: All operational standards (0005, 0011)

- ⚪ **Phase 2 (Local Development):** Not Started
  - Prerequisites: Phase 1A completion
  - ADRs affected: ADR-0008, ADR-0009, ADR-0010

### Critical Path

1. Implement testing strategy (plan/testing-validation-strategy.md)
2. Execute .NET 10 upgrade (ADR-0001)
3. Build Helm charts (ADR-0009)
4. Create kind local dev (ADR-0008)
5. Implement remaining operational standards (ADR-0005, ADR-0011)

---

## Configuration Architecture Overview

Red Dog uses a **4-layer configuration hierarchy** to separate concerns and enable cloud portability:

```
┌─────────────────────────────────────────────────────────────────┐
│ Layer 1: Deployment-Time Configuration (Helm Values)           │
│ ─────────────────────────────────────────────────────────────── │
│ Cloud-specific infrastructure settings                          │
│ • values-aks.yaml        → Azure-specific (AKS cluster, ACR)   │
│ • values-aks-aca.yaml    → Azure Container Apps variant        │
│ • values-eks.yaml        → AWS-specific (EKS cluster, ECR)     │
│ • values-gke.yaml        → GCP-specific (GKE cluster, GCR)     │
│ • values-local.yaml      → kind local development              │
│                                                                 │
│ ADR: ADR-0009 (Helm Multi-Environment Deployment) ⚪ Planned    │
└─────────────────────────────────────────────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────────────────┐
│ Layer 2: Infrastructure Configuration (Environment Variables)  │
│ ─────────────────────────────────────────────────────────────── │
│ Runtime binding addresses and platform settings                 │
│ • SERVICE_PORT=5100      → HTTP server listen port             │
│ • DAPR_HTTP_PORT=5180    → Dapr sidecar HTTP port              │
│ • ASPNETCORE_ENVIRONMENT → Deployment mode (Development/Prod)  │
│                                                                 │
│ Set in: Kubernetes Deployment YAML (from Helm templates)       │
│ Read via: Environment.GetEnvironmentVariable()                 │
│                                                                 │
│ ADR: ADR-0006 (Infrastructure Configuration) 🔵 Accepted       │
└─────────────────────────────────────────────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────────────────┐
│ Layer 3: Application Configuration (Dapr Configuration API)    │
│ ─────────────────────────────────────────────────────────────── │
│ Business rules and feature flags (runtime-updatable)            │
│ • maxRetryAttempts=3     → Resilience policy settings          │
│ • orderTimeout=300       → Business logic timeouts             │
│ • featureFlags.loyalty   → Enable/disable features             │
│                                                                 │
│ Stored in: Dapr configuration.yaml component                   │
│ Read via: DaprClient.GetConfiguration()                        │
│                                                                 │
│ ADR: ADR-0004 (Dapr Configuration API) ⚪ NOT IMPLEMENTED       │
│ Status: Zero GetConfiguration() calls exist in codebase        │
└─────────────────────────────────────────────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────────────────┐
│ Layer 4: Secrets (Dapr Secret Store)                           │
│ ─────────────────────────────────────────────────────────────── │
│ Sensitive credentials never stored in code or config files      │
│ • Azure: Azure Key Vault    (AKS deployments)                  │
│ • AWS: AWS Secrets Manager  (EKS deployments)                  │
│ • GCP: GCP Secret Manager   (GKE deployments)                  │
│ • Local: Kubernetes Secrets (kind local dev)                   │
│                                                                 │
│ Accessed via: DaprClient.GetSecretAsync("reddog.secrets", key) │
│                                                                 │
│ ADR: ADR-0002 (Cloud-Agnostic Configuration) 🟢 Implemented    │
└─────────────────────────────────────────────────────────────────┘
```

**Key Principle:** Settings flow top-to-bottom. Cloud-specific details stay in Layer 1 (Helm), application code only touches Layers 3-4 (Dapr APIs).

---

## Configuration Decision Tree

**"Where should I put this setting?"**

Use this decision tree when you need to configure anything in Red Dog:

```
START: I need to configure...
│
├─❓ Is it a SECRET? (password, API key, connection string)
│  │
│  └─ YES → Use Dapr Secret Store (Layer 4)
│           │
│           ├─ 1. Add to Azure Key Vault / AWS Secrets Manager / GCP Secret Manager
│           ├─ 2. Reference in secrets.yaml component: manifests/branch/base/components/secrets.yaml
│           ├─ 3. Access via: DaprClient.GetSecretAsync("reddog.secrets", "key-name")
│           │
│           └─ 📖 See: ADR-0002 (Cloud-Agnostic Configuration via Dapr) 🟢 Implemented
│
├─❓ Is it CLOUD-SPECIFIC INFRASTRUCTURE? (region, cluster endpoint, storage account)
│  │
│  └─ YES → Use Helm Values File (Layer 1)
│           │
│           ├─ For Azure AKS:       values-aks.yaml
│           ├─ For Azure ACA:       values-aks-aca.yaml
│           ├─ For AWS EKS:         values-eks.yaml
│           ├─ For GCP GKE:         values-gke.yaml
│           └─ For local dev:       values-local.yaml
│           │
│           └─ 📖 See: ADR-0009 (Helm Multi-Environment Deployment) ⚪ NOT IMPLEMENTED
│              Status: charts/ directory doesn't exist yet
│
├─❓ Is it a RUNTIME ADDRESS or PLATFORM SETTING? (port, URL, environment mode)
│  │
│  └─ YES → Use Environment Variable (Layer 2)
│           │
│           ├─ Examples: SERVICE_PORT, DAPR_HTTP_PORT, ASPNETCORE_ENVIRONMENT
│           ├─ Set in: Kubernetes Deployment YAML (generated from Helm templates)
│           ├─ Read via: Environment.GetEnvironmentVariable("VAR_NAME")
│           │
│           └─ 📖 See: ADR-0006 (Infrastructure Configuration) 🔵 Accepted
│
└─❓ Is it a BUSINESS RULE or FEATURE FLAG? (retry count, timeout, toggle)
   │
   └─ YES → Use Dapr Configuration API (Layer 3)
            │
            ⚠️  WARNING: NOT IMPLEMENTED YET
            │
            ├─ Planned approach:
            │  1. Define in configuration.yaml component
            │  2. Subscribe via: DaprClient.GetConfiguration()
            │  3. Get updates at runtime without redeployment
            │
            └─ 📖 See: ADR-0004 (Dapr Configuration API) ⚪ NOT IMPLEMENTED
               Status: Zero GetConfiguration() calls in codebase
               Current workaround: Use environment variables or hardcode in appsettings.json
```

**If none of the above apply:** Ask in team channel or review existing ADRs for similar scenarios.

---

## Role-Based Reading Guides

Choose your role to get a curated reading path:

### 👨‍💻 For Developers (Writing Service Code)

**Start here if you're:** Building or modifying microservices

**Essential ADRs (read first):**
1. [ADR-0002: Cloud-Agnostic Configuration via Dapr](adr-0002-cloud-agnostic-configuration-via-dapr.md) 🟢
   - How to access secrets using DaprClient
2. [ADR-0006: Infrastructure Configuration via Environment Variables](adr-0006-infrastructure-configuration-via-environment-variables.md) 🔵
   - How to read ports, URLs, runtime settings
3. [ADR-0005: Kubernetes Health Probe Standardization](adr-0005-kubernetes-health-probe-standardization.md) 🔵
   - How to implement `/healthz`, `/livez`, `/readyz` endpoints
4. [ADR-0011: OpenTelemetry Observability Standard](adr-0011-opentelemetry-observability-standard.md) ⚪
   - How to implement logging, tracing, metrics (planned migration from Serilog)

**Also useful:**
- [Web API Standards](../standards/web-api-standards.md) - HTTP API conventions (CORS, error handling, versioning)
- [CLAUDE.md](../../CLAUDE.md) - Common development commands (`dapr run` examples)

**Quick reference:** Use the [Configuration Decision Tree](#configuration-decision-tree) when adding new settings

---

### 🔧 For Platform Operators (Deploying Services)

**Start here if you're:** Deploying to Kubernetes, managing infrastructure, setting up environments

**Essential ADRs (read first):**
1. [ADR-0009: Helm Multi-Environment Deployment](adr-0009-helm-multi-environment-deployment.md) ⚪
   - How to deploy to AKS, EKS, GKE using Helm charts (planned)
2. [ADR-0008: kind Local Development Environment](adr-0008-kind-local-development-environment.md) ⚪
   - How to set up local Kubernetes environment (planned)
3. [ADR-0007: Cloud-Agnostic Deployment Strategy](adr-0007-cloud-agnostic-deployment-strategy.md) 🔵
   - Overall multi-cloud deployment approach
4. [ADR-0010: Nginx Ingress Controller](adr-0010-nginx-ingress-controller-cloud-agnostic.md) ⚪
   - How HTTP routing works across clouds (planned)

**Current state (as of 2025-11-09):**
- ⚠️ Helm charts not created yet (ADR-0009 planned)
- ⚠️ kind setup not implemented (ADR-0008 planned)
- ✅ Current deployment: Use `dapr run` locally per [CLAUDE.md](../../CLAUDE.md)

**Also useful:**
- [Modernization Strategy](../../plan/modernization-strategy.md) - 8-phase roadmap showing when deployment infrastructure will be built

---

### 🎯 For Decision Makers (Understanding Architecture)

**Start here if you're:** Making technology choices, evaluating architecture, understanding strategic direction

**Essential ADRs (read first):**
1. [ADR-0001: .NET 10 LTS Adoption](adr-0001-dotnet10-lts-adoption.md) 🔵
   - Why upgrading from .NET 6.0 to 10.0
2. [ADR-0007: Cloud-Agnostic Deployment Strategy](adr-0007-cloud-agnostic-deployment-strategy.md) 🔵
   - How Dapr enables deployment to any cloud platform
3. [ADR-0002: Cloud-Agnostic Configuration via Dapr](adr-0002-cloud-agnostic-configuration-via-dapr.md) 🟢
   - How secrets management works across Azure, AWS, GCP

**Strategic context:**
- [Modernization Strategy](../../plan/modernization-strategy.md) - Complete 8-phase roadmap
  - Phase 0: Cleanup (completed)
  - Phase 1A: .NET 10 upgrade (not started, blocked by testing strategy)
  - Phase 1B: Polyglot migration (.NET → Go/Python/Node.js)
  - Phases 2-8: Infrastructure modernization

**Current status:**
- See [CLAUDE.md: Current Development Status](../../CLAUDE.md#current-development-status)
- **Actual state:** All services .NET 6.0 with Dapr 1.5.0
- **Target state:** Polyglot architecture (.NET/Go/Python/Node.js) with Dapr 1.16

---

## Related Documentation

This ADR hub connects to other key documentation:

| Document | Purpose | When to Use |
|----------|---------|-------------|
| **[CLAUDE.md](../../CLAUDE.md)** | Development guide, current status, common commands | First stop for developers, shows actual vs planned state |
| **[Web API Standards](../standards/web-api-standards.md)** | HTTP API implementation conventions | Implementing REST endpoints, CORS, error handling |
| **[Modernization Strategy](../../plan/modernization-strategy.md)** | 8-phase transformation roadmap | Understanding project timeline and dependencies |
| **[Testing Strategy](../../plan/testing-validation-strategy.md)** | Testing baseline and validation approach | Setting up tests (prerequisite for Phase 1A) |
| **[Documentation Improvement Plan](../../plan/documentation-structure-improvement-plan.md)** | How this hub was created | Understanding documentation structure decisions |

**Navigation tips:**
- 📋 Need current status? → [CLAUDE.md](../../CLAUDE.md)
- 🏗️ Need architectural decision? → You're here (ADR hub)
- 📐 Need API implementation guide? → [Web API Standards](../standards/web-api-standards.md)
- 🗓️ Need project timeline? → [Modernization Strategy](../../plan/modernization-strategy.md)

---

## Maintaining This Hub

### When to Update This Document

✅ **Always update when:**
- Creating a new ADR (add to category index above)
- Changing ADR implementation status (update status icon 🟢🟡🔵⚪)
- Adding new configuration layer (update decision tree)
- Completing a modernization phase (update status in ADR descriptions)

✅ **Review quarterly:**
- Verify all status icons reflect actual implementation state
- Check that cross-reference links are valid
- Ensure role-based reading guides match team structure

### ADR Lifecycle

```
Proposed → Accepted → Implemented
              ↓
         Superseded (if replaced by newer ADR)
              or
         Deprecated (if no longer relevant)
```

**Template:** Use `adr-template.md` when creating new ADRs

**Numbering:** Next ADR will be ADR-0012 (zero-padded, 4 digits)

### Cross-Reference Validation

Run this check monthly to ensure no orphaned ADRs:

```bash
# Check for ADRs not referenced in README.md
cd docs/adr
for adr in adr-*.md; do
  if ! grep -q "$adr" README.md; then
    echo "⚠️  Orphaned ADR: $adr"
  fi
done
```

---

**Last Updated:** 2025-11-09
**Document Owner:** Architecture Team
**Questions?** See [CLAUDE.md](../../CLAUDE.md) for development guidance or ask in team channel
