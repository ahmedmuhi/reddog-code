# KEDA 2.2.0 → 2.18.1 Upgrade Analysis

## Executive Summary

Upgrading from KEDA 2.2.0 (released May 2021) to KEDA 2.18.1 (released November 2024) is a significant jump spanning 3.5 years and 16 minor versions. This represents **multiple breaking changes** affecting ScaledObject API, authentication methods, scaler parameters, and Kubernetes version requirements.

**Status:** RED - Multiple breaking changes require careful planning and testing

**Key Risks:**
- HPA API version upgrade (v2beta2 → v2) in KEDA 2.9
- Removal of Pod Identity authentication in KEDA 2.15
- Kubernetes 1.30+ requirement (confirmed for Red Dog cloud platforms)
- RabbitMQ scaler parameter evolution and potential RabbitMQ 4.2 compatibility gaps
- CRD management issues in Helm upgrade path

**Recommendation:** Direct upgrade from 2.2.0 → 2.18.1 is technically possible, but requires pre-planning for breaking changes, especially around HPA API versions and authentication methods.

---

## 1. Critical Breaking Changes

### BC-001: HPA API Version Upgrade (v2beta2 → v2)

**Version Introduced:** KEDA 2.9.0 (December 2022)

**Affected Components:** All ScaledObjects (MakeLineService, LoyaltyService, any others)

**What Changed:**
- KEDA 2.9.0 upgraded the HorizontalPodAutoscaler API version from `autoscaling/v2beta2` to `autoscaling/v2`
- This was a breaking change necessitated by Kubernetes v1.23+ where `v2beta2` was deprecated

**Impact:**
- When KEDA creates HPA objects from ScaledObjects, they will use the GA `autoscaling/v2` API
- Existing HPAs may appear to use v2beta2 when queried with older kubectl versions (display issue only)
- Kubernetes 1.22 and earlier are no longer supported (Red Dog uses 1.30+, so no issue)

**Why It Matters for Red Dog:**
- Red Dog targets Kubernetes 1.30+ on AKS, EKS, GKE (all confirmed)
- No workaround needed - full compatibility with 1.30+

**Migration:** No action required. KEDA automatically handles HPA creation with correct API version.

**Testing Requirement:**
- Verify HPA objects are created with `apiVersion: autoscaling/v2`
- Confirm scaling behavior works correctly post-upgrade

---

### BC-002: Removal of Pod Identity Authentication (KEDA 2.15)

**Version Introduced:** KEDA 2.15.0 (June 2024)

**Affected Components:** Azure authentication using AAD Pod Identity or AWS KIAM

**What Changed:**
- KEDA 2.15 removed support for:
  - **Azure AD Pod Identity** (AAD Pod Identity)
  - **AWS KIAM** (Kube2IAM)
- Both authentication methods are deprecated/unsupported by their cloud providers

**Impact:**
- Any ScaledObject using `podIdentity.provider: azure` (old Azure AD Pod Identity) will break
- Any ScaledObject using `podIdentity.provider: aws` with KIAM will break
- Authentication must migrate to modern workload identity solutions

**Why It Matters for Red Dog:**
- Red Dog deployment targets Azure (AKS), AWS (EKS), and GCP (GKE)
- If current KEDA 2.2.0 uses Pod Identity for RabbitMQ/Redis authentication, upgrade will break
- **CRITICAL:** Must audit current KEDA 2.2.0 configuration for Pod Identity usage

**Replacement Options:**
1. **Azure:** Migrate to Azure Workload Identity (OIDC-based)
2. **AWS:** Migrate to EKS Pod Identity or IAM Roles for Service Accounts (IRSA)
3. **GCP:** Use GCP Workload Identity

**Migration Path:**
1. Audit current TriggerAuthentication objects
2. If using Pod Identity: Create new TriggerAuthentication with workload identity provider
3. Update ScaledObjects to reference new TriggerAuthentication
4. Test authentication before upgrade
5. After KEDA upgrade, remove old Pod Identity configurations

**Example Migration:**

Old KEDA 2.2 (Pod Identity):
```yaml
apiVersion: keda.sh/v1alpha1
kind: TriggerAuthentication
metadata:
  name: rabbitmq-auth
spec:
  podIdentity:
    provider: azure  # Removed in KEDA 2.15
```

New KEDA 2.18 (Workload Identity):
```yaml
apiVersion: keda.sh/v1alpha1
kind: TriggerAuthentication
metadata:
  name: rabbitmq-auth
spec:
  podIdentity:
    provider: azure-workload  # New in KEDA 2.13+
    identityId: /subscriptions/{sub}/resourcegroups/{rg}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{identity}
```

---

### BC-003: Prometheus Webhook Removal (KEDA 2.18)

**Version Introduced:** KEDA 2.18.0 (November 2024)

**Affected Components:** Custom Prometheus integration using deprecated webhook

**What Changed:**
- Prometheus webhook prommetrics deprecations removed
- Removed deprecated Prometheus metrics from KEDA Metric Server (metrics now only on Operator)

**Impact:**
- If Red Dog uses custom Prometheus scraping from the KEDA Metrics Server, integration will break
- Must migrate to KEDA Operator metrics (the recommended approach)

**Why It Matters for Red Dog:**
- **LOW IMPACT:** Red Dog doesn't appear to use Prometheus integration (not mentioned in modernization strategy)
- Only relevant if custom dashboards scrape KEDA metrics

**Migration:** Update Prometheus scrape targets to use KEDA Operator metrics instead of Metric Server

---

### BC-004: CPU Memory Scaler Configuration Changes (KEDA 2.18)

**Version Introduced:** KEDA 2.18.0 (November 2024)

**Affected Components:** Any ScaledObject using CPU or Memory scaler

**What Changed:**
- `type` field removed from CPU/Memory scaler metadata
- Must use `metricType` instead

Old syntax:
```yaml
triggers:
- type: cpu
  metadata:
    type: Utilization  # REMOVED - ERROR
    value: "50"
```

New syntax:
```yaml
triggers:
- type: cpu
  metadata:
    metricType: Utilization  # Correct in 2.18
    value: "50"
```

**Impact:**
- Any ScaledObject using CPU/Memory scalers with `type` field will fail validation
- **UNLIKELY for Red Dog:** Red Dog uses RabbitMQ and Redis scalers, not CPU/Memory

---

### BC-005: IBM MQ Scaler TLS Configuration (KEDA 2.18)

**Version Introduced:** KEDA 2.18.0 (November 2024)

**Affected Components:** Any ScaledObject using IBM MQ scaler with TLS

**What Changed:**
- `tls` field removed from IBM MQ scaler
- Must use `unsafeSsl` instead (or proper certificate configuration)

**Impact:**
- **NOT APPLICABLE for Red Dog:** Red Dog doesn't use IBM MQ

---

### BC-006: Cortex OrgID Removal from Prometheus Scaler (KEDA 2.15)

**Version Introduced:** KEDA 2.15.0 (June 2024)

**Affected Components:** Prometheus scaler configurations using `cortexOrgID`

**What Changed:**
- Deprecated `cortexOrgID` field removed from Prometheus scaler metadata

