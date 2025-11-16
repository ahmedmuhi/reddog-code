# Architectural Decision Records (ADRs)

> **Navigation hub** for all architectural decisions. Implementation details live in individual ADRs.

## Quick Start

- **New to project?** See [Role-Based Guides](#role-based-reading-guides)
- **Find a decision:** [ADR Index](#adr-index)
- **Adding config/secrets?** [Configuration Decision Tree](#configuration-decision-tree)
- **Check implementation:** Status icons (🟢 Implemented, 🟡 In Progress, 🔵 Accepted, ⚪ Planned)

---

## Current Project Status (2025-11-16)

**Implemented:**
- ✅ .NET 10 LTS - ALL 12 services upgraded (ADR-0001)
- ✅ Dapr Secret Store - secrets.yaml component, GetSecretAsync() in use (ADR-0002)
- ✅ Helm Charts - charts/reddog/ with templates for all services (ADR-0009)
- ✅ UI Stack - Vue 3.5 + Vite 7.2 + ESLint 9 + TypeScript + Day.js + Chart.js 4

**In Progress:**
- 🟡 Secret Management Strategy - Kubernetes Secrets as transport layer (ADR-0013)
- 🟡 OpenTelemetry - Some services adopted, others still Serilog-only (ADR-0011)

**Not Yet Started:**
- ⚪ kind local development environment (ADR-0008)
- ⚪ Dapr Configuration API for business rules (ADR-0004)
- ⚪ Health probe migration to /healthz, /livez, /readyz (ADR-0005)

---

## ADR Index

### Core Platform

| ADR | Decision | Status |
|-----|----------|--------|
| [ADR-0001](adr-0001-dotnet10-lts-adoption.md) | .NET 10 LTS Adoption | 🟢 Implemented |
| [ADR-0002](adr-0002-cloud-agnostic-configuration-via-dapr.md) | Cloud-Agnostic Config (Dapr Secret Store) | 🟢 Implemented |
| [ADR-0003](adr-0003-ubuntu-2404-base-image-standardization.md) | Ubuntu 24.04 Base Images | 🔵 Accepted |

### Configuration & Secrets

| ADR | Decision | Status |
|-----|----------|--------|
| [ADR-0002](adr-0002-cloud-agnostic-configuration-via-dapr.md) | Secrets via Dapr Secret Store | 🟢 Implemented |
| [ADR-0004](adr-0004-dapr-configuration-api-standardization.md) | Business Rules via Dapr Config API | ⚪ Planned |
| [ADR-0006](adr-0006-infrastructure-configuration-via-environment-variables.md) | Infrastructure via Environment Variables | 🔵 Accepted |
| [ADR-0012](adr-0012-dapr-bindings-object-storage.md) | Object Storage via Dapr Bindings | 🟢 Implemented (local) |
| [ADR-0013](adr-0013-secret-management-strategy.md) | Kubernetes Secrets as Transport Layer | 🟡 In Progress |

### Deployment & Infrastructure

| ADR | Decision | Status |
|-----|----------|--------|
| [ADR-0007](adr-0007-cloud-agnostic-deployment-strategy.md) | Cloud-Agnostic via Containerized Infrastructure | 🔵 Accepted |
| [ADR-0008](adr-0008-kind-local-development-environment.md) | kind for Local Development | ⚪ Planned |
| [ADR-0009](adr-0009-helm-multi-environment-deployment.md) | Helm Multi-Environment Deployment | 🟡 In Progress |
| [ADR-0010](adr-0010-nginx-ingress-controller-cloud-agnostic.md) | Nginx Ingress Controller | ⚪ Planned |

### Operational Standards

| ADR | Decision | Status |
|-----|----------|--------|
| [ADR-0005](adr-0005-kubernetes-health-probe-standardization.md) | Kubernetes Health Probes (/healthz, /livez, /readyz) | 🔵 Accepted |
| [ADR-0011](adr-0011-opentelemetry-observability-standard.md) | OpenTelemetry for Logging/Tracing/Metrics | 🟡 In Progress |

---

## Configuration Decision Tree

**Where should I put this setting?**

```
Is it a SECRET? (password, API key, connection string)
  → Dapr Secret Store (ADR-0002) 🟢
    Access: DaprClient.GetSecretAsync("reddog.secrets", "key-name")

Is it CLOUD-SPECIFIC? (region, cluster endpoint, storage account)
  → Helm Values (ADR-0009) 🟡
    Files: values-local.yaml, values-azure.yaml, values-eks.yaml

Is it a RUNTIME ADDRESS? (port, URL, environment mode)
  → Environment Variable (ADR-0006) 🔵
    Access: Environment.GetEnvironmentVariable("SERVICE_PORT")

Is it a BUSINESS RULE? (retry count, timeout, feature flag)
  → Dapr Configuration API (ADR-0004) ⚪ NOT IMPLEMENTED
    Current workaround: appsettings.json or environment variables
```

---

## Role-Based Reading Guides

### For Developers

**Building microservices? Read these:**
1. [ADR-0002](adr-0002-cloud-agnostic-configuration-via-dapr.md) - Access secrets via DaprClient 🟢
2. [ADR-0006](adr-0006-infrastructure-configuration-via-environment-variables.md) - Read ports/URLs from env vars 🔵
3. [ADR-0005](adr-0005-kubernetes-health-probe-standardization.md) - Implement health endpoints 🔵
4. [ADR-0011](adr-0011-opentelemetry-observability-standard.md) - Add observability 🟡

**Also:** [Web API Standards](../standards/web-api-standards.md), [CLAUDE.md](../../CLAUDE.md)

### For Platform Operators

**Deploying infrastructure? Read these:**
1. [ADR-0009](adr-0009-helm-multi-environment-deployment.md) - Helm charts in charts/reddog/ 🟡
2. [ADR-0008](adr-0008-kind-local-development-environment.md) - Local Kubernetes setup ⚪
3. [ADR-0007](adr-0007-cloud-agnostic-deployment-strategy.md) - Multi-cloud strategy 🔵
4. [ADR-0010](adr-0010-nginx-ingress-controller-cloud-agnostic.md) - Ingress routing ⚪

**Also:** [Modernization Strategy](../../plan/modernization-strategy.md)

### For Decision Makers

**Evaluating architecture? Read these:**
1. [ADR-0001](adr-0001-dotnet10-lts-adoption.md) - .NET 10 adoption rationale 🟢
2. [ADR-0007](adr-0007-cloud-agnostic-deployment-strategy.md) - Cloud-agnostic approach 🔵
3. [ADR-0002](adr-0002-cloud-agnostic-configuration-via-dapr.md) - Dapr for portability 🟢

**Also:** [Modernization Strategy](../../plan/modernization-strategy.md)

---

## Related Documentation

| Document | Purpose |
|----------|---------|
| [CLAUDE.md](../../CLAUDE.md) | Development guide, current status, commands |
| [Web API Standards](../standards/web-api-standards.md) | HTTP API conventions, CORS, errors |
| [Modernization Strategy](../../plan/modernization-strategy.md) | 8-phase transformation roadmap |
| [Testing Strategy](../../plan/testing-validation-strategy.md) | Testing baseline and validation |

---

## Maintaining This Hub

**Update this README when:**
- Creating a new ADR (add to index table)
- Changing implementation status (update icon: 🟢🟡🔵⚪)
- Completing major milestones (update Current Project Status)

**ADR template:** Use `adr-template.md` for new ADRs
**Next ADR number:** ADR-0014

**Single source of truth:** Implementation details live in individual ADRs, not here.

---

**Last Updated:** 2025-11-16
**Questions?** See [CLAUDE.md](../../CLAUDE.md) or ask in team channel
