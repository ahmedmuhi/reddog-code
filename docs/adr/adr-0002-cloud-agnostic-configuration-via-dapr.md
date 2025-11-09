---
title: "ADR-0002: Cloud-Agnostic Configuration via Dapr Abstraction"
status: "Accepted"
date: "2025-11-02"
authors: "Red Dog Modernization Team"
tags: ["architecture", "decision", "dapr", "multi-cloud", "portability"]
supersedes: ""
superseded_by: ""
---

# ADR-0002: Cloud-Agnostic Configuration via Dapr Abstraction

## Status

**Accepted**

## Implementation Status

**Current State:** 🟢 Implemented

**What's Working:**
- Dapr secret store component configured: manifests/branch/base/components/secrets.yaml
- Services actively use DaprClient.GetSecretAsync() to retrieve secrets
- Abstraction working across development and production environments
- No cloud-specific SDK dependencies in application code

**What's Not Working:**
- Dapr version 1.3.0 (from 2021) is outdated; should upgrade to 1.16.0 for enhanced features
- Workload Identity integration (SEC-005) not yet implemented (planned for Phase 8)

**Evidence:**
- manifests/branch/base/components/secrets.yaml:1 - Dapr secret store component definition
- Service code references DaprClient.GetSecretAsync() calls (actual implementation active)
- ADR-0004 cross-reference confirms Dapr abstraction layer is operational

**Dependencies:**
- **Blocks:** All other ADRs rely on this cloud-agnostic abstraction principle
- **Supports:** ADR-0004 (Config API), ADR-0007 (Deployment Strategy), ADR-0009 (Helm)
- **Depends On:** None (foundational architecture decision)

**Next Steps:**
1. Upgrade Dapr from 1.3.0 to 1.16 (plan/modernization-strategy.md Phase 3)
2. Implement Workload Identity for secret access (plan/modernization-strategy.md Phase 8)
3. Document Dapr component configuration patterns for new services

## Context

Red Dog's modernization strategy targets deployment across multiple cloud platforms and orchestrators:
- **Azure**: Azure Kubernetes Service (AKS), Azure Container Apps
- **AWS**: Elastic Kubernetes Service (EKS)
- **GCP**: Google Kubernetes Engine (GKE)

**Key Constraints:**
- Services must deploy to 4+ different platforms without code changes
- Configuration management must be platform-agnostic (no cloud-specific SDKs in application code)
- REQ-004 mandates multi-platform deployment support (documented in `plan/orderservice-dotnet10-upgrade.md`)
- Teaching/demo focus requires simplified deployment (one codebase, multiple targets)
- Cannot maintain separate codebases or branches for each cloud provider

**Platform-Specific Integration Challenges:**
- **Secrets Management**: Azure Key Vault vs AWS Secrets Manager vs GCP Secret Manager
- **State Storage**: Azure Redis vs AWS ElastiCache vs GCP Memorystore
- **Pub/Sub Messaging**: Azure Service Bus vs AWS SNS/SQS vs GCP Pub/Sub
- **Output Bindings**: Azure Blob Storage vs AWS S3 vs GCP Cloud Storage

**Available Approaches:**
1. **Platform-Specific SDKs**: Use Azure SDK for Azure deployments, AWS SDK for AWS, etc. (requires conditional compilation or feature flags)
2. **Kubernetes-Only Abstractions**: CSI drivers, native Kubernetes secrets (limited to Kubernetes platforms, excludes Container Apps)
3. **Dapr Abstraction**: Use Dapr components to abstract platform differences via sidecar pattern

## Decision

**Adopt Dapr as the abstraction layer for all platform-specific integrations** in Red Dog services (OrderService, AccountingService, MakeLineService, LoyaltyService, ReceiptGenerationService, VirtualCustomers, VirtualWorker).

**Scope:**
- **Secrets Management**: Use Dapr secret store component (backed by Azure Key Vault, AWS Secrets Manager, or GCP Secret Manager depending on deployment)
- **State Management**: Use Dapr state store component (MakeLineService, LoyaltyService)
- **Pub/Sub Messaging**: Use Dapr pub/sub component (all services subscribe to `orders` topic)
- **Output Bindings**: Use Dapr output bindings (ReceiptGenerationService, VirtualWorker)

