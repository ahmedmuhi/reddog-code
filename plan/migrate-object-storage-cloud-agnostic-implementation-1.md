---
goal: "Migrate Object Storage to Cloud-Agnostic Strategy (MinIO Local, S3/Blob/GCS Cloud)"
version: 1.0
date_created: 2025-11-09
last_updated: 2025-11-09
owner: "Red Dog Modernization Team"
status: 'Planned'
tags: [infrastructure, migration, phase-0, storage, dapr, minio, s3, blob, gcs]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

This plan migrates object storage from Azure Blob (cloud-specific) to a cloud-agnostic strategy: MinIO for local development, and cloud-native storage (S3, Blob, GCS) for production deployments.

**Critical Rationale:**
- Current implementation uses Azure Blob Storage (contradicts ADR-0007 cloud-agnostic mandate)
- Azure Blob does NOT support S3 API natively
- Solution: Use cloud-native Dapr bindings per environment (code unchanged)

**Duration**: 3-4 days (within Phase 0)

## 1. Requirements & Constraints

### Functional Requirements

- **REQ-001**: ReceiptGenerationService stores receipts in object storage
- **REQ-002**: Local development uses MinIO (S3-compatible) via `bindings.aws.s3`
- **REQ-003**: AWS deployments use S3 via `bindings.aws.s3` with IRSA
- **REQ-004**: Azure deployments use Blob Storage via `bindings.azure.blobstorage` with Workload Identity
- **REQ-005**: GCP deployments use Cloud Storage via `bindings.gcp.bucket` with Workload Identity

### Security Requirements

- **SEC-001**: Azure uses Workload Identity (no storage account keys in manifests)
- **SEC-002**: AWS uses IRSA (no access keys in manifests)
- **SEC-003**: GCP uses Workload Identity (no service account keys in manifests)

### Constraints

- **CON-001**: Azure Blob Storage does NOT support S3 API natively
- **CON-002**: Different Dapr bindings required per cloud (cloud-specific component YAMLs)
- **CON-003**: Application code unchanged (Dapr binding API abstraction)

## 2. Implementation Steps

### Implementation Phase 1: MinIO Setup for Local Dev (Day 1)

