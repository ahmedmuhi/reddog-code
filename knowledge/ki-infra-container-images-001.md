---
id: KI-INFRA_CONTAINER_IMAGES-001
title: Infrastructure Container Image Selection Policy
tags:
  - red-dog
  - infrastructure
  - containers
  - images
  - kubernetes
last_updated: 2025-11-23
source_sessions: []
source_plans: []
confidence: high
status: Active
owner: red-dog-modernization-team
notes: Baseline image selection guidance for RabbitMQ, Redis, SQL Server/PostgreSQL, Nginx, and similar infra containers.
---

# Summary

This Knowledge Item defines how to choose container images for infrastructure dependencies (message brokers, caches, databases, reverse proxies) used by Red Dog and similar projects.
It focuses on stable principles (vendor, OS, architecture, variants) rather than specific tags, so that individual charts and images can evolve without changing the policy.

## Key Facts

- **FACT-001**: Red Dog relies on a small set of core infrastructure containers: RabbitMQ (message broker), Redis (cache/state), relational database (SQL Server or PostgreSQL), and a web proxy (e.g. Nginx).
- **FACT-002**: Image selection for infrastructure has a large impact on performance, security, and operability (e.g. Redis performance differences across OS bases).
- **FACT-003**: Application containers follow their own base-image standard (see ADR-0003); infra containers should align with that standard where practical but may use vendor-recommended bases.
- **FACT-004**: Specific image tags (e.g. `4.1-management`, `7-bookworm`) are **environment-level** or **plan-level** decisions and should not be hard-coded into ADRs or KIs.
- **FACT-005**: Many infra images ship with “management” or “metrics-enabled” variants that are important for Kubernetes observability (e.g. RabbitMQ management plugin, Redis metrics).

## Constraints

- **CON-001**: Infrastructure containers MUST use **official vendor or CNCF-recognised images** (Docker Hub “official images” or vendor registries like MCR/Bitnami), not random community images.
- **CON-002**: Infra images SHOULD be based on **glibc-based Debian/Ubuntu** (e.g. Debian 12 Bookworm, Ubuntu LTS) unless the vendor explicitly recommends something else.
- **CON-003**: Alpine-based images MUST NOT be used for high-throughput infra components (e.g. Redis, RabbitMQ) unless a benchmark or vendor guidance explicitly validates them for the expected workload.
- **CON-004**: Image tags for infra components MUST be pinned in **Helm values / infra manifests**, not left as `latest` and not embedded in ADR text.
- **CON-005**: Multi-architecture support (AMD64 + ARM64) SHOULD be preserved where the official image supports it, so local dev on ARM (e.g. Apple Silicon) and cloud clusters behave consistently.
- **CON-006**: Images used in production MUST have a clear upstream support story (security updates, CVEs) and MUST NOT be abandoned or unmaintained forks.

## Patterns & Recommendations

- **PAT-001 – Prefer official upstream images**
  - Use official vendor images from:
    - Docker Hub “official” repositories (e.g. `rabbitmq`, `redis`, `postgres`, `nginx`),
    - Microsoft Container Registry (`mcr.microsoft.com/mssql/server`), or
    - Vendor-curated Helm charts (e.g. Bitnami) that reference those images.
  - Avoid re-building infra images unless there is a strong, documented reason (e.g. custom CA bundles).

- **PAT-002 – Align infra OS with app OS where sensible**
  - Prefer Debian 12 / Ubuntu LTS bases for infra to align with .NET Noble-based images (ADR-0003) and simplify debugging.
  - It is acceptable for databases to lag one OS version behind if that is what the vendor supports (e.g. SQL Server on Ubuntu 22.04 while apps run on Ubuntu 24.04).

- **PAT-003 – Use management / metrics variants**
  - For Kubernetes environments, prefer infra images with built-in management and metrics (e.g. `rabbitmq:management`, Redis charts with metrics sidecars).
  - Ensure these variants expose Prometheus-compatible metrics where possible.

- **PAT-004 – Pin tags in infra layer, not architecture**
  - Pin exact tags (`4.1-management`, `7-bookworm`, `2022-latest`) in Helm `values.yaml` or infra manifests.
  - When upgrading, update those manifests and test; ADRs and KIs stay the same.

- **PAT-005 – Document image choices once per component**
  - For each infra component, maintain a short rationale in infra docs (e.g. `docs/infra-images.md`) explaining:
    - Chosen repo and tag pattern,
    - OS base,
    - Any known performance/security considerations.

- **PAT-006 – Prefer license-friendly databases when possible**
  - Where functional requirements allow, prefer PostgreSQL over SQL Server for production-like deployments to avoid license complications.
  - SQL Server containers remain acceptable for teaching, local dev, and explicit MS SQL scenarios; licensing boundaries must be honoured.

## Risks & Open Questions

### Risks

- **RISK-001**: Using non-vendor or unsupported images can lead to missing security patches and unpredictable behaviour.
- **RISK-002**: Choosing Alpine for performance-sensitive workloads (e.g. Redis) can reduce throughput and complicate debugging due to musl vs glibc differences.
- **RISK-003**: Pinning tags inconsistently (some in charts, some in manifests, some in docs) increases the chance of drift across environments.

### Open Questions

- **OPEN-001**: For each major infra component (RabbitMQ, Redis, DB), what are the agreed “minimum supported” and “recommended” version ranges for Red Dog going forward?
- **OPEN-002**: Should there be a shared “infra base OS” policy across multiple repos (Red Dog, client projects), or is it acceptable for each repo to choose independently?

## Source & Provenance

- Derived from historical infra design discussions and ADR-0007 refactor sessions around containerized infrastructure.
- Related ADRs / external docs:
  - ADR-0003: Ubuntu 24.04 base image standardization for .NET services.
  - ADR-0007: Cloud-agnostic deployment strategy (containers vs PaaS).
  - Vendor docs for RabbitMQ, Redis, SQL Server, PostgreSQL, Nginx official images.
