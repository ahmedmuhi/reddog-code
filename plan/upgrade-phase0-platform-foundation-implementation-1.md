---
goal: "Phase 0: Platform Foundation - Complete Infrastructure Upgrade Before Service Modernization"
version: 1.0
date_created: 2025-11-09
last_updated: 2025-11-09
owner: "Red Dog Modernization Team"
status: 'Planned'
tags: [infrastructure, upgrade, phase-0, platform, dapr, keda, certmanager, prerequisite]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

**Phase 0: Platform Foundation** is a critical prerequisite for the Red Dog modernization strategy. This phase upgrades the infrastructure platform (Dapr, KEDA, cert-manager) and supporting components (state stores, object storage, data infrastructure) to support .NET 10 and polyglot service migrations.

**Why Phase 0 is Required:**
- Dapr 1.3.0 is incompatible with modern .NET features and 3+ years outdated
- All Red Dog services run Dapr sidecars (blocking dependency for Phase 1)
- KEDA 2.2.0 predates Kubernetes 1.30 API changes
- State stores must migrate from Redis (Dapr 1.16 incompatibility with Redis 7/8)
- Object storage must adopt cloud-agnostic strategy (ADR-0007)

**Duration**: 3–4 weeks
**Dependencies**: None (Phase 0 is the foundation)
**Enables**: Phase 1 (.NET 6→10), Phase 2–7 (Polyglot migrations)

## Table of contents

