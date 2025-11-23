# Architectural Decision Records (ADRs)

> Navigation hub for all architectural decisions. Individual ADR files hold the full rationale and implementation notes.

---

## Status Legend

- 🟢 **Implemented** – Decision is implemented in the codebase/infrastructure.
- 🟡 **In Progress** – Decision is accepted and partially implemented.
- 🔵 **Accepted** – Decision is agreed and documented, implementation not yet started or only stubbed.
- ⚪ **Planned** – Decision area identified; ADR exists but design/implementation still to be done.

---

## Current Project Status (as of 2025-11-16)

### 🟢 Implemented

- **ADR-0001** – .NET 10 LTS adoption for all current services.  
- **ADR-0002** – Dapr Secret Store in place (secretstore component + `GetSecretAsync` usage in services).  
- **ADR-0006** – Infrastructure/runtime configuration via environment variables is the standard and used in practice (ports, Dapr ports, runtime mode).

### 🟡 In Progress

- **ADR-0009** – Helm multi-environment deployment:  
  - Baseline `charts/reddog/` application charts exist for all services.  
  - Multi-environment values, documentation, and full automation are still being completed.
- **ADR-0010** – Nginx Ingress Controller (cloud-agnostic):  
  - Ingress controller and wrapper chart in place for cloud environments.  
  - Full local (kind) validation and documented workflows still pending.
- **ADR-0011** – OpenTelemetry observability standard:  
  - Some services emit traces/metrics/logs via OTel; others remain Serilog-only.
- **ADR-0012** – Dapr bindings for object storage:  
  - Local: `bindings.localstorage` + `emptyDir` volumes + `dapr.io/volume-mounts-rw` implemented for Receipt Generation.  
  - Cloud blob bindings (Azure Blob, S3, GCS) planned but not yet rolled out.
- **ADR-0013** – Secret management strategy:  
  - Kubernetes Secrets used as the transport layer for many workloads.  
  - Consistent patterns for KEDA, all infra components, and cloud secret-manager integration still being rolled out.

### 🔵 Accepted (not yet implemented)

- **ADR-0003** – Ubuntu 24.04 base image standardization for all application containers.  
- **ADR-0005** – Kubernetes health probe endpoint standardization (`/healthz`, `/livez`, `/readyz`) across services.  
- **ADR-0007** – Cloud-agnostic deployment strategy using containerized infrastructure.

### ⚪ Planned / Not Started

- **ADR-0004** – Dapr Configuration API for business rules and application config (runtime-tunable).  
- **ADR-0008** – kind-based local development environment (standardized setup, scripts, and docs).

---

## ADR Index

### Core Platform

| ADR | Decision | Status |
|-----|----------|--------|
| [ADR-0001](adr-0001-dotnet10-lts-adoption.md) | .NET 10 LTS adoption | 🟢 Implemented |
| [ADR-0002](adr-0002-cloud-agnostic-configuration-via-dapr.md) | Dapr Secret Store for secrets (cloud-agnostic config building block) | 🟢 Implemented |
| [ADR-0003](adr-0003-ubuntu-2404-base-image-standardization.md) | Ubuntu 24.04 base image standardization for application containers | 🔵 Accepted |

### Configuration & Secrets

| ADR | Decision | Status |
|-----|----------|--------|
| [ADR-0002](adr-0002-cloud-agnostic-configuration-via-dapr.md) | Secrets access via Dapr Secret Store (app-facing interface) | 🟢 Implemented |
| [ADR-0004](adr-0004-dapr-configuration-api-standardization.md) | Application config & business rules via Dapr Configuration API | ⚪ Planned |
| [ADR-0006](adr-0006-infrastructure-configuration-via-environment-variables.md) | Infrastructure/runtime config via environment variables | 🟢 Implemented |
| [ADR-0012](adr-0012-dapr-bindings-object-storage.md) | Object storage via Dapr bindings (local + cloud-specific bindings) | 🟡 In Progress |
| [ADR-0013](adr-0013-secret-management-strategy.md) | Secret management strategy (Kubernetes/Container Apps Secrets as transport layer) | 🟡 In Progress |

