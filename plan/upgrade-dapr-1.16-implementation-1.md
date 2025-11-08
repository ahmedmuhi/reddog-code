---
goal: "Upgrade Dapr from 1.3.0 to 1.16.2 for .NET 10 and Modern Platform Support"
version: 1.0
date_created: 2025-11-09
last_updated: 2025-11-09
owner: "Red Dog Modernization Team"
status: 'Planned'
tags: [infrastructure, upgrade, phase-0, dapr, runtime, sidecar]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

This plan upgrades Dapr (Distributed Application Runtime) from version 1.3.0 (released 2021) to 1.16.2 (released 2024), enabling Red Dog services to run on .NET 10 and leverage modern Dapr features including Workload Identity Federation, OpenTelemetry integration, and the Configuration API.

**Critical Context:**
- Dapr 1.3.0 is 3+ years outdated and incompatible with modern .NET features
- All Red Dog services run Dapr sidecars (blocking dependency for Phase 1)
- Dapr .NET SDK 1.16.2 does NOT support .NET 10 yet - must use HTTP/gRPC APIs
- Direct upgrade 1.3.0 → 1.16.2 is supported (no incremental steps required)

**Duration**: 1 week (within Phase 0)
**Risk Level**: Medium (7 breaking changes identified)

## 1. Requirements & Constraints

### Functional Requirements

- **REQ-001**: Dapr 1.16.2 runtime must be installed in all target Kubernetes clusters
- **REQ-002**: All services must use Dapr HTTP/gRPC APIs (not .NET SDK) until SDK adds .NET 10 support
- **REQ-003**: Service-to-service invocations must include explicit `Content-Type: application/json` header
- **REQ-004**: Dapr component YAML files must be updated for any spec changes
- **REQ-005**: Dapr sidecar annotations must be verified on all deployments
- **REQ-006**: mTLS must be enabled for service-to-service communication
- **REQ-007**: Workload Identity Federation must be configured for cloud deployments

### Technical Requirements

- **REQ-008**: Kubernetes 1.30+ required (verified available on AKS, EKS, GKE)
- **REQ-009**: Helm 3.x for Dapr chart upgrade
- **REQ-010**: Dapr CLI 1.16 for local development
- **REQ-011**: RabbitMQ 4.2.0 compatibility verified (AMQP 0.9.1 support)
- **REQ-012**: Redis 6.2.14 for local dev (cloud uses Cosmos DB, DynamoDB, Firestore)

### Security Requirements

- **SEC-001**: Workload Identity Federation for Azure (no client secrets)
- **SEC-002**: IAM Roles for Service Accounts (IRSA) for AWS
- **SEC-003**: Workload Identity for GCP
- **SEC-004**: Dapr mTLS enabled for encrypted service communication
- **SEC-005**: Sentry service configured for certificate management

### Constraints

- **CON-001**: Dapr .NET SDK 1.16.2 does NOT support .NET 10 - workaround: HTTP/gRPC APIs
- **CON-002**: Dapr 1.16 does NOT support Redis 7/8 (only 6.x) - use cloud-native state stores
- **CON-003**: Default `Content-Type: application/json` header auto-injection removed in Dapr 1.9 - must add explicitly
- **CON-004**: Component names must be unique across all namespaces (Dapr 1.13+ requirement)
- **CON-005**: Dapr runtime upgrade must complete before any service code changes

### Patterns to Follow

- **PAT-001**: Use Dapr HTTP API for service invocation: `http://localhost:3500/v1.0/invoke/<app-id>/method/<method>`
- **PAT-002**: Use Dapr state API: `http://localhost:3500/v1.0/state/<store-name>`
- **PAT-003**: Use Dapr pub/sub API: `http://localhost:3500/v1.0/publish/<pubsub-name>/<topic>`
- **PAT-004**: Use Dapr bindings API: `http://localhost:3500/v1.0/bindings/<binding-name>`
- **PAT-005**: Use Dapr secrets API: `http://localhost:3500/v1.0/secrets/<store-name>/<secret-name>`
- **PAT-006**: Use Dapr configuration API: `http://localhost:3500/v1.0-alpha1/configuration/<store-name>`

## 2. Implementation Steps

### Implementation Phase 1: Pre-Upgrade Preparation (Days 1-2)

