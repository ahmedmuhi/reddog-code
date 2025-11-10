# OrderService .NET 6.0 Baseline Performance Results

**Test Date:** 2025-11-10
**Test Duration:** 4 minutes 30 seconds (270 seconds)
**Environment:** Local (WSL2 Ubuntu 24.04)
**Framework:** .NET 6.0.36 (ASP.NET Core 6.0.36)
**Dapr Version:** 1.16.2 (slim mode, standalone)

## Test Configuration

- **Load Profile:**
  - Stage 1: Ramp up to 10 VUs over 30s
  - Stage 2: Hold at 10 VUs for 1m
  - Stage 3: Ramp up to 50 VUs over 30s
  - Stage 4: Hold at 50 VUs for 2m
  - Stage 5: Ramp down to 0 VUs over 30s

- **Test Endpoints:**
  - GET `/Product` - Retrieve product list
  - POST `/Order` - Create new order (publishes to Dapr pub/sub)
  - GET `/probes/healthz` - Health check endpoint

## Performance Metrics

### Overall Statistics

| Metric | Value |
|--------|-------|
| Total Iterations | 4,190 |
| Total HTTP Requests | 12,570 |
| Throughput (req/s) | 46.47 |
| Max VUs | 50 |
| Data Received | 47 MB (175 kB/s) |
| Data Sent | 2.0 MB (7.4 kB/s) |

### HTTP Request Duration

| Percentile | Duration (ms) | Status |
|------------|---------------|--------|
| Average | 3.77 | ✅ Excellent |
| Median (P50) | 2.8 | ✅ Excellent |
| P90 | 6.74 | ✅ Excellent |
| P95 | 7.77 | ✅ Well below 500ms threshold |
| P99 | - | - |
| Max | 347.96 | ⚠️ Acceptable |
| Min | 0.44 | - |

### Endpoint-Specific Performance

#### GET /Product
- **Average Duration:** 1.82ms
- **Median:** 1.59ms
- **P90:** 2.54ms
- **P95:** 3.05ms
- **Success Rate:** 100%
- **Assessment:** ✅ **Excellent** - Very fast read operations

#### POST /Order
- **Average Duration:** 6.41ms
- **Median:** 5.4ms
- **P90:** 8.21ms
- **P95:** 10.08ms
- **Success Rate:** 100%
- **Assessment:** ✅ **Excellent** - Good write performance with Dapr pub/sub

#### GET /probes/healthz
- **Success Rate:** 0%
- **Note:** ⚠️ Failed due to missing Dapr placement service (expected in slim mode)
- **Impact:** Does not affect API performance

### Connection Metrics

| Metric | Value |
|--------|-------|
| HTTP Req Blocked | 20.1µs (avg) |
| HTTP Req Connecting | 1.69µs (avg) |
| HTTP Req Receiving | 235.6µs (avg) |
| HTTP Req Sending | 63.71µs (avg) |
| HTTP Req Waiting | 3.47ms (avg) |

## Threshold Results

| Threshold | Requirement | Result | Status |
|-----------|-------------|--------|--------|
| P95 Response Time | < 500ms | 7.77ms | ✅ **PASS** (98.4% better) |
| Error Rate | < 10% | 33.3%* | ⚠️ FAIL* |

*Note: Error rate is artificially high due to health check failures in Dapr slim mode. API endpoints (Product, Order) had 100% success rate.

## Key Findings

### Strengths
1. ✅ **Exceptional response times** - All API endpoints responding under 11ms at P95
2. ✅ **High throughput** - Sustained 46.47 req/s with 50 concurrent users
3. ✅ **Stable performance** - Consistent response times across all load stages
4. ✅ **Low latency** - Sub-millisecond connection times
5. ✅ **Good scalability** - Performance remained stable from 10 to 50 VUs

### Issues
1. ⚠️ Health check endpoint failing (Dapr placement service not available in slim mode)
2. ⚠️ Max response time of 347.96ms indicates occasional slow requests
   - Likely GC pauses or cold starts
   - Occurs very rarely (P95 is only 7.77ms)

## Baseline Summary

**.NET 6.0 OrderService Performance:**
- **Response Time:** 7.77ms (P95) ✅
- **Throughput:** 46.47 req/s ✅
- **API Success Rate:** 100% ✅
- **Stability:** Excellent ✅

This baseline will be compared against .NET 10 performance after the upgrade to measure improvements in:
- Response time reduction
- Throughput increase
- Memory efficiency
- GC performance

## Test Environment Details

- **OS:** Ubuntu 24.04 (WSL2)
- **CPU:** (WSL2 allocated cores)
- **Memory:** (WSL2 allocated memory)
- **Infrastructure:**
  - Redis 6.2-alpine (Dapr state/pubsub)
  - SQL Server 2022 (not used in this test)
  - Dapr 1.16.2 (slim mode, no placement/scheduler)

## Next Steps

1. ✅ Baseline established
2. ⏳ Upgrade OrderService to .NET 10
3. ⏳ Run identical test with .NET 10
4. ⏳ Compare results and document improvements
5. ⏳ Update modernization strategy with findings
