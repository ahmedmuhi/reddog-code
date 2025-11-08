# Dapr 1.3.0 → 1.16.2 Upgrade: Executive Summary

**Status**: READY FOR UPGRADE ✓ (with conditions)
**Risk Level**: MEDIUM
**Complexity**: MODERATE
**Estimated Timeline**: 3-4 weeks

---

## KEY FINDINGS

### Critical Blockers: NONE
Dapr 1.16.2 upgrade is technically feasible with no blocking issues.

### Critical Warnings: 1

#### ⚠️ Redis Version Incompatibility
- **Issue**: Red Dog plans to upgrade Redis to 8.0.5, but Dapr 1.16 only supports Redis 6.x
- **Impact**: State store would fail completely if Redis 7+ used
- **Solution**: Keep Redis at 6.x LTS for now
  - OR wait for Dapr 7/8 support (unknown timeline)
  - OR switch to PostgreSQL state store (major refactoring)
- **Recommendation**: Use Redis 6.x with Dapr 1.16

---

## BREAKING CHANGES THAT AFFECT RED DOG

### 1. Service Invocation: Explicit Content-Type Header Required ⚠️
**Introduced**: Dapr 1.9 (Oct 2022)
- **Before**: Dapr auto-added `Content-Type: application/json` header
- **After**: Must explicitly add header in every inter-service HTTP call

**Affected Services**: OrderService, MakeLineService, LoyaltyService, AccountingService, VirtualWorker

**Migration Required**: Add to all service-to-service HTTP requests:
```http
Content-Type: application/json
```

**Effort**: Medium (audit all service invocation code, add headers)

---

### 2. Component Names Must Be Globally Unique ⚠️
**Introduced**: Dapr 1.13 (Mar 2024)
- **Before**: Could have `reddog` pubsub + `reddog` state store
- **After**: Names must be unique across ALL component types

**Current Status**: Unknown - requires audit of YAML manifests

**Migration Required**: If conflicts exist, rename components
- Example: `reddog` → `reddog-pubsub`, `reddog-state`, `reddog-secrets`

**Effort**: Low (likely no changes needed, verify)

---

### 3. Actor Reminders: Scheduler Becomes Default ⚠️
**Introduced**: Dapr 1.15 (Feb 2025)
- **Before**: Actor reminders stored in state store
- **After**: Actor reminders managed by Scheduler service by default

**Affected**: VirtualWorker (uses actor reminders)

**Automatic Migration**: Dapr 1.15 auto-migrates existing reminders
- One-time operation per actor type
- Should be transparent

**Data Loss Risk**: Upgrading 1.14→1.15 removes Scheduler data directory
- Could lose reminders if Scheduler was previously used
- Mitigation: Create etcd backup before upgrade

**Effort**: Low (mostly automatic, requires testing)

---

## CHANGES THAT DO NOT AFFECT RED DOG

✓ Pub/Sub API (RabbitMQ): Stable - no changes
✓ State Management API (Redis): Stable - no changes
✓ Secrets API: Stable - no changes
✓ Bindings API: Stable - no changes
✓ Kubernetes Annotations: Stable - no changes
✓ Dapr Sidecar Ports (3500, 50001): Unchanged
✓ Component YAML Schema: v1alpha1 unchanged

---

## COMPATIBILITY MATRIX

| Component | Current | Target | Compatible? | Notes |
|-----------|---------|--------|------------|-------|
| **Dapr Runtime** | 1.3.0 | 1.16.2 | ✓ YES | Direct upgrade OK |
| **Kubernetes** | 1.26+ | 1.30+ | ✓ YES | All confirmed clouds |
| **RabbitMQ** | 3.x | 4.2 | ✓ YES | Compatible with Dapr |
| **Redis** | 6.x | 6.x (NOT 8.x) | ✓ YES | Dapr 1.16 not compatible with Redis 7+ |
| **.NET SDK** | .NET 6 | HTTP APIs only | ✓ YES | No SDK available for .NET 10 yet |
| **Azure Container Apps** | 1.13.6 | 1.16.2 pending | ❓ PENDING | Not available yet in ACA |

---

## MANDATORY ACTIONS BEFORE UPGRADE

### 1. Code Review & Modification ✓
- **What**: Add explicit `Content-Type: application/json` header to all service invocations
- **Why**: Dapr 1.9+ no longer auto-adds default header
- **Scope**: All inter-service HTTP calls
- **Effort**: Medium
- **Testing**: HTTP integration tests required

### 2. Component Audit ✓
- **What**: Verify no duplicate component names across types
- **Why**: Dapr 1.13+ enforces globally unique names
- **Scope**: All YAML files in `manifests/` directories
- **Effort**: Low
- **Action**: Rename if conflicts found

### 3. Infrastructure Check ✓
- **What**: Confirm Redis version is 6.x (NOT 7.x or 8.x)
- **Why**: Dapr 1.16 incompatible with Redis 7+
- **Scope**: Production, staging, development
- **Effort**: Low
- **Action**: Keep Redis at 6.x or plan alternative