### Deployment & Infrastructure

| ADR | Decision | Status |
|-----|----------|--------|
| [ADR-0007](adr-0007-cloud-agnostic-deployment-strategy.md) | Cloud-agnostic deployment strategy (containerized infra, multi-cloud) | 🔵 Accepted |
| [ADR-0008](adr-0008-kind-local-development-environment.md) | kind-based local Kubernetes development environment | ⚪ Planned |
| [ADR-0009](adr-0009-helm-multi-environment-deployment.md) | Helm multi-environment deployment (values per environment) | 🟡 In Progress |
| [ADR-0010](adr-0010-nginx-ingress-controller-cloud-agnostic.md) | Nginx Ingress controller as cloud-agnostic ingress layer | 🟡 In Progress |

### Operational Standards

| ADR | Decision | Status |
|-----|----------|--------|
| [ADR-0005](adr-0005-kubernetes-health-probe-standardization.md) | Kubernetes health probes (`/healthz`, `/livez`, `/readyz`) | 🔵 Accepted |
| [ADR-0011](adr-0011-opentelemetry-observability-standard.md) | OpenTelemetry standard for logging, tracing, and metrics | 🟡 In Progress |

---

## Configuration Decision Tree

**Question:** “Where should this setting live?”

```text
Is it a SECRET?
(password, API key, connection string, credential)

  → Source & Transport: Kubernetes/Container Apps Secrets (ADR-0013)
      - Local: Helm creates Secrets from gitignored values-local.yaml
      - Cloud: External Secrets / CSI / cloud secret managers hydrate Secrets
  → App Access: Dapr Secret Store (ADR-0002)
      - Example: DaprClient.GetSecretAsync("<secret-store-name>", "KEY_NAME")
````

```text
Is it a RUNTIME / INFRASTRUCTURE SETTING?
(port, bind address, Dapr HTTP/GRPC port, runtime environment, log level)

  → Environment variables (ADR-0006)
      - Examples:
          ASPNETCORE_URLS
          DAPR_HTTP_PORT / DAPR_GRPC_PORT
          NODE_ENV / ASPNETCORE_ENVIRONMENT
```

```text
Is it a CLOUD-SPECIFIC OR DEPLOYMENT DETAIL?
(region, storage account name, cluster endpoint, base URLs per environment)

  → Helm values + deployment configuration (ADR-0007, ADR-0009)
      - Files:
          values-local.yaml
          values-azure.yaml
          values-eks.yaml
          values-gke.yaml
      - Passed into Kubernetes/Container Apps manifests via Helm templates.
      - If a cloud-specific value is also a SECRET, follow the “SECRET” branch instead.
```

```text
Is it an APPLICATION-LEVEL BUSINESS RULE OR FEATURE FLAG?
(retry counts, timeouts, max order size, feature toggles, A/B flags)

  → Target state: Dapr Configuration API (ADR-0004)  ⚪ Planned
      - Intended:
          - Centralized config stores (Redis / App Configuration / PostgreSQL)
          - Runtime updates via SubscribeConfiguration

  → Current interim workaround (until ADR-0004 is implemented):
      - Prefer configuration files (e.g., appsettings.json / language-idiomatic config),
      - Avoid adding new business rules as environment variables unless there is no alternative,
      - Keep a clear distinction: env vars for infrastructure only (ADR-0006).
