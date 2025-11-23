---
id: KI-UBUNTU_BASE_IMAGES-001
title: Ubuntu 24.04 base images for Red Dog application containers
tags:
  - red-dog
  - architecture
  - containers
  - docker
  - ubuntu
  - security
  - base-images
last_updated: 2025-11-22
source_sessions:
  - .claude/sessions/2025-11-02-1105-orderservice-dotnet10-refinement.md
source_plans:
  - plan/orderservice-dotnet10-upgrade.md
  - plan/modernization-strategy.md
confidence: high
status: Active
owner: Red Dog Modernization Team
notes: Canonical source-of-truth for image specifics lives in per-service Dockerfiles and docker-base-images docs.
---

# Summary

This knowledge item defines the long-term standard for operating system base images used by Red Dog **application containers**. All services we build and maintain must run on Ubuntu 24.04 LTS (or direct derivatives), regardless of language (.NET, Go, Python, Node.js).  

Infrastructure containers (databases, caches, proxies, etc.) are explicitly excluded and continue to use official upstream images. This KI is the canonical reference when creating or updating Dockerfiles for Red Dog application services.

## Key Facts

- **FACT-001**: Red Dog is a polyglot system with application services in .NET, Go, Python, Node.js, plus a Node.js-based UI build step.
- **FACT-002**: All Red Dog application containers MUST use Ubuntu 24.04 LTS as their base OS (direct derivatives allowed for multi-stage builds).
- **FACT-003**: .NET 10 official ASP.NET runtime images are already based on Ubuntu 24.04 and are the default for .NET services.
- **FACT-004**: Go, Python, and Node.js services are expected to build and run on Ubuntu 24.04-based images (build and runtime stages).
- **FACT-005**: This standard applies only to application containers; infrastructure containers (Redis, PostgreSQL, SQL Server, RabbitMQ, Nginx, etc.) use official vendor/upstream images.
- **FACT-006**: Red Dog targets multiple platforms (AKS, Azure Container Apps, EKS, GKE) where Ubuntu is a first-class, widely deployed Linux distribution.
- **FACT-007**: Ubuntu 24.04 is an LTS release with a support window that comfortably covers the .NET 10 LTS lifecycle and the intended lifetime of Red Dog.
- **FACT-008**: Using a single OS family simplifies vulnerability management, base image patching, and operational runbooks.

## Constraints

- **CON-001**: Any new or updated **application** Dockerfile MUST use an Ubuntu 24.04-based image as the final runtime base (e.g. `ubuntu:24.04` or an Ubuntu 24.04-based runtime image).
- **CON-002**: .NET services MUST use the official .NET 10 ASP.NET runtime images (Ubuntu 24.04-based) unless a future ADR explicitly changes this.
- **CON-003**: Go, Python, and Node.js services MUST NOT introduce alternative base OS families (e.g. Debian, Alpine) in their runtime stages.
- **CON-004**: Infrastructure containers (databases, caches, queues, proxies) MUST NOT be rebuilt just to “match Ubuntu”; they SHOULD use their official upstream images as documented in infra/deployment ADRs.
- **CON-005**: Changes to the base OS standard (e.g. moving from Ubuntu 24.04 to a future LTS) MUST be done via a new ADR that supersedes ADR-0003 and an update to this KI.
- **CON-006**: Image builds SHOULD pin to specific Ubuntu 24.04-based tags or digests where appropriate to avoid accidental base image drift.
- **CON-007**: Any exception to the Ubuntu 24.04 application base OS MUST be explicitly documented (rationale + scope) and linked from this KI or a superseding ADR.

## Patterns & Recommendations

- **PAT-001**: For .NET application services, use the official .NET 10 SDK image for builds and the official ASP.NET 10 runtime image (Ubuntu 24.04-based) for the runtime stage in multi-stage Dockerfiles.
- **PAT-002**: For Go services, use an Ubuntu 24.04-based Go build image (or Ubuntu 24.04 + Go toolchain) for the build stage and a minimal `ubuntu:24.04` (or Ubuntu 24.04-based slim) runtime stage containing only the compiled binary and required certificates.
- **PAT-003**: For Python services, use Ubuntu 24.04-based Python images for both build and runtime; use multi-stage builds or slim variants to keep runtime images small while retaining the Ubuntu base.
- **PAT-004**: For Node.js services and the UI build, use Ubuntu 24.04-based Node.js images for the build stage; the final runtime for the UI MAY be an upstream Nginx or similar infra image, not covered by this KI.
- **PAT-005**: When drafting Dockerfiles, keep OS-specific logic (package management, shell paths) aligned with Ubuntu/`apt` conventions; avoid introducing Debian/Alpine-specific tooling in application containers.
- **PAT-006**: Centralize “which image do we use for which service” in shared documentation (e.g. `docs/docker-base-images.md`) that references this KI and ADR-0003, instead of repeating rationale in every Dockerfile.
- **PAT-007**: Periodically (e.g. annually) review base image tags and digests for security updates, and rebuild application images to pick up Ubuntu 24.04 security patches.
- **PAT-008**: For workshops, documentation, and examples, always phrase container OS assumptions as “Ubuntu 24.04-based application containers” to keep the mental model consistent for learners.

## Risks & Open Questions

### Risks

- **RISK-001**: Diverging from some language-official images (Go/Python/Node usually document Debian/Alpine-based images) may reduce copy-paste compatibility with community examples and guidance.
- **RISK-002**: Ubuntu 24.04-based images may be larger than language-specific “slim” or Alpine images, which can increase image pull times and storage usage if not mitigated with multi-stage builds and pruning.
- **RISK-003**: Relying on Ubuntu 24.04 for all application runtimes introduces a dependency on Canonical’s ecosystem and update cadence; if image streams lag or change, adjustments may be required.
- **RISK-004**: Some language ecosystems may ship performance-tuned builds only in their official images; Ubuntu-based runtimes might differ slightly in performance characteristics and need benchmarking.
- **RISK-005**: If individual teams quietly introduce non-Ubuntu base images for convenience, OS fragmentation will reappear, undermining the benefits captured in this KI.

### Open Questions

- **OPEN-001**: At what point (e.g. Ubuntu 26.04 LTS release, or end-of-standard-support for 24.04) should we formally revisit and potentially supersede this standard?
- **OPEN-002**: Do we want to adopt Ubuntu Pro or a similar extended-support model for long-lived demo deployments, and if so, how does that affect our image selection and tagging?
- **OPEN-003**: For performance-critical services, do benchmarks indicate any meaningful differences between Ubuntu 24.04-based images and language-official Debian/Alpine images that would justify targeted exceptions?
- **OPEN-004**: Should we define a stricter image-pinning policy (e.g. always pin to digests for runtime images) to minimize unexpected changes from base image updates?

## Source & Provenance

- Derived from sessions:
  - `.claude/sessions/2025-11-02-1105-orderservice-dotnet10-refinement.md` (Ubuntu 24.04 standardization discussion)
- Related implementation plans:
  - `plan/orderservice-dotnet10-upgrade.md` (Dockerfile & containerization work for .NET services)
  - `plan/modernization-strategy.md` (polyglot service migration phases and containerization)
- Related ADRs / external docs:
  - `docs/adr/adr-0003-ubuntu-24-04-base-image-standardization.md` (source ADR for this KI)
  - `docs/adr/adr-0001-dotnet10-lts-adoption.md` (documenting .NET 10 LTS and its Ubuntu 24.04 base)
  - `docs/adr/adr-0007-cloud-agnostic-deployment-strategy.md` (infrastructure image selection and cloud deployment strategy)
