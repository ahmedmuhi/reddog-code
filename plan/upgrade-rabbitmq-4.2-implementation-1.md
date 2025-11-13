---
goal: "Upgrade cloud RabbitMQ deployment to 4.2.0 for long-term support and Khepri-backed metadata reliability"
version: 1.0
date_created: 2025-11-14
last_updated: 2025-11-14
owner: "Red Dog Modernization Team"
status: "Planned"
tags: [infrastructure, messaging, rabbitmq, phase-0, dapr, keda]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

RabbitMQ 4.2.0 shipped on 28 Oct 2025 and is the newest generally available (GA) release with community support through October 2028, making it the right target for the Phase 0 platform foundation upgrade.citeturn0search0 The 4.2 line also finalizes Khepri—the Raft-based metadata store—as the recommended default, improving safety and recovery characteristics for clustered brokers.citeturn0search1

**Scope Clarification**

- Cloud environments (AKS/EKS/GKE) will host RabbitMQ 4.2.0. Local development continues to use Redis for pub/sub to keep kind resource usage low.
- This plan covers the runtime upgrade, Helm/Infrastructure-as-Code changes, integration with Dapr components, and validation. Future KEDA ScaledObjects and autoscaling logic will live in `plan/keda-cloud-autoscaling-implementation-1.md`.

**Duration:** ~3 working days (prep, rollout, validation) per cloud environment  
**Risk Level:** MEDIUM (critical messaging backbone but no local dev dependency)

## 1. Requirements & Constraints

### Functional Requirements

- **REQ-001:** RabbitMQ must run version 4.2.0 (management image) in all cloud clusters.
- **REQ-002:** Dapr `pubsub.rabbitmq` components must point at the upgraded cluster using the new metadata pass-through structure (values files per environment).
- **REQ-003:** Management plugin, Prometheus metrics (port 15692), and TLS termination remain enabled for observability.
- **REQ-004:** Brokers must retain current exchanges, queues, bindings, and policies during migration (backup + restore if needed).

### Technical Requirements

- **REQ-005:** Erlang/OTP 26.x is required (RabbitMQ 4.2 packages depend on Erlang ≥26 and <28).citeturn0search6
- **REQ-006:** Kubernetes clusters must provide PersistentVolumeClasses with SSD-backed storage (1 GiB+ per replica) for queue durability.
- **REQ-007:** Use Bitnami RabbitMQ Helm chart (`oci://registry-1.docker.io/bitnamicharts/rabbitmq`) or a cloud managed offering with equivalent configuration controls (TLS, metrics, Khepri flag).
- **REQ-008:** Enable/verify the `khepri_db` feature flag post-upgrade to benefit from the default metadata store.citeturn0search1

### Security Requirements

- **SEC-001:** All credentials (default user, TLS certs, operator tokens) must be sourced via Kubernetes Secrets per ADR-0013; no inline literals in values files.
- **SEC-002:** Management UI must retain SSO or IP allow-list protections used today.
- **SEC-003:** Ensure CVE-2025-30219 (virtual host XSS) mitigation by being on ≥4.0.3 (fulfilled by 4.2).citeturn0search10

### Constraints

- **CON-001:** Local kind environment will not host RabbitMQ; testing is performed in cloud clusters and/or ephemeral staging clusters.
- **CON-002:** Dapr + application changes must remain compatible with Redis-based local metadata (handled via values overrides).
- **CON-003:** Managed services (Azure Service Bus, etc.) are out of scope—RabbitMQ remains the reference broker to stay cloud-agnostic per ADR-0007.

## 2. Implementation Steps

### Phase 1 – Inventory & Readiness (Day 0-1)

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-101 | Document current RabbitMQ topology (number of nodes/pods, storage classes, metrics endpoints) | | |
| TASK-102 | Export definitions: `rabbitmqctl export_definitions /tmp/rabbitmq-defs.json` or use HTTP API for managed offerings | | |
| TASK-103 | Capture current Dapr component metadata (`values-*.yaml`) to verify alignment with new metadata structure | | |
| TASK-104 | Verify Erlang/OTP runtime version on nodes or managed service | | |
| TASK-105 | Confirm monitoring expectations (Prometheus scrape config, Grafana dashboards) for 4.2 metrics | | |

