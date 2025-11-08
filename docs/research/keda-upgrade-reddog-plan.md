# KEDA Upgrade Implementation Plan for Red Dog

## Current Status

**Red Dog KEDA Configuration:**
- Current Version: 2.2.0 (May 2021)
- Installation Method: Helm via Flux (HelmRelease in `manifests/branch/dependencies/keda/keda.yaml`)
- Namespace: `keda`
- Target Services: Unknown (ScaledObject definitions not found in current codebase)

**Platform Requirements:**
- ✅ Kubernetes 1.30+ (confirmed on AKS, EKS, GKE)
- ✅ RabbitMQ 4.2.0 (planned upgrade)
- ✅ Redis 8.0.5 (planned upgrade)
- ✅ Dapr 1.16.2 (planned upgrade)

---

## Phase 1: Discovery & Planning (Week 1)

### Task 1.1: Audit Current KEDA Usage

**Objective:** Understand how Red Dog currently uses KEDA

**Action Items:**

1. **Search for ScaledObject definitions:**
   ```bash
   find /home/ahmedmuhi/code/reddog-code -name "*.yaml" -o -name "*.yml" | xargs grep -l "ScaledObject"
   ```

2. **Document all TriggerAuthentication objects:**
   ```bash
   find /home/ahmedmuhi/code/reddog-code -name "*.yaml" -o -name "*.yml" | xargs grep -l "TriggerAuthentication"
   ```

3. **Check if KEDA is currently active in production:**
   ```bash
   # In production/staging cluster
   kubectl get scaledobject -A
   kubectl get triggerauthentication -A
   kubectl get hpa -n reddog-retail
   ```

4. **Review KEDA operator logs:**
   ```bash
   kubectl logs -n keda deployment/keda-operator --tail=100
   ```

**Deliverable:** Document titled `KEDA-CURRENT-USAGE.md` containing:
- List of all ScaledObjects and their triggers
- List of all TriggerAuthentication objects
- Current authentication methods (Pod Identity, secrets, etc.)
- Any known issues or limitations

---

### Task 1.2: Assess Authentication Method

**Objective:** Determine if Red Dog uses Pod Identity (critical for KEDA 2.15 upgrade)

**Action Items:**

1. **Check for Pod Identity usage in TriggerAuthentication:**
   ```bash
   # In codebase
   grep -r "podIdentity" /home/ahmedmuhi/code/reddog-code/manifests/ || echo "Not found"

   # In running cluster
   kubectl get triggerauthentication -A -o yaml | grep -A5 "podIdentity"
   ```

2. **If Pod Identity found:**
   - Document which provider: `azure`, `aws`, or `aws-kiam`
   - Identify which scalers use it
   - Assess migration effort to workload identity

3. **If using Kubernetes secrets:**
   - Document secret structure
   - Plan for secret migration if needed
   - Verify secrets are in TriggerAuthentication (not inline)

**Deliverable:** Authentication assessment document with migration path if Pod Identity is used

---

### Task 1.3: Create Upgrade Project Plan

**Objective:** Define detailed project timeline and resource requirements

**Timeline:**

| Phase | Duration | Key Milestones |
|-------|----------|-----------------|
| Discovery & Planning | 1 week | Audit complete, auth method identified |
| Staging Environment | 2 weeks | All 7 tests passed, issues documented |
| Production Prep | 1 week | Communication sent, runbooks prepared |
| Production Upgrade | 1 day | 30-minute maintenance window |
| Post-Upgrade Monitoring | 4 weeks | Daily checks, then weekly, then monthly |

**Resource Requirements:**

- **DevOps Engineer:** Full-time for planning & testing, part-time for monitoring
- **Platform Engineer:** Review authentication migration
- **On-Call Engineer:** Available during production upgrade window
- **Communication Lead:** Notify teams of maintenance window

**Risk Register:**

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Pod Identity not compatible | Medium | High | Test in staging first |
| Helm CRD management fails | Medium | High | Patch CRDs before upgrade |
| Scaling doesn't work post-upgrade | Low | High | Comprehensive testing in staging |
| RabbitMQ/Redis connectivity issues | Medium | High | Test with 4.2/8.0 in staging |
| Kubernetes version incompatibility | Low | High | Verify all clusters are 1.30+ |

**Success Criteria:**