**Rationale:**
- **POR-001**: **Zero Code Changes Across Platforms**: Application code uses `DaprClient` API (`localhost:3500`), identical across all deployments. No conditional compilation, no feature flags, no platform detection logic.
- **POR-002**: **Universal Sidecar Architecture**: All platforms (Container Apps, AKS, EKS, GKE) use Dapr sidecar pattern. "Managed Dapr" (Container Apps) vs "self-managed" (EKS/GKE) differs only in injection method, not architecture.
- **POR-003**: **Component-Based Configuration**: Platform differences isolated to YAML component files (`reddog.secretstore.yaml`, `reddog.state.makeline.yaml`, etc.), not application code.
- **POR-004**: **Simplified Testing**: Single code path to test, regardless of deployment target. Developer machines run same code as production (using Redis/local components).
- **POR-005**: **Teaching/Demo Value**: Demonstrates modern cloud-native patterns. Instructors deploy to any cloud without explaining platform-specific SDKs.
- **POR-006**: **Future Migration Flexibility**: Switching clouds requires only updating Dapr component YAML files, not rewriting application code.

## Consequences

### Positive

- **POS-001**: **Single Codebase Maintenance**: One `OrderService` codebase deploys to 4+ platforms. Eliminates branch/fork maintenance burden.
- **POS-002**: **Simplified Developer Onboarding**: Developers learn Dapr API once, work across all platforms. No need to learn Azure SDK, AWS SDK, GCP SDK simultaneously.
- **POS-003**: **Consistent Local Development**: Local development uses same `DaprClient` code as production. `manifests/local/branch/` components mirror cloud deployment patterns.
- **POS-004**: **Platform Feature Abstraction**: Application code agnostic to whether secrets come from Azure Key Vault, AWS Secrets Manager, or GCP Secret Manager.
- **POS-005**: **Deployment Simplicity**: Deployment scripts (`deploy-aks.sh`, `deploy-container-apps.sh`, `deploy-eks.sh`) differ only in component YAML, not Docker images.
- **POS-006**: **Cloud Vendor Independence**: No lock-in to cloud-specific APIs. Migration cost reduced to YAML configuration changes.
- **POS-007**: **Observability Consistency**: Dapr metrics, tracing, and logs uniform across platforms (OpenTelemetry integration via Dapr 1.16).
- **POS-008**: **Component Ecosystem**: Access to 100+ Dapr components (state stores, pub/sub brokers, bindings) without custom integration code.

### Negative