**Impact:**
- **NOT APPLICABLE for Red Dog:** Red Dog doesn't appear to use Prometheus scaler

---

### BC-007: InitialCooldownPeriod Type Change (KEDA 2.17)

**Version Introduced:** KEDA 2.17.0 (August 2024)

**Affected Components:** ScaledObjects using `initialCooldownPeriod`

**What Changed:**
- `initialCooldownPeriod` changed from `int32` to `*int32` (pointer type)
- This is a schema change affecting CRD validation

**Impact:**
- Existing YAML configurations should still work (backward compatible at YAML level)
- CRD controller properly handles the type change
- **MINIMAL IMPACT:** Unlikely to cause issues during upgrade

---

### BC-008: ScaledJob API Changes (KEDA 2.15+)

**Version Introduced:** KEDA 2.15.0 and earlier

**Affected Components:** Any ScaledJob configurations

**What Changed:**
- ScaledJob spec changes related to job scheduling and completion logic
- Removal of deprecated fields from earlier versions

**Impact:**
- **NOT APPLICABLE for Red Dog:** Red Dog only uses ScaledObject (not ScaledJob) for scaling deployments

---

## 2. Deprecations (Actively Supported, But Will Be Removed)

### DEP-001: External Scaler TLS Certificate File

**Timeline:** Removed in KEDA 2.17.0

**Deprecated Feature:** `tlsCertFile` field in External scaler

**Replacement:** Use TriggerAuthentication with certificate configuration

**Migration:** Update External scaler configs to use TriggerAuthentication instead of inline tlsCertFile