- All 7 test scenarios pass in staging
- All ScaledObjects reach "Ready" status post-upgrade
- No error logs in KEDA operator for 24 hours
- Production services scale correctly based on queue/list depth
- 4-week post-upgrade monitoring shows no regressions

---

## Phase 2: Staging Environment Testing (Week 2-3)

### Task 2.1: Set Up Staging Environment

**Objective:** Create identical staging cluster for upgrade validation

**Prerequisites:**
- Kubernetes 1.30+ cluster
- RabbitMQ 4.2.0 deployed
- Redis 8.0.5 deployed
- Dapr 1.16.2 installed
- Monitoring and alerting configured

**Setup Steps:**

```bash
# Deploy staging cluster (e.g., AKS)
az aks create --resource-group staging-rg --name staging-cluster \
  --kubernetes-version 1.30 --node-vm-size Standard_D4s_v3

# Verify prerequisites
kubectl version --short
helm list

# Deploy test services
kubectl apply -f manifests/branch/base/deployments/make-line-service.yaml
kubectl apply -f manifests/branch/base/deployments/loyalty-service.yaml
```

**Deliverable:** Staging environment validation checklist completed

---

### Task 2.2: Pre-Upgrade Backup

**Objective:** Ensure rollback capability

**Backup Steps:**

```bash
# Backup entire keda namespace
kubectl get all -n keda -o yaml > staging-keda-namespace-backup.yaml

# Backup Helm release
helm get values keda -n keda > staging-keda-values-backup.yaml

# Backup all CRDs
kubectl get crd scaledobjects.keda.sh -o yaml > staging-scaledobjects-crd.yaml
kubectl get crd scaledjobs.keda.sh -o yaml > staging-scaledjobs-crd.yaml
kubectl get crd triggerauthentications.keda.sh -o yaml > staging-triggerauthentications-crd.yaml
kubectl get crd clustertriggerauthentications.keda.sh -o yaml > staging-clustertriggerauthentications-crd.yaml

# Backup application configuration
kubectl get scaledobject -A -o yaml > staging-scaledobjects-all.yaml
kubectl get triggerauthentication -A -o yaml > staging-triggerauthentications-all.yaml
```

**Deliverable:** Complete backup stored in version control or secure backup location

---

### Task 2.3: Apply Helm CRD Ownership Patches

**Objective:** Prevent Helm upgrade CRD conflicts

**Patch Script:**

```bash
#!/bin/bash
# File: scripts/keda-patch-crds.sh

echo "Patching KEDA CRDs for Helm 2.2.1+ compatibility..."

kubectl patch crd scaledobjects.keda.sh -p \
  '{"metadata":{"annotations":{"meta.helm.sh/release-name":"keda","meta.helm.sh/release-namespace":"keda"},"labels":{"app.kubernetes.io/managed-by":"Helm"}}}'

kubectl patch crd scaledjobs.keda.sh -p \
  '{"metadata":{"annotations":{"meta.helm.sh/release-name":"keda","meta.helm.sh/release-namespace":"keda"},"labels":{"app.kubernetes.io/managed-by":"Helm"}}}'

kubectl patch crd triggerauthentications.keda.sh -p \
  '{"metadata":{"annotations":{"meta.helm.sh/release-name":"keda","meta.helm.sh/release-namespace":"keda"},"labels":{"app.kubernetes.io/managed-by":"Helm"}}}'

kubectl patch crd clustertriggerauthentications.keda.sh -p \
  '{"metadata":{"annotations":{"meta.helm.sh/release-name":"keda","meta.helm.sh/release-namespace":"keda"},"labels":{"app.kubernetes.io/managed-by":"Helm"}}}'

echo "CRD patching complete"
```

**Run Patch:**

```bash
chmod +x scripts/keda-patch-crds.sh
./scripts/keda-patch-crds.sh
```

**Deliverable:** Patch script stored in `scripts/` directory

---

### Task 2.4: Upgrade KEDA to 2.18.1

**Objective:** Perform upgrade in staging

**Upgrade Steps:**

```bash
# Update Helm repository
helm repo add kedacore https://kedacore.github.io/charts || helm repo update kedacore
helm repo update

# Verify new version is available
helm search repo kedacore/keda --version 2.18.1

# Upgrade KEDA
helm upgrade keda kedacore/keda --namespace keda --version 2.18.1 --values values.yaml

# Monitor upgrade progress
kubectl rollout status deployment/keda-operator -n keda --timeout=5m

# Verify operator is ready
kubectl get pods -n keda
kubectl logs -n keda deployment/keda-operator
```