- **GOAL-001**: Deploy MinIO in Docker Compose for local development

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-101 | Create `docker-compose.yml` with MinIO service (ports 9000, 9001) | | |
| TASK-102 | Configure MinIO with `MINIO_ROOT_USER=minioadmin`, `MINIO_ROOT_PASSWORD=minioadmin` | | |
| TASK-103 | Create auto-initialization script to create `receipts` bucket on startup | | |
| TASK-104 | Test MinIO access via browser UI (http://localhost:9001) | | |
| TASK-105 | Create Dapr component `reddog.binding.receipt.yaml` for MinIO (S3 binding with `endpoint: http://localhost:9000`) | | |

### Implementation Phase 2: AWS S3 Binding (Day 2)

- **GOAL-002**: Create S3 binding for AWS deployments

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-201 | Create S3 bucket `reddog-receipts-<env>` in AWS | | |
| TASK-202 | Create IAM role with S3 read/write policy | | |
| TASK-203 | Create `manifests/overlays/aws/reddog.binding.s3.yaml` (IRSA authentication, no access keys) | | |
| TASK-204 | Add `eks.amazonaws.com/role-arn` annotation to ServiceAccount | | |
| TASK-205 | Test receipt creation via Dapr binding | | |

### Implementation Phase 3: Azure Blob Binding (Day 2-3)

- **GOAL-003**: Create Azure Blob binding

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-301 | Create Azure Storage Account and `receipts` container | | |
| TASK-302 | Create Managed Identity and grant Storage Blob Data Contributor role | | |
| TASK-303 | Create `manifests/overlays/azure/reddog.binding.blob.yaml` (Workload Identity, no account keys) | | |
| TASK-304 | Add `azure.workload.identity/client-id` annotation to ServiceAccount | | |
| TASK-305 | Test receipt creation via Dapr binding | | |

### Implementation Phase 4: GCP Cloud Storage Binding (Day 3)

- **GOAL-004**: Create GCS binding

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-401 | Create GCS bucket `reddog-receipts-<project-id>` | | |
| TASK-402 | Grant Storage Object Admin role to Kubernetes ServiceAccount | | |
| TASK-403 | Create `manifests/overlays/gcp/reddog.binding.gcs.yaml` (Workload Identity, no keys) | | |
| TASK-404 | Add `iam.gke.io/gcp-service-account` annotation to ServiceAccount | | |
| TASK-405 | Test receipt creation via Dapr binding | | |

### Implementation Phase 5: Data Migration and Testing (Day 4)

- **GOAL-005**: Migrate existing receipts and validate functionality

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-501 | Export existing receipts from Azure Blob (if any) | | |
| TASK-502 | Import receipts to new storage (MinIO local, S3/Blob/GCS cloud) | | |
| TASK-503 | Test ReceiptGenerationService receipt creation | | |
| TASK-504 | Test receipt retrieval (GET operation) | | |
| TASK-505 | Test receipt deletion (DELETE operation) | | |
| TASK-506 | Load test with 100 concurrent receipt operations | | |

## 3. Alternatives

- **ALT-001**: **Use S3Proxy Gateway for Azure** - Rejected: Adds infrastructure complexity, maintenance overhead
- **ALT-002**: **Deploy MinIO in Production** - Rejected: Cloud-native storage reduces ops complexity for teaching demos
- **ALT-003**: **Unified S3 API Across All Clouds** - Rejected: Azure Blob doesn't support S3 API natively

## 4. Dependencies

- **DEP-001**: Dapr 1.16.2 installed (supports S3, Blob, GCS bindings)
- **DEP-002**: MinIO for local dev (Docker Compose)
- **DEP-003**: AWS S3, Azure Blob Storage, GCP Cloud Storage accounts
- **DEP-004**: Workload Identity configured on clusters

## 5. Files

### Dapr Component Files

- **FILE-001**: `manifests/branch/base/components/reddog.binding.receipt.yaml` (MinIO for local dev)
- **FILE-002**: `manifests/overlays/aws/reddog.binding.s3.yaml` (AWS S3)
- **FILE-003**: `manifests/overlays/azure/reddog.binding.blob.yaml` (Azure Blob)
- **FILE-004**: `manifests/overlays/gcp/reddog.binding.gcs.yaml` (GCP Cloud Storage)

### Docker Compose File

- **FILE-005**: `docker-compose.yml` (MinIO service definition)

### Service Deployment Files (No Changes Required)

- **FILE-006**: `manifests/branch/base/deployments/receipt-generation-service.yaml` (unchanged - Dapr abstraction)

## 6. Testing

- **TEST-001**: Object Create Test - Store receipt, verify uploaded
- **TEST-002**: Object Get Test - Retrieve stored receipt, verify content
- **TEST-003**: Object Delete Test - Delete receipt, verify removal
- **TEST-004**: List Objects Test - List all receipts in bucket/container
- **TEST-005**: Load Test - 100 concurrent uploads, verify success

## 7. Risks & Assumptions

- **RISK-001**: **Receipt Data Loss During Migration** - **Mitigation**: Export receipts before migration, verify import
- **RISK-002**: **Workload Identity Misconfiguration** - **Mitigation**: Test in staging, document configuration steps
- **ASSUMPTION-001**: Existing receipt data volume is small (<10GB)
- **ASSUMPTION-002**: Dapr bindings support all required operations (create, get, delete, list)

## 8. Related Specifications / Further Reading

- [Dapr Output Bindings](https://docs.dapr.io/developing-applications/building-blocks/bindings/)
- [Dapr S3 Binding](https://docs.dapr.io/reference/components-reference/supported-bindings/s3/)
- [Dapr Azure Blob Binding](https://docs.dapr.io/reference/components-reference/supported-bindings/blobstorage/)
- [Dapr GCP Bucket Binding](https://docs.dapr.io/reference/components-reference/supported-bindings/gcpbucket/)
- [MinIO Documentation](https://min.io/docs/minio/kubernetes/)
- [Phase 0: Platform Foundation](./upgrade-phase0-platform-foundation-implementation-1.md)
- [Dapr 1.16 Upgrade](./upgrade-dapr-1.16-implementation-1.md)
