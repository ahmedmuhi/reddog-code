```yaml
goal: "Migrate State Stores from Redis to Cloud-Native Databases (Cosmos DB, DynamoDB, Firestore)"
version: 1.1
date_created: 2025-11-09
last_updated: 2025-11-17
owner: "Red Dog Modernization Team"
status: 'Planned'
tags: [infrastructure, migration, phase-0, state, dapr, cosmosdb, dynamodb, firestore, redis, sqlite]
```

# Introduction

This plan migrates Dapr state stores from Redis (incompatible with Dapr 1.16 + Redis 7/8) to cloud-native managed databases: Azure Cosmos DB (NoSQL), AWS DynamoDB, and GCP Cloud Firestore.

**Critical Rationale:**

  - Dapr 1.16 does NOT support Redis 7/8 (only 6.x)
  - Redis 6.2.14 is EOL (July 2024) - unsuitable for production
  - Cloud-native state stores eliminate version incompatibility
  - Application code unchanged (Dapr abstraction)

**Duration**: 3-4 days (within Phase 0)

## 1. Requirements & Constraints

### Functional Requirements

  - **REQ-001**: MakeLineService state (order queue) must migrate without data loss
  - **REQ-002**: LoyaltyService state (customer points) must migrate without data loss
  - **REQ-003**: **(Updated)** Local development uses **SQLite** via `state.sqlite` for zero-setup simplicity.
  - **REQ-004**: Azure deployments use Cosmos DB (NoSQL API) via `state.azure.cosmosdb`
  - **REQ-005**: AWS deployments use DynamoDB via `state.aws.dynamodb`
  - **REQ-006**: GCP deployments use Cloud Firestore via `state.gcp.firestore`

### Security Requirements

  - **SEC-001**: Azure uses Workload Identity (no connection strings in manifests)
  - **SEC-002**: AWS uses IRSA (IAM Roles for Service Accounts)
  - **SEC-003**: GCP uses Workload Identity

### Constraints

  - **CON-001**: Dapr 1.16 supports Redis 6.x only (not 7/8)
  - **CON-002**: State data must be exported/imported during migration (from original Redis)
  - **CON-003**: Application code unchanged (Dapr state API abstraction)

## 2. Implementation Steps

### Implementation Phase 1: Pre-Migration Data Export (Day 1)

  - **GOAL-001**: Export existing state data from the *current* Redis

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-101 | Export MakeLineService state: `dapr state list -a makeline-service --state-store reddog.state.makeline > makeline-state.json` | | |
| TASK-102 | Export LoyaltyService state: `dapr state list -a loyalty-service --state-store reddog.state.loyalty > loyalty-state.json` | | |
| TASK-103 | Verify exported data is valid JSON | | |
| TASK-104 | Backup Redis dump file as additional safety measure | | |

### Implementation Phase 2: Create Cloud-Native & Local State Store Components (Day 1-2)

  - **GOAL-002**: Create Dapr component YAML files for each cloud and local dev

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-201 | Create `manifests/overlays/azure/reddog.state.cosmosdb.yaml` (Cosmos DB component with Workload Identity) | | |
| TASK-202 | Create Azure Cosmos DB database and container (partition key: `partitionKey`) | | |
| TASK-203 | Create `manifests/overlays/aws/reddog.state.dynamodb.yaml` (DynamoDB component with IRSA) | | |
| TASK-204 | Create AWS DynamoDB table (partition key: `key`) | | |
| TASK-205 | Create `manifests/overlays/gcp/reddog.state.firestore.yaml` (Firestore component with Workload Identity) | | |
| TASK-206 | Create GCP Firestore database | | |
| TASK-207 | **(Updated)** Create `manifests/branch/base/components/reddog.state.sqlite.yaml` (**SQLite** component for local dev) | | |

### Implementation Phase 3: Configure Workload Identity (Day 2-3)

  - **GOAL-003**: Configure cloud authentication

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-301 | Azure: Create Managed Identity and grant Cosmos DB Data Contributor role | | |
| TASK-302 | Azure: Federate Managed Identity with Kubernetes ServiceAccount | | |
| TASK-303 | Azure: Add `azure.workload.identity/client-id` annotation to ServiceAccount | | |
| TASK-304 | AWS: Create IAM role with DynamoDB read/write policy | | |
| TASK-305 | AWS: Add `eks.amazonaws.com/role-arn` annotation to ServiceAccount | | |
| TASK-306 | GCP: Grant Firestore User role to Kubernetes ServiceAccount | | |
| TASK-307 | GCP: Add `iam.gke.io/gcp-service-account` annotation to ServiceAccount | | |

