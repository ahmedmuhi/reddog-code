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

---

## Notes

**Critical Reminder from Testing Strategy:**
> "⚠️ CRITICAL: Complete this FIRST in Phase 1.x - before ANY .NET 10 upgrade work begins."

**Expected Performance Improvements (Post .NET 10 Upgrade):**
- P95 latency: 5-15% faster (JIT improvements)
- Throughput: 10-20% higher (HTTP/3, runtime optimizations)
- Memory usage: 10-15% lower (GC improvements)

These baselines will be used in Phase 6 to validate upgrade success.

---
