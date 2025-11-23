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

This ADR records the architectural decision to use Dapr as the primary abstraction layer
for platform-specific integrations (secrets, state, pub/sub, bindings) across all Red Dog
services.

Implementation details, current Dapr runtime versions, and per-cloud component mappings
are tracked in:

- `plan/modernization-strategy.md`
- deployment guides under `docs/deploy-*.md`
- Kubernetes manifests under `manifests/**`

As of 2025-11-23:

- Application code uses `DaprClient` for secrets, state, pub/sub, and service invocation.
- Logical Dapr components such as `reddog.secretstore`, `reddog.state.makeline`,
  `reddog.state.loyalty`, `reddog.pubsub`, and receipt/worker bindings are defined in
  `manifests/branch/base/components/` and overridden by environment-specific manifests.
- No cloud-provider SDKs (Azure/AWS/GCP) or direct Redis clients are referenced in the
  application code; platform differences are isolated to Dapr components and manifests.

This ADR does not attempt to track day-to-day implementation status beyond this note.

## Context

Red Dog’s modernization strategy targets deployment across multiple platforms:

- Azure Kubernetes Service (AKS)
- Azure Container Apps
- AWS Elastic Kubernetes Service (EKS)
- Google Kubernetes Engine (GKE)

Key constraints:

- Services must deploy to multiple platforms without application code changes.
- Configuration and integration with cloud services must be platform-agnostic.
- Teaching and demo goals require a single codebase that can be deployed to different
  environments without conditional compilation or feature flags.
- We cannot maintain separate branches or divergent code paths per cloud provider.

Typical platform-specific integrations include:

- **Secrets management**: Azure Key Vault, AWS Secrets Manager, GCP Secret Manager.
- **State storage**: Dapr state stores backed by Redis or database-backed services
  such as Azure Cosmos DB, AWS DynamoDB, or GCP Firestore, depending
  on environment.
- **Pub/Sub messaging**: Azure Service Bus, AWS SNS/SQS, GCP Pub/Sub (via a single
  logical Dapr pub/sub component).
- **Bindings**: Azure Blob Storage, AWS S3, GCP Cloud Storage, schedules/cron
  (via Dapr bindings).

We needed a consistent way for services to access these capabilities without embedding
cloud provider SDKs into the application code.

## Decision

**Adopt Dapr as the abstraction layer for all platform-specific integrations** in Red Dog services
(OrderService, AccountingService, MakeLineService, LoyaltyService, ReceiptGenerationService,
VirtualCustomers, VirtualWorker, and future services).

### Scope

Within this decision:

- **Secrets**  
  All services use a Dapr *secret store* component (logical name `reddog.secretstore`)
  for secrets. Each environment configures `reddog.secretstore` to talk to that
  environment’s native secret manager (e.g., Key Vault, Secrets Manager, Secret Manager,
  local file store).

- **State**  
  Services that require state (e.g., MakeLineService, LoyaltyService) use Dapr *state
  store* components (`reddog.state.makeline`, `reddog.state.loyalty`) instead of
  direct Redis or database clients.

- **Pub/Sub**  
  Event-driven flows (e.g., order events) use a Dapr *pub/sub* component
  (`reddog.pubsub`) rather than direct SDKs or broker clients.

- **Output bindings**  
  Outbound integration such as receipt generation and scheduled background jobs use
  Dapr *bindings* (e.g., storage bindings, cron bindings) rather than direct cloud SDK
  calls from the application code.

Application code interacts with these capabilities exclusively via `DaprClient` and
Dapr HTTP/gRPC endpoints, not via cloud provider SDKs.

### Rationale

- **POR-001: Zero code changes across platforms**  
  Application code calls `DaprClient` against `localhost:3500`. Component YAML files
  decide whether that Dapr call ends up in Azure Key Vault, AWS Secrets Manager, or
  another backend. No `#if AZURE` / `#if AWS` or runtime platform branches are needed.

- **POR-002: Universal sidecar architecture**  
  All target platforms support Dapr sidecars. The difference between “managed Dapr”
  (e.g., Azure Container Apps) and “self-managed Dapr” (AKS/EKS/GKE) is operational,
  not architectural. The application architecture remains the same.

- **POR-003: Component-based configuration**  
  Platform differences are encoded in Dapr component manifests
  (e.g., `manifests/branch/base/components/reddog.secretstore.yaml` and overlays),
  not in application code. Changing cloud provider means changing YAML, not rewriting
  services.

- **POR-004: Simplified testing and teaching**  
  Developers test a single Dapr-based code path locally. Instructors can deploy the
  same code to different clouds, using different component backends, without touching
  the service logic.

- **POR-005: Vendor independence**  
  Red Dog avoids lock-in to any single cloud SDK. Moving between providers or adding
  a new one is primarily a manifest change.