- **GOAL-001**: Backup current Dapr configuration and validate prerequisites

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-101 | Backup all Dapr component YAML files from `manifests/branch/base/components/` | | |
| TASK-102 | Document current Dapr sidecar annotations on all deployments | | |
| TASK-103 | Export Dapr runtime version: `kubectl get deploy -n dapr-system dapr-operator -o yaml` | | |
| TASK-104 | Verify Kubernetes version is 1.30+ on all clusters | | |
| TASK-105 | Install Dapr CLI 1.16: `curl -fsSL https://raw.githubusercontent.com/dapr/cli/master/install/install.sh \| /bin/bash -s 1.16.0` | | |
| TASK-106 | Audit all service-to-service invocation code for missing `Content-Type` headers | | |
| TASK-107 | Verify component name uniqueness across namespaces | | |
| TASK-108 | Create staging environment for testing | | |

### Implementation Phase 2: Helm Chart Upgrade (Days 2-3)

- **GOAL-002**: Upgrade Dapr runtime via Helm chart

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-201 | Add Dapr Helm repository: `helm repo add dapr https://dapr.github.io/helm-charts/ && helm repo update` | | |
| TASK-202 | Review Dapr 1.16 Helm values for breaking changes | | |
| TASK-203 | Execute Helm upgrade: `helm upgrade dapr dapr/dapr --namespace dapr-system --version 1.16.2 --wait` | | |
| TASK-204 | Verify Dapr control plane pods are running: `kubectl get pods -n dapr-system` | | |
| TASK-205 | Check Dapr operator version: `kubectl get deploy -n dapr-system dapr-operator -o jsonpath='{.spec.template.spec.containers[0].image}'` | | |
| TASK-206 | Verify Dapr sidecar injector is healthy | | |
| TASK-207 | Check Dapr placement service for state store support | | |
| TASK-208 | Verify Dapr Sentry service for mTLS certificates | | |

### Implementation Phase 3: Component YAML Updates (Days 3-4)

- **GOAL-003**: Update Dapr component specifications for compatibility

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-301 | Update `reddog.pubsub.yaml` for RabbitMQ 4.2 (verify metadata fields) | | |
| TASK-302 | Update `reddog.state.makeline.yaml` for Redis 6.2.14 (local dev) | | |
| TASK-303 | Update `reddog.state.loyalty.yaml` for Redis 6.2.14 (local dev) | | |
| TASK-304 | Create `reddog.state.cosmosdb.yaml` for Azure (replaces Redis in production) | | |
| TASK-305 | Create `reddog.state.dynamodb.yaml` for AWS (replaces Redis in production) | | |
| TASK-306 | Create `reddog.state.firestore.yaml` for GCP (replaces Redis in production) | | |
| TASK-307 | Update `reddog.binding.receipt.yaml` for object storage (MinIO local, S3/Blob/GCS cloud) | | |
| TASK-308 | Update `reddog.secretstore.yaml` for Workload Identity (Azure, AWS, GCP) | | |
| TASK-309 | Create `reddog.configuration.yaml` for Configuration API (ADR-0004) | | |
| TASK-310 | Apply updated component YAMLs: `kubectl apply -f manifests/branch/base/components/` | | |

### Implementation Phase 4: Service Deployment Updates (Days 4-5)

- **GOAL-004**: Update Dapr sidecar annotations on service deployments

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-401 | Verify `dapr.io/enabled: "true"` annotation on all deployments | | |
| TASK-402 | Verify `dapr.io/app-id` matches service name | | |
| TASK-403 | Verify `dapr.io/app-port` matches service HTTP port | | |
| TASK-404 | Add `dapr.io/enable-metrics: "true"` for Prometheus scraping | | |
| TASK-405 | Add `dapr.io/metrics-port: "9090"` for metrics endpoint | | |
| TASK-406 | Configure `dapr.io/log-level: "info"` (production) or `"debug"` (staging) | | |
| TASK-407 | Restart all service pods to inject Dapr 1.16 sidecars: `kubectl rollout restart deployment -n reddog` | | |
| TASK-408 | Verify all pods have `daprd` sidecar container | | |

### Implementation Phase 5: Service Code Changes (Days 5-6)

- **GOAL-005**: Update service-to-service invocation code for Dapr 1.9+ breaking change

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-501 | Identify all `HttpClient.PostAsync()` calls to Dapr service invocation endpoints | | |
| TASK-502 | Add `Content-Type: application/json` header to all POST/PUT requests | | |
| TASK-503 | Example fix: `httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json")` | | |
| TASK-504 | Update OrderService service invocations | | |
| TASK-505 | Update MakeLineService service invocations | | |
| TASK-506 | Update LoyaltyService service invocations | | |
| TASK-507 | Update AccountingService service invocations | | |
| TASK-508 | Update VirtualWorker service invocations | | |
| TASK-509 | Rebuild and redeploy all services with code changes | | |

### Implementation Phase 6: Workload Identity Configuration (Days 6-7)

