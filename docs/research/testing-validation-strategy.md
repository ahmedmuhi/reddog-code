# Testing & Validation Strategy

**Effort Estimate:** 12-16 hours
**Risk Level:** MEDIUM

**Objective:** Establish comprehensive validation strategy for .NET 10 upgrade covering build verification, service integration testing, and deployment readiness.

**Date:** 2025-11-08  
**Research Method:** 3 parallel Haiku agents analyzing build validation, service integration, and deployment readiness

### Overview

Phase 6 focuses on creating a complete testing and validation framework to ensure the .NET 6 → .NET 10 upgrade maintains system reliability, performance, and backward compatibility. This phase addresses the critical gap identified in previous phases: **the project currently has ZERO automated tests**.

**Critical Finding:** The Red Dog Coffee solution has no existing test projects, making validation of the upgrade extremely challenging. This phase recommends immediate creation of a comprehensive test suite.

---

### Agent 1: Build Validation Strategy

#### Executive Summary

Build validation is the first line of defense for detecting .NET 10 incompatibilities. This analysis provides pre-build validation scripts, multi-configuration build verification, automated test execution frameworks, and service startup validation.

**Key Finding:** The project currently lacks:
- global.json for SDK version pinning
- Any test projects (*Tests.csproj patterns not found)
- Health endpoint implementation matching ADR-0005 (/healthz, /livez, /readyz)
- CI validation scripts

#### 1. Pre-Build Validation Checklist

**Purpose:** Verify all prerequisites before attempting compilation

**Script:** `ci/scripts/pre-build-validation.sh`

**Validation Checks:**
1. ✅ .NET SDK 10.x installed (`dotnet --version`)
2. ✅ global.json SDK version = 10.0.x
3. ✅ All .csproj files target `<TargetFramework>net10.0</TargetFramework>`
4. ✅ NuGet packages compatible with .NET 10:
   - Dapr.AspNetCore >= 1.16.0
   - Microsoft.EntityFrameworkCore.SqlServer >= 10.0.0
   - Microsoft.EntityFrameworkCore.Design >= 10.0.0
5. ✅ Dockerfile base images:
   - Build: `mcr.microsoft.com/dotnet/sdk:10.0`
   - Runtime: `mcr.microsoft.com/dotnet/aspnet:10.0`
6. ✅ No vulnerable packages (`dotnet list package --vulnerable`)
7. ✅ No deprecated packages (Microsoft.AspNetCore 2.2.0, old Swashbuckle)
8. ✅ Health endpoints implemented (ADR-0005 compliance)

**Exit Criteria:** All checks pass before proceeding to build

**Effort:** 1 hour to create script

---

#### 2. Build Verification Strategy

**Multi-Configuration Build Commands:**

```bash
# Release build (production)
dotnet build RedDog.sln \
    --configuration Release \
    --no-incremental \
    /p:TreatWarningsAsErrors=true \
    /p:EnforceCodeStyleInBuild=true \
    /p:AnalysisLevel=latest \
    /p:ContinuousIntegrationBuild=true

# Debug build (catch configuration-specific issues)
dotnet build RedDog.sln \
    --configuration Debug \
    --no-incremental \
    /p:TreatWarningsAsErrors=true
```

**Build Artifact Verification:**
- ✅ All service DLLs present in publish output
- ✅ Runtime configuration files reference .NET 10.x (`*.runtimeconfig.json`)
- ✅ Dapr.AspNetCore.dll included in dependencies
- ✅ appsettings.json present (if expected)

**Effort:** 1 hour to create script, 5-10 minutes per build execution

---

#### 3. Automated Test Execution (When Tests Exist)

**Current State:** ❌ **ZERO test projects found in solution**

**Recommended Test Project Structure:**
```
RedDog.OrderService.Tests/          (xUnit, Moq, FluentAssertions)
RedDog.AccountingService.Tests/     (xUnit, Testcontainers for SQL Server)
RedDog.AccountingModel.Tests/       (xUnit, in-memory EF Core)
RedDog.MakeLineService.IntegrationTests/  (Dapr, Redis)
RedDog.LoyaltyService.IntegrationTests/   (Dapr, Redis)
```

**Unit Test Execution Framework:**
```bash
dotnet test RedDog.sln \
    --configuration Release \
    --collect:"XPlat Code Coverage" \
    --results-directory ./test-results \
    --logger "trx" \
    --logger "html" \
    -- \
    --parallel:threads=4
```

**Code Coverage Thresholds:**
- OrderService (API endpoints): **80% minimum**
- AccountingService (SQL-heavy): **75% minimum**
- MakeLineService (state management): **85% minimum**
- LoyaltyService (business logic): **80% minimum**
- AccountingModel (libraries): **85% minimum**