## Consequences

### Positive

- **POS-001: Single codebase for multiple clouds**  
  The same `OrderService` image runs on AKS, Container Apps, EKS, and GKE. No per-cloud
  forks or branches are required.

- **POS-002: Application code free of cloud SDKs**  
  Teams learn Dapr and domain code, not three different provider SDK ecosystems.

- **POS-003: Consistent local development**  
  Local Dapr components (e.g., Redis, local file secret store) mimic cloud deployments
  without changing application code.

- **POS-004: Cleaner deployments**  
  Deployment differences (Key Vault vs Secrets Manager vs local file secrets, different
  queues or topics) are isolated to manifests and deployment scripts, not to code.

- **POS-005: Easier multi-cloud demos**  
  The architecture demonstrates a modern, portable, sidecar-based pattern that can be
  reused in teaching and workshops.

### Negative

- **NEG-001: Dapr as a hard dependency**  
  Services rely on Dapr sidecars being present and healthy. Running services completely
  standalone requires additional work.

- **NEG-002: Learning curve**  
  Teams must learn Dapr concepts (components, sidecars, building blocks) in addition to
  the cloud platforms themselves.

- **NEG-003: Resource overhead**  
  Each pod runs two processes (app + sidecar). This adds memory and CPU overhead compared
  to bare application containers.

- **NEG-004: Version coordination**  
  Dapr runtime, CLI, and SDK versions must be kept within a supported and compatible
  range across local dev and all clusters.

- **NEG-005: Feature parity**  
  Only features exposed by Dapr components are available. Some advanced provider-specific
  capabilities (e.g., HSM-backed features) may not be reachable via the abstraction.

- **NEG-006: Debugging complexity**  
  Troubleshooting often spans both application code and the Dapr sidecar, adding an
  extra hop and layer of configuration to reason about.

## Alternatives Considered

### Platform-specific SDKs (conditional compilation or flags)

- **ALT-001: Description**  
  Use Azure SDK on Azure, AWS SDK on AWS, GCP SDK on GCP, guarded by `#if` blocks or
  runtime configuration (`CLOUD_PROVIDER`).

- **ALT-002: Rejection Reason**  
  Multiplies code paths, testing complexity, and failure modes. Increases cognitive load
  on developers and makes the code harder to reason about and teach.

### Kubernetes-only abstractions (CSI drivers, ConfigMaps, Secrets)

- **ALT-003: Description**  
  Use Kubernetes-native mechanisms for secrets and configuration, plus direct clients
  for Redis, queues, etc.

- **ALT-004: Rejection Reason**  
  Does not handle non-Kubernetes platforms (e.g., Container Apps) cleanly. Leaves
  state, pub/sub, and bindings to be solved service-by-service with direct SDKs.

### Feature flags with runtime platform detection

- **ALT-005: Description**  
  Single codebase, but each operation branches at runtime based on platform identity
  or configuration to select the correct SDK.

- **ALT-006: Rejection Reason**  
  Distributes platform branching logic throughout the codebase, complicates testing,
  and increases the risk of misconfiguration and partial updates.

### Single-cloud, direct SDK usage (Azure-only)

- **ALT-007: Description**  
  Decide to only support Azure and use Azure SDKs directly, no abstraction.

- **ALT-008: Rejection Reason**  
  Conflicts with Red Dog’s teaching goal of demonstrating multi-cloud portability and
  cloud-agnostic architecture. Introduces vendor lock-in and reduces the value of the
  sample.

## Implementation Notes

These notes describe how this decision influences related work. Operational details are
captured in modernization plans and deployment docs.

- **IMP-001: Component strategy**  
  Logical Dapr component names (e.g., `reddog.secretstore`, `reddog.state.makeline`,
  `reddog.pubsub`) are stable across environments. Each environment provides its own
  component implementation and backend configuration.

- **IMP-002: Environment mapping**  
  Local, AKS, Container Apps, EKS, and GKE deployments use the same application images
  and logical component names. Only the component YAML definitions differ between
  environments.

- **IMP-003: Runtime versions**  
  Dapr runtime and SDK versions must remain within supported ranges and be kept
  compatible across environments. Specific upgrade steps and version pins (e.g., moving
  from older Dapr releases to newer ones) are handled in
  `plan/modernization-strategy.md` and cluster-level configuration, not in this ADR.

- **IMP-004: Success criteria**  
  - No cloud provider SDKs are referenced from application code.
  - All platform-specific integrations (secrets, state, pub/sub, bindings) are accessed
    exclusively via `DaprClient`/Dapr HTTP.
  - The same container images can be deployed to multiple platforms by changing only
    Dapr component and deployment manifests.