```

If in doubt:

* For credentials → **Secrets (ADR-0013) + Dapr Secret Store (ADR-0002)**.
* For ports/URLs/mode → **Environment variables (ADR-0006)**.
* For business rules → **Plan for Dapr Config API (ADR-0004)** and use a temporary file-based config.

---

## Role-Based Reading Guides

### For Application Developers

**Start with:**

1. **Secrets & configuration**

   * [ADR-0002](adr-0002-cloud-agnostic-configuration-via-dapr.md) – How to access secrets via Dapr Secret Store. 🟢
   * [ADR-0006](adr-0006-infrastructure-configuration-via-environment-variables.md) – How to read ports, URLs, env modes. 🟢
   * [ADR-0004](adr-0004-dapr-configuration-api-standardization.md) – Future model for business rules via Dapr Config API. ⚪

2. **Operational patterns**

   * [ADR-0005](adr-0005-kubernetes-health-probe-standardization.md) – Implementing `/healthz`, `/livez`, `/readyz`. 🔵
   * [ADR-0011](adr-0011-opentelemetry-observability-standard.md) – Logging/tracing/metrics expectations. 🟡

3. **Platform assumptions**

   * [ADR-0001](adr-0001-dotnet10-lts-adoption.md) – .NET 10 LTS baseline. 🟢
   * [ADR-0003](adr-0003-ubuntu-2404-base-image-standardization.md) – Runtime OS assumptions for containers. 🔵

**Also see:**

* [Web API Standards](../standards/web-api-standards.md)
* [CLAUDE.md](../../CLAUDE.md) for dev workflow, commands, and AI-agent guidance.

---

### For Platform / DevOps / SRE

**Start with:**

1. **Deployment model**

   * [ADR-0007](adr-0007-cloud-agnostic-deployment-strategy.md) – Cloud-agnostic stack & infra containers. 🔵
   * [ADR-0009](adr-0009-helm-multi-environment-deployment.md) – Helm structure, values, and environments. 🟡
   * [ADR-0008](adr-0008-kind-local-development-environment.md) – Standard local cluster approach. ⚪

2. **Ingress & networking**

   * [ADR-0010](adr-0010-nginx-ingress-controller-cloud-agnostic.md) – Nginx Ingress controller choice and patterns. 🟡

3. **Secrets & storage**

   * [ADR-0013](adr-0013-secret-management-strategy.md) – Secret sourcing, Kubernetes Secrets, and integration with cloud secret managers. 🟡
   * [ADR-0012](adr-0012-dapr-bindings-object-storage.md) – Object storage bindings (local vs cloud). 🟡

**Also see:**

* [Modernization Strategy](../../plan/modernization-strategy.md)
* Platform-specific implementation plans (under `plan/`).

---

### For Architects / Decision Makers

**Start with:**

1. **High-level platform choices**

   * [ADR-0001](adr-0001-dotnet10-lts-adoption.md) – Rationale for .NET 10 LTS. 🟢
   * [ADR-0003](adr-0003-ubuntu-2404-base-image-standardization.md) – Ubuntu 24.04 as the base OS. 🔵
   * [ADR-0007](adr-0007-cloud-agnostic-deployment-strategy.md) – Multi-cloud and containerized infra strategy. 🔵

2. **Configuration and portability**

   * [ADR-0002](adr-0002-cloud-agnostic-configuration-via-dapr.md) – Dapr as the abstraction layer for config/secrets. 🟢
   * [ADR-0004](adr-0004-dapr-configuration-api-standardization.md) – Planned runtime configuration strategy. ⚪
   * [ADR-0013](adr-0013-secret-management-strategy.md) – Secret management model. 🟡

**Also see:**

* [Modernization Strategy](../../plan/modernization-strategy.md) for phase-by-phase roadmap.

---

## Related Documentation

| Document                                                                   | Purpose                                                             |
| -------------------------------------------------------------------------- | ------------------------------------------------------------------- |
| [CLAUDE.md](../../CLAUDE.md)                                               | Development guide, operational modes, commands, and current status. |
| [Web API Standards](../standards/web-api-standards.md)                     | HTTP API conventions, versioning, CORS, and error handling.         |
| [Modernization Strategy](../../plan/modernization-strategy.md)             | 8-phase modernization roadmap and milestones.                       |
| [Testing & Validation Strategy](../../plan/testing-validation-strategy.md) | Testing expectations and validation stages.                         |

---

## Maintaining This Hub

Update this README when:

* A new ADR is created (add it to the appropriate index table, with status).

* An ADR changes status (update icons and the “Current Project Status” section).

* Major milestones are reached (update the summary at the top).

* **ADR template:** `adr-template.md`

* **Next ADR number:** **ADR-0014** (update this when you add a new ADR).

* **Single source of truth:** The detailed rationale and implementation notes live in each ADR file; this README is an index and status overview.

---

**Last Updated:** 2025-11-16