- **GOAL-006**: Configure Workload Identity Federation for cloud deployments

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-601 | Azure: Create Managed Identity and federate with ServiceAccount | | |
| TASK-602 | Azure: Add `azure.workload.identity/client-id` annotation to ServiceAccount | | |
| TASK-603 | Azure: Update secret store component with `azureClientId` metadata | | |
| TASK-604 | AWS: Create IAM role and trust policy for IRSA | | |
| TASK-605 | AWS: Add `eks.amazonaws.com/role-arn` annotation to ServiceAccount | | |
| TASK-606 | AWS: Update S3 binding component to use IRSA (omit accessKey/secretKey) | | |
| TASK-607 | GCP: Create Service Account and Workload Identity binding | | |
| TASK-608 | GCP: Add `iam.gke.io/gcp-service-account` annotation to ServiceAccount | | |
| TASK-609 | GCP: Update GCS binding component to use Workload Identity | | |

### Implementation Phase 7: Testing and Validation (Days 7)

- **GOAL-007**: Validate Dapr 1.16 functionality

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-701 | Test Dapr health endpoint: `curl http://localhost:3500/v1.0/healthz` | | |
| TASK-702 | Test service invocation: `curl http://localhost:3500/v1.0/invoke/order-service/method/healthz` | | |
| TASK-703 | Test pub/sub publish: POST to `/v1.0/publish/reddog.pubsub/orders` | | |
| TASK-704 | Test state store read/write: POST to `/v1.0/state/reddog.state.makeline` | | |
| TASK-705 | Test output binding: POST to `/v1.0/bindings/reddog.binding.receipt` | | |
| TASK-706 | Test secret retrieval: GET `/v1.0/secrets/reddog.secretstore/sqlconnection` | | |
| TASK-707 | Test configuration retrieval: GET `/v1.0-alpha1/configuration/reddog.configuration` (if implemented) | | |
| TASK-708 | Validate Dapr mTLS certificates: `kubectl get pods -n reddog -o jsonpath='{.items[0].spec.volumes[?(@.name=="dapr-identity-token")].projected}'` | | |
| TASK-709 | Run integration tests (end-to-end order flow) | | |
| TASK-710 | Monitor Dapr metrics in Prometheus: `dapr_runtime_service_invocation_*` | | |

## 3. Alternatives

- **ALT-001**: **Wait for Dapr .NET SDK .NET 10 Support**
  - **Rejected**: Unknown timeline blocks modernization. HTTP/gRPC API workaround is viable.

- **ALT-002**: **Incremental Upgrade (1.3 → 1.6 → 1.10 → 1.16)**
  - **Rejected**: Research confirms direct upgrade is supported, incremental adds unnecessary complexity and risk.

- **ALT-003**: **Skip Dapr Upgrade, Keep 1.3.0**
  - **Rejected**: Dapr 1.3.0 lacks critical features (Workload Identity, Configuration API) and is incompatible with modern .NET.

- **ALT-004**: **Migrate to .NET 9 Instead of .NET 10**
  - **Rejected**: Dapr SDK supports .NET 9, but .NET 10 is the target LTS version per ADR-0001. HTTP API workaround is acceptable interim solution.

## 4. Dependencies

### Infrastructure Dependencies

- **DEP-001**: Kubernetes 1.30+ clusters (AKS, EKS, GKE) - verified 2025-11-09
- **DEP-002**: Helm 3.x installed
- **DEP-003**: kubectl access to target clusters
- **DEP-004**: Dapr Helm repository: `https://dapr.github.io/helm-charts/`

### Component Dependencies

- **DEP-005**: RabbitMQ 4.2.0-management (upgraded separately)
- **DEP-006**: Redis 6.2.14 (local dev only, cloud uses Cosmos DB/DynamoDB/Firestore)
- **DEP-007**: Azure Cosmos DB (Azure deployments)
- **DEP-008**: AWS DynamoDB (AWS deployments)
- **DEP-009**: GCP Cloud Firestore (GCP deployments)
- **DEP-010**: MinIO (local dev object storage)
- **DEP-011**: AWS S3, Azure Blob Storage, GCP Cloud Storage (cloud object storage)

### Research Dependencies

- **DEP-012**: `docs/research/dapr-upgrade-breaking-changes-1.3-to-1.16.md`
- **DEP-013**: `docs/research/dapr-1.16-upgrade-executive-summary.md`
- **DEP-014**: ADR-0002 (Cloud-Agnostic Configuration via Dapr Abstraction)

## 5. Files

### Dapr Component Files (Update Required)

