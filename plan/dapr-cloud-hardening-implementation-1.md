---
goal: "Dapr Cloud Hardening & Identity Federation"
version: 1.0
date_created: 2025-11-13
last_updated: 2025-11-13
owner: Red Dog Modernization Team
status: 'Planned'
tags: [dapr, workload-identity, hardening, cloud]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

This companion plan covers the production-hardening items deferred from `plan/upgrade-dapr-1.16-implementation-1.md`: service invocation fixes required by Dapr 1.9+, and workload identity federation across Azure, AWS, and GCP (plus the cloud-specific components those identities enable). It activates **after** the core runtime/Helm/component upgrade is complete.

## Scope & Dependencies

- **Depends on:** `plan/upgrade-dapr-1.16-implementation-1.md` (Phases 1–4 & 7 complete)
- **Feeds into:** state/object storage migration plans, cloud deployment overlays
- **Environments:** Staging + Cloud (AKS, EKS, GKE)

## Implementation Phases

### Phase A: Service Invocation Compliance (former Phase 5)

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-A01 | Inventory all `HttpClient` calls to Dapr service invocation endpoints | | |
| TASK-A02 | Add explicit `Content-Type: application/json` header for POST/PUT requests | | |
| TASK-A03 | Update OrderService invocation helpers | | |
| TASK-A04 | Update MakeLineService invocation helpers | | |
| TASK-A05 | Update LoyaltyService invocation helpers | | |
| TASK-A06 | Update AccountingService invocation helpers | | |
| TASK-A07 | Update VirtualWorker invocation helpers | | |
| TASK-A08 | Rebuild + redeploy upgraded services and run smoke tests | | |

### Phase B: Cloud Workload Identity & Component Overlays (former Phase 6)

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-B01 | Azure: create Managed Identity + federate ServiceAccounts | | |
| TASK-B02 | Azure: annotate workloads (`azure.workload.identity/client-id`) | | |
| TASK-B03 | Azure: update secret/object components to use MI (Cosmos/Blob) | | |
| TASK-B04 | AWS: provision IAM roles/IRSA trust, annotate ServiceAccounts | | |
| TASK-B05 | AWS: update DynamoDB/S3 components to use IRSA credentials | | |
| TASK-B06 | GCP: configure Workload Identity binding + annotations | | |
| TASK-B07 | GCP: update Firestore/GCS components to use WI | | |
| TASK-B08 | Validate mTLS + secret access per cloud (smoke tests) | | |

## Deliverables

1. Source changes enforcing Dapr invocation headers + regression tests.
2. Cloud-specific manifests (or Helm values) for Cosmos/DynamoDB/Firestore + blob/S3/GCS bindings that rely on Workload Identity.
3. Evidence of successful end-to-end smoke tests per cloud.

## Risks

- Identity misconfiguration blocking secret/state access → mitigate via staging dry-runs.
- Divergent service code paths (HTTP vs gRPC) → enforce shared helper library.

## Completion Criteria

- All TASK-Axx and TASK-Bxx rows marked complete with links to PRs/artifacts.
- Cloud deployment docs updated to reference the new identity requirements.
- CI/CD pipeline contains validation steps for header compliance + identity health checks.