**Coverage Tools:**
- Coverlet (XPlat Code Coverage collector)
- ReportGenerator (HTML reports)
- Codecov (CI integration)

**Effort:** 8-12 hours to create test infrastructure + 3-4 hours per service for unit tests

---

#### 4. Service Startup Verification

**Health Endpoint Validation Script:** `ci/scripts/validate-health-endpoints.sh`

**Services to Validate:**
- OrderService (port 5100)
- AccountingService (port 5700)
- MakeLineService (port 5200)
- LoyaltyService (port 5400)
- ReceiptGenerationService (port 5300)
- VirtualWorker (port 5500)

**Health Checks (ADR-0005 Standard):**
1. **GET /healthz** → 200 OK (startup probe - basic health)
2. **GET /livez** → 200 OK (liveness probe - process alive)
3. **GET /readyz** → 200 OK (readiness probe - dependencies healthy)

**Current State:** ⚠ **Services currently use `/probes/healthz` and `/probes/ready` (non-standard)**

**Required Changes:** Migrate to ADR-0005 paths during Program.cs refactoring

**Dapr Connectivity Verification:**
```bash
# Test Dapr sidecar health
curl http://localhost:3500/v1.0/healthz  # Should return 204 No Content

# Test state store (Redis)
curl -X POST http://localhost:3500/v1.0/state/reddog.state.makeline \
  -H "Content-Type: application/json" \
  -d '[{"key":"test","value":"test"}]'

# Test pub/sub
curl -X POST http://localhost:3500/v1.0/publish/reddog.pubsub/orders \
  -H "Content-Type: application/json" \
  -d '{"test":"message"}'
```

**Effort:** 1 hour to create health validation script

---

#### Build Validation Summary

**Checklist:**
- [ ] Pre-build validation (framework version, SDK, packages, Dockerfiles)
- [ ] Multi-configuration build (Debug + Release with TreatWarningsAsErrors)
- [ ] Build artifact verification (DLLs, runtime config)
- [ ] NuGet vulnerability scan (dotnet list package --vulnerable)
- [ ] Unit tests execution (when test projects created - currently 0 tests)
- [ ] Integration tests execution (Dapr sidecar + service startup)
- [ ] Service startup verification (all services start without errors)
- [ ] Health endpoint validation (/healthz, /livez, /readyz per ADR-0005)
- [ ] Dapr connectivity verification (sidecar health, state store, pub/sub)
- [ ] Code coverage enforcement (80%+ threshold when tests added)

**Total Effort:** 11-16 hours
- Build validation implementation: 3-4 hours
- Test automation setup: 8-12 hours (includes creating test projects)

**Success Criteria:**
- ✅ All 8 projects build without errors in both Debug and Release
- ✅ Zero build warnings with TreatWarningsAsErrors=true
- ✅ All projects publish successfully with complete artifacts
- ✅ All services start successfully with Dapr sidecars
- ✅ Health endpoints return 200 OK within 30 seconds
- ✅ No vulnerable NuGet packages detected

**GO/NO-GO:** ✅ **PROCEED** with .NET 10 upgrade (build validation framework ready for implementation)

---

### Agent 2: Service Integration Verification

#### Executive Summary

Service integration testing validates the distributed microservices communicate correctly via Dapr after the .NET 10 upgrade. This analysis covers pub/sub message flow, state store operations, service-to-service invocation, logging/telemetry, backward compatibility, and runtime behavior.

**Key Finding:** Current CI pipelines have **no integration tests** and **no test execution**. All validation will be manual smoke testing unless comprehensive integration tests are created.

#### 1. Service-to-Service Integration Tests

**Architecture:** 
- **Publisher:** OrderService publishes `OrderSummary` to `orders` topic (Dapr pub/sub)
- **Subscribers:** 
  - MakeLineService (adds to queue in Redis state)
  - LoyaltyService (updates loyalty points in Redis state)
  - AccountingService (stores in SQL Server)
  - ReceiptGenerationService (generates receipt via output binding)

**End-to-End Message Flow Test:**

**Test Scenario:**
1. Submit order via OrderService POST /order
2. Verify MakeLineService received order (check Redis state `reddog.state.makeline`)
3. Verify LoyaltyService updated points (check Redis state `reddog.state.loyalty`)
4. Verify AccountingService stored order (query SQL Server `Orders` table)
5. Verify ReceiptGenerationService created receipt (check blob storage binding)

**Expected Propagation Time:** < 5 seconds (RabbitMQ pub/sub + Dapr overhead)