**Troubleshooting If Upgrade Fails:**

```bash
# Check Helm release status
helm status keda -n keda

# Check for CRD conflicts
kubectl get crd scaledobjects.keda.sh -o yaml | grep managedBy

# If CRD issue, rollback and retry with patches
helm rollback keda -n keda
# Run patch script again
# Then retry helm upgrade
```

**Deliverable:** Upgrade completion report with timestamps and any issues encountered

---

### Task 2.5: Run Test Suite (7 Tests)

**Objective:** Validate all KEDA functionality post-upgrade

**TEST-001: ScaledObject Creation and Status**

```bash
# Create test ScaledObject
cat > test-scaledobject.yaml <<EOF
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: test-make-line-scaler
  namespace: reddog-retail
spec:
  scaleTargetRef:
    name: make-line-service
  minReplicaCount: 1
  maxReplicaCount: 5
  triggers:
  - type: rabbitmq
    metadata:
      host: amqp://guest:guest@rabbitmq:5672/
      queueName: test-queue
      mode: QueueLength
      value: "5"
EOF

kubectl apply -f test-scaledobject.yaml

# Verify status
kubectl get scaledobject test-make-line-scaler -n reddog-retail
kubectl describe scaledobject test-make-line-scaler -n reddog-retail

# Check HPA was created with v2 API
kubectl get hpa -n reddog-retail test-make-line-scaler -o yaml | grep "apiVersion:"

# Expected output: "apiVersion: autoscaling/v2"
```

**Success Criteria:**
- ScaledObject shows `ACTIVE: True`
- HPA created with `apiVersion: autoscaling/v2`
- Status condition is `Ready: True`
- No error logs in KEDA operator

**TEST-002: RabbitMQ Scaler Queue Monitoring**

```bash
# Verify RabbitMQ is accessible
curl -u guest:guest http://rabbitmq:15672/api/queues

# Publish test messages
docker exec <rabbitmq-container> \
  rabbitmq-admin publish queue=test-queue payload="test message" count=100

# Monitor pod scaling
kubectl get pods -n reddog-retail -w

# Expected: make-line-service pods should scale up (approximately 100/5 = 20 pods)

# Check scaler activity in logs
kubectl logs -n keda deployment/keda-operator -f | grep -i rabbitmq

# Expected: Logs should show queue length detection
```

**Success Criteria:**
- Pods scale from 1 to ~20
- No error messages in KEDA logs
- HPA shows scaling activity: `kubectl describe hpa -n reddog-retail`

**TEST-003: Redis Scaler List Monitoring**

```bash
# Verify Redis is accessible
redis-cli -h redis-cache ping
# Expected: PONG

# Clear test list
redis-cli -h redis-cache DEL test-list

# Create ScaledObject for Redis list
cat > test-redis-scaler.yaml <<EOF
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: test-loyalty-scaler
  namespace: reddog-retail
spec:
  scaleTargetRef:
    name: loyalty-service
  minReplicaCount: 1
  maxReplicaCount: 5
  triggers:
  - type: redis
    metadata:
      address: redis-cache:6379
      listName: test-list
      listLength: "10"
EOF

kubectl apply -f test-redis-scaler.yaml

# Push items to list
redis-cli -h redis-cache RPUSH test-list item{1..50}

# Monitor pod scaling
kubectl get pods -n reddog-retail -w

# Expected: loyalty-service pods should scale up (approximately 50/10 = 5 pods)

# Verify scaling down
redis-cli -h redis-cache DEL test-list
kubectl get pods -n reddog-retail -w
# Expected: Pods should scale down after cooldownPeriod (default 300s)
```

**Success Criteria:**
- Pods scale up when list items added
- Pods scale down when list cleared
- Scaling respects cooldownPeriod
- No Redis connectivity errors

**TEST-004: Workload Identity Authentication** (If Using)

