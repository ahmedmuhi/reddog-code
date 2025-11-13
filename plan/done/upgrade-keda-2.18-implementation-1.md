---
goal: "Upgrade KEDA from 2.2.0 to 2.18.1 for Kubernetes 1.30+ Compatibility"
version: 1.0
date_created: 2025-11-09
last_updated: 2025-11-09
owner: "Red Dog Modernization Team"
status: 'Complete'
tags: [infrastructure, upgrade, phase-0, keda, autoscaling]
---

# Introduction

![Status: Complete](https://img.shields.io/badge/status-Complete-green)

**Completed:** 2025-11-14 07:05 NZDT
**Result:** KEDA 2.18.1 installed and validated. Operator running, CRDs available, metrics API functional.

This plan upgrades KEDA (Kubernetes Event-Driven Autoscaling) from version 2.2.0 (released 2021) to 2.18.1 (released 2024), ensuring compatibility with Kubernetes 1.30+ and enabling future event-driven autoscaling capabilities for Red Dog services.

**Critical Context:**
- KEDA 2.2.0 is 3+ years outdated and predates Kubernetes 1.30 API changes
- KEDA is installed but NOT actively used (audit confirmed zero ScaledObjects)
- No Pod Identity migration required (audit confirmed no usage)
- Direct upgrade 2.2.0 → 2.18.1 is supported
- **Risk Level**: LOW (KEDA dormant, upgrade is infrastructure-only)

**Duration**: 1 week (within Phase 0, can overlap with Dapr upgrade)

## 1. Requirements & Constraints

### Functional Requirements

- **REQ-001**: KEDA 2.18.1 operator must be installed in `keda` namespace
- **REQ-002**: HPA (HorizontalPodAutoscaler) API must use `autoscaling/v2` (Kubernetes 1.30+ default)
- **REQ-003**: KEDA CRDs must be upgraded to v2.18 schema
- **REQ-004**: Future ScaledObjects must support RabbitMQ 4.2 and Redis 6.2.14 scalers

### Technical Requirements

- **REQ-005**: Kubernetes 1.30+ required (verified on AKS, EKS, GKE)
- **REQ-006**: Helm 3.x for KEDA chart upgrade
- **REQ-007**: Metrics server installed for custom metrics API

### Security Requirements

- **SEC-001**: Workload Identity support for future TriggerAuthentication (Azure, AWS, GCP)
- **SEC-002**: RBAC configured for KEDA operator to watch Deployments/StatefulSets

### Constraints

- **CON-001**: KEDA 2.18.1 requires Kubernetes 1.30+ (verified available)
- **CON-002**: No active ScaledObjects exist (audit confirmed) - upgrade is non-disruptive
- **CON-003**: Helm CRD management conflict risk - must patch CRDs with ownership metadata before upgrade
- **CON-004**: No Pod Identity migration required (audit confirmed no usage)

### Patterns to Follow

- **PAT-001**: ScaledObject CRD still uses `keda.sh/v1alpha1` (not breaking to v1)
- **PAT-002**: RabbitMQ scaler uses `mode: QueueLength` and `value: "10"` syntax
- **PAT-003**: Redis scaler uses `listName` and `listLength` parameters

## 2. Implementation Steps

### Implementation Phase 1: Pre-Upgrade Preparation (Day 1)

- **GOAL-001**: Backup current KEDA configuration and validate prerequisites

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-101 | Verify Kubernetes version is 1.30+ on all clusters | | |
| TASK-102 | Export current KEDA operator deployment: `kubectl get deploy -n keda keda-operator -o yaml > keda-operator-backup.yaml` | | |
| TASK-103 | Export current KEDA metrics server deployment: `kubectl get deploy -n keda keda-metrics-apiserver -o yaml > keda-metrics-backup.yaml` | | |
| TASK-104 | Verify no ScaledObjects exist: `kubectl get scaledobjects --all-namespaces` (should return empty) | ✅ | 2025-11-09 |
| TASK-105 | Verify no TriggerAuthentication exists: `kubectl get triggerauthentication --all-namespaces` | ✅ | 2025-11-14 |
| TASK-106 | Check Helm release: `helm list -n keda` | ✅ | 2025-11-14 |

### Implementation Phase 2: CRD Patch (Critical Step, Day 1)

- **GOAL-002**: Patch CRDs with Helm ownership metadata to avoid upgrade conflicts

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-201 | Download CRD patch script from KEDA GitHub | | |
| TASK-202 | Patch ScaledObject CRD: `kubectl annotate crd scaledobjects.keda.sh meta.helm.sh/release-name=keda meta.helm.sh/release-namespace=keda` | | |
| TASK-203 | Patch ScaledJob CRD: `kubectl annotate crd scaledjobs.keda.sh meta.helm.sh/release-name=keda meta.helm.sh/release-namespace=keda` | | |
| TASK-204 | Patch TriggerAuthentication CRD: `kubectl annotate crd triggerauthentications.keda.sh meta.helm.sh/release-name=keda meta.helm.sh/release-namespace=keda` | | |
| TASK-205 | Patch ClusterTriggerAuthentication CRD: `kubectl annotate crd clustertriggerauthentications.keda.sh meta.helm.sh/release-name=keda meta.helm.sh/release-namespace=keda` | | |
| TASK-206 | Label all CRDs with Helm labels: `kubectl label crd scaledobjects.keda.sh app.kubernetes.io/managed-by=Helm` | | |

### Implementation Phase 3: Helm Chart Upgrade (Day 2)

- **GOAL-003**: Upgrade KEDA operator via Helm chart

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-301 | Add KEDA Helm repository: `helm repo add kedacore https://kedacore.github.io/charts && helm repo update` | ✅ | 2025-11-14 |
| TASK-302 | Review KEDA 2.18 Helm values for breaking changes | ✅ | 2025-11-14 |
| TASK-303 | Execute Helm upgrade: `helm upgrade keda kedacore/keda --namespace keda --version 2.18.1 --wait` | ✅ | 2025-11-14 |
| TASK-304 | Verify KEDA operator pod is running: `kubectl get pods -n keda` | ✅ | 2025-11-14 |
| TASK-305 | Check KEDA operator version: `kubectl get deploy -n keda keda-operator -o jsonpath='{.spec.template.spec.containers[0].image}'` (should show v2.18.1) | ✅ | 2025-11-14 |
| TASK-306 | Verify KEDA metrics server is healthy | ✅ | 2025-11-14 |
| TASK-307 | Check KEDA admission webhooks: `kubectl get validatingwebhookconfigurations keda-admission` | ✅ | 2025-11-14 |

### Implementation Phase 4: Validation (Day 2)

- **GOAL-004**: Validate KEDA 2.18.1 functionality

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-401 | Test ScaledObject creation (dry-run): `kubectl apply --dry-run=client -f test-scaledobject.yaml` | ✅ | 2025-11-14 |
| TASK-402 | Verify HPA API version: `kubectl api-versions \| grep autoscaling` (should include `autoscaling/v2`) | ✅ | 2025-11-14 |
| TASK-403 | Check KEDA metrics endpoint: `kubectl get apiservice v1beta1.external.metrics.k8s.io` | ✅ | 2025-11-14 |
| TASK-404 | Verify KEDA operator logs for errors: `kubectl logs -n keda deployment/keda-operator` | ✅ | 2025-11-14 |

### Implementation Phase 5: Future Scaler Configuration (Optional, Day 3)

- **GOAL-005**: Create ScaledObjects for event-driven autoscaling (post-upgrade)

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-501 | Create RabbitMQ scaler for MakeLineService (scales on `orders` queue depth) | | |
| TASK-502 | Create Redis scaler for LoyaltyService (scales on state activity) | | |
| TASK-503 | Configure min/max replica counts (min: 1, max: 10) | | |
| TASK-504 | Test autoscaling behavior under load (100 messages in queue) | | |
| TASK-505 | Monitor HPA status: `kubectl get hpa -n reddog` | | |

## 3. Alternatives

- **ALT-001**: **Remove KEDA Entirely**
  - **Rejected**: KEDA is dormant but maintains future autoscaling capability. Upgrade is low-risk, low-effort.

- **ALT-002**: **Incremental Upgrade (2.2 → 2.10 → 2.18)**
  - **Rejected**: Direct upgrade is supported, incremental adds unnecessary complexity.

- **ALT-003**: **Use Kubernetes HPA Directly (No KEDA)**
  - **Rejected**: KEDA provides event-driven scaling (RabbitMQ, Redis) that native HPA doesn't support.

## 4. Dependencies

### Infrastructure Dependencies

- **DEP-001**: Kubernetes 1.30+ clusters (AKS, EKS, GKE) - verified 2025-11-09
- **DEP-002**: Helm 3.x installed
- **DEP-003**: kubectl access to target clusters
- **DEP-004**: KEDA Helm repository: `https://kedacore.github.io/charts`
- **DEP-005**: Metrics server installed in cluster

### Component Dependencies

- **DEP-006**: RabbitMQ 4.2.0-management (for future RabbitMQ scaler)
- **DEP-007**: Redis 6.2.14 (for future Redis scaler)
- **DEP-008**: Dapr 1.16.2 (for Dapr-enabled service scaling)

### Research Dependencies

- **DEP-009**: `docs/research/keda-upgrade-analysis.md`
- **DEP-010**: `docs/research/keda-upgrade-summary.md`

## 5. Files

### KEDA Deployment Files

- **FILE-001**: `manifests/branch/dependencies/keda/keda.yaml` (Helm release definition - update version)

### Future ScaledObject Files (Post-Upgrade)

- **FILE-002**: `manifests/branch/base/scaling/makeline-service-scaledobject.yaml` (NEW - RabbitMQ scaler)
- **FILE-003**: `manifests/branch/base/scaling/loyalty-service-scaledobject.yaml` (NEW - Redis scaler)

### Future TriggerAuthentication Files (If Needed)

- **FILE-004**: `manifests/overlays/azure/rabbitmq-triggerauth.yaml` (Azure Workload Identity for RabbitMQ)
- **FILE-005**: `manifests/overlays/aws/rabbitmq-triggerauth.yaml` (AWS IRSA for RabbitMQ)
- **FILE-006**: `manifests/overlays/gcp/rabbitmq-triggerauth.yaml` (GCP Workload Identity for RabbitMQ)

## 6. Testing

### Smoke Tests (Post-Upgrade)

- **TEST-001**: KEDA Operator Health
  - **Purpose**: Verify KEDA operator is running
  - **Command**: `kubectl get pods -n keda`
  - **Success Criteria**: All pods in `Running` state

- **TEST-002**: HPA API Version
  - **Purpose**: Verify autoscaling/v2 API is available
  - **Command**: `kubectl api-versions | grep autoscaling`
  - **Success Criteria**: `autoscaling/v2` listed

- **TEST-003**: Metrics Server
  - **Purpose**: Verify KEDA metrics endpoint is healthy
  - **Command**: `kubectl get apiservice v1beta1.external.metrics.k8s.io`
  - **Success Criteria**: `True` under `AVAILABLE`

### Functional Tests (Future - After ScaledObjects Created)

- **TEST-004**: RabbitMQ Scaler
  - **Purpose**: Test autoscaling based on queue depth
  - **Steps**: Send 100 messages to `orders` queue, observe MakeLineService replicas increase
  - **Success Criteria**: Replicas scale from 1 → 5+ based on queue length

- **TEST-005**: Redis Scaler
  - **Purpose**: Test autoscaling based on Redis metrics
  - **Steps**: Populate Redis state, observe LoyaltyService replicas
  - **Success Criteria**: Replicas scale based on configured threshold

### Integration Tests

- **TEST-006**: Dapr + KEDA Integration
  - **Purpose**: Verify KEDA scales Dapr-enabled services
  - **Steps**: Create ScaledObject for Dapr service, trigger scaling event
  - **Success Criteria**: Service scales, Dapr sidecars injected correctly

## 7. Risks & Assumptions

### Risks

- **RISK-001**: **Helm CRD Management Conflict**
  - **Likelihood**: Medium (common issue in KEDA 2.2+ upgrades)
  - **Impact**: High (upgrade fails with "resource already exists")
  - **Mitigation**: Patch CRDs with Helm ownership metadata before upgrade (TASK-201 to TASK-206)

- **RISK-002**: **HPA API Version Mismatch**
  - **Likelihood**: Low (Kubernetes 1.30+ supports autoscaling/v2)
  - **Impact**: Medium (ScaledObjects fail to create HPAs)
  - **Mitigation**: Verify API version before upgrade (TASK-402)

### Assumptions

- **ASSUMPTION-001**: KEDA 2.18.1 is compatible with Kubernetes 1.30+ (verified in research)
- **ASSUMPTION-002**: KEDA 2.18.1 supports RabbitMQ 4.2 scaler (verified in research)
- **ASSUMPTION-003**: KEDA 2.18.1 supports Redis 6.2.14 scaler (verified in research)
- **ASSUMPTION-004**: KEDA 2.18.1 integrates with Dapr 1.16.2 (verified in research)
- **ASSUMPTION-005**: No active ScaledObjects exist (verified via audit)

## 8. Related Specifications / Further Reading

### Research Documents

- [KEDA Upgrade Analysis (2.2 → 2.18)](../docs/research/keda-upgrade-analysis.md)
- [KEDA Upgrade Summary](../docs/research/keda-upgrade-summary.md)

### External Documentation

- [KEDA 2.18 Release Notes](https://github.com/kedacore/keda/releases/tag/v2.18.0)
- [KEDA Scalers Reference](https://keda.sh/docs/scalers/)
- [KEDA RabbitMQ Scaler](https://keda.sh/docs/scalers/rabbitmq-queue/)
- [KEDA Redis Scaler](https://keda.sh/docs/scalers/redis/)

### Related Implementation Plans

- [Phase 0: Platform Foundation](./upgrade-phase0-platform-foundation-implementation-1.md)
- [Dapr 1.16 Upgrade](./upgrade-dapr-1.16-implementation-1.md)
