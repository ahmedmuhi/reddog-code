---
goal: "Upgrade RabbitMQ deployment to 4.2.0 for all environments (Local & Cloud)"
version: 1.1
date_created: 2025-11-14
last_updated: 2025-11-17
owner: "Red Dog Modernization Team"
status: Planned
tags:
  - infrastructure
  - messaging
  - rabbitmq
  - phase-0
  - dapr
  - keda
  - local-dev
---

# Upgrade RabbitMQ to 4.2.0 (Local & Cloud)

RabbitMQ 4.2.0 shipped on 28 Oct 2025 and is the latest GA release with community support through October 2028. This version finalizes Khepri—the Raft-based metadata store—as the recommended default, making it the preferred target for the Phase 0 platform foundation upgrade.

**Scope Clarification (v1.1)**

- **Updated:** This plan covers **all** environments. To ensure local/prod parity, local development will run a containerized RabbitMQ 4.2.0 instance. This replaces the previous Redis-based pub/sub setup in local dev.
- Cloud environments (AKS/EKS/GKE) will run RabbitMQ 4.2.0 (via Helm chart).
- Plan covers the runtime upgrade, Helm/IaC changes, and Dapr component integration for both local and cloud.

**Estimated duration:** ~3-4 working days (prep, rollout, validation)
**Risk level:** MEDIUM

## Table of contents