**Error Scenarios to Test:**
- ❌ Dapr sidecar down → Should fail gracefully with 503 Service Unavailable
- ❌ RabbitMQ broker down → Should retry and log errors, return 500
- ❌ Redis state store unavailable → Subscribers fail gracefully, log errors
- ❌ SQL Server unavailable → Accounting fails, other subscribers still process

**Validation Points:**
- Message published to `orders` topic via RabbitMQ
- All 4 subscribers receive message (100% delivery rate)
- State persisted in Redis (ETag concurrency control working)
- Data persisted in SQL Server (foreign keys intact)
- Receipt written to blob storage (binding invoked successfully)

**Effort:** 4-6 hours to create end-to-end test

---

**State Store Operations Test:**

**CRUD Operations to Test:**
- **CREATE:** Save new order to Redis via Dapr (`POST /v1.0/state/reddog.state.makeline`)
- **READ:** Retrieve order from Redis (`GET /v1.0/state/reddog.state.makeline/{key}`)
- **UPDATE:** Modify existing order (save with same key)
- **DELETE:** Remove order from Redis (`DELETE /v1.0/state/reddog.state.makeline/{key}`)

**Concurrency Control Test:**
- Use ETags for FirstWrite mode (prevent conflicting updates)
- Expected behavior: 409 Conflict when ETag mismatch detected
- Validate optimistic concurrency pattern works correctly

**State Stores to Test:**
- `reddog.state.makeline` (Redis) - MakeLineService order queue
- `reddog.state.loyalty` (Redis) - LoyaltyService customer points

**Effort:** 3-4 hours to create state store test suite

---

**Service Invocation Test:**

**Dapr HTTP API Pattern:**
```
http://localhost:3500/v1.0/invoke/{app-id}/method/{method-name}
```

**Test Scenario:**
- OrderService → MakeLineService via Dapr (`invoke/make-line-service/method/orders/{storeId}`)
- Expected: Array of orders in MakeLine queue
- HTTP Status: 200 OK
- Response format: JSON array

**Validation:**
- mTLS encryption enabled (Dapr automatically encrypts)
- Service discovery working (app-id resolves to Kubernetes service)
- Error handling (invoke non-existent service → 404/500)

**Effort:** 3-4 hours to create service invocation tests

---

#### 2. Logging & Telemetry Verification

**Structured Logging Requirements:**

**Expected Log Entry (.NET with OpenTelemetry):**
```json
{
  "@t": "2025-11-08T10:30:45.1234567Z",
  "@mt": "Customer Order received: {CustomerOrder}",
  "@l": "Information",
  "serviceName": "OrderService",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "spanId": "00f067aa0ba902b7",
  "orderId": "123e4567-e89b-12d3-a456-426614174000",
  "storeId": "redmond"
}
```

**Validation Checks:**
- ✅ Timestamp in ISO 8601 UTC format
- ✅ TraceId present (OpenTelemetry trace context)
- ✅ SpanId present (distributed tracing)
- ✅ Log levels: Information, Warning, Error, Debug
- ✅ Contextual properties: OrderId, CustomerId, ServiceName
- ✅ JSON format (recommended for structured query in Grafana/Splunk)

**Current State:** ⚠ **Logs may not have structured trace context yet** (OpenTelemetry instrumentation needed)

**Effort:** 4-6 hours to validate logging + add structured logging if missing

---

**Distributed Tracing Verification:**

**Trace Backend:** Jaeger (OpenTelemetry OTLP protocol)

**Expected Trace Spans:**
1. OrderService (POST /order) - Root span
2. Dapr pub/sub publish - Middleware span
3. MakeLineService (subscribe handler) - Child span
4. LoyaltyService (subscribe handler) - Child span
5. AccountingService (subscribe handler) - Child span
6. ReceiptGenerationService (subscribe handler) - Child span

**Total Spans:** 6+ (one per service involved in order flow)

**Dapr Tracing Configuration:**
```yaml
# Dapr Configuration (manifests/branch/base/components/reddog.config.yaml)
apiVersion: dapr.io/v1alpha1
kind: Configuration
metadata:
  name: reddog.config
spec:
  tracing:
    samplingRate: "1"  # 100% sampling for testing
    otel:
      endpointAddress: "http://otel-collector.observability.svc.cluster.local:4317"
      isSecure: false
      protocol: grpc
```

**Validation:**
- Query Jaeger for trace by TraceId
- Verify all 6 services appear in trace
- Verify span duration (< 500ms per span for p95)

**Effort:** 4-6 hours to setup Jaeger + validate traces

---

**Metrics Collection Verification:**

**Prometheus Metrics to Export:**
- `http_server_requests_total{service="order-service", status="200"}` - HTTP request count
- `dapr_http_server_request_count{app_id="order-service"}` - Dapr invocation count
- `process_resident_memory_bytes{service="order-service"}` - Memory usage
- `dotnet_gc_collections_count{generation="gen2"}` - .NET GC metrics

