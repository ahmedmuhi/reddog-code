---
title: "ADR-0003: Ubuntu 24.04 Base Image Standardization for Application Containers"
status: "Accepted"
date: "2025-11-02"
authors: "Red Dog Modernization Team"
tags: ["architecture", "decision", "docker", "containers", "ubuntu", "security"]
supersedes: ""
superseded_by: ""
---

# ADR-0003: Ubuntu 24.04 Base Image Standardization for Application Containers

## Status

**Accepted**

## Context

Red Dog is moving from a .NET-only codebase to a polyglot microservices architecture:

- .NET 10 (OrderService, AccountingService)
- Go (MakeLineService, VirtualWorker)
- Python (ReceiptGenerationService, VirtualCustomers)
- Node.js (LoyaltyService, UI build)

These services will run as containers on multiple platforms (AKS, Azure Container Apps, EKS, GKE).

Without a standard base operating system for application containers, we risk:

- OS fragmentation across services (Debian here, Ubuntu there, maybe Alpine elsewhere).
- More complex vulnerability management (different CVE feeds and patch cycles per OS).
- Higher operational and teaching overhead (“which OS is this service running?” per service).
- Divergent container hardening and debugging practices.

At the same time:

- .NET 10 upstream images already use Ubuntu 24.04 by default.
- Ubuntu 24.04 LTS has a clear support window suitable for the expected lifetime of the Red Dog sample.

We need a single, explicit standard for the base OS family used in application containers.

## Decision

**All Red Dog application containers MUST use Ubuntu 24.04 LTS as their base operating system (or a direct derivative).**

In practice:

- .NET services use the official .NET 10 images, which are based on Ubuntu 24.04.
- Go, Python, and Node.js services build on Ubuntu 24.04-based images for both build and runtime stages.
- Where multi-stage builds are used, the final runtime image MUST be Ubuntu 24.04-based.

This ADR does **not** standardize image names or tags beyond “Ubuntu 24.04-based”; concrete image choices and Dockerfile patterns are documented in repository knowledge items and implementation guides, not in this ADR.

## Scope

### In Scope

- **Application/service containers** owned by the Red Dog team:
  - OrderService
  - AccountingService
  - MakeLineService
  - LoyaltyService
  - ReceiptGenerationService
  - VirtualCustomers
  - VirtualWorker
  - UI build container (Node.js build stage)

### Out of Scope

- **Infrastructure / third-party containers**, for which we use upstream images:
  - Databases (e.g., PostgreSQL, SQL Server)
  - Caches and queues (e.g., Redis, RabbitMQ)
  - Reverse proxies / ingress (e.g., Nginx used to serve the built UI)
- Host OS for Kubernetes nodes or container runtimes (controlled by the platform).

Infrastructure image selection and rationale are covered by ADR-0007 and related documentation.

## Rationale

- **OS-001: Single base OS family**  
  One OS family (Ubuntu 24.04) across all application containers simplifies operations, security posture, and on-call debugging.

- **OS-002: Alignment with .NET defaults**  
  .NET 10 official images have already standardized on Ubuntu 24.04; following that default avoids divergence between .NET and non-.NET services.

- **OS-003: LTS lifecycle suitability**  
  Ubuntu 24.04 LTS has a support window that comfortably covers the intended lifetime of Red Dog’s reference implementation and the .NET 10 LTS window.

- **OS-004: Simplified security and compliance**  
  A single base OS allows security tooling and CVE triage to focus on one set of security advisories and patching practices.

- **OS-005: Teaching and documentation clarity**  
  For workshops and education, “all application containers run Ubuntu 24.04” is simple to explain and remember.

- **OS-006: Cloud platform ubiquity**  
  Ubuntu images are first-class citizens on the target platforms (AKS, EKS, GKE, Azure Container Apps), reducing surprises related to OS support.

## Consequences

### Positive

- **POS-001: Operational consistency**  
  Common debugging and troubleshooting steps (package inspection, logs, shell tools) are the same across all application containers.

- **POS-002: Unified vulnerability management**  
  Security scanning and patching can assume an Ubuntu 24.04 baseline for application images, reducing noise and fragmentation.

- **POS-003: Easier container hardening**  
  Hardening guidance (TLS ciphers, OS packages, user accounts) is reusable across services because they share the same base OS family.

- **POS-004: Clear story for users and learners**  
  Documentation and diagrams can describe “Ubuntu 24.04 application containers” without exception lists.

### Negative

- **NEG-001: Divergence from some language defaults**  
  Go, Python, and Node.js official images typically use Debian-based or Alpine variants; we deviate from those defaults.

- **NEG-002: Potential image size overhead**  
  Ubuntu 24.04 images may be larger than language-specific “slim” or Alpine images, increasing base image size (partially mitigated by multi-stage builds).

- **NEG-003: Dependence on Ubuntu 24.04 ecosystem**  
  If Ubuntu 24.04 images for a given runtime lag behind official language images, we may wait longer for certain runtime features or need to adjust our image choice.

- **NEG-004: Benchmarking and tuning required**  
  Some workloads may perform slightly differently compared to language-official images; performance needs to be verified and tuned where relevant.

## Implementation Notes

- Per-language Dockerfile patterns (build vs runtime stages, recommended base images, and image pinning strategy) are documented in repository knowledge items and service-specific migration guides.
- Application images SHOULD pin to specific Ubuntu 24.04-based tags or digests to avoid unexpected base image changes during builds.
- Infrastructure containers (databases, caches, proxies) continue to use upstream official images; cross-image compatibility (e.g., glibc vs musl) is handled within ADR-0007 and related infra documentation.
- When upgrading to a future Ubuntu LTS as a new standard, this ADR SHOULD be superseded by a new ADR documenting the new baseline and migration strategy.

## Relationship to Other ADRs

- **ADR-0001** – .NET 10 LTS Adoption  
  .NET 10 default images are Ubuntu 24.04-based; ADR-0003 aligns the rest of the stack with this baseline.

- **ADR-0002** – Cloud-Agnostic Configuration via Dapr Abstraction  
  Standardizing the container OS supports a consistent multi-cloud deployment story for Dapr-enabled services.

- **ADR-0007** – Cloud-Agnostic Deployment Strategy  
  Defines how containers (including infrastructure images) are deployed across AKS, EKS, GKE, and Container Apps; ADR-0003 constrains the base OS for the application subset of those containers.
