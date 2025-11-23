# Architectural Decision Records (ADRs)

> Navigation hub for all architectural decisions. Individual ADR files hold the full rationale and implementation notes.

---

## Status Legend

- 🟢 **Implemented** – Decision is implemented in the codebase/infrastructure.
- 🟡 **In Progress** – Decision is accepted and partially implemented.
- 🔵 **Accepted** – Decision is agreed and documented, implementation not yet started or only stubbed.
- ⚪ **Planned** – Decision area identified; ADR exists but design/implementation still to be done.

---

## Foundational Abstraction & Platform

### [ADR-0002](adr-0002-cloud-agnostic-configuration-via-dapr.md) – Dapr Abstraction 🟢
* **Decision:** Adopt Dapr as the abstraction layer.
* **Scope:** Secrets (Secret Store), State (State Store), Pub/Sub messaging, and Output Bindings.
* **Rationale:** Zero code changes across platforms, universal sidecar architecture, and vendor independence.

### [ADR-0007](adr-0007-cloud-agnostic-deployment-strategy.md) – Cloud-Agnostic Deployment 🔵
* **Decision:** Container-first infrastructure model with a per-environment strategy.
* **Strategy:**
  * **Local:** Infrastructure runs inside Kubernetes (e.g., RabbitMQ, Redis containers).
  * **Production:** Supports both containers and cloud PaaS services (e.g., Cosmos DB, DynamoDB).
* **Abstraction Boundary:** Dapr or standard protocols (SQL, AMQP, Redis).

### [ADR-0003](adr-0003-ubuntu-2404-base-image-standardization.md) – Base Image Standard 🔵
* **Decision:** All application containers must use Ubuntu 24.04 LTS.
* **Rationale:** Alignment with .NET 10 defaults and simplified security compliance (single operating system family).

---

## Configuration & Secret Management

### [ADR-0013](adr-0013-secret-management-strategy.md) – Secret Management Strategy 🟡
* **Consumption Layer:** Application services must use Dapr Secret API; Infrastructure/Platform must use native secret object store (e.g., Kubernetes Secrets).
* **Transport Layer:** Kubernetes Secret as the canonical transport object.
* **Source Layer:** Managed secret store in the cloud or gitignored values locally.

### [ADR-0006](adr-0006-infrastructure-configuration-via-environment-variables.md) – Infrastructure Configuration 🟢
* **Decision:** Use environment variables for infrastructure and runtime configuration.
* **Scope:** Listening ports, Dapr HTTP port, runtime mode (e.g., `NODE_ENV`).
* **Out of Scope:** Business rules and secrets.

### [ADR-0004](adr-0004-dapr-configuration-api-standardization.md) – Application Configuration ⚪
* **Decision:** Standardize on the Dapr Configuration API for application configuration.
* **Types:** Business rules (e.g., maximum order size, feature flags).
* **Benefits:** Enables runtime configuration updates (subscription-based).

---

## Deployment & Local Development

### [ADR-0009](adr-0009-helm-multi-environment-deployment.md) – Helm Multi-Environment Deployment 🟡
* **Decision:** Use Helm charts with environment-specific values.
* **Goal:** Single source of truth (chart templates + value files).
* **Target:** kind, Azure Container Apps, AKS, EKS, or GKE.
* **Status:** Migration from raw manifests in progress.

### [ADR-0008](adr-0008-kind-local-development-environment.md) – Local Development with kind ⚪
* **Decision:** Adopt kind (Kubernetes in Docker) as the standard local development environment.
* **Rationale:** Production parity (same Kubernetes primitives, same Dapr components), offline development, and cost control.

### [ADR-0010](adr-0010-nginx-ingress-controller-cloud-agnostic.md) – Nginx Ingress Controller 🟡
* **Decision:** Adopt Nginx Ingress Controller as the standard HTTP/HTTPS entry point.
* **Rationale:** Cloud-agnostic traffic routing, same Ingress template across clouds, and path-based routing (single load balancer).

---

## Operational Standards

### [ADR-0011](adr-0011-opentelemetry-observability-standard.md) – OpenTelemetry Observability 🟡
* **Decision:** Adopt OpenTelemetry (OTEL/OTLP) for all logs, traces, and metrics.
* **Principles:** Native OTEL used in app code, OTEL Collector handles fan-out and backend vendor portability, trace-first design.

### [ADR-0005](adr-0005-kubernetes-health-probe-standardization.md) – Health Probe Standardization 🔵
* **Decision:** Standardize on three HTTP health endpoints:
  * `/healthz`: Startup (basic process health).
  * `/livez`: Liveness (not deadlocked).
  * `/readyz`: Readiness (dependencies okay).
* **Rationale:** Improved reliability; orchestrator can detect restart traffic or remove from load balancer.

### [ADR-0012](adr-0012-dapr-bindings-object-storage.md) – Object Storage Bindings 🟡
* **Decision:** Use Dapr output bindings for object storage access.
* **Implementation:**
  * **Local:** Ephemeral local storage binding.
  * **Cloud:** Cloud-native blob storage (Azure Blob, S3, GCS) via respective Dapr component.

---

## Runtime Baseline

### [ADR-0001](adr-0001-dotnet10-lts-adoption.md) – .NET 10 LTS Adoption 🟢
* **Decision:** Adopt .NET 10 LTS for Order Service and future .NET services.
* **Rationale:** Extended support horizon (to Nov 2028), avoids migration from .NET 8/9, and performance/runtime improvements.
* **Status:** Target frameworks `.net10.0` for all csproj files; SDK pinned in `global.json`.

---

## Related Documentation

| Document                                                                   | Purpose                                                             |
| -------------------------------------------------------------------------- | ------------------------------------------------------------------- |
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