- **FILE-001**: `manifests/branch/base/components/reddog.pubsub.yaml` (RabbitMQ pub/sub)
- **FILE-002**: `manifests/branch/base/components/reddog.state.makeline.yaml` (State store - MakeLineService)
- **FILE-003**: `manifests/branch/base/components/reddog.state.loyalty.yaml` (State store - LoyaltyService)
- **FILE-004**: `manifests/branch/base/components/reddog.binding.receipt.yaml` (Object storage binding)
- **FILE-005**: `manifests/branch/base/components/reddog.secretstore.yaml` (Secret store)
- **FILE-006**: `manifests/branch/base/components/reddog.configuration.yaml` (NEW - Configuration API)

### Cloud-Specific Component Files (New)

- **FILE-007**: `manifests/overlays/azure/reddog.state.cosmosdb.yaml` (Azure state store)
- **FILE-008**: `manifests/overlays/azure/reddog.binding.blob.yaml` (Azure Blob binding)
- **FILE-009**: `manifests/overlays/aws/reddog.state.dynamodb.yaml` (AWS state store)
- **FILE-010**: `manifests/overlays/aws/reddog.binding.s3.yaml` (AWS S3 binding)
- **FILE-011**: `manifests/overlays/gcp/reddog.state.firestore.yaml` (GCP state store)
- **FILE-012**: `manifests/overlays/gcp/reddog.binding.gcs.yaml` (GCP Cloud Storage binding)

### Service Deployment Files (Annotation Updates)

- **FILE-013**: `manifests/branch/base/deployments/order-service.yaml`
- **FILE-014**: `manifests/branch/base/deployments/make-line-service.yaml`
- **FILE-015**: `manifests/branch/base/deployments/loyalty-service.yaml`
- **FILE-016**: `manifests/branch/base/deployments/accounting-service.yaml`
- **FILE-017**: `manifests/branch/base/deployments/receipt-generation-service.yaml`
- **FILE-018**: `manifests/branch/base/deployments/virtual-worker.yaml`
- **FILE-019**: `manifests/branch/base/deployments/virtual-customers.yaml`

### Service Code Files (Content-Type Header Fix)

- **FILE-020**: `RedDog.OrderService/Program.cs` (or relevant service invocation code)
- **FILE-021**: `RedDog.MakeLineService/Program.cs`
- **FILE-022**: `RedDog.LoyaltyService/Program.cs`
- **FILE-023**: `RedDog.AccountingService/Program.cs`
- **FILE-024**: `RedDog.VirtualWorker/Program.cs`

## 6. Testing

### Smoke Tests (Post-Upgrade)

- **TEST-001**: Dapr Health Check
  - **Purpose**: Verify Dapr runtime is healthy
  - **Command**: `curl http://localhost:3500/v1.0/healthz`
  - **Success Criteria**: Returns `200 OK`

- **TEST-002**: Service Invocation
  - **Purpose**: Test service-to-service calls via Dapr
  - **Command**: `curl http://localhost:3500/v1.0/invoke/order-service/method/healthz`
  - **Success Criteria**: Returns `200 OK`, service responds

### Functional Tests

- **TEST-003**: Pub/Sub Publish
  - **Purpose**: Test message publishing to RabbitMQ
  - **Command**: `POST /v1.0/publish/reddog.pubsub/orders` with OrderSummary JSON
  - **Success Criteria**: Message published successfully, subscribers receive it

- **TEST-004**: State Store Read/Write
  - **Purpose**: Test state persistence
  - **Command**: `POST /v1.0/state/reddog.state.makeline` with key-value pair, then `GET /v1.0/state/reddog.state.makeline/key`
  - **Success Criteria**: Value persists and is retrieved correctly

- **TEST-005**: Output Binding
  - **Purpose**: Test receipt storage via binding
  - **Command**: `POST /v1.0/bindings/reddog.binding.receipt` with receipt data
  - **Success Criteria**: Receipt stored in MinIO/S3/Blob/GCS

- **TEST-006**: Secret Retrieval
  - **Purpose**: Test secret access from secret store
  - **Command**: `GET /v1.0/secrets/reddog.secretstore/sqlconnection`
  - **Success Criteria**: Secret retrieved successfully

### Integration Tests

- **TEST-007**: End-to-End Order Flow
  - **Purpose**: Validate complete order lifecycle
  - **Steps**: VirtualCustomers → OrderService → pub/sub → MakeLineService → VirtualWorker
  - **Success Criteria**: Order created, processed, completed without errors

- **TEST-008**: Loyalty Points Accrual
  - **Purpose**: Test LoyaltyService state operations
  - **Steps**: Place order, verify loyalty points updated in state store
  - **Success Criteria**: Points persist in Cosmos DB/DynamoDB/Firestore