```bash
# Only run if Red Dog uses workload identity on AKS/EKS/GKE

# For Azure (AKS with Workload Identity):
cat > test-workload-identity.yaml <<EOF
apiVersion: keda.sh/v1alpha1
kind: TriggerAuthentication
metadata:
  name: test-rabbitmq-wi
  namespace: reddog-retail
spec:
  podIdentity:
    provider: azure-workload
    identityId: /subscriptions/{sub}/resourcegroups/{rg}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{name}
EOF

kubectl apply -f test-workload-identity.yaml

# Verify TriggerAuthentication is ready
kubectl get triggerauthentication test-rabbitmq-wi -n reddog-retail
# Expected: READY: True

# Check pod identity webhook injected token
kubectl exec -it <keda-operator-pod> -n keda -- ls /var/run/secrets/workload-identity-token/
# Expected: File should exist
```

**Success Criteria:**
- TriggerAuthentication shows `READY: True`
- No authentication errors in KEDA logs
- Pods can authenticate without credentials in manifest

**TEST-005: HPA Scaling Behavior**

```bash
# Verify HPA was created with correct API version
kubectl get hpa -n reddog-retail -o yaml | grep "apiVersion:"
# Expected: "apiVersion: autoscaling/v2"

# Trigger scaling by creating load
# (Publish messages to RabbitMQ or add items to Redis)

# Monitor HPA decisions
kubectl describe hpa -n reddog-retail test-make-line-scaler | grep -A10 "Metrics:"
# Expected: Should show:
# - Target Pods for QueueLength scaler
# - Current value and target value
# - Scale-up/scale-down times

# Verify cooldown period is respected
# (Wait 5 minutes between scaling events)
kubectl describe hpa -n reddog-retail test-make-line-scaler | grep "Last Scale Time:"
```

**Success Criteria:**
- HPA uses `autoscaling/v2` API
- Metrics properly evaluated
- Scaling decisions logged
- Cooldown period respected

**TEST-006: Dapr + KEDA Integration**

```bash
# Verify make-line-service has Dapr sidecar
kubectl get pods -n reddog-retail make-line-service-xxxx -o jsonpath='{.spec.containers[*].name}'
# Expected: make-line-service daprd

# Both containers should be running
kubectl describe pod -n reddog-retail <make-line-service-pod> | grep -A3 "Containers:"

# Test Dapr service invocation from scaled pod
kubectl exec -it <make-line-service-pod> -c make-line-service -- \
  curl http://localhost:3500/v1.0/invoke/loyalty-service/method/status

# Verify Dapr sidecar initialization succeeded
kubectl logs -n reddog-retail <make-line-service-pod> -c daprd | head -20
# Expected: Initialization logs, no connection errors
```

**Success Criteria:**
- New pods created with both app container and Dapr sidecar
- Dapr sidecar initializes successfully
- Service-to-service invocation works
- No Dapr initialization errors

**TEST-007: Error Handling and Fallback**

```bash
# Create ScaledObject with fallback configuration
cat > test-fallback.yaml <<EOF
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: test-fallback
  namespace: reddog-retail
spec:
  scaleTargetRef:
    name: make-line-service
  minReplicaCount: 1
  maxReplicaCount: 5
  fallback:
    failureThreshold: 3
    replicas: 3
  triggers:
  - type: rabbitmq
    metadata:
      host: amqp://guest:guest@rabbitmq-unreachable:5672/
      queueName: test-queue
      mode: QueueLength
      value: "5"
EOF

kubectl apply -f test-fallback.yaml

# Wait for scaler to fail (3 polling intervals = ~90 seconds)
sleep 100

# Verify fallback replicas
kubectl describe scaledobject test-fallback -n reddog-retail | grep -i "fallback"
# Expected: Shows fallback is active, 3 replicas

# Verify pod count
kubectl get pods -n reddog-retail | grep make-line-service | wc -l
# Expected: 3

# Fix the RabbitMQ connection (restore connection)
kubectl set env deployment/keda-operator RABBITMQ_HOST=rabbitmq:5672 -n keda

# Wait for recovery
sleep 60

# Verify normal scaling resumes
kubectl describe scaledobject test-fallback -n reddog-retail | grep -i "status"
# Expected: Should return to "Active"
```

**Success Criteria:**
- ScaledObject detects scaler failure after 3 failures
- Fallback replicas set to specified count
- Scaling resumes when scaler recovers
- Clear error messages in logs during failure

---

### Task 2.6: Document Test Results

**Objective:** Create comprehensive test report

**Test Report Template:**