**Metrics Exporter:** OpenTelemetry Prometheus Exporter (port 8889)

**Recommended Grafana Dashboards:**
- ASP.NET Core Dashboard (Grafana ID: 10915)
- Dapr Dashboard (Grafana ID: 14214)
- Redis Dashboard (Grafana ID: 763)
- RabbitMQ Dashboard (Grafana ID: 10991)

**Effort:** 4-4 hours to setup metrics collection

---

#### 3. Backward Compatibility Verification

**API Contract Validation:**

**Method:** Compare OpenAPI schemas before/after upgrade

**Tools:**
- openapi-diff (npm package)
- Manual jq comparison if openapi-diff unavailable

**Validation Checks:**
- ❌ No removed endpoints (breaking change)
- ❌ No required fields added to request schemas (breaking change)
- ❌ No changed HTTP status codes for existing scenarios (breaking change)
- ✅ OK: Adding new optional fields
- ✅ OK: Adding new endpoints
- ✅ OK: Deprecating (but not removing) endpoints

**Test Scenario:**
1. Export .NET 6 OpenAPI schema (`/swagger/v1/swagger.json`)
2. Export .NET 10 OpenAPI schema (`/openapi/v1.json`)
3. Compare endpoint counts, paths, request/response schemas
4. Test actual API responses (GET /product, POST /order)

**Expected Result:** 100% backward compatible (no breaking changes)

**Effort:** 4-5 hours to create comparison scripts

---

**Message Schema Validation:**

**Message Type:** OrderSummary (published to `orders` topic)

**Expected Schema:**
```json
{
  "orderId": "guid",
  "storeId": "string",
  "firstName": "string",
  "lastName": "string",
  "loyaltyId": "string",
  "orderDate": "ISO 8601 DateTime",
  "orderTotal": "decimal",
  "orderItems": [
    {
      "productId": "int",
      "productName": "string",
      "quantity": "int",
      "unitCost": "decimal",
      "unitPrice": "decimal",
      "imageUrl": "string"
    }
  ]
}
```

**Validation:**
- .NET 6 OrderService publishes message → Capture from RabbitMQ
- .NET 10 OrderService publishes message → Capture from RabbitMQ
- Compare field names and types (should be identical)
- Verify all subscribers can deserialize .NET 10 messages

**Effort:** 4-5 hours to create message schema validation

---

**Database Schema Validation:**

**EF Core Migration Compatibility:**

**Validation Steps:**
1. Backup .NET 6 database schema
2. Run .NET 10 EF Core migrations (`dotnet ef database update`)
3. Compare schema versions (check `__EFMigrationsHistory` table)
4. Verify data integrity (row counts match before/after)
5. Check foreign key constraints intact

**EF Core 10 Changes to Validate:**
- JSON columns (new EF Core 10 feature - may alter schema)
- DateOnly/TimeOnly types (if used in models)
- Compiled models (may have different behavior)

**Rollback Test:** Ensure migrations can be reverted without data loss

**Effort:** 4-6 hours to create database compatibility tests

---

#### 4. Runtime Behavior Validation

**Performance Baseline Comparison:**

**Load Testing Tool:** k6 (Grafana load testing tool)

**Test Configuration:**
- Duration: 60 seconds
- Virtual Users: 50
- Ramp-up: 30s to 10 VUs → 1min at 50 VUs → 30s ramp-down

**Expected .NET 10 Performance:**
- **P50 Latency:** < 200ms
- **P95 Latency:** < 500ms (SLA threshold)
- **P99 Latency:** < 1000ms (SLA threshold)
- **Throughput:** 80-100 req/sec
- **Error Rate:** < 1%

**Expected Improvements vs .NET 6:**
- P95 latency: 5-15% faster (JIT improvements)
- Throughput: 10-20% higher (HTTP/3, runtime optimizations)
- Memory usage: 10-15% lower (GC improvements)

**Acceptance Criteria:** < 10% performance degradation (if any regression observed)

**Effort:** 3-4 hours to create load test scripts + 2 hours for baseline measurement

---

**Resource Usage Monitoring:**

**Metrics to Track:**
- CPU usage (millicores) under load
- Memory usage (MB) under load
- Pod restart count (should be 0)
- Network I/O (Rx/Tx bytes)

**Monitoring Method:** `kubectl top pod` during load test

**Expected Resource Usage (.NET 10):**
- CPU: 200-400m per pod (under 50 VU load)
- Memory: 150-250MB per pod (stable, no leaks)
- Network: Proportional to request rate

**Comparison:** .NET 10 should have similar or better resource usage than .NET 6