### Implementation Phase 4: Data Import (Day 3)

  - **GOAL-004**: Import state data to cloud-native stores

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-401 | Deploy updated Dapr components to staging environment | | |
| TASK-402 | Import MakeLineService state to Cosmos DB/DynamoDB/Firestore | | |
| TASK-403 | Import LoyaltyService state to cloud stores | | |
| TASK-404 | Verify imported data integrity (compare counts, sample values) | | |

### Implementation Phase 5: Testing (Day 3-4)

  - **GOAL-005**: Validate state operations on cloud-native stores

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-501 | Test MakeLineService state read/write (order queue operations) | | |
| TASK-502 | Test LoyaltyService state read/write (loyalty points) | | |
| TASK-503 | Test state delete operations | | |
| TASK-504 | Test state transactions (multi-item operations) | | |
| TASK-505 | Test state query API (Dapr 1.5+ feature) | | |
| TASK-506 | Load test with 100 concurrent state operations | | |

## 3. Alternatives

  - **ALT-001**: **Keep Redis 7/8** - Rejected: Dapr 1.16 incompatibility
  - **ALT-002**: **Use Redis 6.2.14 in Production** - Rejected: EOL, no security updates
  - **ALT-003**: **Wait for Dapr Redis 7/8 Support** - Rejected: Unknown timeline, blocks modernization

## 4. Dependencies

  - **DEP-001**: Dapr 1.16.2 installed
  - **DEP-002**: Azure Cosmos DB account, AWS DynamoDB access, GCP Firestore project
  - **DEP-003**: Workload Identity configured on clusters (from `Dapr Cloud Hardening` plan)

## 5. Files

### Dapr Component Files

  - **FILE-001**: **(Updated)** `manifests/branch/base/components/reddog.state.sqlite.yaml` (local dev)
  - **FILE-002**: `manifests/overlays/azure/reddog.state.cosmosdb.yaml` (Azure)
  - **FILE-003**: `manifests/overlays/aws/reddog.state.dynamodb.yaml` (AWS)
  - **FILE-004**: `manifests/overlays/gcp/reddog.state.firestore.yaml` (GCP)

### Service Deployment Files (No Changes Required)

  - **FILE-005**: `manifests/branch/base/deployments/make-line-service.yaml` (unchanged - Dapr abstraction)
  - **FILE-006**: `manifests/branch/base/deployments/loyalty-service.yaml` (unchanged - Dapr abstraction)

## 6. Testing

  - **TEST-001**: State Write Test - Write key-value pair, verify storage
  - **TEST-002**: State Read Test - Read previously written key, verify value
  - **TEST-003**: State Delete Test - Delete key, verify removal
  - **TEST-004**: State Transaction Test - Multi-item atomic operation
  - **TEST-005**: State Query Test - Query state with filters (Dapr 1.5+)
  - **TEST-006**: Load Test - 100 concurrent operations, verify no errors

## 7. Risks & Assumptions

  - **RISK-001**: **Data Loss During Migration** - **Mitigation**: Export state before migration, verify import
  - **RISK-002**: **Workload Identity Misconfiguration** - **Mitigation**: Test in staging, document configuration steps
  - **ASSUMPTION-001**: Existing state data volume is small (<1GB)
  - **ASSUMPTION-002**: Cosmos DB/DynamoDB/Firestore support all Dapr state operations

## 8. Related Specifications / Further Reading

  - [Dapr State Management](https://docs.dapr.io/developing-applications/building-blocks/state-management/)
  - [Dapr Cosmos DB State Store](https://docs.dapr.io/reference/components-reference/supported-state-stores/setup-azure-cosmosdb/)
  - [Dapr DynamoDB State Store](https://docs.dapr.io/reference/components-reference/supported-state-stores/setup-dynamodb/)
  - [Dapr Firestore State Store](https://docs.dapr.io/reference/components-reference/supported-state-stores/setup-firestore/)
  - [Dapr SQLite State Store](https://www.google.com/search?q=https://docs.dapr.io/reference/components-reference/supported-state-stores/sqlite/)
  - [Phase 0: Platform Foundation](https://www.google.com/search?q=./upgrade-phase0-platform-foundation-implementation-1.md)
  - [Dapr 1.16 Upgrade](https://www.google.com/search?q=./upgrade-dapr-1.16-implementation-1.md)