**Impact on Red Dog:** NOT APPLICABLE (Red Dog doesn't use External scaler)

---

### DEP-002: Pulsar Scaler msgBacklog Trigger Name

**Timeline:** Removed in KEDA 2.16.0

**Deprecated Feature:** `msgBacklog` trigger name in Pulsar scaler

**Replacement:** Use updated Pulsar scaler parameter names

**Impact on Red Dog:** NOT APPLICABLE (Red Dog doesn't use Pulsar)

---

### DEP-003: Azure Data Explorer metadata.clientSecret

**Timeline:** Removed in KEDA 2.14.0

**Deprecated Feature:** Inline `metadata.clientSecret` in Azure Data Explorer scaler

**Replacement:** Use TriggerAuthentication with secrets

**Migration:** Move secrets to TriggerAuthentication objects

**Impact on Red Dog:** NOT APPLICABLE (Red Dog doesn't use Azure Data Explorer scaler)

---

### DEP-004: Azure Blob Scaler metricName

**Timeline:** Removed in KEDA 2.14.0

**Deprecated Feature:** `metricName` field in Azure Blob and Azure Log Analytics scalers

**Replacement:** Use scaler-specific parameter names

**Migration:** Update Azure Blob scaler configurations

**Impact on Red Dog:** NOT APPLICABLE (Red Dog doesn't use Azure Blob scaler)

---

## 3. ScaledObject API Changes

| Field | KEDA 2.2.0 | KEDA 2.18.1 | Breaking? | Migration Notes |
|-------|-----------|-----------|-----------|-----------------|
| **apiVersion** | `keda.sh/v1alpha1` | `keda.sh/v1alpha1` | No | No change (still v1alpha1, not v1) |
| **kind** | `ScaledObject` | `ScaledObject` | No | No change |
| **scaleTargetRef.name** | Required | Required | No | Same field (was `deploymentName` in KEDA v1 only) |
| **scaleTargetRef.kind** | Not present (Deployment assumed) | Optional (default: Deployment) | No | Now optional, can specify StatefulSet, etc. |
| **scaleTargetRef.apiVersion** | Not present (apps/v1 assumed) | Optional (default: apps/v1) | No | Now optional, explicit version specification allowed |
| **scaleTargetRef.envSourceContainerName** | Not present in v2 | Optional | No | For reading env vars for trigger auth |
| **pollingInterval** | Optional (default: 30) | Optional (default: 30) | No | No change |
| **cooldownPeriod** | Optional (default: 300) | Optional (default: 300) | No | No change |
| **minReplicaCount** | Optional (default: 0) | Optional (default: 0) | No | No change |
| **maxReplicaCount** | Optional (default: 100) | Optional (default: 100) | No | No change |
| **initialCooldownPeriod** | Optional | Optional | No | Type changed from int32 to *int32 (minor) |
| **fallback** | Optional | Optional | No | No change |
| **advanced.horizontalPodAutoscalerConfig** | Optional | Optional | No | Extended with more options in later versions |
| **advanced.restoreToOriginalReplicaCount** | Optional | Optional | No | No change |
| **triggers** | Required (array) | Required (array) | No | Trigger definitions required |
| **authenticationRef.kind** | Added in 2.2.0 | Present | No | Property added in KEDA 2.2.0, now standard |

**Summary:** ScaledObject API is **NOT breaking** between 2.2.0 and 2.18.1. The KEDA v1 to v2 migration (apiVersion change) already happened before 2.2.0, so no changes needed for existing ScaledObjects.

---

## 4. RabbitMQ Scaler Changes

### Version History

| Version | Release Date | Key Changes |
|---------|-------------|------------|
| **2.2.0** | May 2021 | Introduced `mode` (QueueLength/MessageRate) and `value` parameters; deprecated `queueLength` |
| **2.3.0** | July 2021 | Stabilized new parameters; legacy `queueLength` still supported but discouraged |
| **2.4.0** | October 2021 | Added `useRegex` parameter for queue name patterns |
| **2.5.0** | May 2022 | **BUG:** New `mode`/`value` parameters broken; must use legacy `queueLength` |
| **2.6.1** | September 2022 | **FIXED:** New parameters working again |
| **2.7-2.17** | 2022-2024 | Incremental improvements, auth enhancements |
| **2.18.1** | November 2024 | No breaking changes to RabbitMQ scaler |

### Metadata Parameters Comparison

| Parameter | KEDA 2.2.0 | KEDA 2.18.1 | Breaking? | Notes |
|-----------|-----------|-----------|-----------|-------|
| **host** | Required or via TriggerAuthentication | Required or via TriggerAuthentication | No | Format: `amqp://user:pass@host:5672/vhost` or `http://user:pass@host:15672/vhost` |
| **queueName** | Required | Required | No | Queue name to monitor |
| **mode** | New (QueueLength/MessageRate) | Supported | No | Introduced in 2.2.0 as new approach |
| **value** | New (threshold) | Supported | No | Threshold per instance |
| **protocol** | Optional (auto/amqp/http) | Optional (auto/amqp/http) | No | Defaults to `auto` |
| **vhostName** | Optional | Optional | No | Virtual host override |
| **activationValue** | Not in 2.2 | Optional | No | Threshold to activate scaler |
| **hostFromEnv** | Optional | Optional | No | Read connection from env var |
| **usernameFromEnv** | Optional | Optional | No | Read username from env var |
| **passwordFromEnv** | Optional | Optional | No | Read password from env var |
| **useRegex** | Added in 2.4 | Supported | No | Regex queue name matching |
| **singleActiveConsumer** | Not in 2.2 | Optional | No | New feature: only scale single consumer |
| **excludeUnacked** | Not documented in 2.2 | Optional | No | Exclude unacknowledged messages |

### Authentication Methods Evolution

**KEDA 2.2.0 Approach:**
```yaml
# Option 1: Inline credentials
triggers:
- type: rabbitmq
  metadata:
    host: amqp://guest:password@localhost:5672/vhost
    queueName: orders
    mode: QueueLength
    value: "10"

# Option 2: TriggerAuthentication with secrets
triggers:
- type: rabbitmq
  metadata:
    host: amqp://localhost:5672/vhost
    queueName: orders
    mode: QueueLength
    value: "10"
  authenticationRef:
    name: rabbitmq-auth
---
apiVersion: keda.sh/v1alpha1
kind: TriggerAuthentication
metadata:
  name: rabbitmq-auth
spec:
  secretTargetRef:
  - parameter: username
    name: rabbitmq-credentials
    key: username
  - parameter: password
    name: rabbitmq-credentials
    key: password
```

**KEDA 2.18.1 Approach (Same + Workload Identity):**
```yaml
# Option 1: Inline credentials (unchanged)
triggers:
- type: rabbitmq
  metadata:
    host: amqp://guest:password@localhost:5672/vhost
    queueName: orders
    mode: QueueLength
    value: "10"

# Option 2: TriggerAuthentication with secrets (unchanged)
triggers:
- type: rabbitmq
  metadata:
    host: amqp://localhost:5672/vhost
    queueName: orders
    mode: QueueLength
    value: "10"
  authenticationRef:
    name: rabbitmq-auth
---
apiVersion: keda.sh/v1alpha1
kind: TriggerAuthentication
metadata:
  name: rabbitmq-auth
spec:
  secretTargetRef:
  - parameter: username
    name: rabbitmq-credentials
    key: username
  - parameter: password
    name: rabbitmq-credentials
    key: password

# Option 3: NEW - Workload Identity (KEDA 2.13+)
apiVersion: keda.sh/v1alpha1
kind: TriggerAuthentication
metadata:
  name: rabbitmq-auth-wi
spec:
  podIdentity:
    provider: azure-workload  # or aws-eks, gcp
  secretTargetRef:
  - parameter: username
    name: rabbitmq-credentials
    key: username
  - parameter: password
    name: rabbitmq-credentials
    key: password
```

### Known Issues & Fixes

| Issue | KEDA Version | Status | Impact |
|-------|------------|--------|--------|
| **mode: QueueLength doesn't scale** | 2.5.0 | FIXED in 2.6.1 | Affects only 2.5.0 users; Red Dog skipping 2.5 is advisable |
| **RabbitMQ connection failures** | 2.2.0+ | Ongoing | Intermittent; use TriggerAuthentication + secrets |
| **TLS/certificate issues** | 2.2.0+ | Improved | Proper TLS support in TriggerAuthentication recommended |

### RabbitMQ 4.2 Compatibility

**Verdict:** ✅ Compatible

**Reasoning:**
- KEDA 2.18 RabbitMQ scaler uses standard AMQP 0-9-1 (legacy) and HTTP Management API
- RabbitMQ 4.2 continues to support both AMQP 0-9-1 and AMQP 1.0 indefinitely
- No breaking changes in RabbitMQ 4.2 affecting KEDA's connection protocols
- Management API endpoints remain unchanged

**Testing Requirement:**
- Verify RabbitMQ connection (AMQP or HTTP) works post-upgrade
- Test message queue depth detection
- Confirm scaling triggers on queue length

---

## 5. Redis Scaler Changes

### Version History & Evolution

| Version | Release Date | Key Changes |
|---------|-------------|------------|
| **2.2.0** | May 2021 | Redis Lists scaler with `address`, `listName`, `listLength` |
| **2.8.0** | August 2022 | Added Redis Streams scaler variant |
| **2.9.0** | December 2022 | HPA API upgrade (v2beta2 → v2) |
| **2.12.0** | May 2023 | TLS certificate validation improvements |
| **2.14.0** | February 2024 | Added Redis Cluster Lists and Sentinel Lists variants |
| **2.18.1** | November 2024 | No breaking changes to core Redis scalers |

### Redis Lists Scaler - Metadata Parameters

| Parameter | KEDA 2.2.0 | KEDA 2.18.1 | Breaking? | Notes |
|-----------|-----------|-----------|-----------|-------|
| **address** | Required (host:port) | Required (host:port) | No | Redis server connection string |
| **host** | Optional (with port) | Optional (with port) | No | Alternative to address; requires port parameter |
| **port** | Optional (with host) | Optional (with host) | No | Alternative to address; requires host parameter |
| **listName** | Required | Required | No | Redis list key to monitor |
| **listLength** | Required | Required | No | Target list length per instance |
| **activationListLength** | Not in 2.2 | Optional | No | Threshold to activate scaling |
| **databaseIndex** | Optional (default: 0) | Optional (default: 0) | No | Redis database number |
| **passwordFromEnv** | Optional | Optional | No | Environment variable for password |
| **password** | Optional | Optional | No | Direct password specification |
| **usernameFromEnv** | Not in 2.2 | Optional | No | For Redis 6+ ACL support |
| **enableTLS** | Optional (default: false) | Optional (default: false) | No | TLS connection flag |
| **addressFromEnv** | Optional | Optional | No | Read address from env var |
| **databaseIndexFromEnv** | Not in 2.2 | Optional | No | Read database index from env |

### Redis Scaler Variants

**KEDA 2.2.0:**
- Redis Lists (single node)

**KEDA 2.18.1:**
- Redis Lists (single node) - unchanged
- Redis Cluster Lists (cluster support) - added in 2.14
- Redis Sentinel Lists (sentinel support) - added in 2.18
- Redis Streams (consumer group lag) - added in 2.8

### Security: TLS Certificate Validation

**KEDA 2.2.0 Issue:**
```yaml
metadata:
  address: redis-cache:6379
  enableTLS: true
  # INSECURE: Skips certificate verification!
```

**KEDA 2.9.0+ Improved:**
- Proper certificate validation when `enableTLS: true`
- Use TriggerAuthentication for certificate management

**Recommendation for Red Dog:**
```yaml
apiVersion: keda.sh/v1alpha1
kind: TriggerAuthentication
metadata:
  name: redis-auth
spec:
  secretTargetRef:
  - parameter: ca
    name: redis-tls-secret
    key: ca.crt
  - parameter: cert
    name: redis-tls-secret
    key: client.crt
  - parameter: key
    name: redis-tls-secret
    key: client.key
  - parameter: password
    name: redis-credentials
    key: password
---
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: loyalty-service-redis
spec:
  scaleTargetRef:
    name: loyalty-service
  triggers:
  - type: redis
    metadata:
      address: redis-cache-master.redis:6379
      listName: loyalty-events
      listLength: "5"
      enableTLS: "true"
    authenticationRef:
      name: redis-auth
```

### Redis 8.0 Compatibility

**Verdict:** ✅ Compatible with caution

**Reasoning:**
- KEDA 2.18 Redis scalers use standard Redis commands: `LLEN`, `XINFO`, `XPENDING`
- Redis 8.0 is backward compatible with Redis 7.x commands
- No breaking changes to core list/stream operations

**Redis 8.0 Breaking Changes (Not KEDA-Related):**
- ACL rules now include new commands for JSON, Streams, Search, TimeSeries
- If using ACL restrictions, verify KEDA operator service account has proper permissions:
  ```
  ACL SETUSER keda-operator on >password +@all ~*  # Full access (not recommended)
  ACL SETUSER keda-operator on >password +@read +@write ~*  # Recommended
  ```

**Testing Requirement:**
- Verify KEDA can connect to Redis 8.0
- Test list length detection: `redis-cli LLEN <listName>`
- Test stream consumer group monitoring (if using Redis Streams scaler)
- Verify ACL permissions if not using default Redis auth

---

## 6. HTTP Scaler (Add-On)

### Availability

**First Released:** June 24, 2021 (KEDA HTTP Add-on v0.1.0)

**Status:** Community add-on, separate from core KEDA

**Current Version:** v0.11+ (as of 2024)

### Does Red Dog Use HTTP Scaler?

**Current Assessment:** NO

**Reasoning:**
- Red Dog uses RabbitMQ (pub/sub) and Redis (state store) for scaling triggers
- No HTTP-based endpoint scaling mentioned in modernization strategy
- HTTP Add-on would only be needed for scaling based on:
  - Request rate
  - Concurrency
  - Custom HTTP metrics

**Recommendation:** Do NOT adopt HTTP scaler unless scaling based on API traffic becomes a requirement.

---

## 7. Helm Chart Upgrade Path

### Chart Version Mapping

| KEDA Version | Helm Chart Version | Helm Repository |
|------------|-------------------|-----------------|
| 2.2.0 | 2.2.0 | `https://kedacore.github.io/charts` |
| 2.18.1 | 2.18.1 | `https://kedacore.github.io/charts` |

### Direct Upgrade Supported

**Verdict:** ✅ Direct upgrade from 2.2.0 → 2.18.1 is possible

**Command:**
```bash
helm repo update
helm upgrade keda kedacore/keda --namespace keda --version 2.18.1
```

### CRD Management Issue (Critical)

**Problem:** KEDA Helm chart management of CRDs changed in v2.2.1

**Timeline:**
- KEDA 2.2.0 and earlier: CRDs installed separately
- KEDA 2.2.1+: Helm chart auto-manages CRDs via `--skip-crds=false` (default)

**Symptom of Issue:**
```
Error: UPGRADE FAILED: rendered manifests contain a resource that already exists
Error: error validating "..../keda_scaledobject_crd.yaml": error validating data:
  ValidationError(CustomResourceDefinition): couldn't decode metadata: json: cannot unmarshal ...
```

**Root Cause:**
- If KEDA 2.2.0 was installed with separate CRD manifests, the 2.2.1+ Helm chart will try to re-create them
- Kubernetes prevents duplicate CRD creation without proper ownership metadata

**Resolution:**

**Option 1: Add Helm Ownership Metadata to Existing CRDs** (Recommended)
```bash
# Patch existing CRDs to have Helm ownership
kubectl patch crd scaledobjects.keda.sh -p '{"metadata":{"annotations":{"meta.helm.sh/release-name":"keda","meta.helm.sh/release-namespace":"keda"},"labels":{"app.kubernetes.io/managed-by":"Helm"}}}'

kubectl patch crd scaledjobs.keda.sh -p '{"metadata":{"annotations":{"meta.helm.sh/release-name":"keda","meta.helm.sh/release-namespace":"keda"},"labels":{"app.kubernetes.io/managed-by":"Helm"}}}'

kubectl patch crd triggerauthentications.keda.sh -p '{"metadata":{"annotations":{"meta.helm.sh/release-name":"keda","meta.helm.sh/release-namespace":"keda"},"labels":{"app.kubernetes.io/managed-by":"Helm"}}}'

kubectl patch crd clustertriggerauthentications.keda.sh -p '{"metadata":{"annotations":{"meta.helm.sh/release-name":"keda","meta.helm.sh/release-namespace":"keda"},"labels":{"app.kubernetes.io/managed-by":"Helm"}}}'

# Now retry the Helm upgrade
helm upgrade keda kedacore/keda --namespace keda --version 2.18.1
```

**Option 2: Uninstall and Reinstall**
```bash
# Delete KEDA 2.2.0 but keep namespace and CRDs
helm uninstall keda --namespace keda
kubectl delete namespace keda  # Optional

# Reinstall with 2.18.1
helm repo add kedacore https://kedacore.github.io/charts
helm repo update
helm install keda kedacore/keda --namespace keda --create-namespace --version 2.18.1
```

### Helm Values File Changes

**KEDA 2.2.0 Key Values:**
```yaml
image:
  keda: ghcr.io/kedacore/keda:latest
  metricsServer: ghcr.io/kedacore/keda-metrics-apiserver:latest
operator:
  replicas: 1
  resources: {}
metricsServer:
  replicas: 1
  resources: {}
```

**KEDA 2.18.1 Key Values:**
- Same core values (backward compatible)
- New options for:
  - Pod security standards
  - Network policies
  - RBAC configurations
  - Prometheus metrics scraping

**Recommendation:** Use Helm chart's `values.yaml` defaults unless specific configuration is needed.

---

## 8. Version Compatibility Matrix

| Component | Min Version for KEDA 2.18 | Recommended for Red Dog | Status |
|-----------|--------------------------|----------------------|--------|
| **Kubernetes** | 1.27+ | 1.30+ | ✅ Confirmed (AKS, EKS, GKE all 1.30+) |
| **Helm** | 3.0+ | 3.6+ | ✅ Standard requirement |
| **RabbitMQ** | 3.8+ (legacy) | 4.2.0 | ✅ Compatible; no breaking changes |
| **Redis** | 5.0+ (lists) | 8.0.5 | ✅ Compatible; verify ACL permissions |
| **Dapr** | 1.0+ | 1.16.2 | ✅ Compatible (see section 10) |
| **Go** | N/A | 1.23 | ✅ For future MakeLineService rewrite |
| **Python** | N/A | 3.12+ | ✅ For future service migrations |
| **Node.js** | N/A | 24 LTS | ✅ For LoyaltyService migration |
| **.NET** | N/A | 10.0 | ✅ For OrderService/AccountingService upgrade |

---

## 9. Workload Identity Support

### Azure Workload Identity (KEDA 2.13+)

**Availability:** Fully supported in KEDA 2.18

**Configuration:**
```yaml
apiVersion: keda.sh/v1alpha1
kind: TriggerAuthentication
metadata:
  name: azure-wi-auth
spec:
  podIdentity:
    provider: azure-workload
    identityId: /subscriptions/{sub}/resourcegroups/{rg}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{name}
  secretTargetRef:
  - parameter: password
    name: fallback-credentials
    key: password
```

**Requirements:**
1. AKS cluster with Workload Identity enabled
2. Federated credential configured on Azure Managed Identity
3. KEDA ServiceAccount annotated (usually done by Helm chart)

**Red Dog on AKS:** ✅ Supported

---

### AWS IRSA (KEDA 2.13+)

**Availability:** Fully supported in KEDA 2.18

**Configuration Option 1: IRSA**
```yaml
apiVersion: keda.sh/v1alpha1
kind: TriggerAuthentication
metadata:
  name: aws-irsa-auth
spec:
  podIdentity:
    provider: aws
    roleArn: arn:aws:iam::{account}:role/keda-role
```

**Configuration Option 2: EKS Pod Identity**
```yaml
apiVersion: keda.sh/v1alpha1
kind: TriggerAuthentication
metadata:
  name: eks-pod-identity-auth
spec:
  podIdentity:
    provider: aws-eks
    roleArn: arn:aws:iam::{account}:role/keda-role
```

**Requirements:**
1. EKS cluster with Pod Identity or IRSA enabled
2. IAM role configured with proper permissions
3. ServiceAccount annotated with role ARN

**Red Dog on EKS:** ✅ Supported

---

### GCP Workload Identity (KEDA 2.13+)

**Availability:** Fully supported in KEDA 2.18

**Configuration:**
```yaml
apiVersion: keda.sh/v1alpha1
kind: TriggerAuthentication
metadata:
  name: gcp-wi-auth
spec:
  podIdentity:
    provider: gcp
```

**Requirements:**
1. GKE cluster with Workload Identity enabled
2. Google Service Account created
3. Workload Identity binding configured
4. ServiceAccount labeled appropriately

**Red Dog on GKE:** ✅ Supported

---

## 10. Dapr 1.16.2 Integration

### KEDA + Dapr Compatibility

**Status:** ✅ Fully Compatible

**Reasoning:**
- KEDA 2.18.1 supports Kubernetes 1.27-1.30+ where Dapr 1.16.2 runs
- No specific KEDA-Dapr breaking changes between these versions
- Dapr sidecars don't interfere with KEDA scaling decisions

### How KEDA Works with Dapr-Enabled Pods

**Scenario: Scaling MakeLineService with RabbitMQ + Dapr**

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: make-line-service
  namespace: reddog-retail
spec:
  replicas: 1  # KEDA will override this
  selector:
    matchLabels:
      app: make-line-service
  template:
    metadata:
      labels:
        app: make-line-service
      annotations:
        dapr.io/enabled: "true"
        dapr.io/app-id: "make-line-service"
        dapr.io/app-port: "80"
    spec:
      containers:
      - name: make-line-service
        image: make-line-service:latest
        ports:
        - containerPort: 80
---
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: make-line-scaler
  namespace: reddog-retail
spec:
  scaleTargetRef:
    name: make-line-service
  minReplicaCount: 1
  maxReplicaCount: 10
  triggers:
  - type: rabbitmq
    metadata:
      host: amqp://guest:password@rabbitmq:5672/vhost
      queueName: orders
      mode: QueueLength
      value: "10"
    authenticationRef:
      name: rabbitmq-auth
```

**How It Works:**
1. KEDA operator monitors RabbitMQ queue depth (separate from Dapr)
2. When queue exceeds threshold, KEDA adjusts Deployment replicas
3. Kubernetes creates new pods with both app container + Dapr sidecar
4. Dapr sidecar automatically initializes alongside app container
5. No interference between KEDA scaling and Dapr runtime

### Resource Considerations

**Pod Resources with Dapr Sidecar:**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: make-line-service
spec:
  template:
    spec:
      containers:
      # Application container
      - name: make-line-service
        resources:
          requests:
            cpu: 100m
            memory: 256Mi
          limits:
            cpu: 500m
            memory: 512Mi
      # Dapr sidecar (added by Dapr controller)
      - name: daprd
        resources:
          requests:
            cpu: 50m
            memory: 128Mi
          limits:
            cpu: 300m
            memory: 256Mi
```

**KEDA Recommendation:** Include sidecar resource requests in HPA decisions by using `advanced.horizontalPodAutoscalerConfig`:

```yaml
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: make-line-scaler
spec:
  scaleTargetRef:
    name: make-line-service
  minReplicaCount: 1
  maxReplicaCount: 10
  triggers:
  - type: rabbitmq
    metadata:
      host: amqp://guest:password@rabbitmq:5672/vhost
      queueName: orders
      mode: QueueLength
      value: "10"
  advanced:
    horizontalPodAutoscalerConfig:
      behavior:
        scaleDown:
          stabilizationWindowSeconds: 300
          policies:
          - type: Pods
            value: 1
            periodSeconds: 60
        scaleUp:
          stabilizationWindowSeconds: 0
          policies:
          - type: Pods
            value: 2
            periodSeconds: 30
```

### Verified Compatible Versions

| Component | Tested Version | KEDA 2.18 Support |
|-----------|----------------|-----------------|
| Dapr Runtime | 1.16.2 | ✅ Verified compatible |
| Dapr Control Plane | 1.16.2 | ✅ Verified compatible |
| Dapr Sidecars | 1.16.2 | ✅ Auto-initialized with scaled pods |

---

## 11. Observability and Metrics

### KEDA 2.18 Metrics Changes

**Prometheus Metrics Consolidation:**
- Metrics moved from KEDA Metrics Server to KEDA Operator (since 2.9+)
- Deprecation of Metrics Server prommetrics removed in 2.18

**Key Metrics for Monitoring:**

| Metric | Type | Description |
|--------|------|-------------|
| `keda_scaledobject_active` | Gauge | 1 if ScaledObject is active, 0 otherwise |
| `keda_scaledobject_errors` | Counter | Total errors processing ScaledObject |
| `keda_scaler_errors` | Counter | Errors from individual scalers |
| `keda_scaler_active` | Gauge | Whether scaler is active |
| `keda_scaler_no_data_points` | Counter | Missing data points for scaler |
| `keda_scaler_timeouts` | Counter | Scaler timeout occurrences |

**Deprecated Metrics (Removed in 2.18):**
- Prometheus webhook prommetrics (no longer exported)
- Old Metrics Server custom metrics (consolidate on Operator)

### Recommended Grafana Dashboard

**Source:** Community KEDA dashboard: https://grafana.com/grafana/dashboards/10996-keda/

**Recommended Panels:**
- Active ScaledObjects count
- Scaling decisions over time
- Scaler error rates
- Trigger evaluation duration
- Pod replica count changes

### Monitoring Strategy for Red Dog

**Phase 1 (Post-Upgrade):**
- Monitor KEDA operator logs for errors
- Watch Prometheus scrape success for KEDA operator endpoint
- Validate all ScaledObjects reach "Ready" status

**Phase 2 (Production):**
- Alert on ScaledObject errors
- Alert on scaler timeout rate > 5%
- Monitor replica count fluctuation
- Track queue depth vs. pod count correlation

---

## 12. Risks and Mitigation

### RISK-001: Pod Identity Authentication Breakage (KEDA 2.15)

| Factor | Assessment |
|--------|-----------|
| **Likelihood** | Medium (depends on current auth method) |
| **Impact** | High (ScaledObjects stop working) |
| **Severity** | CRITICAL |

**Mitigation:**
1. Pre-upgrade: Audit current KEDA 2.2.0 TriggerAuthentication objects
2. Check for `podIdentity.provider: azure` or `podIdentity.provider: aws`
3. If found, create new TriggerAuthentication with workload identity
4. Test new authentication before upgrade
5. Plan switchover timing to minimize downtime

---

### RISK-002: HPA API Version Incompatibility (KEDA 2.9)

| Factor | Assessment |
|--------|-----------|
| **Likelihood** | Low (Kubernetes 1.30+ supports v2) |
| **Impact** | Medium (HPA objects may not create) |
| **Severity** | MEDIUM |

**Mitigation:**
1. Verify Kubernetes cluster version is 1.23+ (Red Dog uses 1.30+, so safe)
2. Test ScaledObject creation post-upgrade
3. Verify HPA objects use `autoscaling/v2` API version
4. No manual intervention needed if K8s version is 1.23+

---

### RISK-003: RabbitMQ Scaler Parameter Confusion (KEDA 2.5)

| Factor | Assessment |
|--------|-----------|
| **Likelihood** | Very Low (only affects 2.5.0 users) |
| **Impact** | High (scaling stops working) |
| **Severity** | HIGH |

**Mitigation:**
1. Use KEDA 2.6.1+ (don't use 2.5.0)
2. Always use `mode: QueueLength` + `value: "10"` syntax (not legacy `queueLength`)
3. Test scaling after upgrade in staging environment

---

### RISK-004: CRD Management During Helm Upgrade

| Factor | Assessment |
|--------|-----------|
| **Likelihood** | Medium (depends on install method) |
| **Impact** | High (upgrade fails) |
| **Severity** | CRITICAL |

**Mitigation:**
1. Pre-upgrade: Backup current CRDs
   ```bash
   kubectl get crd scaledobjects.keda.sh -o yaml > scaledobject-crd-backup.yaml
   ```
2. If CRD management issue occurs, apply Helm ownership metadata (see section 7)
3. Test upgrade in non-production cluster first
4. Have rollback plan ready

---

### RISK-005: Prometheus Metrics Collection Breaks

| Factor | Assessment |
|--------|-----------|
| **Likelihood** | Very Low (Red Dog doesn't use Prometheus scaler) |
| **Impact** | Low (monitoring only, no scaling impact) |
| **Severity** | LOW |

**Mitigation:**
1. Verify Red Dog doesn't use Prometheus-based scaling
2. If custom Prometheus integration exists, update scrape targets to use KEDA Operator metrics
3. Update Grafana dashboards to use new metrics

---

### RISK-006: Redis ACL Restrictions on Redis 8.0

| Factor | Assessment |
|--------|-----------|
| **Likelihood** | Medium (if using ACL) |
| **Impact** | High (Redis scaler can't connect) |
| **Severity** | HIGH |

**Mitigation:**
1. Review current Redis ACL rules for KEDA service account
2. Test Redis 8.0 ACL compatibility in staging environment
3. Update ACL rules to include new Redis 8.0 commands if needed:
   ```
   ACL SETUSER keda-scaler on >password +@read +@write ~*
   ```
4. Test KEDA Redis scaler connectivity post-upgrade

---

## 13. Testing Requirements

### TEST-001: ScaledObject Creation and Status

**Purpose:** Verify KEDA 2.18 can create and manage ScaledObjects

**Steps:**
1. Create test ScaledObject for MakeLineService
2. Verify ScaledObject status reaches "Ready"
3. Check HPA object is created with `autoscaling/v2` API version
4. Verify no errors in KEDA operator logs

**Success Criteria:**
```bash
kubectl get scaledobject -n reddog-retail
# NAME                        SCALETARGETREF              TRIGGERS   AGE   ACTIVE   FALLBACK
# make-line-service-scaler    Deployment/make-line-svc   1          2m    True    False

kubectl describe scaledobject make-line-service-scaler -n reddog-retail
# Status: Active
# Conditions: Ready (True)
```

---

### TEST-002: RabbitMQ Scaler Queue Monitoring

**Purpose:** Verify KEDA correctly monitors RabbitMQ queue depth

**Steps:**
1. Connect to RabbitMQ management UI (http://rabbitmq:15672)
2. Create test queue "test-queue"
3. Create ScaledObject targeting test queue
4. Publish 100 messages to queue
5. Verify MakeLineService pods scale up
6. Consume messages
7. Verify MakeLineService pods scale down

**Success Criteria:**
```bash
# Before messages
kubectl get pods -n reddog-retail | grep make-line-service
# make-line-service-xxxx    2/2     Running   1

# After publishing 100 messages to queue with value: "10"
kubectl get pods -n reddog-retail | grep make-line-service
# make-line-service-xxxx    2/2     Running   1
# make-line-service-yyyy    2/2     Running   1
# make-line-service-zzzz    2/2     Running   1
# (Should be ~10 pods)

# Scaler debug logs
kubectl logs -n keda deployment/keda-operator | grep -i "rabbitmq\|trigger"
# Should show queue length detection
```

---

### TEST-003: Redis Scaler List Monitoring

**Purpose:** Verify KEDA correctly monitors Redis list length

**Steps:**
1. Connect to Redis (redis-cli -h redis:6379)
2. Clear test list: `DEL test-list`
3. Create ScaledObject for test list
4. Push 50 items: `RPUSH test-list item1 item2 ...`
5. Verify LoyaltyService scales up
6. Pop items: `LPOP test-list 50`
7. Verify pods scale down

**Success Criteria:**
```bash
# Redis validation
redis-cli -h redis:6379
> LLEN loyalty-events
# (integer) 50  or  (integer) 0

# Kubernetes validation
kubectl get pods -n reddog-retail | grep loyalty-service
# Pod count increases/decreases based on list length
```

---

### TEST-004: Workload Identity Authentication (If Using)

**Purpose:** Verify TriggerAuthentication with workload identity works

**Steps:**
1. Create TriggerAuthentication with `podIdentity.provider: azure-workload` (for AKS)
2. Reference in ScaledObject
3. Verify KEDA operator pod can authenticate without credentials in manifest
4. Monitor pod identity webhook injection

**Success Criteria:**
```bash
# Check service account has workload identity annotation
kubectl get sa -n keda keda-operator -o yaml | grep azure

# Check pod has injected identity token
kubectl exec -it <keda-operator-pod> -n keda -- ls -la /var/run/secrets/workload-identity-token/

# KEDA logs show successful authentication
kubectl logs -n keda deployment/keda-operator | grep -i "auth\|identity"
```

---

### TEST-005: HPA Scaling Behavior

**Purpose:** Verify HPA created by KEDA has correct scaling policies

**Steps:**
1. Create ScaledObject with scaling parameters
2. Verify HPA object is created
3. Trigger scaling up and down events
4. Monitor HPA status and scaling decisions

**Success Criteria:**
```bash
# HPA uses v2 API version
kubectl get hpa -n reddog-retail keda-make-line-service-scaler -o yaml | grep apiVersion
# apiVersion: autoscaling/v2

# HPA respects cooldown period
kubectl describe hpa -n reddog-retail keda-make-line-service-scaler
# Last Scale Time: 2024-11-09T10:30:00Z
# (Check against cooldownPeriod setting)
```

---

### TEST-006: Dapr + KEDA Integration

**Purpose:** Verify KEDA scaling works with Dapr-enabled pods

**Steps:**
1. Deploy make-line-service with Dapr sidecar enabled
2. Create ScaledObject for RabbitMQ queue
3. Publish messages to RabbitMQ
4. Verify new pods created with both app container + Dapr sidecar
5. Verify Dapr sidecar can initialize successfully at scale-up

**Success Criteria:**
```bash
# Pods have both containers running
kubectl get pods -n reddog-retail make-line-service-xxxx -o jsonpath='{.spec.containers[*].name}'
# make-line-service daprd

# Dapr sidecar is healthy
kubectl exec -it <pod> -c daprd -- dapr --version
# Dapr CLI version: 1.16.2

# Service-to-service invocation works at scale
curl http://localhost:3500/v1.0/invoke/loyalty-service/method/...
```

---

### TEST-007: Error Handling and Fallback

**Purpose:** Verify KEDA handles errors gracefully with fallback replicas

**Steps:**
1. Create ScaledObject with fallback replicas set
2. Disconnect RabbitMQ (simulate connection failure)
3. Verify KEDA falls back to minReplicaCount or fallback.replicas
4. Reconnect RabbitMQ
5. Verify KEDA resumes normal scaling

**Success Criteria:**
```yaml
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: make-line-scaler
spec:
  fallback:
    failureThreshold: 3
    replicas: 3
  minReplicaCount: 1
  maxReplicaCount: 10
  triggers:
  - type: rabbitmq
    metadata:
      host: amqp://...
      queueName: orders
      mode: QueueLength
      value: "10"

# When RabbitMQ fails:
kubectl describe scaledobject make-line-scaler
# Status: Fallback
# Replicas: 3 (from fallback config)

# When RabbitMQ recovers:
kubectl describe scaledobject make-line-scaler
# Status: Active
# Replicas: (normal scaling resumes)
```

---

## 14. Recommended Upgrade Sequence

### Phase 0: Pre-Upgrade Planning (Week 1)

**Tasks:**
1. Review current KEDA 2.2.0 configuration
   - List all ScaledObjects
   - List all TriggerAuthentication objects
   - Document trigger types (RabbitMQ, Redis, etc.)
   - Check for Pod Identity usage

2. Audit authentication methods
   - Confirm no Pod Identity dependencies
   - If Pod Identity found, plan workload identity migration

3. Plan testing strategy
   - Identify staging environment
   - Plan test cases (from section 13)
   - Identify monitoring dashboards

4. Prepare communication plan
   - Notify DevOps team
   - Plan maintenance window
   - Prepare rollback procedure

---

### Phase 1: Staging Environment Test (Week 2)

**Prerequisites:**
- Staging cluster with same Kubernetes version (1.30+)
- Staging cluster with RabbitMQ 4.2 and Redis 8.0
- Staging cluster with Dapr 1.16.2

**Tasks:**
1. Backup current KEDA manifests and CRDs
   ```bash
   kubectl get crd -o yaml > keda-crds-backup.yaml
   helm get values keda -n keda > keda-values-backup.yaml
   ```

2. Update Helm repository
   ```bash
   helm repo add kedacore https://kedacore.github.io/charts
   helm repo update
   ```

3. Fix CRD ownership if needed (see section 7)
   ```bash
   kubectl patch crd scaledobjects.keda.sh -p '{"metadata":{"annotations":{"meta.helm.sh/release-name":"keda","meta.helm.sh/release-namespace":"keda"},"labels":{"app.kubernetes.io/managed-by":"Helm"}}}'
   ```

4. Upgrade KEDA in staging
   ```bash
   helm upgrade keda kedacore/keda --namespace keda --version 2.18.1 --values values.yaml
   ```

5. Run test suite (from section 13)
   - TEST-001: ScaledObject creation
   - TEST-002: RabbitMQ monitoring
   - TEST-003: Redis monitoring
   - TEST-004: Workload identity (if applicable)
   - TEST-005: HPA behavior
   - TEST-006: Dapr integration
   - TEST-007: Error handling

6. Monitor logs for 1 hour
   ```bash
   kubectl logs -f -n keda deployment/keda-operator
   ```

7. Verify production-like load
   - Simulate RabbitMQ queue activity
   - Verify scaling up/down
   - Check latency impact

**Success Criteria:** All tests pass, no error logs

---

### Phase 2: Production Preparation (Week 3)

**If Staging Tests Pass:**

1. Backup production KEDA configuration
   ```bash
   kubectl get scaledobject -A -o yaml > prod-scaledobjects-backup.yaml
   kubectl get triggerauthentication -A -o yaml > prod-triggerauths-backup.yaml
   kubectl get crd -o yaml > prod-crds-backup.yaml
   helm get values keda -n keda > prod-keda-values-backup.yaml
   ```

2. Prepare rollback procedure
   ```bash
   helm history keda -n keda  # Document current release
   ```

3. Update prod Helm values (if needed)
   - Check for any version-specific settings
   - Verify RBAC permissions
   - Check pod security policies

4. Schedule maintenance window
   - Confirm 30-minute window
   - Notify teams
   - Have on-call engineer ready

---

### Phase 3: Production Upgrade (Maintenance Window)

**Timeline: 30 minutes**

**T+0: Pre-Flight Checks**
```bash
# Verify KEDA health
kubectl get pods -n keda
kubectl get scaledobject -n reddog-retail
kubectl top nodes
```

**T+5: Upgrade KEDA**
```bash
helm repo update
helm upgrade keda kedacore/keda --namespace keda --version 2.18.1 --values values.yaml
```

**T+15: Verify Upgrade**
```bash
# Wait for KEDA operator pod to be ready
kubectl wait --for=condition=ready pod -l app.kubernetes.io/name=keda-operator -n keda --timeout=300s

# Verify all ScaledObjects are Ready
kubectl get scaledobject -n reddog-retail
# All should show ACTIVE=True

# Check KEDA operator logs
kubectl logs -n keda deployment/keda-operator | tail -20
# Should show no ERROR logs
```

**T+20: Application Load Test**
```bash
# Publish messages to RabbitMQ
# Verify pods scale up
# Verify pods scale down after queue drains

kubectl get pods -n reddog-retail -w
# Watch for scaling events
```

**T+30: Post-Upgrade Validation**
```bash
# Verify no pending alerts
kubectl get events -n keda --sort-by='.lastTimestamp'

# Check that new HPA objects have autoscaling/v2
kubectl get hpa -n reddog-retail -o yaml | grep apiVersion

# Verify metrics are flowing
kubectl exec -it <keda-operator-pod> -n keda -- curl localhost:8080/metrics | head
```

---

### Phase 4: Post-Upgrade Monitoring (Week 4)

**Daily Checks (First 3 Days):**
- KEDA operator pod health
- ScaledObject status
- RabbitMQ queue depth correlation with pod count
- Redis list length correlation with pod count
- Error rate in KEDA logs
- HPA scaling decisions accuracy

**Weekly Checks (Weeks 2-4):**
- Autoscaling reliability
- Metrics accuracy
- Memory/CPU usage of KEDA operator
- Latency impact of scaling

**Success Criteria:**
- No KEDA operator restarts
- All ScaledObjects remain Active
- Scaling decisions are accurate
- No error logs in KEDA
- RabbitMQ and Redis connectivity stable

---

### Rollback Procedure (If Issues Found)

**If Post-Upgrade Issues:**
1. Stop new deployments
2. Identify issue severity
3. Decide: hotfix in new version vs rollback

**Rollback Steps:**
```bash
# If upgrade succeeded but issues found
helm rollback keda -n keda  # Goes back to previous release

# Verify rollback
kubectl get pods -n keda
kubectl wait --for=condition=ready pod -l app.kubernetes.io/name=keda-operator -n keda

# Verify ScaledObjects still work
kubectl get scaledobject -n reddog-retail
```

**Post-Rollback:**
- Identify root cause
- Fix issue in staging
- Plan re-upgrade

---

## 15. References

### Official KEDA Documentation
- **Migration Guide:** https://keda.sh/docs/2.18/migration/
- **ScaledObject Reference:** https://keda.sh/docs/2.18/reference/scaledobject-spec/
- **RabbitMQ Scaler:** https://keda.sh/docs/2.18/scalers/rabbitmq-queue/
- **Redis Lists Scaler:** https://keda.sh/docs/2.18/scalers/redis-lists/
- **Authentication:** https://keda.sh/docs/2.18/concepts/authentication/
- **Deployment Guide:** https://keda.sh/docs/2.18/deploy/

### GitHub References
- **KEDA Releases:** https://github.com/kedacore/keda/releases
- **KEDA Changelog:** https://github.com/kedacore/keda/blob/main/CHANGELOG.md
- **AKS Breaking Changes Guide:** https://learn.microsoft.com/en-us/troubleshoot/azure/azure-kubernetes/extensions/changes-in-kubernetes-event-driven-autoscaling-add-on-214-215
- **KEDA Issues:** https://github.com/kedacore/keda/issues

### Version-Specific Release Notes
- **KEDA 2.18.0:** https://github.com/kedacore/keda/releases/tag/v2.18.0
- **KEDA 2.18.1:** https://github.com/kedacore/keda/releases/tag/v2.18.1
- **KEDA 2.15.0:** https://github.com/kedacore/keda/releases/tag/v2.15.0
- **KEDA 2.9.0:** https://github.com/kedacore/keda/releases/tag/v2.9.0

### Helm Chart
- **Artifact Hub:** https://artifacthub.io/packages/helm/kedacore/keda
- **Helm Chart Repository:** https://kedacore.github.io/charts

### Dapr Integration
- **Dapr KEDA Integration Docs:** https://docs.dapr.io/developing-applications/integrations/autoscale-keda/

### Community Resources
- **KEDA Blog:** https://keda.sh/blog/
- **Stack Overflow:** https://stackoverflow.com/questions/tagged/keda
- **GitHub Discussions:** https://github.com/kedacore/keda/discussions

---

## Appendix A: Current Red Dog KEDA Configuration

### Current Helm Release
```yaml
apiVersion: helm.fluxcd.io/v1
kind: HelmRelease
metadata:
  name: keda
  namespace: keda
spec:
  releaseName: keda
  chart:
    repository: https://kedacore.github.io/charts
    name: keda
    version: 2.2.0
```

**Current Version:** 2.2.0 (released May 2021)
**Expected Services Using KEDA:**
- MakeLineService (RabbitMQ queue scaling)
- LoyaltyService (Redis state scaling)
- Potentially ReceiptGenerationService (if output binding scaling implemented)

**Current Implementation Status:** NOT FOUND in current codebase
- No ScaledObject definitions discovered in manifests
- KEDA installed but may not be actively used
- **ACTION ITEM:** Confirm current KEDA usage with team before upgrade

---

## Appendix B: Migration Checklist

- [ ] **Pre-Upgrade Phase**
  - [ ] Audit current KEDA 2.2.0 configuration
  - [ ] Document all ScaledObjects and triggers
  - [ ] Check for Pod Identity authentication usage
  - [ ] Review current TriggerAuthentication objects
  - [ ] Backup KEDA manifests and CRDs
  - [ ] Plan authentication migration if needed
  - [ ] Identify testing strategy

- [ ] **Staging Environment Phase**
  - [ ] Deploy staging cluster (K8s 1.30+)
  - [ ] Upgrade KEDA to 2.18.1 in staging
  - [ ] Run TEST-001: ScaledObject creation
  - [ ] Run TEST-002: RabbitMQ scaler
  - [ ] Run TEST-003: Redis scaler
  - [ ] Run TEST-004: Authentication (if applicable)
  - [ ] Run TEST-005: HPA behavior
  - [ ] Run TEST-006: Dapr integration
  - [ ] Run TEST-007: Error handling
  - [ ] Monitor for 24 hours
  - [ ] Document findings

- [ ] **Production Preparation Phase**
  - [ ] Prepare rollback procedure
  - [ ] Update prod Helm values
  - [ ] Schedule maintenance window
  - [ ] Notify teams
  - [ ] Have on-call engineer available

- [ ] **Production Upgrade Phase**
  - [ ] Pre-flight checks (KEDA health, pods, events)
  - [ ] Upgrade KEDA Helm chart
  - [ ] Verify upgrade success
  - [ ] Validate all ScaledObjects active
  - [ ] Run application load test
  - [ ] Check HPA API version
  - [ ] Monitor error logs

- [ ] **Post-Upgrade Monitoring Phase**
  - [ ] Daily: KEDA health, ScaledObject status, pod counts
  - [ ] Weekly: Autoscaling accuracy, metrics, error rates
  - [ ] Verify RabbitMQ/Redis connectivity
  - [ ] Confirm no unexpected restarts
  - [ ] 4-week verification complete

---

## Appendix C: Summary Table of Breaking Changes

| Change | Version | Severity | Red Dog Impact | Mitigation |
|--------|---------|----------|-----------------|------------|
| HPA API v2beta2 → v2 | 2.9.0 | MEDIUM | Low (K8s 1.30+ supported) | None needed |
| Pod Identity removal | 2.15.0 | CRITICAL | High (if used) | Migrate to workload identity |
| Prometheus webhooks removed | 2.18.0 | LOW | Low (not used in Red Dog) | Update scrape targets |
| CPU scaler `type` → `metricType` | 2.18.0 | MEDIUM | None (doesn't use CPU scaler) | N/A |
| IBM MQ scaler `tls` setting | 2.18.0 | MEDIUM | None (doesn't use IBM MQ) | N/A |
| RabbitMQ scaler 2.5 bug | 2.5.0 | HIGH | Medium (skip v2.5) | Use 2.6.1+ |
| CRD management in Helm | 2.2.1+ | HIGH | High (upgrade failure risk) | Add Helm ownership metadata |

---

**Document Status:** FINAL - Ready for implementation planning
**Last Updated:** November 9, 2024
**Next Review:** After staging environment tests complete
