---
id: KI-INFRA_CONTAINERS_VS_PAAS-001
title: Infrastructure Containers vs PaaS Decision Policy
tags:
  - red-dog
  - infrastructure
  - cloud-agnostic
  - containers
  - paas
last_updated: 2025-11-23
source_sessions: []
source_plans: []
confidence: high
status: Active
owner: red-dog-modernization-team
notes: Encodes the stable policy behind ADR-0007 about when to run infra in containers vs PaaS, and how that interacts with Dapr.
---

# Summary

This Knowledge Item captures the stable policy for choosing between **containerized infrastructure** and **cloud PaaS services** for Red Dog.
It focuses on environment roles (local, non-prod, prod), cloud-agnostic teaching goals, and the invariant that application code talks to infrastructure via standard protocols/Dapr rather than cloud-specific SDKs.

## Key Facts

- **FACT-001**: Red Dog uses a small set of core infrastructure capabilities: message broker (pub/sub), cache/state store, and relational database.
- **FACT-002**: Application code communicates with these dependencies via Dapr components and standard protocols (AMQP, Redis, SQL), not via cloud-specific SDKs, per ADR-0002.
- **FACT-003**: Local development and teaching scenarios require a **fully self-contained environment** that can run without any cloud account.
- **FACT-004**: Production-like deployments may value SLAs, managed backups, and autoscaling offered by PaaS services.
- **FACT-005**: The choice between containers and PaaS MUST NOT change the application code; only Dapr component configuration and connection details change.

## Constraints

- **CON-001:** For **local development and test**, all core infra (message broker, cache/state, relational DB) MUST be runnable as containers inside a Kubernetes cluster (kind or similar) with **no hard dependency on PaaS**.
- **CON-002:** Application code MUST NOT take direct dependencies on cloud-specific infra SDKs (Azure Service Bus SDK, AWS SQS SDK, etc.) for messaging/state in the core Red Dog sample.
- **CON-003:** For production and higher environments, teams MAY choose containerized infra, PaaS infra, or a mix, but:
  - Logical Dapr component names (e.g. `reddog.pubsub`, `reddog.state.makeline`) MUST remain stable.
  - Application contracts and behaviour MUST remain consistent.
- **CON-004:** Any environment that uses PaaS MUST still expose infra to applications via **standard protocols or Dapr components**, not direct SDK coupling baked into business logic.
- **CON-005:** When infra choices differ by environment (e.g. Redis container in non-prod, managed Redis in prod), the difference MUST be captured in manifests/Helm values, not in application code branches or `#if` directives.
- **CON-006:** Teaching and workshop material MUST be demonstrable using only the containerized infra path, so students can reproduce the system with a laptop and a cluster.

## Patterns & Recommendations

- **PAT-001 – Containers as the default for local and non-prod**
  - Use containerized infra (RabbitMQ, Redis, database) for:
    - Local development (kind cluster),
    - Shared dev/test clusters.
  - This maximizes parity and portability while avoiding cloud account friction.

- **PAT-002 – PaaS as an opt-in for production**
  - For production, choose between:
    - Containerized infra (StatefulSets) when you want strict control and portability across clouds, or
    - PaaS offerings (e.g. Azure Cache for Redis, managed RabbitMQ, hosted PostgreSQL) when SLA, scale, or operational simplicity are primary concerns.
  - Either choice MUST preserve the Dapr component boundary and logical names.

- **PAT-003 – Always preserve the Dapr abstraction**
  - Regardless of infra choice:
    - Keep using `DaprClient` for state, pub/sub, bindings.
    - Map Dapr components to either container endpoints (e.g. `rabbitmq.rabbitmq.svc`) or PaaS endpoints (e.g. Azure Service Bus) as needed.
  - New features that need infra integration SHOULD be implemented via Dapr building blocks by default.

- **PAT-004 – One mental model for students**
  - When designing exercises and docs:
    - Explain the architecture once in terms of **Dapr + Kubernetes + containers**.
    - Treat PaaS usage as an advanced variation layered on top (e.g. “swap Redis StatefulSet for managed Redis via a different component configuration”).

- **PAT-005 – Evaluate PaaS vs containers with explicit criteria**
  - When deciding for a specific environment, consider:
    - Required SLA and RPO/RTO,
    - Team’s operational capacity (Kubernetes + backups),
    - Data residency and compliance,
    - Cost profile (node hours vs PaaS pricing),
    - Portability requirements (multi-cloud vs single-cloud).

- **PAT-006 – Keep migration paths one-way simple**
  - Prefer designs where:
    - Moving from containers → PaaS is mostly a Dapr component and connection-string change.
    - Rolling back to containers is still possible if PaaS is unavailable or too costly.

## Risks & Open Questions

### Risks

- **RISK-001**: If production-only features leak into app code via PaaS SDKs, Red Dog ceases to be portable and local dev becomes harder or impossible without that cloud.
- **RISK-002**: Over-committing to self-hosted infra for real client workloads can create operational burden (backups, patching, HA) beyond what the team can support.
- **RISK-003**: Divergent infra choices across environments without clear documentation can cause “it works in dev, fails in prod” situations.

### Open Questions

- **OPEN-001**: For teaching versus real-world client use, what is the default guidance when a client requests PaaS for everything but still wants the Red Dog architecture and Dapr patterns?
- **OPEN-002**: Should there be a formal checklist or scorecard for “when to prefer PaaS vs containers” that can be reused for multiple client engagements?

## Source & Provenance

- Derived from refactoring ADR-0007 (“Cloud-Agnostic Deployment Strategy”) to separate architectural principles from implementation details.
- Related ADRs / external docs:
  - ADR-0001: .NET 10 LTS adoption (runtime baseline for application services).
  - ADR-0002: Cloud-agnostic configuration via Dapr (abstraction boundary).
  - ADR-0007: Cloud-agnostic deployment strategy (containers vs PaaS across environments).
  - Local dev environment and deployment strategy documents under `docs/` and `manifests/`.
