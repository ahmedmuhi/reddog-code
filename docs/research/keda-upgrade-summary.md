# KEDA 2.2.0 → 2.18.1 Upgrade Summary (Quick Reference)

## At a Glance

**Current:** KEDA 2.2.0 (May 2021)
**Target:** KEDA 2.18.1 (November 2024)
**Time Span:** 3.5 years, 16 minor versions
**Upgrade Type:** Direct (2.2.0 → 2.18.1) - No incremental steps required
**Kubernetes:** 1.30+ confirmed on AKS, EKS, GKE ✅

---

## 5 Critical Things to Know

### 1. **HPA API Version Changes (KEDA 2.9)**
- Old: `autoscaling/v2beta2`
- New: `autoscaling/v2` (GA version)
- **Impact on Red Dog:** None - Kubernetes 1.30+ supports both
- **Action:** Verify HPA objects after upgrade use v2

### 2. **Pod Identity Authentication Removed (KEDA 2.15)** ⚠️ CRITICAL
- **Removed:** `podIdentity.provider: azure` (AAD Pod Identity)
- **Removed:** `podIdentity.provider: aws` (KIAM)
- **If Red Dog Uses Pod Identity:** Must migrate to workload identity BEFORE upgrade
- **Migration Options:**
  - Azure: Use `azure-workload` provider
  - AWS: Use `aws-eks` provider
  - GCP: Use `gcp` provider
- **Action:** Audit current KEDA configuration for Pod Identity usage

### 3. **RabbitMQ Scaler - Parameter Evolution (KEDA 2.2-2.6)**
- **Syntax Change:** `queueLength: "10"` → `mode: QueueLength` + `value: "10"`
- **Known Bug:** KEDA 2.5.0 broke new syntax - **SKIP 2.5.0**, use 2.6.1+
- **RabbitMQ 4.2 Compatibility:** ✅ Compatible (AMQP 0-9-1 still supported)
- **Action:** Use modern `mode`/`value` syntax, not legacy `queueLength`

### 4. **Redis 8.0 Compatibility**
- **Verdict:** ✅ Compatible
- **Watch Out:** Redis 8.0 ACL rules changed - update KEDA service account permissions
- **Action:** Test Redis 8.0 ACL with KEDA in staging

### 5. **Helm CRD Management Issue (KEDA 2.2.1+)** ⚠️ CRITICAL
- **Problem:** Helm chart started auto-managing CRDs in v2.2.1
- **Symptom:** Helm upgrade fails with "resource already exists" error
- **Solution:** Add Helm ownership metadata to existing CRDs before upgrade
- **Action:** Run CRD patching script before upgrade (see Helm section)

---

## Breaking Changes Summary Table

