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

Red Dog's modernization strategy migrates from a .NET-only architecture (10 services) to a polyglot architecture (8 services across 5 languages):
- **.NET 10**: OrderService, AccountingService
- **Go**: MakeLineService, VirtualWorker
- **Python**: ReceiptGenerationService, VirtualCustomers
- **Node.js 24**: LoyaltyService
- **Vue.js 3.5 (Node.js 24 build)**: UI

**Key Constraints:**
- Multi-cloud deployment targets (AKS, Container Apps, EKS, GKE) require consistent container base images
- Teaching/demo focus requires simplified operational explanations ("What OS do your containers run?")
- Security maintenance must be manageable across 8 different services in 5 languages
- REQ-004 (from `plan/orderservice-dotnet10-upgrade.md`) mandates platform-agnostic Docker images

**Current State:**
- Existing .NET 6.0 services use unclear/mixed base images (likely Debian-based `mcr.microsoft.com/dotnet/aspnet:6.0`)
- No standardized container OS strategy documented
- Upcoming polyglot migration introduces potential for mixed base images (Debian for Go/Python/Node, Ubuntu for .NET)

**Available Base Image Options:**

| Language | Official Image | Base OS | Support |
|----------|---------------|---------|---------|
| .NET 10 | `mcr.microsoft.com/dotnet/aspnet:10.0` | Ubuntu 24.04 (default as of Oct 30, 2025) | Microsoft 3-year LTS |
| Go | `golang:1.23-bookworm` | Debian 12 Bookworm | Golang team |
| Python | `python:3.12-slim-bookworm` | Debian 12 Bookworm | Python team |
| Node.js | `node:24-bookworm-slim` | Debian 12 Bookworm | Node.js team |

**Canonical's Alternative:**
- **ubuntu/go**, **ubuntu/python**, **ubuntu/node** images available on Docker Hub
- Ubuntu 24.04 "Noble Numbat" LTS (EOL April 2029)
- Canonical is a Docker Verified Publisher with LTS Docker Image Portfolio
- Free 5-year security maintenance, 12-year support with Ubuntu Pro
- 24-hour critical CVE response SLA

**Problem:**
Using official language-specific images creates **OS fragmentation**: Ubuntu for .NET, Debian for Go/Python/Node. This complicates:
- Vulnerability scanning (different CVE databases, patch cycles)
- Operations runbooks ("Which OS is service X running?")
- Teaching narratives ("We use Debian for some, Ubuntu for others... why?")

## Decision

**Adopt Ubuntu 24.04 LTS "Noble Numbat" as the standardized base operating system for all Red Dog container images.**

**Implementation:**
- **.NET 10 services**: Use `mcr.microsoft.com/dotnet/aspnet:10.0` (already Ubuntu 24.04 default)
- **Go services**: Use `ubuntu/go` from Canonical (Ubuntu 24.04-based)
- **Python services**: Use `ubuntu/python` from Canonical (Ubuntu 24.04-based)
- **Node.js services**: Use `ubuntu/node` from Canonical (Ubuntu 24.04-based)

## Scope

This ADR applies to **application containers** (services we build and maintain):
- OrderService, AccountingService, MakeLineService, LoyaltyService, ReceiptGenerationService, VirtualCustomers, VirtualWorker
- RedDog.UI build stage (Node.js compilation)

This ADR does **NOT** apply to **infrastructure containers** (third-party dependencies we consume):
- Redis, PostgreSQL, SQL Server, Nginx (runtime for UI), RabbitMQ, message brokers, databases
- **Rationale:** Use official upstream-maintained images for:
  - **Performance optimization:** Official images use glibc (2x faster than musl on Alpine for Redis/databases)
  - **Security hardening:** Vendor-provided security patches and CVE response SLAs
  - **Maintenance reduction:** Avoid custom build pipelines for infrastructure components
  - **Battle-tested reliability:** Production-validated by millions of deployments

**Infrastructure image selection documented in ADR-0007 (Cloud-Agnostic Deployment Strategy).**

**Rationale:**
- **STD-001**: **Single Base OS Family**: All 8 services run on Ubuntu 24.04, eliminating OS fragmentation. Operators learn one OS, one package manager (apt), one CVE database (Ubuntu Security Notices).
- **STD-002**: **Aligned with .NET Default**: Microsoft changed .NET 10 default images to Ubuntu 24.04 (October 30, 2025). Standardizing on Ubuntu aligns with this upstream decision.
- **STD-003**: **Canonical LTS Commitment**: Ubuntu 24.04 LTS supported until April 2029 (5 years), with optional 12-year Ubuntu Pro support. Outlives most runtime releases (.NET 10 EOL November 2028).
- **STD-004**: **Unified Security Patching**: Single vendor (Canonical) provides base OS patches for all images. Simplifies vulnerability management, reduces scanner noise from OS-level CVEs.
- **STD-005**: **Teaching/Demo Simplification**: "All Red Dog services run on Ubuntu 24.04" - single, clear message. No need to explain Debian vs Ubuntu differences during workshops.
- **STD-006**: **Cloud Provider Ubiquity**: Ubuntu is the most widely deployed Linux distribution on AKS, EKS, GKE, and Azure Container Apps. Node images pre-validated by cloud providers.