**Effort:** 2-3 hours to create resource monitoring scripts

---

#### Integration Verification Summary

**Checklist:**
- [ ] Pub/sub message flow (OrderService → 4 subscribers)
- [ ] State store operations (Redis CRUD + ETag concurrency)
- [ ] Service-to-service invocation (Dapr HTTP API with mTLS)
- [ ] Structured logging (JSON format with TraceId/SpanId)
- [ ] Distributed tracing (Jaeger traces span 5+ services)
- [ ] Metrics collection (Prometheus scrapes OpenTelemetry exporter)
- [ ] API contract compatibility (OpenAPI schemas match)
- [ ] Message schema compatibility (OrderSummary format unchanged)
- [ ] Database schema compatibility (EF Core migrations preserve data)
- [ ] Performance baseline (P95 < 10% degradation)
- [ ] Resource usage (CPU/memory within acceptable range)

**Total Effort:** 48-68 hours (6-8.5 developer-days)
- Integration test creation: 16-24 hours
- Telemetry verification: 12-16 hours
- Backward compatibility testing: 12-16 hours
- Performance baseline testing: 8-12 hours

**Success Criteria:**
- ✅ All pub/sub messages delivered (100% success rate)
- ✅ State operations succeed with concurrency control
- ✅ Service invocation completes with mTLS
- ✅ Logs contain TraceId/SpanId (100% of entries)
- ✅ Distributed traces span 5+ services
- ✅ API responses match .NET 6 baseline
- ✅ OrderSummary messages consumed by all subscribers
- ✅ Database migrations complete without data loss
- ✅ P95 latency degradation < 10%
- ✅ CPU/memory increase < 15%

**GO/NO-GO:** ✅ **PROCEED** if all criteria met; ⚠ **DEFER** if performance degrades > 10% (profile and optimize first)

---

### Agent 3: Deployment Readiness Check

#### Executive Summary

Deployment readiness ensures the .NET 10 upgrade can be safely deployed to UAT and production environments. This analysis covers pre-deployment validation, UAT deployment strategies (blue-green, canary), smoke testing, and production rollout checklists.

**Key Finding:** Current deployment process lacks:
- Blue-green/canary deployment strategies (potential downtime)
- Automated smoke tests
- Performance baseline for .NET 6 (no comparison data)
- Comprehensive rollback plan
- Security scanning automation (Trivy)

#### 1. Pre-Deployment Validation

**Container Image Verification:**

**Registry:** GitHub Container Registry (ghcr.io/azure/reddog-retail-demo)

**Images to Verify:**
1. reddog-retail-order-service:{git-sha}
2. reddog-retail-accounting-service:{git-sha}
3. reddog-retail-makeline-service:{git-sha}
4. reddog-retail-loyalty-service:{git-sha}
5. reddog-retail-receipt-service:{git-sha}
6. reddog-retail-virtual-worker:{git-sha}
7. reddog-retail-virtual-customers:{git-sha}
8. reddog-ui:{git-sha}

**Verification Checks:**
- ✅ Image exists in registry (`docker pull` succeeds)
- ✅ Image metadata correct (created timestamp, size)
- ✅ Image tagged with git SHA (traceability)
- ✅ Image size reasonable (< 500MB for services, < 200MB for UI)

**Effort:** 2 hours to create verification script

---

**Container Security Scanning:**

**Tool:** Trivy (Aqua Security vulnerability scanner)

**Scan Severity:** HIGH, CRITICAL

**Acceptance Criteria:**
- ❌ **NO-GO if:** Any CRITICAL vulnerabilities (blocking issue)
- ⚠ **DEFER if:** More than 5 HIGH vulnerabilities (review required)
- ✅ **GO if:** 0 CRITICAL, < 5 HIGH vulnerabilities

**Scan Output:** JSON format + detailed report per service

**Effort:** 2 hours to automate Trivy scanning

---

**Kubernetes Manifest Validation:**

**Tool:** kubectl dry-run + kustomize build

**Manifests to Validate:**
- Deployments (all 8 services)
- Services (ClusterIP, LoadBalancer)
- Dapr Components (7 components: pubsub, state stores, bindings, config, secret store)
- ConfigMaps (if any)
- Secrets (ensure no hardcoded values)

**Health Probe Validation (ADR-0005):**
- `startupProbe.httpGet.path` = `/healthz`
- `livenessProbe.httpGet.path` = `/livez`
- `readinessProbe.httpGet.path` = `/readyz`

**Current State:** ⚠ **Services currently use `/probes/healthz` and `/probes/ready`** (non-standard, requires update)

**Effort:** 1 hour to create manifest validation script

---

**Dapr Component Validation:**