```markdown
# KEDA 2.18.1 Staging Test Report

## Environment
- Cluster: Staging AKS
- Kubernetes Version: 1.30.x
- KEDA Version: 2.18.1
- RabbitMQ Version: 4.2.0
- Redis Version: 8.0.5
- Dapr Version: 1.16.2
- Test Date: YYYY-MM-DD

## Test Results

### TEST-001: ScaledObject Creation
- Status: ✅ PASS / ❌ FAIL
- Duration: X minutes
- Issues: (none) / (describe)
- Notes: HPA created with v2 API, all conditions ready

### TEST-002: RabbitMQ Queue Monitoring
- Status: ✅ PASS / ❌ FAIL
- Duration: X minutes
- Scaling Pattern: Pods scaled from 1 to 20
- Issues: (none) / (describe)

### TEST-003: Redis List Monitoring
- Status: ✅ PASS / ❌ FAIL
- Duration: X minutes
- Scaling Pattern: Pods scaled from 1 to 5
- Issues: (none) / (describe)

### TEST-004: Workload Identity (if applicable)
- Status: ✅ PASS / ⏭️ SKIPPED / ❌ FAIL
- Issues: (none) / (describe)

### TEST-005: HPA Behavior
- Status: ✅ PASS / ❌ FAIL
- HPA API Version: autoscaling/v2 ✅
- Metrics: Properly evaluated ✅
- Issues: (none) / (describe)

### TEST-006: Dapr Integration
- Status: ✅ PASS / ❌ FAIL
- Sidecar Initialization: Successful ✅
- Service Invocation: Working ✅
- Issues: (none) / (describe)

### TEST-007: Error Handling
- Status: ✅ PASS / ❌ FAIL
- Fallback Activation: X seconds
- Recovery Time: X seconds
- Issues: (none) / (describe)

## Overall Assessment
- ✅ Ready for Production / ⚠️ Ready with Caveats / ❌ Not Ready

## Issues Found
(none) /
1. Issue 1: Description, impact, resolution
2. Issue 2: ...

## Recommendations
1. Recommendation 1
2. Recommendation 2

## Sign-Off
- Tested By: [Name]
- Date: YYYY-MM-DD
- Approved By: [Name]
```

**Deliverable:** Comprehensive test report stored in docs/research/

---

### Task 2.7: 24-Hour Stability Check

**Objective:** Verify no regression over extended period

**Monitoring Checklist:**

```bash
# During 24-hour monitoring period:

# Every 4 hours:
kubectl get pods -n keda
kubectl get scaledobject -A
kubectl logs -n keda deployment/keda-operator | tail -50 | grep -i error

# Daily:
kubectl top nodes
kubectl top pods -n keda

# Verify scaling still working:
# - Publish 50 messages, verify scale-up
# - Consume messages, verify scale-down
# - Check that scaling timing is consistent

# Check for memory leaks:
kubectl get pods -n keda -o custom-columns=NAME:.metadata.name,MEMORY:.status.containerStatuses[0].lastState.running.finishedAt --watch
```

**Success Criteria:**
- No KEDA operator restarts
- Consistent memory usage (no growth)
- No error logs
- Scaling works reliably

**Deliverable:** 24-hour stability report

---

## Phase 3: Production Preparation (Week 3)

### Task 3.1: Communication Plan

**Objective:** Notify teams of upgrade plan

**Communication Items:**

1. **Email Announcement (1 week before)**
   ```
   Subject: KEDA Upgrade Scheduled - Maintenance Window Required

   Dear Team,

   We are upgrading KEDA from version 2.2.0 to 2.18.1 to support
   modern Kubernetes features and improve scaling reliability.

   Maintenance Window:
   - Date: [Date]
   - Time: [HH:MM - HH+0.5:MM] UTC
   - Duration: 30 minutes
   - Expected Impact: Minimal (auto-scaling may be paused)

   Changes:
   - KEDA operator will be restarted
   - ScaledObjects will be verified
   - Pod scaling may not occur during window
   - Services remain available

   Please mark this on your calendar and plan accordingly.
   ```

2. **Pre-Upgrade Briefing (2 days before)**
   - Walkthrough of upgrade procedure
   - Q&A session
   - Escalation contacts

3. **Post-Upgrade Validation (1 hour after)**
   - Confirm all services healthy
   - Check scaling is functioning
   - Review error logs

---

### Task 3.2: Prepare Runbooks

**Objective:** Create step-by-step procedures for upgrade execution

**Runbooks to Create:**

