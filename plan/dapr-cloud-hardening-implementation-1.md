---
goal: "Dapr Cloud Hardening & Identity Federation"
version: 1.1
date_created: 2025-11-13
last_updated: 2025-11-17
owner: Red Dog Modernization Team
status: Planned
tags:
  - dapr
  - workload-identity
  - hardening
  - cloud
  - refactor
---

# Dapr Cloud Hardening & Identity Federation

This companion plan covers production-hardening items deferred from `plan/upgrade-dapr-1.16-implementation-1.md`. The work is split into two main areas:

- Service invocation fixes required by Dapr 1.9+ (ensure compliant Content-Type handling)
- Workload identity federation across Azure, AWS, and GCP and cloud-specific components.

This plan activates **after** the core runtime/Helm/component upgrade is complete.

**Version 1.1:** Updated Phase A to enforce a DRY helper library for service invocation and added a Phase B prerequisite to secure the Dapr control plane.

## Table of contents

- [Scope & Dependencies](#scope--dependencies)
- [Implementation Phases](#implementation-phases)
  - [Phase A: Service Invocation Compliance (Refactor)](#phase-a-service-invocation-compliance-refactor)
  - [Phase B: Cloud Workload Identity & Component Overlays](#phase-b-cloud-workload-identity--component-overlays)
- [Deliverables](#deliverables)
- [Risks & Mitigations](#risks--mitigations)
- [Completion Criteria](#completion-criteria)
- [Acceptance Criteria](#acceptance-criteria)
- [References](#references)

## Scope & Dependencies

- **Depends on:** `plan/upgrade-dapr-1.16-implementation-1.md` (Phases 1–4 & 7 must be complete)
- **Feeds into:**
  - `plan/migrate-state-stores...` (this plan is a **hard blocker**)
  - `plan/migrate-object-storage...` (this plan is a **hard blocker**)
- **Environments:** Staging and Cloud (AKS, EKS, GKE). **Local dev is not affected.**

## Implementation Phases

## Implementation Phases


### Phase A: Service Invocation Compliance (Refactor)

**Goal:** Centralize Dapr service invocation HTTP behavior by implementing a shared `DaprInvocationHelper` (e.g., inside `RedDog.Shared`) that enforces the Dapr 1.9+ `Content-Type` requirement.

Tasks (Phase A):

- [ ] TASK-A01 — Inventory all `HttpClient` calls to Dapr service invocation endpoints across all services.
- [ ] TASK-A02 — **Refactor**: Create `DaprInvocationHelper` in `RedDog.Shared` to centralize `HttpClient` logic for service-to-service calls.
- [ ] TASK-A03 — Implement explicit `Content-Type: application/json` header for all POST/PUT requests in `DaprInvocationHelper`.
- [ ] TASK-A04 — Update `OrderService` to use `DaprInvocationHelper` for all Dapr invocation calls.
- [ ] TASK-A05 — Update `MakeLineService` to use `DaprInvocationHelper`.
- [ ] TASK-A06 — Update `LoyaltyService` to use `DaprInvocationHelper`.
- [ ] TASK-A07 — Update `AccountingService` to use `DaprInvocationHelper`.
- [ ] TASK-A08 — Update `VirtualWorker` to use `DaprInvocationHelper`.
- [ ] TASK-A09 — Rebuild, redeploy, and run smoke tests across services to validate invocation behavior.


### Phase B: Cloud Workload Identity & Component Overlays

**Goal:** Configure Dapr and all applications to use secure, passwordless Workload Identity in each target cloud provider (Azure, AWS, GCP), and remove secrets from component YAMLs.

Prerequisite: Dapr control plane must have permissions to validate federated identities (see TASK-B01).

Tasks (Phase B):

- [ ] TASK-B01 — **Prerequisite:** Grant the Dapr control plane (e.g., `dapr-sidecar-injector`) permissions to validate federated identities.
  - **Azure:** Grant `Managed Identity Operator` role to the Dapr control plane identity.
  - **AWS/GCP:** Verify OIDC trust policies for the Dapr control plane ServiceAccount.
- [ ] TASK-B02 — Azure: Create user-assigned Managed Identities for each Dapr-enabled application.
- [ ] TASK-B03 — Azure: Federate Managed Identities with applications' Kubernetes `ServiceAccount`s.
- [ ] TASK-B04 — Azure: Annotate workload `ServiceAccount` manifests with `azure.workload.identity/client-id`.
- [ ] TASK-B05 — Azure: Update Dapr component YAMLs (CosmosDB, Blob Storage) to use Workload Identity (remove static connection strings).
- [ ] TASK-B06 — AWS: Provision IAM Roles for Service Accounts (IRSA) and attach DynamoDB/S3 policies.
- [ ] TASK-B07 — AWS: Annotate `ServiceAccount` manifests with `eks.amazonaws.com/role-arn`.
- [ ] TASK-B08 — AWS: Update Dapr component YAMLs (DynamoDB, S3) to use IRSA.
- [ ] TASK-B09 — GCP: Configure Workload Identity bindings between K8s `ServiceAccount` and GCP `ServiceAccount`.
- [ ] TASK-B10 — GCP: Annotate `ServiceAccount` manifests with `iam.gke.io/gcp-service-account`.
- [ ] TASK-B11 — GCP: Update Dapr component YAMLs (Firestore, GCS) to use Workload Identity.
- [ ] TASK-B12 — Validate mTLS and successful, passwordless secret/state/blob access in staged environments for each cloud.

## Deliverables

1. **Source Code** — A new `DaprInvocationHelper` in a shared library and refactored services leveraging it.
2. **Manifests** — Cloud-specific Dapr component manifests (e.g., `state.azure.cosmosdb.yaml`, `bindings.aws.s3.yaml`) that do not contain hardcoded secrets and instead use Workload Identity.
3. **Evidence** — Test artifacts (smoke tests, logs) showing successful secretless Dapr component access and mTLS across clouds.

## Risks & Mitigations

- **Identity misconfiguration** — Risk: Identity bindings or role policies misconfigured.
  - Mitigation: Implement `TASK-B01` for control plane permissions, validate in staging with `TASK-B12`.
- **Refactoring error** — Risk: Shared helper refactor introduces regressions.
  - Mitigation: Use `TASK-A09` for smoke tests, and create API contract tests to cover service invocations.
- **Cloud-specific nuances** — Risk: Different clouds require different YAML and binding semantics.
  - Mitigation: Create per-cloud overlays and document differences for platform engineers.

## Completion Criteria

- All TASK-Axx and TASK-Bxx items are completed with linked PRs and artifact evidence.
- Cloud deployment docs updated to reference new Workload Identity requirements and Dapr configuration changes.
- CI/CD pipeline includes validation steps for:
  - `Content-Type: application/json` header compliance for Dapr invocations
  - Workload Identity health checks across Azure/AWS/GCP
  - mTLS checks and secretless access validations

---

## Acceptance Criteria

The plan is accepted once all of the following have been validated in staging and documented:

- Dapr service invocation behavior is consistent across services using `DaprInvocationHelper`.
- All Dapr components are configured with Workload Identity and do not use static secrets.
- Automated CI checks confirm header compliance and identity health.
- Platform documentation includes migration steps and roles, bindings, and required annotations per cloud.

## References

- Dependent plan: `plan/upgrade-dapr-1.16-implementation-1.md`
- Related: `plan/migrate-state-stores...`, `plan/migrate-object-storage...`