- **NEG-001**: **Dapr Dependency**: Services cannot run without Dapr sidecar (3500/3501 ports required). Increases deployment complexity vs standalone executables.
- **NEG-002**: **Learning Curve**: Team must learn Dapr concepts (components, building blocks, sidecar architecture) in addition to cloud platforms.
- **NEG-003**: **Resource Overhead**: Each service pod/container runs two processes (app + Dapr sidecar). Typical overhead: +20-50 MB memory, +0.01-0.05 CPU cores per sidecar.
- **NEG-004**: **Dapr Version Management**: Must coordinate Dapr CLI (local), Dapr runtime (cluster), and Dapr SDK (.NET package) versions. Upgrade friction across 4+ platforms.
- **NEG-005**: **Component Feature Parity**: Limited to features supported by Dapr components. Platform-specific features (e.g., Azure Key Vault's HSM-backed keys) may not be accessible via Dapr abstraction.
- **NEG-006**: **Debugging Complexity**: Troubleshooting involves both application code and Dapr sidecar. Network calls routed through localhost:3500 add indirection.
- **NEG-007**: **Performance Overhead**: Service-to-service calls add ~1-5ms latency (HTTP → Dapr sidecar → HTTP). State operations add serialization/deserialization overhead.

## Alternatives Considered

### Platform-Specific SDKs (Conditional Compilation)

- **ALT-001**: **Description**: Use Azure SDK for Azure deployments, AWS SDK for AWS, GCP SDK for GCP. Use `#if AZURE`, `#if AWS`, `#if GCP` preprocessor directives or runtime feature flags to select SDK at build/runtime.
- **ALT-002**: **Rejection Reason**: Violates DRY principle. Requires maintaining 3+ code paths in every service. Testing burden multiplied by number of platforms. Build pipelines require platform-specific Docker images. Developer confusion about which code path executes in production.

### Kubernetes-Only Abstractions (CSI Drivers, ConfigMaps, Secrets)

- **ALT-003**: **Description**: Use Kubernetes-native primitives. Secrets via CSI driver (mounts Azure Key Vault as volume), state via Kubernetes-native Redis Helm charts, pub/sub via Kubernetes Jobs/CronJobs.
- **ALT-004**: **Rejection Reason**: Does not support Azure Container Apps (serverless, not full Kubernetes). CSI drivers require pod volume mounts (timing issues during startup, complexity in Dockerfile). Kubernetes primitives lack state management abstractions (manual Redis client code, no retry/consistency guarantees). Eliminates Container Apps deployment target.

### Feature Flags with Platform Detection

- **ALT-005**: **Description**: Single codebase with runtime platform detection (`Environment.GetEnvironmentVariable("CLOUD_PROVIDER")`). Load appropriate SDK based on flag value.
- **ALT-006**: **Rejection Reason**: Runtime branching logic scattered throughout codebase. Unit testing requires mocking 3+ SDK implementations. Human error risk (forgot to update one platform's code path). Environment variable misconfiguration creates production outages. Does not simplify code complexity.

### Direct Cloud Provider APIs (No Abstraction)

- **ALT-007**: **Description**: Use Azure-only deployments, abandon multi-cloud strategy. Directly use Azure SDK for all integrations.
- **ALT-008**: **Rejection Reason**: Contradicts modernization goal of cloud-agnostic architecture. Eliminates AWS/GCP deployment scenarios (valuable for teaching/demos). Creates vendor lock-in. Does not demonstrate portable cloud-native patterns.

## Implementation Notes

- **IMP-001**: **Existing Implementation**: Red Dog already uses Dapr (current version 1.3.0 from 2021). This ADR formalizes and modernizes the existing approach (upgrade to Dapr 1.16).
- **IMP-002**: **Dapr 1.16 Requirement**: Upgrade from Dapr 1.3.0 to 1.16 (September 2025 release). Includes Workload Identity support (critical for SEC-005 requirement), performance improvements, OpenTelemetry integration.
- **IMP-003**: **Component Configuration Strategy**:
  - **Local Development**: `manifests/local/branch/` uses Redis (state/pub/sub), local file system (bindings), local secret store
  - **AKS**: Azure Key Vault (secrets), Azure Redis (state), Azure Service Bus (pub/sub), Azure Blob Storage (bindings)
  - **Container Apps**: Managed Dapr components (Azure-backed, invisible sidecar injection)
  - **EKS/GKE**: AWS/GCP equivalents (Secrets Manager, ElastiCache/Memorystore, SNS/Pub/Sub, S3/Cloud Storage)
- **IMP-004**: **Workload Identity Integration**: Dapr secret store component uses Workload Identity (no Service Principal certificates). See `plan/modernization-strategy.md` Phase 8 for migration steps.
- **IMP-005**: **Sidecar Architecture Universality**: All platforms use sidecar pattern:
  - **Container Apps**: Azure injects Dapr sidecar automatically (invisible, fully managed)
  - **AKS**: Dapr extension or Helm chart injects sidecar via `dapr.io/enabled: true` annotation
  - **EKS/GKE**: Self-managed Dapr control plane, manual sidecar injection via Helm
  - **Application Code**: Identical across all platforms (`DaprClient` calls `localhost:3500`)
- **IMP-006**: **Success Criteria**: Zero platform-specific code in application layer (no `#if`, no SDK references), all services deploy to 4+ platforms using same Docker image, component YAML files validate against Dapr 1.16 schema.
- **IMP-007**: **Documentation Strategy**: Create deployment guides (`docs/deploy-aks.md`, `docs/deploy-container-apps.md`, `docs/deploy-eks.md`, `docs/deploy-gke.md`) showing component YAML differences, not code differences.

## References

- **REF-001**: Related Requirement: `plan/orderservice-dotnet10-upgrade.md` REQ-004 (multi-platform deployment support)
- **REF-002**: Related Plan: `plan/modernization-strategy.md` (Phase 3: Dapr 1.16 upgrade, Phase 8: Workload Identity)
- **REF-003**: Research Document: `Research/dapr-secret-store-vs-azure-key-vault-aks-comparison.md` (Dapr secret store vs direct Azure SDK)
- **REF-004**: Session Log: `.claude/sessions/2025-11-02-1105-orderservice-dotnet10-refinement.md` (Dapr sidecar architecture clarification)
- **REF-005**: Dapr Docs: [Dapr building blocks overview](https://docs.dapr.io/concepts/building-blocks-concept/)
- **REF-006**: Dapr Docs: [Multi-cloud deployments](https://docs.dapr.io/operations/hosting/)
- **REF-007**: Azure Docs: [Dapr in Azure Container Apps](https://learn.microsoft.com/azure/container-apps/dapr-overview)
- **REF-008**: Session Note: Container Apps, AKS, EKS, and GKE all use identical sidecar architecture. "Managed" refers to injection method, not presence of sidecars.