1. **Pre-Upgrade Checklist** (`scripts/keda-upgrade-preflights.sh`)
   ```bash
   #!/bin/bash
   echo "Pre-Upgrade KEDA Checklist"
   kubectl get pods -n keda
   kubectl get scaledobject -n reddog-retail
   kubectl logs -n keda deployment/keda-operator | tail -20
   echo "Check: All pods running, no recent errors"
   ```

2. **Backup Procedure** (`scripts/keda-backup.sh`)
   ```bash
   #!/bin/bash
   DATE=$(date +%Y%m%d-%H%M%S)
   BACKUP_DIR="backups/keda-$DATE"
   mkdir -p $BACKUP_DIR
   kubectl get all -n keda -o yaml > $BACKUP_DIR/keda-namespace.yaml
   helm get values keda -n keda > $BACKUP_DIR/keda-values.yaml
   # ... more backups
   ```

3. **Upgrade Procedure** (`scripts/keda-upgrade.sh`)
   - Patch CRDs
   - Update Helm repo
   - Execute upgrade
   - Monitor progress

4. **Validation Procedure** (`scripts/keda-validate-upgrade.sh`)
   - Check operator health
   - Verify HPA API version
   - Test RabbitMQ scaling
   - Test Redis scaling

5. **Rollback Procedure** (`scripts/keda-rollback.sh`)
   - Helm rollback
   - Verify restoration
   - Post-rollback tests

---

### Task 3.3: Risk Mitigation

**Objective:** Identify and address potential issues

**Risk Mitigation Plans:**

| Risk | Mitigation |
|------|-----------|
| Helm CRD conflict | Run patch script before upgrade, test in staging |
| Pod Identity broken | Audit pre-upgrade, migrate to workload identity, test in staging |
| Scaling stops working | Comprehensive testing in staging, validation checklist post-upgrade |
| RabbitMQ connectivity | Test with RabbitMQ 4.2 in staging, verify scaler logs |
| Redis connectivity | Test with Redis 8.0 in staging, verify ACL permissions |

---

### Task 3.4: Prepare Rollback Plan

**Objective:** Enable quick recovery if issues occur

**Rollback Decision Criteria:**

**Automatic Rollback Triggers:**
- KEDA operator pod crashes repeatedly (>5 in 5 minutes)
- >50% of ScaledObjects show error status
- Scaling completely stops for >10 minutes

**Manual Rollback Triggers:**
- Pod Identity authentication broken
- HPA objects not created
- Major scalers not functioning (RabbitMQ or Redis)

**Rollback Procedure:**

```bash
# 1. Stop new deployments (prevent further issues)
kubectl scale deployment make-line-service --replicas=2 -n reddog-retail

# 2. Identify issue severity (check logs)
kubectl logs -n keda deployment/keda-operator | tail -100 | grep -i error

# 3. Decide: Hotfix vs Rollback
# If issue is minor -> hotfix in place
# If issue is major -> rollback immediately

# 4. Execute rollback
helm rollback keda -n keda

# 5. Verify rollback
kubectl wait --for=condition=ready pod -l app.kubernetes.io/name=keda-operator -n keda
kubectl get scaledobject -n reddog-retail
kubectl get hpa -n reddog-retail

# 6. Test scaling post-rollback
# (Run TEST-002 and TEST-003 again)

# 7. Post-rollback analysis
# Document root cause and plan remediation
```

---

## Phase 4: Production Upgrade (Day of Upgrade)

### Upgrade Timeline

**T-30 min: Pre-Upgrade Briefing**
```bash
# All teams online
# Review runbooks
# Confirm maintenance window
# Identify escalation contacts
```

**T-15 min: Pre-Flight Checks**
```bash
./scripts/keda-upgrade-preflights.sh
# Expected: All pods running, no recent errors
```

**T-10 min: Backup Current State**
```bash
./scripts/keda-backup.sh
# Expected: Backup directory created with manifests
```

**T-5 min: CRD Patch (if not already done)**
```bash
./scripts/keda-patch-crds.sh
# Expected: All 4 CRDs patched with Helm ownership
```

**T+0 min: Execute Upgrade**
```bash
./scripts/keda-upgrade.sh
# Command:
helm upgrade keda kedacore/keda --namespace keda --version 2.18.1
```

