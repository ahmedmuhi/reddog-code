---
title: "ADR-0007: Cloud-Agnostic Deployment via Containerized Infrastructure"
status: "Accepted"
date: "2025-11-09"
authors: "Red Dog Modernization Team"
tags: ["architecture", "decision", "cloud-agnostic", "containers", "infrastructure", "portability"]
supersedes: ""
superseded_by: ""
---

# ADR-0007: Cloud-Agnostic Deployment via Containerized Infrastructure

## Status

**Accepted**

### Implementation Note

As of 2025-11-23:

- RabbitMQ and Redis are deployed as containerized services in Kubernetes using Helm charts under `manifests/branch/dependencies/`.
- Application services talk to infrastructure only via Dapr components and standard protocols (AMQP, Redis, SQL) per **ADR-0002**.
- Local development is converging on a `kind`-based Kubernetes cluster per **ADR-0008**; Docker Compose has been removed from the repo.
- The relational database for Red Dog examples is currently provided by a containerized SQL Server instance for demo purposes; production-grade choices and migrations (e.g. PostgreSQL or managed databases) are handled in plans, not in this ADR.

This ADR does not attempt to track day-to-day implementation status beyond this note. Concrete chart versions, image tags, and rollout steps live in the modernization plan and environment-specific deployment docs.

---

## Context

Red Dog’s modernization strategy targets:

- **Multiple platforms:** Azure Kubernetes Service (AKS), Azure Container Apps, AWS Elastic Kubernetes Service (EKS), and Google Kubernetes Engine (GKE).
- **Teaching/demo scenarios:** students and engineers should learn portable, cloud-agnostic patterns rather than cloud-specific services.

Key constraints:

- The **same application code** should run on all target platforms without code changes.
- Infrastructure dependencies must be consumable through **stable protocols and abstractions** (e.g., AMQP, Redis protocol, SQL, Dapr components), not through cloud-specific SDKs.
- Local development must be possible **without any cloud account** and should mirror production behavior as closely as practical.

Typical infrastructure dependencies:

- **Message broker (pub/sub)**
  - Cloud PaaS examples: Azure Service Bus, AWS SQS/SNS, GCP Pub/Sub.
  - Containerized example: RabbitMQ in Kubernetes.
- **State/cache**
  - Cloud PaaS examples: Azure Cache for Redis, AWS ElastiCache, GCP Memorystore.
  - Containerized example: Redis in Kubernetes.
- **Relational database**
  - Cloud PaaS examples: Azure SQL, AWS RDS, GCP Cloud SQL.
  - Containerized examples: SQL Server, PostgreSQL in Kubernetes.

Problem:

- Relying directly on PaaS services for these dependencies creates:
  - **Vendor lock-in** (different APIs, auth models, and semantics per cloud).
  - **Local development friction** (emulators or cloud connectivity are required).
  - **Teaching complexity** (“on Azure do X, on AWS do Y, on GCP do Z”).
- Red Dog needs a **single architectural story**: the app runs on “any Kubernetes” with the same code and the same Dapr abstraction layer.

Historical context:

- In May 2021 (`3d91853`), Red Dog migrated from `pubsub.azure.servicebus` to `pubsub.rabbitmq` specifically to validate a cloud-agnostic deployment based on containerized infrastructure.
- ADR-0002 later standardized on Dapr as the abstraction layer for secrets, state, pub/sub, and bindings. This ADR defines **where** those backing services run in each environment.

---

## Decision

Adopt a **container-first infrastructure model for local, dev, and test** environments, while allowing **pluggable PaaS or containerized infrastructure in production** behind the same Dapr and protocol abstractions.

### Environment Strategy

1. **Local / Dev / Test (non-production)**

   - Run infrastructure dependencies **inside the Kubernetes cluster** as containers:
     - Message broker (e.g., RabbitMQ).
     - State/cache (e.g., Redis).
     - Relational database (e.g., SQL Server or PostgreSQL) for demo data.
   - Use **Dapr components** pointing at in-cluster services (e.g., `rabbitmq.<ns>.svc.cluster.local`, `redis.<ns>.svc.cluster.local`).
   - Local development uses a **kind** cluster (see ADR-0008) with the same manifests/Helm charts as shared non-prod clusters where possible.

2. **Production / Higher Environments**

   - Red Dog examples and teaching material must support **both**:
     - Containerized infrastructure (RabbitMQ, Redis, database in Kubernetes), and
     - Cloud PaaS equivalents (e.g., Service Bus, managed Redis, managed databases),
   - **Application code remains unchanged**; only:
     - Dapr components, connection strings, or Helm values change, and
     - Infrastructure endpoints switch between in-cluster services and managed PaaS endpoints.

3. **Abstraction boundary**

   - Application services **never bind directly** to PaaS SDKs for infrastructure concerns.
   - All infrastructure access goes through:
     - **Dapr components** (pub/sub, state, bindings, secret stores) per ADR-0002, or
     - **Standard protocols** (SQL, AMQP, Redis) via connection strings managed as configuration.
   - This ADR governs *where* those endpoints live (containers vs managed PaaS), not the higher-level HTTP or domain contracts (covered elsewhere).

---

## Scope

In scope for this ADR:

- Containerized **infrastructure dependencies**:
  - Message brokers (e.g., RabbitMQ).
  - State/cache engines (e.g., Redis).
  - Relational databases used by Red Dog samples (SQL Server, PostgreSQL).
- Their deployment model across **local/dev/test** vs **production/higher** environments.

Out of scope (covered by other ADRs):

- **Application containers** and base OS images (see ADR-0003).
- **Object/blob storage** (see ADR-0012 for Dapr bindings strategy).
- **Secret sourcing and consumption** (see ADR-0013 for secret management strategy).
- **Observability stack** (see ADR-0011 for OpenTelemetry).

