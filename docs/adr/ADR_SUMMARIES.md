# Architecture Decision Record (ADR) Summaries

This document provides a consolidated summary of all Architecture Decision Records (ADRs) for the Red Dog project. It serves as a quick reference for architectural decisions, standards, and patterns.

## Summary Table

| ADR | Title | Status | Date |
|:---:|---|---|---|
| [0001](#adr-0001-net-10-lts-adoption) | .NET 10 LTS Adoption | Accepted | 2025-11-02 |
| [0002](#adr-0002-cloud-agnostic-configuration-via-dapr) | Cloud-Agnostic Configuration via Dapr | Accepted | 2025-11-02 |
| [0003](#adr-0003-ubuntu-2404-noble-numbat-base-images) | Ubuntu 24.04 Noble Numbat Base Images | Accepted | 2025-11-02 |
| [0004](#adr-0004-dapr-configuration-api-standardization) | Dapr Configuration API Standardization | Accepted | 2025-11-03 |
| [0005](#adr-0005-kubernetes-health-probe-standardization) | Kubernetes Health Probe Standardization | Accepted | 2025-11-04 |
| [0006](#adr-0006-infrastructure-configuration-via-environment-variables) | Infrastructure Configuration via Environment Variables | Accepted | 2025-11-04 |
| [0007](#adr-0007-cloud-agnostic-deployment-strategy) | Cloud-Agnostic Deployment Strategy | Accepted | 2025-11-08 |
| [0008](#adr-0008-kind-local-development-environment) | kind Local Development Environment | Accepted | 2025-11-08 |
| [0009](#adr-0009-helm-based-multi-environment-deployment-strategy) | Helm-Based Multi-Environment Deployment Strategy | Accepted | 2025-11-09 |
| [0010](#adr-0010-nginx-ingress-controller-for-cloud-agnostic-traffic-routing) | Nginx Ingress Controller for Cloud-Agnostic Traffic Routing | Implemented | 2025-11-09 |
| [0011](#adr-0011-opentelemetry-observability-standard-logs-traces-metrics) | OpenTelemetry Observability Standard | Accepted | 2025-11-09 |
| [0012](#adr-0012-dapr-bindings-for-object-storage) | Dapr Bindings for Object Storage | Implemented | 2025-11-11 |
| [0013](#adr-0013-secret-management-strategy) | Secret Management Strategy | Accepted | 2025-11-14 |

---

## ADR-0001: .NET 10 LTS Adoption

**Status:** Accepted  
**Date:** 2025-11-02

### Decision
Standardize on **.NET 10 (LTS)** for all backend microservices to leverage long-term support, performance improvements, and C# 14 features. This replaces the previous mix of .NET 6/8 versions.

### Key Technical Details
- **Target Framework:** `net10.0`
- **Language Version:** C# 14
- **SDK Version:** 10.0.100
- **Migration Path:** Upgrade `.csproj` files, update Dockerfiles, and refactor deprecated APIs.

### Implications
- **Positive:** Unified toolchain, performance gains (PGO), access to latest cloud-native libraries.
- **Negative:** Requires immediate code changes and CI/CD updates; potential breaking changes from older versions.

---

## ADR-0002: Cloud-Agnostic Configuration via Dapr

**Status:** Accepted  
**Date:** 2025-11-02

### Decision
Use **Dapr (Distributed Application Runtime)** as the abstraction layer for all state management, pub/sub messaging, and secret management. This enables the application code to remain identical across Local (Redis), Azure (CosmosDB/Service Bus), AWS (DynamoDB/SNS+SQS), and GCP (Firestore/PubSub) environments.

### Key Technical Details
- **State Management:** `statestore` component (Redis locally, cloud-native in prod).
- **Pub/Sub:** `pubsub` component (Redis locally, cloud-native in prod).
- **Secrets:** `secretstore` component.
- **Pattern:** Code depends on Dapr APIs, not specific SDKs (e.g., no Azure SDK in business logic).

### Implications
- **Positive:** True "write once, run anywhere" portability; zero code changes to switch clouds.
- **Negative:** Dependency on Dapr sidecar; slight latency overhead; debugging complexity shifts to Dapr configuration.

---

## ADR-0003: Ubuntu 24.04 Noble Numbat Base Images

**Status:** Accepted  
**Date:** 2025-11-02

### Decision
Standardize on **Ubuntu 24.04 LTS (Noble Numbat)** as the base container image for all services. Specifically, use Microsoft's Chiseled Ubuntu images where possible for security and size optimization.

### Key Technical Details
- **Base Image:** `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` (for production).
- **Dev Image:** `mcr.microsoft.com/dotnet/sdk:10.0-noble` (for build/dev).
- **Architecture:** Multi-arch support (AMD64/ARM64).

### Implications
- **Positive:** Reduced attack surface (chiseled images), long-term security updates (until 2029), consistent OS environment.
- **Negative:** Chiseled images lack shell/debugging tools, requiring different images for troubleshooting.

---

## ADR-0004: Dapr Configuration API Standardization

**Status:** Accepted  
**Date:** 2025-11-03

### Decision
Adopt the **Dapr Configuration API (Alpha)** for dynamic application configuration (feature flags, business settings), replacing static `appsettings.json` and environment variables for non-infrastructure config.

### Key Technical Details
- **Component:** `configuration.redis` (Local), `configuration.azure.appconfig` (Azure).
- **Usage:** Subscribe to configuration updates in real-time without restarting pods.
- **Scope:** Business logic settings only (not infrastructure connection strings).

### Implications
- **Positive:** Dynamic updates, centralized configuration management, consistent API across environments.
- **Negative:** API is in Alpha (stability risk); requires Dapr 1.16+; adds complexity for simple static settings.

---

## ADR-0005: Kubernetes Health Probe Standardization

**Status:** Accepted  
**Date:** 2025-11-04

### Decision
Implement standardized Kubernetes health probes (`livenessProbe`, `readinessProbe`, `startupProbe`) across all services using the **Microsoft.Extensions.Diagnostics.HealthChecks** library.

*Note: A supplementary document `adr-0005-implementation-notes.md` provides specific C# implementation patterns.*

### Key Technical Details
- **Endpoints:**
  - `/healthz` (Liveness): Checks if process is running.
  - `/readyz` (Readiness): Checks dependencies (Dapr, DB).
  - `/livez` (Startup): Initial startup check.
- **Library:** `Microsoft.Extensions.Diagnostics.HealthChecks`.
- **Pattern:** Use `HealthCheckService` to aggregate checks.

### Implications
- **Positive:** Consistent self-healing behavior, automated traffic routing (don't send traffic until ready), improved availability.
- **Negative:** Incorrectly configured probes can cause restart loops; readiness checks must be lightweight to avoid cascading failures.

---

## ADR-0006: Infrastructure Configuration via Environment Variables

**Status:** Accepted  
**Date:** 2025-11-04

### Decision
Use **Environment Variables** exclusively for infrastructure-level configuration (ports, hostnames, connection strings) that must be set at deployment time. This separates "infrastructure config" (Env Vars) from "application config" (Dapr Config API).

### Key Technical Details
- **Format:** `SCREAMING_SNAKE_CASE` (e.g., `SERVICE_PORT`, `REDIS_HOST`).
- **Source:** Kubernetes ConfigMaps and Secrets.
- **Prohibited:** Hardcoded values in `appsettings.json` for infrastructure pointers.

### Implications
- **Positive:** Follows 12-Factor App methodology; compatible with all container orchestrators; clear separation of concerns.
- **Negative:** Managing large numbers of env vars can be cumbersome without tooling (Helm).

---

## ADR-0007: Cloud-Agnostic Deployment Strategy

**Status:** Accepted  
**Date:** 2025-11-08

### Decision
Deploy infrastructure dependencies (RabbitMQ, Redis, SQL Server) as **containers** within the Kubernetes cluster for Local, Dev, and Test environments, while supporting **PaaS** (Azure Service Bus, CosmosDB) for Production via Dapr component swapping.

### Key Technical Details
- **Pattern:** "Container-First" default.
- **Mechanism:** Helm charts deploy infrastructure containers by default.
- **Switching:** Change Dapr component YAML to point to PaaS resources without changing app code.

### Implications
- **Positive:** Consistent developer experience (everything runs in cluster); cost-effective for non-prod; enables offline development.
- **Negative:** Managing stateful workloads (DBs) in Kubernetes requires operational overhead (backups, persistence) if used long-term.

---

## ADR-0008: kind Local Development Environment

**Status:** Accepted  
**Date:** 2025-11-08

### Decision
Adopt **kind (Kubernetes in Docker)** as the standard local development environment, replacing Docker Compose. This ensures the local environment matches the production Kubernetes environment (manifests, networking, Dapr sidecars).

### Key Technical Details
- **Tool:** `kind`
- **Config:** Custom `kind-config.yaml` for port mapping (80/443).
- **Registry:** Local Docker registry for rapid iteration.
- **Workflow:** `tilt` or `skaffold` (implied) for inner loop.

### Implications
- **Positive:** "Production-like" local environment; catches Kubernetes-specific issues early; supports full Dapr sidecar architecture.
- **Negative:** Higher resource usage than Docker Compose; steeper learning curve for developers new to K8s.

---

## ADR-0009: Helm-Based Multi-Environment Deployment Strategy

**Status:** Accepted  
**Date:** 2025-11-09

### Decision
Use **Helm Charts** as the single source of truth for deployment manifests. Environment-specific differences (Local vs. Azure vs. AWS) are managed via `values-{env}.yaml` files, not separate manifest branches.

### Key Technical Details
- **Structure:** `charts/reddog/` (Application), `charts/infrastructure/` (Dependencies).
- **Values Files:** `values-local.yaml`, `values-azure.yaml`, `values-aws.yaml`, `values-gcp.yaml`.
- **Pattern:** Single Chart, Multiple Values.

### Implications
- **Positive:** DRY (Don't Repeat Yourself) principles; easy to diff environments; industry standard packaging.
- **Negative:** Helm templating complexity; need to maintain multiple values files.

---

## ADR-0010: Nginx Ingress Controller for Cloud-Agnostic Traffic Routing

**Status:** Implemented  
**Date:** 2025-11-09  
**Last Updated:** 2025-11-23

### Decision
Use **Nginx Ingress Controller** deployed via Helm wrapper chart across all environments (Local kind, Azure AKS, AWS EKS, GCP GKE) for cloud-agnostic HTTP/HTTPS routing.

### Key Technical Details
- **Chart:** `charts/external/nginx-ingress/` (wraps ingress-nginx 4.14.0)
- **Configuration:** Base values + environment-specific overrides (Azure/AWS/GCP)
- **Application:** `charts/reddog/templates/ingress.yaml` - Red Dog ingress template
- **Routing:** Path-based routing (`/`, `/api/orders`, `/api/makeline`, `/api/accounting`)

### Implications
- **Positive:** Unified config across clouds; 75-80% cost savings (single LB); Kubernetes-native; SSL/TLS support; rich features (rate limiting, auth, CORS)
- **Negative:** HTTP/HTTPS only; misses cloud-native WAF/Shield (acceptable for portability); ~128Mi RAM overhead per replica

### Implementation Files
See `charts/external/nginx-ingress/` and [Setup Guide](../guides/nginx-ingress-setup.md)

---

## ADR-0011: OpenTelemetry Observability Standard

**Status:** Accepted  
**Date:** 2025-11-09

### Decision
Adopt **Native OpenTelemetry (OTLP)** for all logs, traces, and metrics. Replace vendor-specific agents and legacy logging frameworks (Serilog sinks) with direct OTLP export to an OpenTelemetry Collector.

### Key Technical Details
- **Protocol:** OTLP (gRPC/HTTP).
- **Collector:** Central OpenTelemetry Collector deployment.
- **Backends:** Jaeger (Traces), Prometheus (Metrics), Loki (Logs) - swappable.
- **Languages:** .NET, Go, Python, Node.js implementations.

### Implications
- **Positive:** Unified observability pipeline; vendor-neutral; automatic trace correlation across polyglot services.
- **Negative:** Migration effort to replace existing logging code; operational overhead of managing the Collector.

---

## ADR-0012: Dapr Bindings for Object Storage

**Status:** Implemented  
**Date:** 2025-11-11

### Decision
Use **Dapr Output Bindings** to abstract object storage. Use `bindings.localstorage` (with `emptyDir`) for local development and cloud-native bindings (Azure Blob, S3, GCS) for production.

### Key Technical Details
- **Local:** `bindings.localstorage` writing to a volume mount.
- **Prod:** `bindings.azure.blobstorage`, `bindings.aws.s3`, etc.
- **Pattern:** App sends payload to Dapr binding; Dapr handles the write.

### Implications
- **Positive:** Application code doesn't change between local FS and cloud Blob storage; simplified testing.
- **Negative:** Local storage is ephemeral (data loss on restart); different Dapr component configs required per env.

---

## ADR-0013: Secret Management Strategy

**Status:** Accepted  
**Date:** 2025-11-14

### Decision
Implement a **Two-Layer Secret Management Strategy**:
1. **Transport:** All workloads read secrets from Kubernetes `Secret` objects (referenced via Helm).
2. **Source:** Secrets are populated into K8s via Helm `stringData` (Local) or External Secrets Operator/CSI Driver (Cloud).

### Key Technical Details
- **Constraint:** No hardcoded secrets in manifests.
- **Local:** `values-local.yaml` (gitignored) populates secrets.
- **Cloud:** Managed Secret Stores (Key Vault, Secrets Manager) sync to K8s Secrets.

### Implications
- **Positive:** Secure by default; clear separation of secret injection vs. consumption; consistent app interface (env vars/files).
- **Negative:** Setup complexity for External Secrets Operator; developer discipline required for local values files.