**T+10 min: Monitor Upgrade Progress**
```bash
kubectl rollout status deployment/keda-operator -n keda --timeout=5m
kubectl logs -n keda deployment/keda-operator -f
# Expected: Operator pod becomes ready, minimal error logs
```

**T+15 min: Validation Checks**
```bash
./scripts/keda-validate-upgrade.sh
# Expected: All checks pass
```

**T+20 min: Application Testing**
```bash
# Publish 100 messages to RabbitMQ test-queue
# Verify pods scale from 1 to 20
kubectl get pods -n reddog-retail -w
```

**T+25 min: Final Verification**
```bash
kubectl get scaledobject -n reddog-retail
# Expected: All ACTIVE=True, STATUS=Ready

kubectl get hpa -n reddog-retail -o yaml | grep "apiVersion:" | sort | uniq
# Expected: autoscaling/v2

kubectl logs -n keda deployment/keda-operator | grep -i error | wc -l
# Expected: 0 or very low
```

**T+30 min: Upgrade Complete**
```bash
# Notify teams
# Close maintenance window
# Begin post-upgrade monitoring
```

---

## Phase 5: Post-Upgrade Monitoring (4 Weeks)

### Daily Monitoring (Days 1-3)

**Every 4 hours:**
```bash
# KEDA operator health
kubectl get pods -n keda
kubectl logs -n keda deployment/keda-operator | tail -20

# ScaledObject status
kubectl get scaledobject -n reddog-retail

# Error rate
kubectl logs -n keda deployment/keda-operator | grep -i error | wc -l
```

**Daily summary:**
- Any KEDA operator restarts?
- Any ScaledObject errors?
- Scaling working as expected?
- Memory/CPU usage normal?

---

### Weekly Monitoring (Weeks 2-4)

**Every Monday:**
```bash
# Overall system health
kubectl describe nodes
kubectl top nodes
kubectl top pods -n keda

# KEDA metrics
kubectl get scaledobject -A
kubectl get hpa -A

# Scaling effectiveness
# - Did pods scale for RabbitMQ queues?
# - Did pods scale for Redis lists?
# - Was scaling timing correct?

# Resource usage trends
# - Is KEDA operator memory stable?
# - Any memory leaks evident?
```

---

### Monthly Sign-Off (Month 1)

**After 4 weeks, confirm:**
- No KEDA operator restarts
- All ScaledObjects stable
- Scaling reliable and accurate
- No memory leaks
- No error trends
- Performance unchanged

**Deliverable:** Monthly post-upgrade report

---

## Appendix: Scripts and Templates

### Script 1: CRD Patch Script

**File:** `scripts/keda-patch-crds.sh`

```bash
#!/bin/bash
set -e

NAMESPACE="keda"
RELEASE_NAME="keda"

echo "Patching KEDA CRDs for Helm 2.2.1+ compatibility..."

CRDS=(
  "scaledobjects.keda.sh"
  "scaledjobs.keda.sh"
  "triggerauthentications.keda.sh"
  "clustertriggerauthentications.keda.sh"
)

for crd in "${CRDS[@]}"; do
  echo "Patching $crd..."
  kubectl patch crd "$crd" -p \
    "{\"metadata\":{\"annotations\":{\"meta.helm.sh/release-name\":\"$RELEASE_NAME\",\"meta.helm.sh/release-namespace\":\"$NAMESPACE\"},\"labels\":{\"app.kubernetes.io/managed-by\":\"Helm\"}}}"
done

echo "CRD patching complete"
```

---

### Script 2: Backup Script

**File:** `scripts/keda-backup.sh`

```bash
#!/bin/bash
set -e

DATE=$(date +%Y%m%d-%H%M%S)
BACKUP_DIR="backups/keda-$DATE"

mkdir -p "$BACKUP_DIR"

echo "Backing up KEDA configuration..."

# Namespace resources
kubectl get all -n keda -o yaml > "$BACKUP_DIR/keda-namespace.yaml"

# Helm release
helm get values keda -n keda > "$BACKUP_DIR/keda-values.yaml"

# CRDs
kubectl get crd scaledobjects.keda.sh -o yaml > "$BACKUP_DIR/crd-scaledobjects.yaml"
kubectl get crd scaledjobs.keda.sh -o yaml > "$BACKUP_DIR/crd-scaledjobs.yaml"
kubectl get crd triggerauthentications.keda.sh -o yaml > "$BACKUP_DIR/crd-triggerauthentications.yaml"
kubectl get crd clustertriggerauthentications.keda.sh -o yaml > "$BACKUP_DIR/crd-clustertriggerauthentications.yaml"

# Application resources
kubectl get scaledobject -A -o yaml > "$BACKUP_DIR/scaledobjects-all.yaml"
kubectl get triggerauthentication -A -o yaml > "$BACKUP_DIR/triggerauthentications-all.yaml"

echo "Backup complete: $BACKUP_DIR"
```