## Consequences

### Positive

- **POS-001**: **Operational Consistency**: Single base OS across all services. Debugging, troubleshooting, and security patching use identical tooling (`apt`, `dpkg`, `ufw`).
- **POS-002**: **Simplified Vulnerability Scanning**: Scan results reference single CVE database (Ubuntu Security Notices). No context switching between Debian Security Tracker and Ubuntu USN.
- **POS-003**: **Canonical Security SLA**: 24-hour response for critical CVEs, free for 5 years. High/critical vulnerabilities patched faster than community-maintained Debian images.
- **POS-004**: **Longer Support Window**: Ubuntu 24.04 EOL April 2029 (5 years) vs Debian 12 Bookworm EOL ~June 2028 (4 years). Extends base OS support beyond .NET 10 lifecycle.
- **POS-005**: **Docker Verified Publisher**: Canonical images vetted by Docker, with guaranteed update cadence and security compliance. Not community/third-party images.
- **POS-006**: **Teaching Clarity**: Instructors explain "Ubuntu everywhere" once. No cognitive overhead from mixed OS strategy. Simplifies slide decks, documentation, workshop guides.
- **POS-007**: **Consistent Cloud Validation**: All cloud providers (Azure, AWS, GCP) pre-validate Ubuntu images for Kubernetes. Reduces deployment surprises or OS-specific bugs.
- **POS-008**: **Ubuntu Pro Upgrade Path**: Option to extend support to 12 years with Ubuntu Pro subscription. Future-proofs long-lived deployments.

### Negative

- **NEG-001**: **Not Language-Official Images**: Deviates from golang, python, and node.js project-maintained images. Language teams optimize official images for specific runtime performance.
- **NEG-002**: **Canonical Dependency**: All non-.NET images sourced from single vendor (Canonical). If Canonical discontinues LTS Docker Image Portfolio, migration required.
- **NEG-003**: **Potential Image Size Increase**: Ubuntu base images may be larger than Debian slim/Alpine variants. Typical overhead: +20-50 MB per image vs slim-bookworm (mitigated by multi-stage builds).
- **NEG-004**: **Community Support Differences**: Language-specific communities (gophers, pythonistas, Node.js devs) primarily document Debian/Alpine-based images. Ubuntu-specific issues may have less Stack Overflow coverage.
- **NEG-005**: **Build Tool Availability**: Canonical's ubuntu/* images may not include latest language build tools immediately after upstream releases. Slight lag compared to language team images.
- **NEG-006**: **Performance Unknowns**: Official language images optimized for runtime performance (e.g., Python PGO builds). Canonical images may lack these optimizations (requires benchmarking to validate).

## Alternatives Considered

### Mixed Base Images (Debian + Ubuntu)

- **ALT-001**: **Description**: Use official language images for each runtime (Debian for Go/Python/Node, Ubuntu for .NET). No standardization.
- **ALT-002**: **Rejection Reason**: Creates OS fragmentation. Security scanning complexity (Debian + Ubuntu CVE databases). Operational overhead (two OS families to maintain). Teaching confusion ("Why different OSes?"). Violates consistency principle.

### Alpine Linux Standardization

- **ALT-003**: **Description**: Use Alpine-based images for all services (golang:alpine, python:alpine, node:alpine, custom .NET Alpine images).
- **ALT-004**: **Rejection Reason**: .NET 10 default is Ubuntu (Microsoft's strategic direction). Alpine uses musl libc (not glibc), causing compatibility issues with some libraries. Smaller ecosystem (fewer pre-built packages). Security patching less transparent than Canonical's Ubuntu Security Notices.

### Debian 12 Bookworm Standardization

- **ALT-005**: **Description**: Use Debian 12 Bookworm for all services (golang:bookworm, python:bookworm, node:bookworm, custom .NET Debian images).
- **ALT-006**: **Rejection Reason**: .NET 10 default is Ubuntu (contradicts Microsoft's upstream decision). Debian 12 EOL ~June 2028 (shorter than Ubuntu 24.04's April 2029). No commercial support SLA like Canonical's 24-hour critical CVE response. Less alignment with cloud provider defaults (Ubuntu dominant on AKS/EKS/GKE).

### Build Official, Deploy Ubuntu (Hybrid)

- **ALT-007**: **Description**: Multi-stage Dockerfiles using official language images for build stage (golang:bookworm), Ubuntu 24.04 for runtime stage (ubuntu:24.04 + manual runtime install).
- **ALT-008**: **Rejection Reason**: Complexity overhead (manual runtime installation, library dependency management). Canonical's ubuntu/* images already provide optimized runtime environments. Risk of version mismatches between build and runtime stages. Not DRY (duplicates Canonical's work).

## Implementation Notes

- **IMP-001**: **Image Selection Table**:

| Service | Language | Build Image | Runtime Image | Notes |
|---------|----------|-------------|---------------|-------|
| OrderService | .NET 10 | `mcr.microsoft.com/dotnet/sdk:10.0` | `mcr.microsoft.com/dotnet/aspnet:10.0` | Microsoft default (Ubuntu 24.04) |
| AccountingService | .NET 10 | `mcr.microsoft.com/dotnet/sdk:10.0` | `mcr.microsoft.com/dotnet/aspnet:10.0` | Microsoft default (Ubuntu 24.04) |
| MakeLineService | Go | `ubuntu/go:24.04` | `ubuntu:24.04` + compiled binary | Multi-stage build |
| VirtualWorker | Go | `ubuntu/go:24.04` | `ubuntu:24.04` + compiled binary | Multi-stage build |
| ReceiptGenerationService | Python | `ubuntu/python:24.04` | `ubuntu/python:24.04-slim` (if available) | Single/multi-stage |
| VirtualCustomers | Python | `ubuntu/python:24.04` | `ubuntu/python:24.04-slim` (if available) | Single/multi-stage |
| LoyaltyService | Node.js 24 | `ubuntu/node:24.04` | `ubuntu/node:24.04-slim` (if available) | Single/multi-stage |
| UI | Node.js 24 | `ubuntu/node:24.04` (build) | `nginx:1.27-bookworm` (runtime) | Static site hosting (see ADR-0007 for infrastructure image rationale) |

- **IMP-002**: **Dockerfile Pattern (Go Example)**:
```dockerfile
# Build stage
FROM ubuntu/go:24.04 AS build
WORKDIR /app
COPY go.mod go.sum ./
RUN go mod download
COPY . .
RUN CGO_ENABLED=0 go build -o makeline-service

# Runtime stage
FROM ubuntu:24.04
RUN apt-get update && apt-get install -y ca-certificates && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/makeline-service /usr/local/bin/
CMD ["makeline-service"]
```

- **IMP-003**: **Validation Steps**:
  1. Verify Canonical `ubuntu/go`, `ubuntu/python`, `ubuntu/node` images exist on Docker Hub
  2. Test multi-stage builds for each language (ensure no runtime issues)
  3. Benchmark application performance (compare vs official language images)
  4. Scan images with Trivy/Grype (establish baseline CVE counts)
  5. Document any Canonical image limitations (missing packages, version lags)

- **IMP-004**: **Rollback Plan**: If Canonical images introduce breaking changes or performance regressions:
  - Option 1: Pin to specific Ubuntu 24.04 image SHA256 digest (prevents unexpected updates)
  - Option 2: Revert to official language images (Debian-based) with documented OS fragmentation trade-off
  - Option 3: Build custom Ubuntu 24.04 images using `ubuntu:24.04` base + manual runtime installation

- **IMP-005**: **Success Criteria**:
  - All 8 service Dockerfiles use Ubuntu 24.04-based images (verified via `docker inspect`)
  - Zero high/critical OS-level CVEs in base images (`docker scan` or Trivy)
  - Application performance within 5% of Debian-based baseline (P95 latency, memory usage)
  - Deployment succeeds on all 4 target platforms (AKS, Container Apps, EKS, GKE)

- **IMP-006**: **Documentation Updates**:
  - Update `plan/orderservice-dotnet10-upgrade.md` TASK-502 with Ubuntu 24.04 requirement
  - Update `plan/modernization-strategy.md` Phase 2-7 service migration Dockerfiles
  - Add Dockerfile examples to each service migration plan (Go, Python, Node.js)
  - Create `docs/docker-base-images.md` explaining Ubuntu 24.04 standardization decision

## References

- **REF-001**: Related ADR: `docs/adr/adr-0001-dotnet10-lts-adoption.md` (documents .NET 10 LTS choice, Ubuntu 24.04 default)
- **REF-002**: Related Plan: `plan/orderservice-dotnet10-upgrade.md` TASK-502 (Dockerfile updates)
- **REF-003**: Related Plan: `plan/modernization-strategy.md` (Phase 2-7 service migrations)
- **REF-004**: Canonical Announcement: [LTS Docker Image Portfolio on Docker Hub](https://canonical.com/blog/canonical-publishes-lts-docker-image-portfolio-on-docker-hub)
- **REF-005**: Microsoft Announcement: [Default .NET images changed to Ubuntu 24.04 (October 30, 2025)](https://github.com/dotnet/dotnet-docker/blob/main/documentation/supported-tags.md)
- **REF-006**: Ubuntu Release: [Ubuntu 24.04 LTS Noble Numbat (EOL April 2029)](https://ubuntu.com/about/release-cycle)
- **REF-007**: Docker Hub: [ubuntu/go](https://hub.docker.com/r/ubuntu/go), [ubuntu/python](https://hub.docker.com/r/ubuntu/python), [ubuntu/node](https://hub.docker.com/r/ubuntu/node)
- **REF-008**: Session Log: `.claude/sessions/2025-11-02-1105-orderservice-dotnet10-refinement.md` (Ubuntu 24.04 research and standardization discussion)
- **REF-009**: Related ADR: `docs/adr/adr-0007-cloud-agnostic-deployment-strategy.md` (infrastructure container image selection and cloud-agnostic portability rationale)