### Phase 2 – Infrastructure Upgrade Path (Day 1)

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-201 | Pull Bitnami RabbitMQ chart 16.x and pin image tag `rabbitmq:4.2.0-management` | | |
| TASK-202 | Configure Helm values per environment (replica count, resources, storage, TLS secrets, Khepri flag) | | |
| TASK-203 | Ensure `khepri_db` feature flag enabled post-upgrade: `rabbitmqctl await_startup && rabbitmqctl enable_feature_flag khepri_db` | | |
| TASK-204 | Implement rolling upgrade (Helm upgrade --reuse-values) or blue/green deployment for managed service | | |
| TASK-205 | Run smoke script (`scripts/run-dapr-makeline-smoke.sh`) pointing to staging cluster to validate pub/sub | | |

### Phase 3 – Dapr & App Integration (Day 2)

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-301 | Update cloud values file (e.g., `values/values-azure.yaml`) with RabbitMQ metadata (host, durable, TLS) | | |
| TASK-302 | Ensure ADR-0013-compliant secrets exist (RabbitMQ URI, TLS certs) and referenced via `secretKeyRef` | | |
| TASK-303 | Re-run `helm template` / `helm upgrade` for `charts/reddog` to confirm Dapr components render correctly | | |
| TASK-304 | Validate Dapr `pubsub.rabbitmq` health by publishing sample events via `rest-samples/order-service.rest` | | |

### Phase 4 – Validation & Cutover (Day 2-3)

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-401 | Monitor cluster for 1 hour—check queue depth, message rates, connection counts, alarms | | |
| TASK-402 | Run end-to-end smoke (VirtualCustomers → OrderService → downstream subscribers) in staging | | |
| TASK-403 | Trigger failover test (restart one node/pod) ensuring quorum/stream queues stay available | | |
| TASK-404 | Validate Prometheus dashboards and alert thresholds for new metrics names if any changed | | |
| TASK-405 | Promote to production (if using blue/green) once metrics stable and backlog zero | | |

### Phase 5 – Documentation & Handoff

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-501 | Update `plan/upgrade-phase0-platform-foundation-implementation-1.md` status + evidence links | | |
| TASK-502 | Archive exported definitions and Helm release logs under `artifacts/rabbitmq-upgrade/<date>/` | | |
| TASK-503 | Provide operational runbook updates (feature flag procedures, rollback steps) | | |

## 3. Alternatives

- **ALT-001:** Stay on RabbitMQ 4.1.x  
  **Rejected:** 4.2 extends community support to Oct 2028 and aligns with Khepri defaults; 4.1 loses community support earlier.citeturn0search0

- **ALT-002:** Replace RabbitMQ with cloud-native broker (e.g., Azure Service Bus)  
  **Rejected:** Violates ADR-0007 cloud-agnostic deployment goal and breaks parity across environments.

- **ALT-003:** Delay upgrade until RabbitMQ 4.3  
  **Rejected:** 4.3 is listed only as “Next” with no release date; staying on 4.1 delays remediation of CVEs and Khepri adoption.citeturn0search0

## 4. Dependencies

- **DEP-001:** ADR-0002 (Dapr secret store usage)  
- **DEP-002:** ADR-0013 (Secret management strategy)  
- **DEP-003:** `docs/research/infrastructure-versions-verification.md` (Bitnami chart and metrics guidance)  
- **DEP-004:** `values/values-azure.yaml.sample` (new metadata reference)  
- **DEP-005:** `plan/keda-cloud-autoscaling-implementation-1.md` for future ScaledObjects

## 5. Files to Update

- `values/values-<env>.yaml` – RabbitMQ metadata + secrets  
- `charts/infrastructure/` (future) – optional RabbitMQ subchart if we decide to vendor it for non-managed clusters  
- `docs/runbooks/rabbitmq.md` (new) – operational procedures  
- `scripts/` – smoke scripts referencing RabbitMQ endpoints if necessary

## 6. Testing & Validation

- **TEST-001:** Publish/subscribe round-trip via Dapr after upgrade (`rest-samples/order-service.rest` + `kubectl logs`)  
- **TEST-002:** High-volume soak (VirtualCustomers) for 30 minutes ensuring queue latency stays within baseline  
- **TEST-003:** Disaster recovery—restore from exported definitions into empty cluster  
- **TEST-004:** Security scan to confirm no critical CVEs remain on the broker image

## 7. Rollback Plan

1. Keep RabbitMQ 4.1.x Helm release (or managed instance) running as “blue” environment.  
2. If regression detected, switch application `rabbitmq_host` secret back to blue endpoint, redeploy Dapr components, and scale down 4.2 nodes.  
3. Re-import saved definitions to guarantee parity if rollback required for longer than one hour.