**Components to Validate:**
1. `reddog.pubsub.yaml` (type: pubsub.redis)
2. `reddog.state.makeline.yaml` (type: state.redis)
3. `reddog.state.loyalty.yaml` (type: state.redis)
4. `reddog.secretstore.yaml` (type: secretstores.kubernetes)
5. `reddog.config.yaml` (type: configuration.redis)
6. `reddog.binding.receipt.yaml` (type: bindings.azure.blobstorage)
7. `reddog.binding.virtualworker.yaml` (type: bindings.http)

**Dapr Version Check:** Ensure Dapr runtime >= 1.16.0 (required for .NET 10 Dapr SDK)

**Effort:** 1 hour to create component validation script

---

#### 2. UAT Deployment Strategies

**Blue-Green Deployment (Zero Downtime):**

**Architecture:**
- **Blue:** Current production (.NET 6) - 2 replicas, service selector: `version=blue`
- **Green:** New version (.NET 10) - 2 replicas, service selector: `version=green`
- **Cutover:** Switch service selector from `blue` to `green` (atomic operation)

**Deployment Steps:**
1. Deploy green (new .NET 10 version) alongside blue
2. Wait for green to be ready (all health probes passing)
3. Smoke test green service (separate service: `order-service-green`)
4. If smoke tests pass → Switch service selector to green
5. Monitor for 10 minutes → If stable, scale down blue

**Rollback:** Switch service selector back to blue (< 2 minutes)

**Kubernetes Manifest:** Separate Deployment objects for blue and green

**Effort:** 3 hours to create blue-green manifests

---

**Canary Deployment (Gradual Rollout):**

**Architecture:**
- **Stable:** Current production (.NET 6) - 9 replicas
- **Canary:** New version (.NET 10) - 1 replica (10% of traffic)
- **Service:** Routes traffic to both stable and canary (label selector: `app=order-service`)

**Rollout Steps:**
1. Deploy canary (1 replica = 10% traffic)
2. Monitor error rate, latency, resource usage for 1 hour
3. If metrics acceptable → Increase canary to 3 replicas (25% traffic)
4. Monitor for 1 hour → Increase to 5 replicas (50% traffic)
5. Monitor for 1 hour → Replace all stable replicas with canary

**Rollback:** Scale canary to 0 (traffic returns to stable)

**Effort:** 2 hours to create canary manifests

---

**Database Migration Strategy:**

**Tool:** EF Core migrations via Kubernetes Job

**Migration Steps:**
1. **Backup database** (SQL Server backup to `/var/opt/mssql/backup/`)
2. **Run migration Job** (container with `dotnet ef database update` command)
3. **Verify migration** (check `__EFMigrationsHistory` table)
4. **Test rollback** (ensure migrations can be reverted if needed)

**Migration Job Characteristics:**
- Runs once (restartPolicy: Never)
- Dapr sidecar enabled (for configuration/secret access)
- Timeout: 5 minutes (migrations should be fast)
- Backoff limit: 3 attempts

**Effort:** 2 hours to create migration Job manifest

---

**Rollback Plan:**

**Automated Rollback Script:** `rollback.sh`

**Rollback Strategies:**
1. **Blue-Green:** Switch service selector back to blue (< 2 minutes)
2. **Deployment History:** `kubectl rollout undo deployment/order-service --to-revision={previous}` (< 5 minutes)

**Rollback Triggers:**
- Error rate > 5% for 5 consecutive minutes
- P95 latency > 1000ms for 5 consecutive minutes
- Critical functionality broken (order placement fails)
- Security incident detected

**Validation After Rollback:**
- All pods running with previous version
- Health endpoints returning 200 OK
- Error rate < 1%
- No alerts firing

**Effort:** 1 hour to create rollback script

---

#### 3. Smoke Testing Scenarios

**Health Endpoint Smoke Tests:**

**Services to Test:**
- OrderService (port 5100)
- AccountingService (port 5700)
- MakeLineService (port 5200)
- LoyaltyService (port 5400)
- ReceiptGenerationService (port 5300)
- VirtualWorker (port 5500)

**Health Checks:**
- GET /healthz → 200 OK (startup probe)
- GET /livez → 200 OK (liveness probe)
- GET /readyz → 200 OK (readiness probe)

**Expected Completion Time:** < 2 minutes for all services

**Effort:** 1 hour to create health endpoint smoke tests

---

**Critical User Flow Tests:**

**Test 1: Order Placement**
1. POST /order to OrderService
2. Verify response contains `orderId`
3. Expected: HTTP 200 OK, orderId is valid GUID

**Test 2: Order Processing**
1. Wait 5 seconds (pub/sub propagation)
2. GET /order from MakeLineService
3. Verify order appears in queue
4. Expected: Order count > 0