---

### Script 3: Upgrade Script

**File:** `scripts/keda-upgrade.sh`

```bash
#!/bin/bash
set -e

echo "KEDA Upgrade Script"
echo "===================="

# Configuration
NAMESPACE="keda"
RELEASE_NAME="keda"
TARGET_VERSION="2.18.1"
TIMEOUT="300s"

# Pre-flight
echo "1. Running pre-flight checks..."
./scripts/keda-upgrade-preflights.sh

# Backup
echo "2. Creating backup..."
./scripts/keda-backup.sh

# Patch CRDs
echo "3. Patching CRDs..."
./scripts/keda-patch-crds.sh

# Update Helm repo
echo "4. Updating Helm repository..."
helm repo add kedacore https://kedacore.github.io/charts || true
helm repo update kedacore

# Verify version exists
echo "5. Verifying target version..."
helm search repo kedacore/keda --version "$TARGET_VERSION"

# Upgrade
echo "6. Executing Helm upgrade..."
helm upgrade "$RELEASE_NAME" kedacore/keda \
  --namespace "$NAMESPACE" \
  --version "$TARGET_VERSION"

# Monitor
echo "7. Waiting for operator to be ready..."
kubectl wait --for=condition=ready pod \
  -l app.kubernetes.io/name=keda-operator \
  -n "$NAMESPACE" \
  --timeout="$TIMEOUT"

echo "Upgrade complete!"
```

---

### Script 4: Validation Script

**File:** `scripts/keda-validate-upgrade.sh`

```bash
#!/bin/bash
set -e

echo "KEDA Upgrade Validation"
echo "======================="

NAMESPACE="keda"
CHECKS_PASSED=0
CHECKS_FAILED=0

check() {
  local description=$1
  local command=$2
  local expected=$3

  echo -n "Checking: $description... "
  result=$(eval "$command" || echo "ERROR")

  if [[ "$result" == *"$expected"* ]]; then
    echo "✅ PASS"
    ((CHECKS_PASSED++))
  else
    echo "❌ FAIL"
    echo "  Expected: $expected"
    echo "  Got: $result"
    ((CHECKS_FAILED++))
  fi
}

# Checks
check "KEDA operator pod running" \
  "kubectl get pods -n $NAMESPACE -l app.kubernetes.io/name=keda-operator -o jsonpath='{.items[0].status.phase}'" \
  "Running"

check "HPA API version is v2" \
  "kubectl get hpa -n reddog-retail -o yaml | grep 'apiVersion:' | head -1" \
  "autoscaling/v2"

check "ScaledObjects are ready" \
  "kubectl get scaledobject -n reddog-retail -o jsonpath='{.items[*].status.ready}' | grep -c true" \
  ""

check "No error logs" \
  "kubectl logs -n $NAMESPACE deployment/keda-operator | grep -i error | wc -l" \
  "0"

# Summary
echo ""
echo "Validation Summary"
echo "===================="
echo "Passed: $CHECKS_PASSED"
echo "Failed: $CHECKS_FAILED"

if [ $CHECKS_FAILED -eq 0 ]; then
  echo "✅ All checks passed"
  exit 0
else
  echo "❌ Some checks failed"
  exit 1
fi
```

---

## Success Criteria Summary

- ✅ KEDA 2.18.1 deployed successfully
- ✅ All ScaledObjects reach "Ready" status
- ✅ HPA objects created with `autoscaling/v2` API
- ✅ RabbitMQ queue scaling verified
- ✅ Redis list scaling verified
- ✅ Dapr integration confirmed (if applicable)
- ✅ No error logs in KEDA operator (24 hours post-upgrade)
- ✅ 4-week stability period with no issues
- ✅ Rollback capability verified but not executed

---

**Plan Status:** Ready for implementation
**Last Updated:** November 9, 2024
**Next Step:** Execute Phase 1 (Discovery & Planning)