- [Requirements & Constraints](#requirements--constraints)
- [Implementation Phases](#implementation-phases)
- [Alternatives](#alternatives)
- [Dependencies](#dependencies)
- [Files](#files)
- [Testing](#testing)
- [Risks & Assumptions](#risks--assumptions)
- [Related Specifications / Further Reading](#related-specifications--further-reading)

## Requirements & Constraints

### Platform Requirements

- **REQ-001**: Kubernetes 1.30+ required on all target platforms (AKS, EKS, GKE, Container Apps)
- **REQ-002**: Dapr 1.16.2 must be installed before any .NET 10 service upgrades
- **REQ-003**: KEDA 2.18.1 required for Kubernetes 1.30+ API compatibility
- **REQ-004**: Cert-Manager 1.19 required for Let's Encrypt ACME v2 protocol
- **REQ-005**: All services must use Dapr HTTP/gRPC APIs (not .NET SDK) until Dapr SDK adds .NET 10 support

### State Store Requirements

- **REQ-006**: Local development uses Redis 6.2.14 (Dapr 1.16 compatible)
- **REQ-007**: Azure deployments use Cosmos DB (NoSQL API) via `state.azure.cosmosdb`
- **REQ-008**: AWS deployments use DynamoDB via `state.aws.dynamodb`
- **REQ-009**: GCP deployments use Cloud Firestore via `state.gcp.firestore`
- **REQ-010**: State store migration must preserve existing state data (MakeLineService queue, LoyaltyService points)

### Object Storage Requirements

- **REQ-011**: Local development uses MinIO (S3-compatible) via `bindings.aws.s3`
- **REQ-012**: AWS deployments use S3 via `bindings.aws.s3` with IRSA
- **REQ-013**: Azure deployments use Blob Storage via `bindings.azure.blobstorage` with Workload Identity
- **REQ-014**: GCP deployments use Cloud Storage via `bindings.gcp.bucket` with Workload Identity

### Infrastructure Container Requirements

- **REQ-015**: SQL Server 2022 (or PostgreSQL 17 alternative) for AccountingService
- **REQ-016**: RabbitMQ 4.2.0-management for pub/sub messaging
- **REQ-017**: Nginx 1.28.0 for ingress and UI static hosting

### Security Requirements

- **SEC-001**: Workload Identity Federation required for all cloud deployments (no long-lived secrets)
- **SEC-002**: TLS certificates auto-renewed via cert-manager (Let's Encrypt)
- **SEC-003**: Dapr mTLS enabled for service-to-service communication
- **SEC-004**: State store and object storage use cloud-native IAM/RBAC (no access keys in manifests)

### Constraints

- **CON-001**: Dapr 1.16.2 does NOT support Redis 7/8 (only 6.x) - cloud-native state stores required
- **CON-002**: Dapr .NET SDK 1.16.2 does NOT support .NET 10 - must use HTTP/gRPC APIs
- **CON-003**: KEDA 2.18.1 requires Kubernetes 1.30+ (verified available on AKS, EKS, GKE)
- **CON-004**: Azure Blob Storage does NOT support S3 API - must use `bindings.azure.blobstorage`
- **CON-005**: Direct upgrade from Dapr 1.3.0 → 1.16.2 supported (no incremental steps required)
- **CON-006**: Direct upgrade from KEDA 2.2.0 → 2.18.1 supported (no incremental steps required)
- **CON-007**: Redis 6.2.14 is EOL (July 2024) - acceptable for local dev only, not production

### Guidelines

- **GUD-001**: Follow ADR-0002 (Cloud-Agnostic Configuration via Dapr Abstraction)
- **GUD-002**: Follow ADR-0007 (Cloud-Agnostic Deployment Strategy)
- **GUD-003**: Prioritize cloud-native managed services over self-managed infrastructure
- **GUD-004**: Minimize code changes (leverage Dapr abstraction for multi-cloud portability)
- **GUD-005**: Test in staging environment before production deployment

## Implementation Phases


### Implementation Phase 1: Pre-Upgrade Validation (Week 1, Days 1–2)

**GOAL-001:** Verify infrastructure readiness and backup current state

Tasks (Phase 1):

- [ ] TASK-101 — Verify Kubernetes version is 1.30+ on all target clusters (AKS, EKS, GKE).
- [ ] TASK-102 — Audit current Dapr usage (components, sidecars, service invocations).
- [x] TASK-103 — Audit current KEDA usage (ScaledObjects, TriggerAuthentication) — already completed: NONE FOUND (2025-11-09).
- [ ] TASK-104 — Backup all Dapr component YAML files (`manifests/branch/base/components/`).
- [ ] TASK-105 — Backup all service deployment YAML files (`manifests/branch/base/deployments/`).
- [ ] TASK-106 — Export current Redis state data (MakeLineService, LoyaltyService) for migration.
- [ ] TASK-107 — Document current service endpoints and health check URLs.
- [ ] TASK-108 — Create staging environment clone for testing.


### Implementation Phase 2: Platform Infrastructure Upgrade (Week 1–2, Days 3–10)

**GOAL-002:** Upgrade Dapr, KEDA, and cert-manager to modern versions

Tasks (Phase 2):

- [ ] TASK-201 — Execute Dapr 1.3.0 → 1.16.2 upgrade (see `upgrade-dapr-1.16-implementation-1.md`).
- [ ] TASK-202 — Execute KEDA 2.2.0 → 2.18.1 upgrade (see `upgrade-keda-2.18-implementation-1.md`).
- [ ] TASK-203 — Execute cert-manager 1.3.1 → 1.19 upgrade (see `upgrade-certmanager-1.19-implementation-1.md`).
- [ ] TASK-204 — Verify Dapr sidecars running in all service pods.
- [ ] TASK-205 — Verify KEDA operator pod is healthy.
- [ ] TASK-206 — Verify cert-manager pods are healthy and issuing certificates.
- [ ] TASK-207 — Run smoke tests for Dapr service invocation.
- [ ] TASK-208 — Validate Dapr HTTP API endpoints respond correctly.

> **Update (2025-11-16):** cert-manager upgrade work now follows the detailed, cloud-only plan in `plan/upgrade-certmanager-1.19-implementation-1.md`; local/kind environments purposely omit cert-manager.


### Implementation Phase 3: State Store Migration (Week 2, Days 8–12)

**GOAL-003:** Migrate from Redis to cloud-native state stores

Tasks (Phase 3):

- [ ] TASK-301 — Execute state store migration (see `migrate-state-stores-cloud-native-implementation-1.md`).
- [ ] TASK-302 — Create Cosmos DB component for Azure (`state.azure.cosmosdb`).
- [ ] TASK-303 — Create DynamoDB component for AWS (`state.aws.dynamodb`).
- [ ] TASK-304 — Create Cloud Firestore component for GCP (`state.gcp.firestore`).
- [ ] TASK-305 — Create Redis 6.2.14 component for local dev (`state.redis`).
- [ ] TASK-306 — Import existing state data to new state stores.
- [ ] TASK-307 — Test MakeLineService state operations (order queue).
- [ ] TASK-308 — Test LoyaltyService state operations (customer points).
- [ ] TASK-309 — Validate state store failover and retry behavior.


### Implementation Phase 4: Object Storage Migration (Week 2–3, Days 10–15)

**GOAL-004:** Migrate from Azure Blob to cloud-agnostic object storage

Tasks (Phase 4):

- [ ] TASK-401 — Execute object storage migration (see `migrate-object-storage-cloud-agnostic-implementation-1.md`).
- [ ] TASK-402 — Deploy MinIO in Docker Compose for local dev.
- [ ] TASK-403 — Create S3 binding for AWS (`bindings.aws.s3` with IRSA).
- [ ] TASK-404 — Create Azure Blob binding for Azure (`bindings.azure.blobstorage` with Workload Identity).
- [ ] TASK-405 — Create GCS binding for GCP (`bindings.gcp.bucket` with Workload Identity).
- [ ] TASK-406 — Test ReceiptGenerationService receipt creation.
- [ ] TASK-407 — Test receipt retrieval and deletion.
- [ ] TASK-408 — Migrate existing receipts to new storage.


### Implementation Phase 5: Supporting Infrastructure Upgrade (Week 3, Days 15–18)

**GOAL-005:** Upgrade SQL Server, RabbitMQ, and Nginx

Tasks (Phase 5):

- [ ] TASK-501 — Execute infrastructure container upgrades (see `upgrade-infrastructure-containers-implementation-1.md`).
- [ ] TASK-502 — Upgrade SQL Server 2019 → 2022 (or deploy PostgreSQL 17).
- [ ] TASK-503 — Upgrade RabbitMQ to 4.2.0-management.
- [ ] TASK-504 — Upgrade Nginx to 1.28.0-bookworm.
- [ ] TASK-505 — Test AccountingService database connectivity.
- [ ] TASK-506 — Test pub/sub messaging through RabbitMQ.
- [ ] TASK-507 — Test UI static hosting via Nginx.
- [ ] TASK-508 — Test ingress routing via Nginx.


### Implementation Phase 6: Integration Testing (Week 3–4, Days 18–23)

**GOAL-006:** Validate end-to-end system functionality

Tasks (Phase 6):

- [ ] TASK-601 — Run full integration test suite.
- [ ] TASK-602 — Test order placement flow (VirtualCustomers → OrderService).
- [ ] TASK-603 — Test order processing flow (OrderService → MakeLineService → VirtualWorker).
- [ ] TASK-604 — Test loyalty points accrual (LoyaltyService).
- [ ] TASK-605 — Test receipt generation (ReceiptGenerationService).
- [ ] TASK-606 — Test accounting aggregation (AccountingService).
- [ ] TASK-607 — Test UI dashboard data display.
- [ ] TASK-608 — Load test with 100 concurrent orders.
- [ ] TASK-609 — Validate Dapr telemetry (OpenTelemetry metrics, traces).
- [ ] TASK-610 — Validate KEDA autoscaling (if ScaledObjects configured).


### Implementation Phase 7: Production Deployment (Week 4, Days 24–25)

**GOAL-007:** Deploy Phase 0 upgrades to production

Tasks (Phase 7):

- [ ] TASK-701 — Schedule production maintenance window (2-hour window).
- [ ] TASK-702 — Communicate upgrade plan to stakeholders.
- [ ] TASK-703 — Execute production Dapr upgrade.
- [ ] TASK-704 — Execute production KEDA upgrade.
- [ ] TASK-705 — Execute production cert-manager upgrade.
- [ ] TASK-706 — Deploy cloud-native state store components.
- [ ] TASK-707 — Deploy cloud-agnostic object storage bindings.
- [ ] TASK-708 — Upgrade SQL Server, RabbitMQ, Nginx.
- [ ] TASK-709 — Run production smoke tests.
- [ ] TASK-710 — Monitor for 24 hours (metrics, logs, errors).


### Implementation Phase 8: Post-Upgrade Monitoring (Week 4, Days 26–28)

**GOAL-008:** Ensure stability and performance

Tasks (Phase 8):

- [ ] TASK-801 — Monitor Dapr sidecar resource usage (CPU, memory).
- [ ] TASK-802 — Monitor state store latency and error rates.
- [ ] TASK-803 — Monitor object storage operation success rates.
- [ ] TASK-804 — Monitor RabbitMQ queue depth and throughput.
- [ ] TASK-805 — Monitor SQL Server query performance.
- [ ] TASK-806 — Monitor cert-manager certificate renewals.
- [ ] TASK-807 — Review Dapr distributed tracing for latency issues.
- [ ] TASK-808 — Document lessons learned and update runbooks.

## Alternatives

- **ALT-001**: **Incremental Dapr Upgrade** (1.3 → 1.6 → 1.10 → 1.16)
  - **Rejected**: Research confirms direct upgrade is supported, incremental adds unnecessary complexity

- **ALT-002**: **Wait for Dapr .NET SDK .NET 10 Support**
  - **Rejected**: Unknown timeline, blocks entire modernization. Workaround: Use HTTP/gRPC APIs directly.

- **ALT-003**: **Keep Redis 7/8 for State Stores**
  - **Rejected**: Dapr 1.16 does not support Redis 7/8. Cloud-native state stores (Cosmos DB, DynamoDB, Firestore) eliminate this constraint.

- **ALT-004**: **Use S3Proxy Gateway for Azure Blob**
  - **Rejected**: Adds infrastructure complexity, maintenance overhead. Use native `bindings.azure.blobstorage` per ADR-0007.

- **ALT-005**: **Skip KEDA Upgrade** (Remove KEDA Entirely)
  - **Rejected**: KEDA is installed but dormant (no active ScaledObjects). Upgrade maintains future autoscaling capability with zero risk.

- **ALT-006**: **Deploy MinIO in Production**
  - **Rejected**: Cloud-native object storage (S3, Blob, GCS) reduces operational complexity, better for teaching demos.

## Dependencies

### Infrastructure Dependencies

- **DEP-001**: Kubernetes 1.30+ clusters available on AKS, EKS, GKE (verified 2025-11-09)
- **DEP-002**: Helm 3.x installed for chart upgrades
- **DEP-003**: kubectl access to target Kubernetes clusters
- **DEP-004**: Docker Compose for local development environment

### Cloud Provider Dependencies

- **DEP-005**: Azure subscription with Cosmos DB, Blob Storage, AKS access
- **DEP-006**: AWS account with DynamoDB, S3, EKS access
- **DEP-007**: GCP project with Cloud Firestore, Cloud Storage, GKE access

### External Service Dependencies

- **DEP-008**: Let's Encrypt ACME v2 endpoints accessible for cert-manager
- **DEP-009**: Docker Hub / Quay.io / MCR accessible for container image pulls
- **DEP-010**: Helm chart repositories accessible (dapr.github.io, kedacore, jetstack)

### Research Document Dependencies

- **DEP-011**: `docs/research/dapr-upgrade-breaking-changes-1.3-to-1.16.md` (breaking changes analysis)
- **DEP-012**: `docs/research/keda-upgrade-analysis.md` (KEDA upgrade analysis)
- **DEP-013**: `docs/research/infrastructure-versions-verification.md` (verified container versions)
- **DEP-014**: Session log: `.claude/sessions/2025-11-09-1018-infrastructure-upgrade-strategy.md`

### Implementation Plan Dependencies

- **DEP-015**: `plan/upgrade-dapr-1.16-implementation-1.md` (detailed Dapr upgrade)
- **DEP-016**: `plan/upgrade-keda-2.18-implementation-1.md` (detailed KEDA upgrade)
- **DEP-017**: `plan/upgrade-certmanager-1.19-implementation-1.md` (detailed cert-manager upgrade)
- **DEP-018**: `plan/upgrade-infrastructure-containers-implementation-1.md` (SQL, Redis, RabbitMQ, Nginx)
- **DEP-019**: `plan/migrate-state-stores-cloud-native-implementation-1.md` (state store migration)
- **DEP-020**: `plan/migrate-object-storage-cloud-agnostic-implementation-1.md` (object storage migration)

## Files

### Dapr Component Files

- **FILE-001**: `manifests/branch/base/components/reddog.pubsub.yaml` - RabbitMQ pub/sub component
- **FILE-002**: `manifests/branch/base/components/reddog.state.makeline.yaml` - State store for MakeLineService
- **FILE-003**: `manifests/branch/base/components/reddog.state.loyalty.yaml` - State store for LoyaltyService
- **FILE-004**: `manifests/branch/base/components/reddog.binding.receipt.yaml` - Object storage binding
- **FILE-005**: `manifests/branch/base/components/reddog.secretstore.yaml` - Secret store component

### Service Deployment Files

- **FILE-006**: `manifests/branch/base/deployments/order-service.yaml` - OrderService deployment
- **FILE-007**: `manifests/branch/base/deployments/make-line-service.yaml` - MakeLineService deployment
- **FILE-008**: `manifests/branch/base/deployments/loyalty-service.yaml` - LoyaltyService deployment
- **FILE-009**: `manifests/branch/base/deployments/accounting-service.yaml` - AccountingService deployment
- **FILE-010**: `manifests/branch/base/deployments/receipt-generation-service.yaml` - ReceiptGenerationService deployment
- **FILE-011**: `manifests/branch/base/deployments/virtual-worker.yaml` - VirtualWorker deployment
- **FILE-012**: `manifests/branch/base/deployments/virtual-customers.yaml` - VirtualCustomers deployment
- **FILE-013**: `manifests/branch/base/deployments/ui.yaml` - UI deployment

### Infrastructure Deployment Sources

- **FILE-014**: `charts/infrastructure/` - Helm chart that provisions Redis + SQL Server secrets for local clusters
- **FILE-015**: `charts/reddog/` - Umbrella chart for application workloads (installs Dapr components + services)
- **FILE-016**: `scripts/setup-local-dev.sh` - Automates upstream Helm installs (dapr/dapr, kedacore/keda, jetstack/cert-manager, ingress-nginx, Bitnami RabbitMQ, etc.)
- **FILE-017**: `values/values-local.yaml` - Local override file consumed by the Helm commands above
- **FILE-018**: `plan/upgrade-keda-2.18-implementation-1.md` / `plan/upgrade-certmanager-1.19-implementation-1.md` - Contain the command matrices that replace the deleted Flux `HelmRelease` manifests
- **FILE-019**: `docs/research/infrastructure-versions-verification.md` - Source of record for chart names/versions now that the Flux YAMLs were removed (2025-11-14 cleanup)

### Docker Compose File (Local Development)

- **FILE-021**: `docker-compose.yml` (to be created) - Local dev infrastructure (MinIO, Redis 6.2.14, RabbitMQ 4.2, SQL Server 2022)

## Testing

### Pre-Upgrade Testing

- **TEST-001**: Kubernetes Version Verification
  - **Purpose**: Confirm Kubernetes 1.30+ on all clusters
  - **Success Criteria**: `kubectl version` shows server 1.30+

### Dapr Upgrade Testing

- **TEST-002**: Dapr Sidecar Injection
  - **Purpose**: Verify Dapr sidecars running in all pods
  - **Success Criteria**: All service pods have `daprd` container

- **TEST-003**: Dapr Service Invocation
  - **Purpose**: Test service-to-service calls via Dapr
  - **Success Criteria**: `curl http://localhost:3500/v1.0/invoke/order-service/method/healthz` returns 200

- **TEST-004**: Dapr Pub/Sub
  - **Purpose**: Test message publishing and subscription
  - **Success Criteria**: OrderService publishes `OrderSummary`, MakeLineService/LoyaltyService receive messages

### State Store Testing

- **TEST-005**: State Write/Read/Delete
  - **Purpose**: Test state operations on cloud-native stores
  - **Success Criteria**: State persists in Cosmos DB/DynamoDB/Firestore, retrieved correctly

- **TEST-006**: State Transactions
  - **Purpose**: Test multi-item transactional state operations
  - **Success Criteria**: MakeLineService queue transactions succeed

### Object Storage Testing

- **TEST-007**: Object Create/Get/Delete
  - **Purpose**: Test receipt storage operations
  - **Success Criteria**: ReceiptGenerationService stores receipts in MinIO/S3/Blob/GCS

### Infrastructure Testing

- **TEST-008**: SQL Server Connectivity
  - **Purpose**: Test AccountingService database access
  - **Success Criteria**: AccountingService queries succeed

- **TEST-009**: RabbitMQ Pub/Sub
  - **Purpose**: Test message broker functionality
  - **Success Criteria**: Messages flow through `orders` topic

- **TEST-010**: Nginx Ingress
  - **Purpose**: Test external HTTP routing
  - **Success Criteria**: External requests route to services correctly

- **TEST-011**: Cert-Manager TLS
  - **Purpose**: Test certificate issuance and renewal
  - **Success Criteria**: HTTPS endpoints use valid Let's Encrypt certificates

### Integration Testing

- **TEST-012**: End-to-End Order Flow
  - **Purpose**: Test complete order lifecycle
  - **Success Criteria**: Order placed → processed → completed → receipt generated → accounting updated

- **TEST-013**: Load Testing
  - **Purpose**: Test system under concurrent load
  - **Success Criteria**: 100 concurrent orders processed without errors

### Performance Testing

- **TEST-014**: Dapr Latency
  - **Purpose**: Measure Dapr sidecar overhead
  - **Success Criteria**: P95 latency < 5ms for service invocation

- **TEST-015**: State Store Latency

## Deliverables

- **Source**: Updated Helm charts, Dapr component manifests, and local Docker / Helm scripts.
- **Documentation**: Updated runbooks, local dev README, upgrade instructions, and migration checklists.
- **Manifests**: Cloud-specific Helm `values` files, new Dapr components for state and bindings using Workload Identity.
- **Evidence**: Smoke test logs, integration test artifacts, and monitoring dashboards for the new versions.

## Acceptance Criteria

- All TASK-##### items are marked complete with reviewing PR links and verification evidence.
- All tests (TEST-001 → TEST-015) pass in staging before production rollout.
- No data loss during state migration; backups and rehydration tested successfully.
- CI/CD pipelines validate Dapr invocation header compliance and Workload Identity integration.
  - **Purpose**: Measure cloud-native state store performance
  - **Success Criteria**: P95 latency < 50ms for state read/write

## Risks & Assumptions

### Critical Risks

- **RISK-001**: **Dapr 1.16 Redis Incompatibility**
  - **Likelihood**: High (confirmed)
  - **Impact**: High (breaks MakeLineService, LoyaltyService)
  - **Mitigation**: Use cloud-native state stores (Cosmos DB, DynamoDB, Firestore), Redis 6.2.14 local only

- **RISK-002**: **State Data Loss During Migration**
  - **Likelihood**: Medium
  - **Impact**: Critical (lose order queue, loyalty points)
  - **Mitigation**: Export state before migration, import to new stores, validate data integrity

- **RISK-003**: **Dapr Service Invocation Breaking Change**
  - **Likelihood**: High (confirmed in research)
  - **Impact**: Medium (service-to-service calls fail)
  - **Mitigation**: Add explicit `Content-Type: application/json` headers to all invocations (Dapr 1.9+ requirement)

- **RISK-004**: **KEDA Helm CRD Conflicts**
  - **Likelihood**: Medium (confirmed in research)
  - **Impact**: High (upgrade fails with "resource already exists")
  - **Mitigation**: Patch CRDs with Helm ownership metadata before upgrade

### Medium Risks

- **RISK-005**: **RabbitMQ 4.2 Compatibility**
  - **Likelihood**: Low
  - **Impact**: Medium (pub/sub messaging breaks)
  - **Mitigation**: Research confirms Dapr 1.16 supports RabbitMQ 4.2 (AMQP 0.9.1)

- **RISK-006**: **Workload Identity Misconfiguration**
  - **Likelihood**: Medium
  - **Impact**: Medium (cloud services authentication fails)
  - **Mitigation**: Test Workload Identity in staging, document configuration steps

- **RISK-007**: **Cert-Manager Certificate Renewal Failure**
  - **Likelihood**: Low
  - **Impact**: Medium (HTTPS access fails)
  - **Mitigation**: Monitor cert-manager logs, test renewal process before expiration

### Low Risks

- **RISK-008**: **Production Downtime Exceeds Maintenance Window**
  - **Likelihood**: Low
  - **Impact**: Medium (extended downtime)
  - **Mitigation**: Test all upgrades in staging, prepare rollback plan

- **RISK-009**: **Performance Degradation After Upgrade**
  - **Likelihood**: Low
  - **Impact**: Medium (slower response times)
  - **Mitigation**: Baseline performance metrics before upgrade, monitor P95 latency

- **RISK-010**: **Incompatibility with Future .NET 10 Services**
  - **Likelihood**: Low
  - **Impact**: Low (Dapr HTTP API workaround adds complexity)
  - **Mitigation**: Monitor Dapr SDK .NET 10 support, migrate when available

### Assumptions

- **ASSUMPTION-001**: Kubernetes 1.30+ is available on AKS, EKS, GKE (verified 2025-11-09)
- **ASSUMPTION-002**: Dapr 1.16.2 is the latest stable release (verified 2025-11-09)
- **ASSUMPTION-003**: KEDA 2.18.1 is the latest stable release (verified 2025-11-09)
- **ASSUMPTION-004**: Cert-Manager 1.19 is compatible with Kubernetes 1.30+ (verified in research)
- **ASSUMPTION-005**: Cloud-native state stores (Cosmos DB, DynamoDB, Firestore) support Dapr state API (verified in Dapr docs)
- **ASSUMPTION-006**: Azure Blob Storage, AWS S3, GCP Cloud Storage support Dapr bindings (verified in Dapr docs)
- **ASSUMPTION-007**: Let's Encrypt rate limits won't block certificate issuance (50 certs/domain/week)
- **ASSUMPTION-008**: Existing state data volume is small enough to migrate within maintenance window (<1GB)
- **ASSUMPTION-009**: No breaking changes in RabbitMQ 4.2 AMQP 0.9.1 protocol (verified in research)
- **ASSUMPTION-010**: KEDA is not actively used (verified via audit: zero ScaledObjects found)

## Related Specifications / Further Reading

### Architectural Decision Records

- [ADR-0001: .NET 10 LTS Adoption](../docs/adr/adr-0001-dotnet10-lts-adoption.md)
- [ADR-0002: Cloud-Agnostic Configuration via Dapr Abstraction](../docs/adr/adr-0002-cloud-agnostic-configuration-via-dapr.md)
- [ADR-0003: Ubuntu 24.04 Base Image Standardization](../docs/adr/adr-0003-ubuntu-2404-base-image-standardization.md)
- [ADR-0007: Cloud-Agnostic Deployment Strategy](../docs/adr/adr-0007-cloud-agnostic-deployment-strategy.md)

### Research Documents

- [Dapr Upgrade Breaking Changes (1.3 → 1.16)](../docs/research/dapr-upgrade-breaking-changes-1.3-to-1.16.md)
- [Dapr 1.16 Upgrade Executive Summary](../docs/research/dapr-1.16-upgrade-executive-summary.md)
- [KEDA Upgrade Analysis (2.2 → 2.18)](../docs/research/keda-upgrade-analysis.md)
- [KEDA Upgrade Summary](../docs/research/keda-upgrade-summary.md)
- [Infrastructure Versions Verification](../docs/research/infrastructure-versions-verification.md)

### Implementation Plans (This Phase)

- [Dapr 1.16 Upgrade Implementation](./upgrade-dapr-1.16-implementation-1.md)
- [KEDA 2.18 Upgrade Implementation](./upgrade-keda-2.18-implementation-1.md)
- [Cert-Manager 1.19 Upgrade Implementation](./upgrade-certmanager-1.19-implementation-1.md)
- [Infrastructure Containers Upgrade Implementation](./upgrade-infrastructure-containers-implementation-1.md)
- [Cloud-Native State Stores Migration](./migrate-state-stores-cloud-native-implementation-1.md)
- [Cloud-Agnostic Object Storage Migration](./migrate-object-storage-cloud-agnostic-implementation-1.md)

### Session Logs

- [Session: Infrastructure Container Upgrade Strategy](./.claude/sessions/2025-11-09-1018-infrastructure-upgrade-strategy.md)

### External Documentation

- [Dapr 1.16 Release Notes](https://blog.dapr.io/posts/2025/09/16/dapr-v1.16-is-now-available/)
- [KEDA 2.18 Release Notes](https://github.com/kedacore/keda/releases/tag/v2.18.0)
- [Cert-Manager Documentation](https://cert-manager.io/docs/)
- [Kubernetes 1.30 Release Notes](https://kubernetes.io/blog/2024/04/17/kubernetes-v1-30-release/)
> **Update (2025-11-16):** The cert-manager upgrade plan now details a cloud-only rollout (no local cert-manager) – see `plan/upgrade-certmanager-1.19-implementation-1.md` for phase-by-phase steps before executing TASK-203/TASK-206.