**Test 3: Dapr Pub/Sub Validation**
1. Check OrderService logs for "published" keyword
2. Expected: Pub/sub publish log entry found

**Expected Completion Time:** < 1 minute per flow

**Effort:** 3 hours to create user flow smoke tests

---

**Service Connectivity Tests:**

**Test 1: SQL Server Connectivity**
- kubectl exec into SQL Server pod
- Run `sqlcmd -S localhost -U sa -Q "SELECT 1"`
- Expected: Connection successful

**Test 2: Redis Connectivity**
- kubectl exec into Redis pod
- Run `redis-cli ping`
- Expected: PONG response

**Test 3: Dapr Sidecar Health**
- kubectl exec into OrderService pod (daprd container)
- Run `wget -q -O- http://localhost:3500/v1.0/healthz`
- Expected: `true` response

**Test 4: Dapr Component Connectivity**
- kubectl get components -n reddog-retail
- Expected: 7 components loaded

**Expected Completion Time:** < 2 minutes for all checks

**Effort:** 2 hours to create connectivity tests

---

**UI Smoke Tests:**

**Test 1: UI Accessibility**
- GET http://ui-service:8080/
- Expected: HTTP 200 OK

**Test 2: UI Content Validation**
- Verify page contains "Red Dog" text
- Expected: Content loads correctly

**Test 3: Static Assets**
- GET /css/app.css → 200 OK
- GET /js/app.js → 200 OK
- Expected: CSS and JavaScript load

**Expected Completion Time:** < 1 minute

**Effort:** 1 hour to create UI smoke tests

---

#### 4. Production Rollout Readiness

**Performance Testing Results:**

**Load Testing Configuration:**
- Tool: k6
- Duration: 60 seconds
- Virtual Users: 50
- Ramp-up: 30s → 1min sustained → 30s ramp-down

**Expected Results:**
- Requests/sec: 80-100 req/sec
- P50 Latency: < 200ms
- P95 Latency: < 500ms (SLA threshold)
- P99 Latency: < 1000ms (SLA threshold)
- Error Rate: < 1%
- CPU Usage: 200-400m per pod
- Memory Usage: 150-250MB per pod

**Acceptance Criteria:** No regression > 10% vs .NET 6 baseline

**Effort:** 4 hours to run performance tests

---

**Security Audit Results:**

**Audit Components:**
1. Container vulnerability scan (Trivy) → 0 CRITICAL, < 5 HIGH
2. Secret management audit → No hardcoded secrets
3. RBAC validation → Role bindings configured
4. Network policy validation → Network policies defined (optional)
5. Dapr mTLS validation → mTLS enabled (service-to-service encryption)

**GO/NO-GO:**
- ❌ **NO-GO if:** Any CRITICAL vulnerabilities or hardcoded secrets
- ✅ **GO if:** All security checks pass

**Effort:** 6 hours to run complete security audit

---

**Disaster Recovery Testing:**

**Test 1: Database Backup**
- Create SQL Server backup to `/var/opt/mssql/backup/`
- Backup file: `RedDog_DR_Test_{timestamp}.bak`
- Expected: Backup file created successfully

**Test 2: Simulate Disaster**
- Drop test table from database
- Expected: Table no longer exists

**Test 3: Restore from Backup**
- Run SQL Server RESTORE command
- Expected: Database restored from backup

**Test 4: Verify Restore**
- Query database for table count
- Expected: All tables present after restore

**Acceptance Criteria:** Zero data loss during restore

**Effort:** 2 hours to create disaster recovery tests

---

**Runbook Documentation:**

**Deployment Runbook Contents:**
1. Pre-deployment checklist (15 min)
2. Database migration steps (5 min)
3. Blue-green deployment steps (10 min)
4. Traffic cutover procedure (2 min)
5. Post-deployment validation (15 min)
6. Cleanup steps (5 min)

**Rollback Runbook Contents:**
1. Rollback triggers (when to rollback)
2. Rollback steps (blue-green or deployment history)
3. Rollback verification (health checks, smoke tests)
4. Post-rollback monitoring (5 minutes)

**Troubleshooting Guide Contents:**
- Pods crash after deployment → Check logs
- Database migration fails → Rollback migration, restore backup
- High latency → Check Dapr sidecar health, verify Redis/SQL connectivity

**Effort:** 4 hours to create runbooks

---

#### Deployment Readiness Summary