### Performance Tests

- **TEST-009**: Dapr Latency Measurement
  - **Purpose**: Measure Dapr sidecar overhead
  - **Tool**: Prometheus metrics `dapr_runtime_service_invocation_req_sent_total`
  - **Success Criteria**: P95 latency < 5ms for service invocation

- **TEST-010**: Load Test (100 Concurrent Orders)
  - **Purpose**: Validate performance under load
  - **Tool**: k6 or Apache Bench
  - **Success Criteria**: No failures, P95 latency < 500ms end-to-end

## 7. Risks & Assumptions

### Critical Risks

- **RISK-001**: **Missing Content-Type Headers**
  - **Likelihood**: High (confirmed in research)
  - **Impact**: High (service invocations fail)
  - **Mitigation**: Audit all `HttpClient` calls, add `Content-Type: application/json` header

- **RISK-002**: **State Data Loss During Migration**
  - **Likelihood**: Medium
  - **Impact**: Critical (lose order queue, loyalty points)
  - **Mitigation**: Export state from Redis before migration, import to Cosmos DB/DynamoDB/Firestore

- **RISK-003**: **Component Name Collision**
  - **Likelihood**: Low
  - **Impact**: High (component initialization fails)
  - **Mitigation**: Verify component name uniqueness across all namespaces (Dapr 1.13+ requirement)

### Medium Risks

- **RISK-004**: **Workload Identity Misconfiguration**
  - **Likelihood**: Medium
  - **Impact**: Medium (cloud service authentication fails)
  - **Mitigation**: Test Workload Identity in staging, document configuration steps, use Dapr troubleshooting guides

- **RISK-005**: **mTLS Certificate Issues**
  - **Likelihood**: Low
  - **Impact**: Medium (service-to-service communication fails)
  - **Mitigation**: Verify Sentry service is healthy, check pod volumes for `dapr-identity-token`

### Assumptions

- **ASSUMPTION-001**: Dapr 1.16.2 is compatible with Kubernetes 1.30+ (verified in research)
- **ASSUMPTION-002**: RabbitMQ 4.2 is compatible with Dapr 1.16 `pubsub.rabbitmq` component (verified in research)
- **ASSUMPTION-003**: Dapr HTTP API is stable (v1.0 endpoint format unchanged)
- **ASSUMPTION-004**: Services can migrate from Dapr .NET SDK to HTTP API without major refactoring
- **ASSUMPTION-005**: Cloud-native state stores (Cosmos DB, DynamoDB, Firestore) fully support Dapr state API (verified in Dapr docs)
- **ASSUMPTION-006**: Existing Redis state data volume is small (<1GB) and can be migrated within maintenance window

## 8. Related Specifications / Further Reading

### Research Documents

- [Dapr Upgrade Breaking Changes (1.3 → 1.16)](../docs/research/dapr-upgrade-breaking-changes-1.3-to-1.16.md)
- [Dapr 1.16 Upgrade Executive Summary](../docs/research/dapr-1.16-upgrade-executive-summary.md)

### Architectural Decision Records

- [ADR-0002: Cloud-Agnostic Configuration via Dapr Abstraction](../docs/adr/adr-0002-cloud-agnostic-configuration-via-dapr.md)
- [ADR-0007: Cloud-Agnostic Deployment Strategy](../docs/adr/adr-0007-cloud-agnostic-deployment-strategy.md)

### External Documentation

- [Dapr 1.16 Release Notes](https://blog.dapr.io/posts/2025/09/16/dapr-v1.16-is-now-available/)
- [Dapr HTTP API Reference](https://docs.dapr.io/reference/api/)
- [Dapr Components Reference](https://docs.dapr.io/reference/components-reference/)
- [Dapr Workload Identity (Azure)](https://docs.dapr.io/operations/components/setup-secret-store/azure-keyvault-with-workload-identity/)
- [Dapr IRSA (AWS)](https://docs.dapr.io/reference/components-reference/supported-secret-stores/aws-secret-manager/)
- [Dapr Workload Identity (GCP)](https://docs.dapr.io/reference/components-reference/supported-secret-stores/gcp-secret-manager/)

### Related Implementation Plans

- [Phase 0: Platform Foundation](./upgrade-phase0-platform-foundation-implementation-1.md)
- [Cloud-Native State Stores Migration](./migrate-state-stores-cloud-native-implementation-1.md)
- [Cloud-Agnostic Object Storage Migration](./migrate-object-storage-cloud-agnostic-implementation-1.md)
