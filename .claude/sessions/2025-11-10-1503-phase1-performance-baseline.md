# Phase 1: Performance Baseline Establishment

**Session Start:** 2025-11-10 15:03 NZDT

---

## Session Overview

This session focuses on implementing Phase 1 of the Testing & Validation Strategy: Baseline Establishment (Performance Baseline). The goal is to establish .NET 6 performance baselines BEFORE any .NET 10 upgrade work begins. These baselines are critical for validating upgrade success and measuring performance improvements.

**Context:** Phase 0 (Prerequisites & Setup) completed successfully on 2025-11-10. All tooling is installed and verified. We are now ready to establish the baseline metrics that will be used to validate the .NET 10 upgrade in Phase 1A.

**Reference:** `plan/testing-validation-strategy.md` - Phase 1: Baseline Establishment (lines 142-200)

---

## Goals

### Primary Goal:

**Establish .NET 6 Performance Baseline** - Capture current performance metrics before any upgrades

This involves:

1. **Set Up Load Testing Infrastructure**
   - Install k6 (Grafana load testing tool)
   - Create load test scripts for key services
   - Configure test parameters (duration, virtual users, ramp-up)

2. **Run Performance Tests on .NET 6 Services**
   - Test OrderService (core REST API)
   - Test MakeLineService (queue management)
   - Test AccountingService (SQL + analytics)
   - Record baseline metrics for each service

3. **Capture Baseline Metrics**
   - P50 Latency (median response time)
   - P95 Latency (95th percentile - SLA threshold)
   - P99 Latency (99th percentile - SLA threshold)
   - Throughput (requests/sec)
   - CPU usage (millicores)
   - Memory usage (MB)
   - Error rate (%)

4. **Store Baseline Results**
   - Save k6 test results to `artifacts/performance/dotnet6-baseline.json`
   - Document baseline metrics for comparison after .NET 10 upgrade
   - Create baseline report for reference

### Secondary Goals:

5. **Validate Current Services Are Running**
   - Ensure all .NET 6 services can run locally with Dapr
   - Verify end-to-end order flow works
   - Confirm services are in known-good state before testing

6. **Document Load Testing Setup**
   - Create k6 test scripts for future use
   - Document test configuration and methodology
   - Provide baseline for Phase 6 (Performance Validation post-upgrade)

---

## Progress

### Update - 2025-11-10 15:03 NZDT

**Summary:** Session started - Phase 1: Performance Baseline Establishment

**Goals Defined:**
- Set up k6 load testing infrastructure
- Run performance tests on .NET 6 services
- Capture and store baseline metrics
- Validate current services are running
- Document load testing setup

**Current Status:**
- Phase 0 complete (all tools installed)
- Ready to begin Phase 1
- Need to validate services are running first
- Then install k6 and create load test scripts

**Next Steps:**
1. Validate .NET 6 services can run locally with Dapr
2. Install k6 load testing tool
3. Create load test scripts for OrderService (initial target)
4. Run baseline performance test
5. Store results in artifacts/performance/

**Reference Documents:**
- `plan/testing-validation-strategy.md` - Phase 1 requirements (lines 142-200)
- `CLAUDE.md` - Local development commands for running services with Dapr
- Expected improvements documented: P95 latency 5-15% faster, throughput 10-20% higher, memory 10-15% lower (post-upgrade)

### Update - 2025-11-10 15:40 NZDT

**Summary:** Phase 0.5 local environment setup completed

**Accomplishments:**
1. ✅ Created Phase 0.5 plan document (`plan/phase-0.5-local-environment-setup.md`)
2. ✅ Created `kind-config.yaml` per ADR-0008 specification
3. ✅ Created Helm chart structure:
   - `charts/reddog/Chart.yaml` (application chart)
   - `charts/infrastructure/Chart.yaml` (infrastructure chart)
   - `values/values-local.yaml` (Redis-based local configuration)
4. ✅ Recovered 6 Dapr component configs from git history
5. ✅ Converted Dapr components to Helm templates (6 components):
   - pubsub.yaml (Redis, not RabbitMQ)
   - statestore-makeline.yaml (Redis)
   - statestore-loyalty.yaml (Redis)
   - secretstore.yaml (Kubernetes secrets)
   - binding-receipt.yaml (local storage)
   - binding-virtualworker.yaml (cron)
6. ✅ Created infrastructure Helm templates:
   - Redis StatefulSet + Service
   - SQL Server StatefulSet + Service + Secret
7. ✅ Created application Helm templates (8 services):
   - OrderService, MakeLineService, LoyaltyService, AccountingService
   - ReceiptGenerationService, VirtualCustomers, VirtualWorker, UI
   - Ingress configuration
8. ✅ Created setup/teardown/status scripts:
   - `scripts/setup-local-dev.sh` (automated cluster setup)
   - `scripts/teardown-local-dev.sh` (cluster cleanup)
   - `scripts/status-local-dev.sh` (health check)
9. ✅ Validated kind cluster creation (successful test)

**Status:**
- Phase 0.5 foundation complete
- kind cluster configuration validated (ports 80/443 mapped correctly)
- Helm charts structured per ADR-0009
- Ready to deploy infrastructure and application

**Blockers Identified:**
- ⚠️ Docker images don't exist yet (need to build all 8 services)
- ⚠️ Bootstrapper strategy needed (SQL database initialization via EF migrations)
- ⚠️ Dapr 1.16 API compatibility needs validation (recovered configs are Dapr 1.5.0)

**Next Steps:**
1. Build Docker images for all 8 services
2. Test infrastructure deployment (Redis + SQL Server)
3. Test application deployment
4. Resolve any Helm template issues
5. Once environment is running, proceed with k6 setup for performance baseline

**Files Created:**
- `kind-config.yaml`
- `plan/phase-0.5-local-environment-setup.md`
- `charts/reddog/Chart.yaml` + 10 templates
- `charts/infrastructure/Chart.yaml` + 3 templates
- `values/values-local.yaml`
- `scripts/setup-local-dev.sh`, `scripts/teardown-local-dev.sh`, `scripts/status-local-dev.sh`
- `manifests/local/branch/` (7 files recovered from git)

---

## Notes

**Critical Reminder from Testing Strategy:**
> "⚠️ CRITICAL: Complete this FIRST in Phase 1.x - before ANY .NET 10 upgrade work begins."

**Expected Performance Improvements (Post .NET 10 Upgrade):**
- P95 latency: 5-15% faster (JIT improvements)
- Throughput: 10-20% higher (HTTP/3, runtime optimizations)
- Memory usage: 10-15% lower (GC improvements)

These baselines will be used in Phase 6 to validate upgrade success.

**Phase 0.5 Decision:**
- Discovered that local development infrastructure was deleted in Phase 0 cleanup
- Created new kind + Helm-based local environment per ADR-0008 and ADR-0009
- Local uses Redis for both state stores and pub/sub (NOT RabbitMQ - cloud only)
- SQL Server runs in Kubernetes (StatefulSet), not docker-compose

---