**Checklist:**
- [ ] All 8 container images built and pushed to GHCR
- [ ] Security scan completed (0 CRITICAL, < 5 HIGH)
- [ ] Kubernetes manifests validated (kubectl dry-run passed)
- [ ] Dapr components validated (all 7 components configured)
- [ ] Health probe paths verified (/healthz, /livez, /readyz per ADR-0005)
- [ ] Database backup created
- [ ] Blue-green deployment manifests ready
- [ ] Rollback plan tested in staging
- [ ] Smoke tests created (health, user flows, connectivity, UI)
- [ ] Performance baseline established (k6 load test)
- [ ] Security audit complete (no blockers)
- [ ] Disaster recovery tested (backup/restore successful)
- [ ] Runbooks documented (deployment, rollback, troubleshooting)
- [ ] Team trained on deployment procedures
- [ ] Monitoring dashboards configured (Grafana, Jaeger)

**Total Effort:** 28-40 hours (3.5-5 days for 1 developer, or 1-2 days for 2-3 developers in parallel)
- UAT deployment setup: 8-12 hours
- Smoke test creation: 6-8 hours
- Performance testing: 4-6 hours
- Security audit: 6-8 hours
- Runbook documentation: 4-6 hours

**Success Criteria:**
- ✅ UAT deployment completes without errors
- ✅ All smoke tests pass (100% success rate)
- ✅ Performance meets SLA (p95 < 500ms, p99 < 1s)
- ✅ No HIGH/CRITICAL security vulnerabilities
- ✅ Rollback tested successfully (< 5 minutes)
- ✅ Team trained on deployment procedures

**GO/NO-GO Decision:**

**GO if:**
- ✅ All deployment readiness criteria met
- ✅ UAT smoke tests 100% pass rate
- ✅ Performance meets SLA (no regression > 10%)
- ✅ Security audit complete (0 CRITICAL, < 5 HIGH)
- ✅ Rollback tested successfully
- ✅ Team trained and confident

**NO-GO if:**
- ❌ Critical smoke tests failing (order placement, pub/sub, database)
- ❌ Performance regression > 20%
- ❌ Unresolved HIGH/CRITICAL vulnerabilities
- ❌ Rollback plan not tested
- ❌ Team not trained

**DEFER if:**
- ⚠ Performance regression 10-20% (investigate, optimize)
- ⚠ 5-10 HIGH vulnerabilities (assess risk, remediate)
- ⚠ < 95% smoke test pass rate (fix failing tests)

**Recommendation:** ✅ **PROCEED** with .NET 10 upgrade if all GO criteria met

---

# Testing & Validation Strategy

Comprehensive build validation, service integration, performance, and observability plans now live in `docs/research/testing-validation-strategy.md`. Refer there when defining test suites or validation steps for the .NET 10 rollout.
# Phase 6 Summary

**Total Phase 6 Effort:** 87-124 hours (11-15.5 developer-days)
- Build Validation Strategy: 11-16 hours
- Service Integration Verification: 48-68 hours
- Deployment Readiness Check: 28-40 hours

**Critical Gaps Identified:**
1. ❌ **ZERO test projects exist** - Most critical gap, blocks automated validation
2. ⚠ **Inconsistent health endpoints** - Current `/probes/*` paths don't match ADR-0005 standard
3. ⚠ **No performance baseline** - No .NET 6 metrics to compare against
4. ⚠ **No blue-green/canary** - Current deployments use rolling updates (potential downtime)
5. ⚠ **Manual smoke testing** - No automated smoke tests for critical flows

**Recommended Actions Before Upgrade:**
1. **Create test projects** (minimum: OrderService.Tests, AccountingService.Tests, AccountingModel.Tests)
2. **Update health endpoints** to ADR-0005 paths (/healthz, /livez, /readyz) during Program.cs refactoring
3. **Establish .NET 6 performance baseline** (run k6 load test, record p95/p99 latency)
4. **Create blue-green deployment manifests** (ensure zero-downtime production rollout)
5. **Implement automated smoke tests** (health endpoints, order placement flow, connectivity)
6. **Set up monitoring** (Grafana dashboards for ASP.NET Core, Dapr, Redis, RabbitMQ)

**Testing Strategy Priority:**
1. **Phase 6A:** Build validation + health endpoint smoke tests (1-2 days) - **BLOCKING**
2. **Phase 6B:** Integration tests (Dapr pub/sub, state stores, service invocation) - **HIGH**
3. **Phase 6C:** Performance baseline + load testing - **HIGH**
4. **Phase 6D:** Blue-green deployment + rollback testing - **CRITICAL for production**
5. **Phase 6E:** Security scanning + disaster recovery - **REQUIRED before production**

**GO/NO-GO Decision:** ✅ **PROCEED TO PHASE 7** (Implementation Planning)

All research phases (Phase 1-6) complete. Ready to begin implementation planning or detailed risk assessment (Phase 7).

---

**End of Phase 6 Analysis**