---

## Rationale

1. **PORT-001 – Multi-cloud portability**

   - The same infrastructure containers (RabbitMQ, Redis, DB) and Kubernetes manifests can be deployed to AKS, EKS, GKE, and on-prem clusters.
   - PaaS adoption becomes an **environment choice**, not an application code fork.

2. **PORT-002 – Protocol-centric design**

   - Dependencies expose **standard, cloud-neutral protocols** (AMQP, Redis protocol, SQL).
   - Dapr components and connection strings re-point those protocols to infrastructure that may be self-hosted or managed, without changing business logic.

3. **PORT-003 – Single local story**

   - Developers use the same “everything in Kubernetes” mental model locally.
   - No need for cloud accounts, emulators, or special-case local code paths.

4. **PORT-004 – Alignment with Dapr abstraction (ADR-0002)**

   - Container-first infrastructure and PaaS alternatives are both hidden behind Dapr components.
   - Only component YAML and Helm values change between environments.

5. **PORT-005 – Teaching clarity**

   - Simple message for workshops and courses:
     - “Red Dog runs as-is on any conforming Kubernetes cluster.”
     - “PaaS is an optional optimization, not an architectural dependency.”

---

## Consequences

### Positive

- **POS-001 – Zero code changes across environments**

  - Local/dev/test use containerized infra; production can use containers or PaaS.
  - Application services and Dapr client usage stay identical.

- **POS-002 – Kubernetes-native architecture**

  - All non-prod environments share the same core stack: services + infra as pods/StatefulSets.
  - This encourages learning standard Kubernetes primitives instead of cloud-specific services.

- **POS-003 – Strong local development story**

  - Developers can run the full stack on a laptop (kind + infra containers).
  - “Works on my machine” issues are reduced because local matches non-prod.

- **POS-004 – Demonstrable multi-cloud pattern**

  - The same manifests and images can be pointed at AKS, EKS, or GKE to demonstrate portability.
  - PaaS services can be introduced later without rewriting the app.

- **POS-005 – Clear separation of concerns**

  - This ADR owns *where infra runs*.
  - ADR-0002 owns *how app talks to infra*.
  - ADR-0003/0008/0009 own *how things are built and deployed*.

### Negative

- **NEG-001 – Operational overhead for containers**

  - Self-hosted RabbitMQ/Redis/DB in Kubernetes require:
    - Backups, upgrades, capacity planning, and security hardening.
  - For real production systems, teams may prefer PaaS despite the portability cost.

- **NEG-002 – StatefulSet complexity**

  - Stateful workloads need persistent volumes, headless services, and careful restart/upgrade policies.
  - This adds cognitive and operational load compared to “use managed Redis/SQL.”

- **NEG-003 – Licensing considerations**

  - SQL Server Developer Edition is fine for dev/test but not for production.
  - Real production deployments must either:
    - Pay for SQL Server licensing, or
    - Prefer open-source alternatives (e.g., PostgreSQL) when appropriate.

- **NEG-004 – Scaling and reliability trade-offs**

  - PaaS offerings often provide autoscaling, multi-AZ, and SLAs out of the box.
  - Containerized infra matches features only with additional engineering effort.

---

## Alternatives Considered

### ALT-001 – Cloud-specific PaaS everywhere

- **Description:** Use Service Bus, managed Redis, managed SQL on Azure; SQS/ElastiCache/RDS on AWS; Pub/Sub/Memorystore/Cloud SQL on GCP.
- **Rejected because:**
  - Violates the core teaching and architecture goal of **cloud-agnostic deployment**.
  - Complicates local development and multi-cloud demos.
  - Forces cloud-specific concepts into the application architecture.

### ALT-002 – Hybrid “PaaS in prod, containers only for local”

- **Description:** Containers only for local, but all “real” environments use PaaS.
- **Rejected because:**
  - Creates a persistent gap between local and non-prod/prod behavior.
  - Increases the risk of differences emerging between emulator/containers and real PaaS services.
  - Weakens the “any Kubernetes cluster” narrative for students.

### ALT-003 – Emulators for local, PaaS in all clusters

- **Description:** Use Azurite, LocalStack, or other emulators locally; always use PaaS in cloud environments.
- **Rejected because:**
  - Emulators often lag or differ from real services (feature gaps, behavior differences).
  - Still locks the architecture to a single cloud’s mental model and APIs.

---

## Implementation Notes (High-Level)

- Infrastructure Helm charts and manifests live under `manifests/branch/dependencies/` (and any future Helm chart directories) and are treated as **infra code**, not app code.
- Image and OS choices for infrastructure containers should:
  - Prefer **official vendor images** (RabbitMQ, Redis, database vendors).
  - Prefer Debian/Ubuntu-based images where practical, to align with ADR-0003.
- Concrete:
  - Chart versions, image tags, resource requests/limits,
  - Backup and scaling configurations,
  - Production vs demo database choices
  are defined in:
  - `plan/modernization-strategy.md`,
  - environment-specific deployment docs (e.g., `docs/deploy-aks.md`, `docs/deploy-eks.md`).

---

## References

- **REF-001:** ADR-0001 – .NET 10 LTS Adoption  
- **REF-002:** ADR-0002 – Cloud-Agnostic Configuration via Dapr  
- **REF-003:** ADR-0003 – Ubuntu 24.04 Noble Numbat Base Images  
- **REF-004:** ADR-0008 – kind Local Development Environment  
- **REF-005:** ADR-0009 – Helm-Based Multi-Environment Deployment Strategy  
- **REF-006:** Git commit `3d91853` – Migration from Azure Service Bus to RabbitMQ for portability  
- **REF-007:** `plan/modernization-strategy.md` – runtime and infra upgrade sequencing  