- [Requirements & Constraints](#requirements--constraints)
- [Implementation Phases & Tasks](#implementation-phases--tasks)
- [Alternatives](#alternatives)
- [Dependencies](#dependencies)
- [Files to Update](#files-to-update)
- [Testing & Validation](#testing--validation)
- [Rollback Plan](#rollback-plan)
- [Deliverables](#deliverables)
- [Acceptance Criteria](#acceptance-criteria)
- [References](#references)

## Requirements & Constraints

### Functional requirements

- **REQ-001:** RabbitMQ must run version 4.2.0 (management image) in all environments.
- **REQ-001.5 (New):** Local development (kind, Docker Desktop) must use a single-node RabbitMQ 4.2.0 instance via `pubsub.rabbitmq`.
- **REQ-002:** Dapr `pubsub.rabbitmq` components must point at the cluster with correct metadata.
- **REQ-003:** Management plugin and Prometheus metrics remain enabled.
- **REQ-004:** Brokers must retain current exchanges, queues, etc. for cloud migration.

### Technical requirements

- **REQ-005:** Erlang/OTP 26.x is required on RabbitMQ nodes.
- **REQ-006:** Cloud Kubernetes clusters must provide PersistentVolumeClasses (SSD-backed storage recommended).
- **REQ-007:** Use the Bitnami RabbitMQ Helm chart in cloud; use a simple Docker container for local development.
- **REQ-008:** Enable/verify the `khepri_db` feature flag after upgrade (cloud).

### Security requirements

- **SEC-001:** Cloud credentials must be sourced via Kubernetes Secrets (per ADR-0013).
- **SEC-002:** Cloud Management UI must retain SSO or IP allow-lists.
- **SEC-003:** Ensure mitigation for CVE-2025-30219 is applied.
### Constraints

- **CON-001 & CON-002:** Local environment will host RabbitMQ; local dev setup is aligned with cloud.
- **CON-003:** Managed services (e.g., Azure Service Bus) are out of scope for this plan.

## Implementation Phases & Tasks

### Phase 1 – Inventory & Readiness (Day 0–1)

Tasks (Phase 1):

- [ ] TASK-101 — Document current *cloud* RabbitMQ topology.
- [ ] TASK-102 — Export cloud definitions: `rabbitmqctl export_definitions /tmp/rabbitmq-defs.json`.
- [ ] TASK-103 — Verify Erlang/OTP runtime version on cloud nodes/managed service.
- [ ] TASK-104 — Confirm Prometheus/monitoring expectations for RabbitMQ 4.2 metrics.

### Phase 2 – Cloud Infrastructure Upgrade Path (Day 1)

Tasks (Phase 2):

- [ ] TASK-201 — Pull Bitnami RabbitMQ chart 16.x and pin image tag `rabbitmq:4.2.0-management`.
- [ ] TASK-202 — Configure Helm values per cloud environment (replicas, storage, TLS, Khepri).
- [ ] TASK-203 — Ensure `khepri_db` feature flag is enabled post-upgrade (cloud): `rabbitmqctl enable_feature_flag khepri_db`.
- [ ] TASK-204 — Implement a rolling upgrade (Helm) or blue/green deployment strategy for cloud.

### Phase 3 – Dapr & App Integration (Day 2)

Tasks (Phase 3):

- [ ] TASK-301 — Update local dev scripts (e.g., `docker-compose.yaml` or startup script) to run a `rabbitmq:4.2.0-management` container.
- [ ] TASK-302 — Create `manifests/branch/base/components/pubsub.rabbitmq.yaml` for local dev (pointing to `amqp://localhost:5672` or container name).
- [ ] TASK-303 — Update cloud values files (e.g., `values/values-azure.yaml`) with new RabbitMQ metadata (host, durable, TLS).
- [ ] TASK-304 — Ensure ADR-0013-compliant secrets exist (RabbitMQ URI, TLS certs) and are referenced.
- [ ] TASK-305 — Re-run `helm template` / `helm upgrade` for `charts/reddog` to confirm Dapr components render correctly in all environments.
- [ ] TASK-306 — Validate Dapr `pubsub.rabbitmq` health by publishing sample events locally and in staging.

### Phase 4 – Validation & Cutover (Day 2–3)

Tasks (Phase 4):

- [ ] TASK-401 — Monitor cloud cluster for 1 hour—check queue depth, message rates, and alarms.
- [ ] TASK-402 — Run end-to-end smoke tests (VirtualCustomers → OrderService → ...) in staging.
- [ ] TASK-403 — Trigger failover test (restart one cloud pod) to verify quorum and resilience.
- [ ] TASK-404 — Validate Prometheus dashboards and alerting in cloud.
- [ ] TASK-405 — Promote to production (blue/green) once stable.

### Phase 5 – Documentation & Handoff

Tasks (Phase 5):

- [ ] TASK-501 — Update `plan/upgrade-phase0-platform-foundation-implementation-1.md` status.
- [ ] TASK-502 — Archive exported cloud definitions.
- [ ] TASK-503 — Update local development README to replace Redis pub/sub steps with the new RabbitMQ container instructions.

## Alternatives

  - **ALT-001:** Stay on RabbitMQ 4.1.x
      - **Rejected:** 4.2 extends community support to Oct 2028.
- **ALT-002:** Replace RabbitMQ with cloud-native broker (e.g., Azure Service Bus)
    - **Rejected:** Violates ADR-0007 (cloud-agnostic goal). Note: selecting a cloud-native broker is a viable alternative but out of scope for this plan.

## Dependencies

  - **DEP-001:** ADR-0002 (Dapr secret store usage)
  - **DEP-002:** ADR-0013 (Secret management strategy)
  - **DEP-003:** `docs/research/infrastructure-versions-verification.md`
  - **DEP-004:** `values/values-azure.yaml.sample`
  - **DEP-005:** `plan/keda-cloud-autoscaling-implementation-1.md`

## Files to Update

- [ ] **(New)** `manifests/branch/base/components/pubsub.rabbitmq.yaml` — for local dev
- [ ] `values/values-<env>.yaml` — Cloud RabbitMQ metadata + secrets
- [ ] `docs/runbooks/rabbitmq.md` — operational procedures
- [ ] **(New)** `README.md` / `docs/local-development.md` — reflect the new local setup

## Testing & Validation

  - **TEST-001:** Publish/subscribe round-trip via Dapr **locally**
  - **TEST-002:** Publish/subscribe round-trip via Dapr **in cloud staging**
  - **TEST-003:** High-volume soak (VirtualCustomers) in staging
  - **TEST-004:** Disaster recovery—restore from exported definitions (cloud).

## Rollback Plan

1. **(Cloud)** Keep RabbitMQ 4.1.x Helm release (or managed instance) running as "blue" during deployment.
2. **(Cloud)** If regression detected, switch `rabbitmq_host` secret back to blue endpoint or failover to the blue release.
3. **(Local)** If local container fails, developers can revert to the previous `redis` pub/sub component and Docker setup as a temporary workaround.
## Deliverables

- **Source code / charts:** Updated Helm chart values, Dapr component manifests, local `docker-compose` (if used).
- **Docs:** Updated runbooks, local dev README and operational runbooks for RabbitMQ.
- **Validation evidence:** Smoke test reports, monitoring dashboards, sample export/restore artifacts.

## Acceptance Criteria

- All tasks (TASK-101 → TASK-503) complete with linked PRs or references.
- Local dev uses RabbitMQ 4.2.0 with Dapr `pubsub.rabbitmq` and documentation updated.
- Cloud rollout completes without data loss and passes smoke and soak tests.
- Monitoring/Prometheus dashboards reflect the upgrade and KPIs are in expected ranges.

## References

- ADR-0013 — Secret Management strategy
- `docs/research/infrastructure-versions-verification.md`
- `values/values-azure.yaml.sample`

<!-- end list -->