| Issue | Version | Severity | Red Dog Impact | Action |
|-------|---------|----------|-----------------|--------|
| HPA v2beta2 → v2 | 2.9 | MEDIUM | None (K8s 1.30+ ok) | Verify post-upgrade |
| Pod Identity removal | 2.15 | **CRITICAL** | High if used | Audit + migrate pre-upgrade |
| RabbitMQ params change | 2.2+ | MEDIUM | Medium (use new syntax) | Use `mode`/`value` |
| Prometheus webhooks | 2.18 | LOW | Low (not used) | Update scrape targets if needed |
| Helm CRD management | 2.2.1+ | **CRITICAL** | High (upgrade fails) | Patch CRDs before upgrade |
| RabbitMQ v2.5.0 bug | 2.5 | HIGH | Medium (skip v2.5) | Use 2.6.1+ |
| CPU scaler `type` field | 2.18 | MEDIUM | None (doesn't use) | N/A |

---

## Pre-Upgrade Checklist

- [ ] **Audit current KEDA configuration**
  ```bash
  kubectl get scaledobject -A
  kubectl get triggerauthentication -A
  kubectl describe triggerauthentication <name> | grep -i podIdentity
  ```

- [ ] **Backup CRDs and configuration**
  ```bash
  kubectl get crd scaledobjects.keda.sh -o yaml > keda-scaledobject-crd-backup.yaml
  helm get values keda -n keda > keda-values-backup.yaml
  ```

- [ ] **Check for Pod Identity usage** ⚠️
  - If found, plan workload identity migration
  - Create new TriggerAuthentication objects with workload identity
  - Test new auth before upgrade

- [ ] **Verify Kubernetes version**
  ```bash
  kubectl version --short
  # Should be 1.30+ for Red Dog
  ```

- [ ] **Prepare staging environment**
  - Deploy K8s 1.30+ with RabbitMQ 4.2 + Redis 8.0
  - Plan test scenarios
  - Have monitoring/alerting ready

---

## Direct Upgrade Steps

### Step 1: Pre-Flight (Before Maintenance Window)

```bash
# Backup everything
kubectl get all -n keda -o yaml > keda-all-backup.yaml
helm get values keda -n keda > keda-values-backup.yaml

# Patch CRDs for Helm management
kubectl patch crd scaledobjects.keda.sh -p \
  '{"metadata":{"annotations":{"meta.helm.sh/release-name":"keda","meta.helm.sh/release-namespace":"keda"},"labels":{"app.kubernetes.io/managed-by":"Helm"}}}'

kubectl patch crd scaledjobs.keda.sh -p \
  '{"metadata":{"annotations":{"meta.helm.sh/release-name":"keda","meta.helm.sh/release-namespace":"keda"},"labels":{"app.kubernetes.io/managed-by":"Helm"}}}'

kubectl patch crd triggerauthentications.keda.sh -p \
  '{"metadata":{"annotations":{"meta.helm.sh/release-name":"keda","meta.helm.sh/release-namespace":"keda"},"labels":{"app.kubernetes.io/managed-by":"Helm"}}}'

kubectl patch crd clustertriggerauthentications.keda.sh -p \
  '{"metadata":{"annotations":{"meta.helm.sh/release-name":"keda","meta.helm.sh/release-namespace":"keda"},"labels":{"app.kubernetes.io/managed-by":"Helm"}}}'
```

### Step 2: Upgrade (During Maintenance Window)

```bash
# Update Helm repo
helm repo add kedacore https://kedacore.github.io/charts
helm repo update

# Upgrade KEDA
helm upgrade keda kedacore/keda --namespace keda --version 2.18.1

# Wait for operator to be ready
kubectl wait --for=condition=ready pod -l app.kubernetes.io/name=keda-operator -n keda --timeout=300s
```

### Step 3: Validation (Post-Upgrade)

```bash
# Verify KEDA health
kubectl get pods -n keda
kubectl logs -n keda deployment/keda-operator | tail -20

# Verify ScaledObjects status
kubectl get scaledobject -A
# All should show: ACTIVE=True, STATUS=Ready

# Verify HPA API version
kubectl get hpa -A -o yaml | grep -A1 "apiVersion:" | grep "autoscaling/v"
# Should show: autoscaling/v2

# Test scaling with real load
# (Check section 13 of full analysis for detailed test procedures)
```

---

## Post-Upgrade Validation

### Quick Health Check (First Hour)

```bash
# KEDA operator should be healthy
kubectl get deployment -n keda keda-operator -o jsonpath='{.status.conditions[?(@.type=="Available")].status}'
# Should output: "True"

# All ScaledObjects should be active
kubectl get scaledobject -n reddog-retail -o jsonpath='{range .items[*]}{.metadata.name}{"\t"}{.status.active}{"\n"}{end}'
# All should show: "true"

# Check for error logs
kubectl logs -n keda deployment/keda-operator | grep -i error | wc -l
# Should be 0 or very low

# Verify HPA objects use v2 API
kubectl get hpa -n reddog-retail -o yaml | grep "apiVersion: autoscaling" | sort | uniq
# Should show: "apiVersion: autoscaling/v2"
```

### RabbitMQ Queue Scaling Test

```bash
# Publish 100 messages to test queue
# Monitor pod scaling
kubectl get pods -n reddog-retail -w

# Expected: Pods should scale up proportional to queue depth
# With value: "10", expect ~10 pods for 100 messages
```

### Redis List Scaling Test

```bash
# Add 50 items to Redis list
redis-cli -h redis-cache:6379 RPUSH loyalty-events item1 item2 ... item50

# Monitor pod scaling
kubectl get pods -n reddog-retail -w

# Expected: Pods should scale based on list length
```

---

## Common Issues & Fixes

### Issue 1: Helm Upgrade Fails with "Resource Exists"

**Symptom:**
```
Error: UPGRADE FAILED: rendered manifests contain a resource that already exists
```

**Fix:**
```bash
# Run CRD patching commands from Step 1 above
# Then retry helm upgrade
helm upgrade keda kedacore/keda --namespace keda --version 2.18.1
```

---

### Issue 2: Pod Identity Authentication Breaks

**Symptom:**
```
TriggerAuthentication fails, RabbitMQ/Redis connection errors in logs
```

**Fix:**
1. Create new TriggerAuthentication with workload identity:
   ```yaml
   apiVersion: keda.sh/v1alpha1
   kind: TriggerAuthentication
   metadata:
     name: rabbitmq-auth-wi
   spec:
     podIdentity:
       provider: azure-workload  # or aws-eks, gcp
     secretTargetRef:
     - parameter: password
       name: rabbitmq-creds
       key: password
   ```
2. Update ScaledObjects to reference new TriggerAuthentication
3. Test connectivity before full rollout

---

### Issue 3: RabbitMQ Queue Not Scaling

**Symptom:**
```
ScaledObject shows ACTIVE=False, no pod scaling occurs
```

**Possible Causes:**
- Using `queueLength` parameter (deprecated) instead of `mode: QueueLength`
- RabbitMQ Management API not accessible
- Queue name incorrect

**Fix:**
```yaml
# Check scaler uses new syntax
triggers:
- type: rabbitmq
  metadata:
    host: amqp://user:pass@rabbitmq:5672/vhost  # Correct format
    queueName: orders                            # Correct
    mode: QueueLength                            # Not "queueLength"
    value: "10"                                  # Numeric as string
```

---

### Issue 4: HPA Not Creating with v2 API

**Symptom:**
```
HPA objects not created, or using old v2beta2
```

**Fix:**
```bash
# Verify Kubernetes version
kubectl version --short
# Should be 1.27+

# Delete old HPA objects (KEDA will recreate with v2)
kubectl delete hpa -n reddog-retail <hpa-name>

# KEDA will auto-recreate with correct API version
```

---

## Rollback Procedure

**If critical issues found:**

```bash
# Rollback to previous version
helm rollback keda -n keda

# Verify rollback
kubectl wait --for=condition=ready pod -l app.kubernetes.io/name=keda-operator -n keda
kubectl get scaledobject -n reddog-retail

# Test scaling again
```

---

## Testing Checklist

- [ ] TEST-001: ScaledObject creation and status
- [ ] TEST-002: RabbitMQ queue monitoring (publish 100 msgs, verify scale-up)
- [ ] TEST-003: Redis list monitoring (add 50 items, verify scale-up)
- [ ] TEST-004: Workload identity auth (if using)
- [ ] TEST-005: HPA scaling behavior (verify v2 API)
- [ ] TEST-006: Dapr + KEDA integration (pods with sidecar)
- [ ] TEST-007: Error handling (disconnect RabbitMQ, verify fallback)

See full analysis document (section 13) for detailed test procedures.

---

## Key Resources

- **Full Analysis:** `/home/ahmedmuhi/code/reddog-code/docs/research/keda-upgrade-analysis.md`
- **Migration Guide:** https://keda.sh/docs/2.18/migration/
- **RabbitMQ Scaler:** https://keda.sh/docs/2.18/scalers/rabbitmq-queue/
- **Redis Lists Scaler:** https://keda.sh/docs/2.18/scalers/redis-lists/
- **Authentication:** https://keda.sh/docs/2.18/concepts/authentication/
- **KEDA Helm Chart:** https://artifacthub.io/packages/helm/kedacore/keda

---

## Decision Matrix

| Question | Answer | Impact |
|----------|--------|--------|
| Is Kubernetes 1.30+? | Yes ✅ | Can upgrade safely |
| Uses Pod Identity? | Unknown⚠️ | Must audit pre-upgrade |
| Uses RabbitMQ 4.2? | Yes ✅ | Compatible, test ACL |
| Uses Redis 8.0? | Yes ✅ | Compatible, test ACL |
| Uses Dapr 1.16.2? | Yes ✅ | Compatible |
| Need direct upgrade? | Yes ✅ | Supported (2.2 → 2.18) |
| Maintenance window available? | Yes ✅ | 30-minute window needed |

**Overall Risk Assessment:** MEDIUM (manageable with proper planning)

---

## Next Steps

1. **Week 1:** Audit current KEDA configuration for Pod Identity usage
2. **Week 2:** Test upgrade in staging environment (run 7 test scenarios)
3. **Week 3:** Plan production upgrade timing and communication
4. **Week 4:** Execute production upgrade during maintenance window
5. **Week 4+:** Monitor and validate for 4 weeks post-upgrade

---

**Document:** KEDA Upgrade Summary
**Last Updated:** November 9, 2024
**Status:** Ready for implementation planning