### 4. Backup Strategy ✓
- **What**: Plan backups before upgrade
- **Why**: Data loss risk if downgrade needed
- **Scope**: Redis, databases, etcd (if using scheduler)
- **Effort**: Low

---

## NEW FEATURES IN DAPR 1.16 (OPTIONAL)

### 1. Workload Identity Federation ✓
- **What**: Zero-trust authentication to Azure, AWS, GCP
- **Benefit**: No long-lived secrets needed
- **When**: Plan for Phase 2 (post-upgrade)
- **Effort**: Medium

### 2. Configuration API ✓
- **What**: Centralized application configuration management
- **Mandated By**: ADR-0004
- **Benefit**: Dynamic config without app restart
- **When**: Plan for Phase 2 (post-upgrade)
- **Effort**: Medium

### 3. OpenTelemetry Integration ✓
- **What**: Unified observability (metrics, traces, logs)
- **Benefit**: Modern observability stack
- **When**: Optional, plan for Phase 2+
- **Effort**: Medium

---

## UPGRADE RISKS

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| **Redis 8.0 incompatibility** | HIGH | HIGH | Keep Redis 6.x |
| **Service invocation failures** | HIGH | MEDIUM | Add Content-Type headers |
| **Actor reminder data loss** | MEDIUM | HIGH | Backup etcd before upgrade |
| **Component load failures** | MEDIUM | MEDIUM | Audit component names |
| **Staging test failures** | MEDIUM | LOW | Full test suite required |

---

## TESTING CHECKLIST

**Required Tests Before Production**:

- [ ] Service Invocation with Content-Type headers
- [ ] Pub/Sub message flow (RabbitMQ)
- [ ] Redis state store operations
- [ ] Secret store access (Azure, AWS, GCP)
- [ ] Kubernetes sidecar injection
- [ ] Actor reminder functionality (VirtualWorker)
- [ ] Helm upgrade procedure
- [ ] Load testing (performance baseline)

**Testing Environment**: Staging (full copy of production)

**Success Criteria**: All tests pass, no error spikes, performance stable

---

## RECOMMENDED UPGRADE SEQUENCE

### Week 1: Planning & Validation
- Code audit (service invocations)
- Infrastructure audit (Redis, Kubernetes, backups)
- Component manifest review
- Test case development

### Week 2: Staging Upgrade
- Deploy Dapr 1.16.2 to staging
- Run full test suite
- Load testing
- Performance validation

### Week 3: Production Upgrade
- Backup critical data
- Upgrade CRDs
- Helm upgrade to 1.16.2
- Rolling restart of pods
- Monitor for errors

### Week 4+: Validation & Next Steps
- 48+ hour monitoring
- Performance baseline confirmation
- Plan Phase 2 (Configuration API, workload identity)
- Plan Phase 3 (.NET 10 upgrade)

---

## GO/NO-GO DECISION

### RECOMMENDATION: GO ✓

**Red Dog Coffee CAN upgrade to Dapr 1.16.2 with manageable effort:**

✓ No blocking technical issues
✓ All core building blocks stable
✓ HTTP APIs backward compatible
✓ RabbitMQ 4.2 compatible
✓ Kubernetes 1.30 compatible

**Prerequisites**:
1. Keep Redis at 6.x (critical constraint)
2. Add explicit Content-Type headers to service invocations
3. Verify component name uniqueness
4. Run full integration test suite

**Go/No-Go Criteria**:
- ✓ No critical blocking issues
- ✓ Workaround for .NET 10 (use HTTP APIs)
- ✓ All breaking changes identified and mitigatable
- ✓ Component compatibility confirmed (except Redis)
- ✓ Upgrade path documented

---

## NEXT STEPS

1. **Immediate** (This week):
   - Review this research with team
   - Create implementation tasks for breaking changes
   - Schedule staging environment upgrade

2. **Short-term** (Next 2 weeks):
   - Implement Content-Type header fixes in all services
   - Run full staging test suite
   - Verify component configurations

3. **Medium-term** (Weeks 3-4):
   - Execute production upgrade
   - Monitor for issues
   - Document any gotchas

4. **Long-term** (Post-upgrade):
   - Plan Configuration API implementation (ADR-0004)
   - Plan workload identity federation adoption
   - Begin .NET 10 upgrade process

---

## ADDITIONAL RESOURCES

- **Detailed Research**: See `dapr-upgrade-breaking-changes-1.3-to-1.16.md`
- **Breaking Changes Reference**: https://docs.dapr.io/operations/support/breaking-changes-and-deprecations/
- **Upgrade Guide**: https://docs.dapr.io/operations/hosting/kubernetes/kubernetes-upgrade/
- **Release Notes**: https://blog.dapr.io/posts/2025/09/16/dapr-v1.16-is-now-available/

---

**Assessment Date**: November 9, 2025
**Confidence Level**: HIGH
**Research Completeness**: COMPREHENSIVE (all 13 minor versions reviewed)
