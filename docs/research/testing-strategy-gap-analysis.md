# Testing Strategy Gap Analysis

**Analysis Date:** 2025-11-08
**Purpose:** Identify gaps between `plan/testing-validation-strategy.md` and `docs/research/dotnet-upgrade-analysis.md`
**Goal:** Ensure testing strategy covers all known risks, breaking changes, and gotchas from upgrade analysis

---

## Executive Summary

This analysis compares the testing and validation strategy against the comprehensive .NET upgrade analysis to identify missing test coverage, unreferenced tools, and knowledge gaps that could lead to upgrade failures.

**Critical Finding:** The testing strategy is comprehensive in structure but **lacks integration with specific breaking changes, gotchas, and critical test scenarios** documented in the upgrade analysis. Without explicit references, there's a risk of losing valuable upgrade research knowledge.

###Total Gaps Identified: **15 gaps**
- **Critical (Blocks Upgrade):** 5 gaps
- **High (Major Risk):** 6 gaps
- **Medium (Should Fix):** 4 gaps

---

## Gap 1: Missing Critical Test Scenarios from Upgrade Analysis

**Severity:** CRITICAL
**Impacts:** Regression testing coverage

**What's Missing:**

The upgrade analysis (Section 3.6) documents **6 specific test scenarios** with detailed validation points:

1. **End-to-End Order Flow** (12 hours) - dotnet-upgrade-analysis.md:1773-1806
2. **State Store Concurrency** (4 hours) - dotnet-upgrade-analysis.md:1809-1828
3. **Database Schema Validation** (2 hours) - dotnet-upgrade-analysis.md:1831-1850
4. **Service Invocation** (3 hours) - dotnet-upgrade-analysis.md:1853-1864
5. **API Backward Compatibility** (2 hours) - dotnet-upgrade-analysis.md:1867-1884
6. **Health Endpoints** (3 hours) - dotnet-upgrade-analysis.md:1887

**Current State in Testing Strategy:**

testing-validation-strategy.md mentions these concepts generally but:
- No specific reference to Section 3.6 test scenarios
- No effort estimates (26 hours total documented in upgrade analysis)
- No validation points mapped from upgrade analysis
- No priority matrix (CRITICAL/HIGH/MEDIUM) from upgrade analysis Section 3.7

**Recommendation:**

Add to testing-validation-strategy.md Agent 2 section:

```markdown
### 2.X Critical Test Scenarios (Per .NET Upgrade Analysis)

Reference: `docs/research/dotnet-upgrade-analysis.md` Section 3.6

**Test Priority Matrix (from dotnet-upgrade-analysis.md:1887):**

| Scenario | Priority | Effort | Reference |
|----------|----------|--------|-----------|
| E2E Order Flow | CRITICAL | 12h | dotnet-upgrade-analysis.md:1773-1806 |
| State Store Concurrency | HIGH | 4h | dotnet-upgrade-analysis.md:1809-1828 |
| Database Schema Validation | HIGH | 2h | dotnet-upgrade-analysis.md:1831-1850 |
| Service Invocation | MEDIUM | 3h | dotnet-upgrade-analysis.md:1853-1864 |
| API Backward Compatibility | MEDIUM | 2h | dotnet-upgrade-analysis.md:1867-1884 |
| Health Endpoints | MEDIUM | 3h | dotnet-upgrade-analysis.md:1887 |

**Minimum Viable Testing (16 hours):**
- E2E Order Flow (12 hours)
- Database Schema Validation (2 hours)
- API Backward Compatibility (2 hours)
```

---

## Gap 2: ETag Optimistic Concurrency Not Tested

**Severity:** CRITICAL
**Impacts:** Data integrity in MakeLineService and LoyaltyService

**What's Missing:**

The upgrade analysis documents critical ETag concurrency patterns:

- **MakeLineService:** dotnet-upgrade-analysis.md:1661-1679 (FirstWrite mode with retry loop)
- **LoyaltyService:** dotnet-upgrade-analysis.md:1682-1701 (Points calculation with concurrency)
- **Dapr API Compatibility:** dotnet-upgrade-analysis.md:1118-1142 (ETag pattern verified compatible)

**Specific Code Pattern:**
```csharp
StateOptions _stateOptions = new StateOptions()
{
    Concurrency = ConcurrencyMode.FirstWrite,  // Optimistic concurrency
    Consistency = ConsistencyMode.Eventual
};

do
{
    state = await GetAllOrdersAsync(orderSummary.StoreId);
    state.Value ??= new List<OrderSummary>();
    state.Value.Add(orderSummary);
    isSuccess = await state.TrySaveAsync(_stateOptions);
} while(!isSuccess);  // Retry on ETag mismatch
```

**Current State in Testing Strategy:**

testing-validation-strategy.md:276-291 mentions "CRUD Operations" and "Concurrency Control Test" but:
- No reference to ETag pattern from upgrade analysis
- No test for retry loop behavior
- No test for concurrent updates with same key
- No validation that TrySaveAsync returns false on ETag mismatch

**Recommendation:**

Update testing-validation-strategy.md:276-291 to reference upgrade analysis:

```markdown
### State Store Concurrency Test (ETag Optimistic Concurrency)

**Reference:** `docs/research/dotnet-upgrade-analysis.md`:
- Section 1.5 (Dapr API Compatibility) - lines 1118-1142
- Section 3.3 (State Store Operations) - lines 1661-1679

**Test Scenario (from dotnet-upgrade-analysis.md:1809-1828):**

1. Create baseline LoyaltySummary (loyaltyId: "test-customer-1", PointTotal: 100)
2. Publish 2 OrderSummary messages concurrently with same loyaltyId
3. Both LoyaltyService instances read state (ETag: v1)
4. Both calculate +50 points
5. First instance TrySaveAsync → succeeds (ETag: v2, PointTotal: 150)
6. Second instance TrySaveAsync → fails (ETag mismatch)
7. Second instance retries: reads state (ETag: v2, PointTotal: 150), adds +50, saves (ETag: v3, PointTotal: 200)

**Validation:**
- Final PointTotal = 200 (not 150 due to lost update)
- No concurrency errors thrown to caller
- Logs show retry message

**Effort:** 4 hours
```

---

## Gap 3: No Reference to Breaking Changes Section

**Severity:** HIGH
**Impacts:** Program.cs refactoring, health endpoints, OpenAPI migration

**What's Missing:**

The upgrade analysis documents **27 breaking changes by priority** (dotnet-upgrade-analysis.md:929-938):

| Priority | Change | Services Affected | Effort |
|----------|--------|-------------------|--------|
| HIGH | Program.cs refactoring (IHostBuilder → WebApplicationBuilder) | 7 services | 20h |
| HIGH | Health endpoints (/probes/* → /healthz, /livez, /readyz) | 6 services | 17-23h |
| MEDIUM | OpenAPI (Swashbuckle → Built-in + Scalar) | 3 services | 3h |
| MEDIUM | EF Core 10 upgrade + compiled model regeneration | AccountingService | 4h |
| MEDIUM | Dapr package updates (1.5.0 → 1.16.0) | 7 services | 3.5h |
| MEDIUM | Serilog REMOVAL - replace with OpenTelemetry | 7 services | 6.5h |

**Current State in Testing Strategy:**

testing-validation-strategy.md mentions these changes in passing:
- Line 172: "⚠ Services currently use `/probes/healthz` and `/probes/ready` (non-standard)"
- Line 946: "Finding:** All 7 services use the deprecated .NET 6 `IHostBuilder` + `Startup.cs` pattern"

But **no explicit test plan** to validate these changes work after refactoring.

**Recommendation:**

Add to testing-validation-strategy.md Agent 1 section:

```markdown
### 4.X Breaking Change Validation Tests

**Reference:** `docs/research/dotnet-upgrade-analysis.md` Section 1 (Breaking Changes) - lines 929-938

Each breaking change requires validation tests:

1. **Program.cs Refactoring (7 services)**
   - Reference: dotnet-upgrade-analysis.md:944-1003
   - Test: Verify all services start without errors after WebApplicationBuilder migration
   - Test: Verify Startup.cs files deleted, Program.cs contains all configuration
   - Validation: Service starts in < 10 seconds, Dapr sidecar connects

2. **Health Endpoint Migration (6 services)**
   - Reference: dotnet-upgrade-analysis.md:1030-1072
   - Test: DELETE old /probes/healthz, /probes/ready endpoints
   - Test: ADD new /healthz, /livez, /readyz endpoints (ADR-0005)
   - Validation: Kubernetes health probes pass, ProbesController.cs deleted

3. **OpenAPI Migration (3 services)**
   - Reference: dotnet-upgrade-analysis.md:1074-1096
   - Test: Remove Swashbuckle.AspNetCore package
   - Test: Add Scalar.AspNetCore package
   - Validation: /openapi/v1.json serves OpenAPI spec, /scalar/v1 serves UI

4. **EF Core Compiled Model Regeneration**
   - Reference: dotnet-upgrade-analysis.md:1478-1520
   - Test: Run `dotnet ef dbcontext optimize` for AccountingModel
   - Validation: Compiled models regenerated, AccountingService starts without errors

5. **Dapr SDK Update (7 services)**
   - Reference: dotnet-upgrade-analysis.md:1099-1142
   - Test: Update Dapr.AspNetCore 1.5.0 → 1.16.0
   - Validation: PublishEventAsync, GetStateEntryAsync, TrySaveAsync, InvokeMethodAsync all work
```

---

## Gap 4: Upgrade Tooling Outputs Not Referenced

**Severity:** HIGH
**Impacts:** Automated validation, CI/CD integration

**What's Missing:**

The upgrade analysis documents **specific tooling workflow** (dotnet-upgrade-analysis.md:94-119):

1. **Upgrade Assistant** → `artifacts/upgrade-assistant/<project>.md`
2. **Workload Sync** → `artifacts/dependencies/<project>-workloads.txt`
3. **Dependency Audit** → `artifacts/dependencies/<project>-outdated.txt`, `<project>-vulnerable.txt`, `<project>-graph.txt`
4. **API Analyzer** (compile-time deprecated API detection) → `artifacts/api-analyzer/<project>.md`
5. **ApiCompat** (binary compatibility validation) → CI/CD integration required

**Current State in Testing Strategy:**

testing-validation-strategy.md:23-33 lists these tools but:
- No mention of artifact directories
- No reference to upgrade analysis tooling workflow (dotnet-upgrade-analysis.md:94-119)
- No integration with CI/CD validation
- Missing ApiCompat tool (binary compatibility checker, complementary to API Analyzer)

**Recommendation:**

Update testing-validation-strategy.md:23-33:

```markdown
### Tool Installation Requirements

**Reference:** `docs/research/dotnet-upgrade-analysis.md` Section: Tooling Workflow (lines 94-119)

Install and verify the following before executing any implementation plan:

- `.NET SDK 10.0.100` (per `global.json`) – confirm via `dotnet --version`.
- `Upgrade Assistant` global tool (`dotnet tool install -g upgrade-assistant`)
  - **Artifact Location:** `artifacts/upgrade-assistant/<project>.md` (per dotnet-upgrade-analysis.md:98)
  - **Integration:** Pre-build validation MUST execute Upgrade Assistant for each project
- `API Analyzer` (compile-time deprecated API detection, built into .NET 5+ SDK)
  - **Artifact Location:** `artifacts/api-analyzer/<project>.md` (per dotnet-upgrade-analysis.md:111)
  - **Integration:** Build MUST fail if API Analyzer reports warnings ≥ Medium severity (CA1416)
  - **When:** Development phase - real-time IDE feedback + compile-time warnings
- `ApiCompat` (binary compatibility validation, MSBuild task)
  - **Package:** `Microsoft.DotNet.ApiCompat.Task` (add to each service project)
  - **Integration:** CI/CD MUST validate no breaking changes against .NET 6 baseline
  - **When:** Build/Pack phase + CI/CD pipeline - post-compilation validation
- Dependency Audit Tools
  - **Artifact Locations:**
    - `artifacts/dependencies/<project>-outdated.txt` (per dotnet-upgrade-analysis.md:106)
    - `artifacts/dependencies/<project>-vulnerable.txt` (per dotnet-upgrade-analysis.md:107)
    - `artifacts/dependencies/<project>-graph.txt` (per dotnet-upgrade-analysis.md:108)
  - **Integration:** CI MUST fail if vulnerabilities detected

**Critical:** All artifacts MUST be created and uploaded during CI runs (per dotnet-upgrade-analysis.md:119).
```

---

## Gap 5: Pub/Sub Message Flow Not Fully Tested

**Severity:** HIGH
**Impacts:** Order processing, event-driven architecture

**What's Missing:**

The upgrade analysis documents **complete pub/sub flow** (dotnet-upgrade-analysis.md:1567-1596):

```
VirtualCustomers → OrderService → PublishEventAsync("orders")
    ↓ ↓ ↓ ↓
MakeLineService    LoyaltyService    AccountingService    ReceiptGenerationService
```

Plus **second flow** for order completion:

```
VirtualWorker → MakeLineService → DELETE /orders → PublishEventAsync("ordercompleted")
    ↓
AccountingService (updates CompletedDate)
```

**Specific Message Schemas:**
- OrderSummary schema (dotnet-upgrade-analysis.md:1605-1629)
- All 4 subscribers documented (dotnet-upgrade-analysis.md:1640-1649)

**Current State in Testing Strategy:**

testing-validation-strategy.md:245-268 describes end-to-end flow but:
- Doesn't reference specific code locations from upgrade analysis
- Missing "ordercompleted" topic flow
- No validation of OrderSummary schema compatibility

**Recommendation:**

Update testing-validation-strategy.md:245-268:

```markdown
### 1.X Pub/Sub Message Flow Test (Complete Architecture)

**Reference:** `docs/research/dotnet-upgrade-analysis.md` Section 3.2 (Pub/Sub Mapping) - lines 1567-1649

**Flow 1: Order Creation (4 subscribers)**

Publisher: OrderService (OrderController.cs:40 per dotnet-upgrade-analysis.md:1600)
Topic: "orders"
Subscribers:
1. MakeLineService (MakelineController.cs:33) → SaveStateAsync (reddog.state.makeline)
2. LoyaltyService (LoyaltyController.cs:28) → Calculate points, SaveStateAsync (reddog.state.loyalty)
3. AccountingService (AccountingController.cs:32) → Insert Order + OrderItems to SQL
4. ReceiptGenerationService (ReceiptGenerationController.cs:26) → InvokeBindingAsync to blob storage

**Flow 2: Order Completion (1 subscriber)**

Publisher: MakeLineService (MakelineController.cs:87 per dotnet-upgrade-analysis.md:1633)
Topic: "ordercompleted"
Subscriber:
1. AccountingService (AccountingController.cs:75) → Update Order.CompletedDate

**Message Schema Validation:**

Verify OrderSummary schema unchanged after .NET 10 upgrade (dotnet-upgrade-analysis.md:1605-1629):
- OrderId: Guid
- OrderDate: DateTime
- OrderCompletedDate: DateTime?
- StoreId: string
- LoyaltyId: string
- OrderItems: List<OrderItemSummary>
- OrderTotal: decimal

**Validation Points:**
- All 4 subscribers receive message within 5 seconds
- OrderCompletedDate published correctly to "ordercompleted" topic
- Schema compatible between .NET 6 and .NET 10 versions
```

---

## Gap 6: No Test for Deprecated Dapr Secret Store Extension

**Severity:** MEDIUM
**Impacts:** AccountingService configuration

**What's Missing:**

The upgrade analysis identifies **deprecated API usage** (dotnet-upgrade-analysis.md:1006-1027):

```csharp
config.AddDaprSecretStore(SecretStoreName, secretDescriptors, daprClient);  // DEPRECATED
```

**Issue:** `AddDaprSecretStore()` is **deprecated in Dapr SDK 1.8+** (per dotnet-upgrade-analysis.md:1022)

**Current State in Testing Strategy:**

testing-validation-strategy.md has no mention of this deprecated API or test to validate replacement.

**Recommendation:**

Add to testing-validation-strategy.md Agent 1 or Agent 2:

```markdown
### 1.X Dapr Configuration API Migration Test

**Reference:** `docs/research/dotnet-upgrade-analysis.md`:
- Section 1.2 (Deprecated Dapr Secret Store Extension) - lines 1006-1027
- Section 2.4 (Dapr Configuration Refactoring) - lines 1416-1475

**Current Code (AccountingService Program.cs:68):**
```csharp
config.AddDaprSecretStore(SecretStoreName, secretDescriptors, daprClient);  // DEPRECATED in Dapr 1.8+
```

**Test Scenarios:**
1. Verify deprecated API still works with Dapr 1.16 (backward compatibility)
2. Plan migration to Dapr Configuration API (ADR-0004) - FUTURE
3. Validate connection string retrieval from Dapr secret store

**Validation:**
- AccountingService starts without errors
- Connection string retrieved: `Configuration["reddog-sql"]` returns valid SQL connection string
- SQL Server connection succeeds


---

## Gap 7: Database Schema Migration Not Fully Tested

**Severity:** HIGH
**Impacts:** EF Core 6 → 10 migrations, data integrity

**What's Missing:**

The upgrade analysis documents **database compatibility** (dotnet-upgrade-analysis.md:474-493):

- **EF Core Migration Compatibility Test** (dotnet-upgrade-analysis.md:1831-1850)
- **Compiled Model Regeneration** (dotnet-upgrade-analysis.md:1478-1520)
- **Database Tables:** Customer, Order, OrderItem, StoreLocation (dotnet-upgrade-analysis.md:1744-1750)

**Current State in Testing Strategy:**

testing-validation-strategy.md:474-493 mentions database validation but:
- No reference to specific tables from upgrade analysis
- No test for compiled model regeneration
- No test for migration rollback

**Recommendation:**

Update testing-validation-strategy.md:474-493:

```markdown
### 3.X Database Schema Validation (EF Core 6 → 10)

**Reference:** `docs/research/dotnet-upgrade-analysis.md`:
- Section 3.5 (Database Operations) - lines 1740-1768
- Section 3.6 Test Scenario 3 (Database Schema Validation) - lines 1831-1850

**Tables to Validate (per dotnet-upgrade-analysis.md:1744-1750):**
1. Customer (LoyaltyId PK, FirstName, LastName)
2. Order (OrderId PK, StoreId, PlacedDate, CompletedDate?, Customer FK, OrderTotal)
3. OrderItem (OrderItemId PK, OrderId FK, ProductId, ProductName, Quantity, UnitCost, UnitPrice, ImageUrl)
4. StoreLocation (defined but not actively used)

**Validation Steps (per dotnet-upgrade-analysis.md:1836-1849):**
1. Backup .NET 6 database schema
2. Run .NET 10 EF Core migrations (`dotnet ef database update`)
3. Compare schema versions (check `__EFMigrationsHistory` table)
4. Verify data integrity (row counts match before/after)
5. Check foreign key constraints intact

**Compiled Model Test (per dotnet-upgrade-analysis.md:1490-1507):**
1. Regenerate compiled models: `dotnet ef dbcontext optimize`
2. Verify AccountingService uses: `options.UseModel(RedDog.AccountingModel.AccountingContextModel.Instance);`
3. Verify startup performance maintained

**Rollback Test:**
- Ensure migrations can be reverted without data loss
- Test database restore from backup
```

---

## Gap 8: No Test for Service Invocation Patterns

**Severity:** MEDIUM
**Impacts:** VirtualCustomers → OrderService, VirtualWorker → MakeLineService

**What's Missing:**

The upgrade analysis documents **4 Dapr service invocation calls** (dotnet-upgrade-analysis.md:1704-1736):

1. VirtualCustomers → GET /product (OrderService)
2. VirtualCustomers → POST /order (OrderService)
3. VirtualWorker → GET /orders/{storeId} (MakeLineService)
4. VirtualWorker → DELETE /orders/{storeId}/{orderId} (MakeLineService)

**Current State in Testing Strategy:**

testing-validation-strategy.md:295-314 mentions service invocation generally but:
- No specific test for VirtualCustomers → OrderService pattern
- No specific test for VirtualWorker → MakeLineService pattern
- No validation of request/response JSON serialization (Dapr message contracts)

**Recommendation:**

Update testing-validation-strategy.md:295-314:

```markdown
### 1.X Service Invocation Test (Dapr HTTP API)

**Reference:** `docs/research/dotnet-upgrade-analysis.md` Section 3.4 (Service Invocation Patterns) - lines 1704-1736

**Test Scenario 1: VirtualCustomers → OrderService**

Code: VirtualCustomers.cs:273, 332-333 (per dotnet-upgrade-analysis.md:1710-1720)

1. GET /product via Dapr:
   ```csharp
   await _daprClient.InvokeMethodAsync<List<Product>>(HttpMethod.Get, "order-service", "product");
   ```

2. POST /order via Dapr:
   ```csharp
   var request = _daprClient.CreateInvokeMethodRequest<CustomerOrder>("order-service", "order", order);
   var response = await _daprClient.InvokeMethodWithResponseAsync(request);
   ```

**Test Scenario 2: VirtualWorker → MakeLineService**

Code: VirtualWorkerController.cs:99, 112 (per dotnet-upgrade-analysis.md:1723-1735)

1. GET /orders/{storeId}:
   ```csharp
   await _daprClient.InvokeMethodAsync<List<OrderSummary>>(HttpMethod.Get, "make-line-service", $"orders/{StoreId}");
   ```

2. DELETE /orders/{storeId}/{orderId}:
   ```csharp
   await _daprClient.InvokeMethodAsync<OrderSummary>(HttpMethod.Delete, "make-line-service", $"orders/{storeId}/{orderId}", orderSummary);
   ```

**Validation:**
- Service discovery working (app-id resolves to Kubernetes service)
- Request/response JSON serialization correct (List<OrderSummary>, CustomerOrder marshaled properly via Dapr)
- mTLS encryption enabled (Dapr automatically encrypts)
- Error handling (invoke non-existent service → 404/500)

**Note:** This validates Dapr message contracts (OrderSummary, CustomerOrder), not logging serialization.


---

## Gap 9: Missing Reference to REST API Endpoint Inventory

**Severity:** MEDIUM
**Impacts:** API contract validation

**What's Missing:**

The upgrade analysis documents **18 REST endpoints** across 6 services (dotnet-upgrade-analysis.md:1524-1562):

- OrderService: 4 endpoints
- MakeLineService: 5 endpoints
- AccountingService: 9 endpoints
- Plus health endpoints

**Current State in Testing Strategy:**

testing-validation-strategy.md:409-434 mentions API contract validation but:
- No reference to specific endpoint inventory
- No priority matrix (CRITICAL vs HIGH vs MEDIUM)
- No file:line references

**Recommendation:**

Add to testing-validation-strategy.md Agent 2 section:

```markdown
### 3.X API Endpoint Validation

**Reference:** `docs/research/dotnet-upgrade-analysis.md` Section 3.1 (REST API Endpoint Inventory) - lines 1524-1562

**Total Endpoints:** 18 across 6 services

**Critical Endpoints (Must Test):**
- POST /order (OrderService:31) - Order creation entry point
- POST /orders (MakeLineService:34) - Add to queue
- DELETE /orders/{storeId}/{orderId} (MakeLineService:70) - Complete order
- POST /accounting/orders (AccountingService:33) - Store order in SQL

**Validation:**
- All endpoints return expected HTTP status codes
- Request/response schemas unchanged (.NET 6 vs .NET 10)
- OpenAPI spec documents all endpoints
- Performance < 500ms p95 latency
```

---

## Gap 10: No Test for VirtualCustomers Deprecated Package

**Severity:** MEDIUM
**Impacts:** VirtualCustomers upgrade

**What's Missing:**

The upgrade analysis identifies **deprecated package** (dotnet-upgrade-analysis.md:205-228):

- **Package:** Microsoft.AspNetCore 2.2.0 (6 years EOL, EOL Dec 2019)
- **Service:** VirtualCustomers
- **Priority:** CRITICAL - must remove before upgrade

**Current State in Testing Strategy:**

testing-validation-strategy.md:72 mentions deprecated packages generally but:
- No specific call-out for Microsoft.AspNetCore 2.2.0
- No test to validate removal doesn't break VirtualCustomers

**Recommendation:**

Add to testing-validation-strategy.md Pre-Build Validation:

```markdown
12. ✅ No deprecated packages (Microsoft.AspNetCore 2.2.0, old Swashbuckle)
    - **CRITICAL:** VirtualCustomers uses Microsoft.AspNetCore 2.2.0 (EOL Dec 2019)
    - **Reference:** dotnet-upgrade-analysis.md:205-228
    - **Action:** Remove package BEFORE .NET 10 upgrade
    - **Validation:** VirtualCustomers builds and runs without Microsoft.AspNetCore 2.2.0
```

---

## Gap 11: No Test for Sync-to-Async Conversions

**Severity:** LOW
**Impacts:** AccountingService, VirtualCustomers performance

**What's Missing:**

The upgrade analysis identifies **blocking I/O operations** (dotnet-upgrade-analysis.md:179, 225):

- **AccountingService:** Synchronous EF Core queries (lines 38, 81 in AccountingController.cs)
- **VirtualCustomers:** `.Wait()` blocking call (line 263 in VirtualCustomers.cs)

**Current State in Testing Strategy:**

No mention of sync-to-async conversions or blocking I/O tests.

**Recommendation:**

Add to testing-validation-strategy.md Agent 1 section:

```markdown
### 1.X Async Pattern Validation

**Reference:** `docs/research/dotnet-upgrade-analysis.md`:
- AccountingService (line 179) - Blocking I/O operations
- VirtualCustomers (line 225) - Sync-Over-Async Anti-Pattern

**Test Scenarios:**
1. Verify no `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` calls in HTTP request paths
2. Verify all EF Core queries use `*Async` methods (ToListAsync, SingleOrDefaultAsync, etc.)
3. Performance test: Verify throughput improvement after async conversion

**Tools:**
- Roslyn analyzer: AsyncFixer (NuGet package)
- Manual code review of Controllers
```

---

## Gap 12: No Performance Baseline Comparison

**Severity:** MEDIUM
**Impacts:** Acceptance criteria for .NET 10 upgrade

**What's Missing:**

The upgrade analysis documents **expected performance improvements** (dotnet-upgrade-analysis.md:514-520):

- P95 latency: 5-15% faster (JIT improvements)
- Throughput: 10-20% higher (HTTP/3, runtime optimizations)
- Memory usage: 10-15% lower (GC improvements)

**Current State in Testing Strategy:**

testing-validation-strategy.md:498-524 mentions performance testing but:
- No .NET 6 baseline captured
- No specific improvement targets from upgrade analysis
- **CRITICAL:** Baseline must be established in Phase 1.x BEFORE any .NET 10 upgrade work

**Recommendation:**

Update testing-validation-strategy.md:498-524:

```markdown
### 1.X Performance Baseline Establishment (BEFORE Upgrade)

**Reference:** `docs/research/dotnet-upgrade-analysis.md`:
- Expected Performance (lines 514-520)
- Acceptance Criteria (line 519)

**⚠️ CRITICAL: Complete this FIRST in Phase 1.x - before ANY .NET 10 upgrade work**

**Step 1: Establish .NET 6 Baseline (Current State)**
1. Run k6 load test against CURRENT .NET 6 services (60s, 50 VUs)
2. Record metrics: P50, P95, P99 latency, throughput, CPU, memory
3. Store baseline in `artifacts/performance/dotnet6-baseline.json`

**Step 2: Compare .NET 10 Performance (Post-Upgrade)**
1. Run same k6 load test
2. Compare against baseline
3. **Expected Improvements (per dotnet-upgrade-analysis.md:514-518):**
   - P95 latency: 5-15% faster
   - Throughput: 10-20% higher
   - Memory usage: 10-15% lower

**Acceptance Criteria (per dotnet-upgrade-analysis.md:519):**
- < 10% performance degradation (if any regression observed)
- **NO-GO if:** Performance degradation > 10%
```

---

## Gap 13: No Reference to Rollback Plan

**Severity:** MEDIUM
**Impacts:** Deployment safety for teaching demonstrations

**What's Missing:**

The upgrade analysis documents **specific rollback strategy** (dotnet-upgrade-analysis.md:2200-2205):

- Keep `.NET6-backup` branch with original code
- Git-based rollback workflow (appropriate for teaching tool)

**Current State in Testing Strategy:**

testing-validation-strategy.md:742-763 mentions rollback but:
- Over-engineered for teaching tool (includes Blue-Green, kubectl rollback)
- No reference to `.NET6-backup` branch from upgrade analysis
- **Context:** This is a teaching demo, not production - no live containers, no orchestration complexity needed

**Recommendation:**

Update testing-validation-strategy.md:742-763:

```markdown
### Rollback Plan (Teaching Tool - Simplified)

**Reference:** `docs/research/dotnet-upgrade-analysis.md` Section 7.3 (Rollback Plan) - lines 2200-2205

**Context:** This is a teaching demonstration tool, not a production system with live containers. Simple git-based rollback is sufficient.

**Git Branch Rollback Strategy:**

1. **Baseline Protection:**
   - `.NET6-backup` branch preserved with original .NET 6 code (per dotnet-upgrade-analysis.md:2202)
   - Tagged as `dotnet6-baseline` for easy reference

2. **Feature Branch Workflow:**
   - All upgrade work on feature branches (e.g., `feature/dotnet10-orderservice`)
   - Test thoroughly before merging to `master`
   - If issues discovered: simply delete feature branch and restart

3. **Rollback Execution:**
   ```bash
   # Rollback to .NET 6 baseline
   git checkout .NET6-backup
   git checkout -b feature/rollback-attempt-2

   # Or reset to tagged baseline
   git checkout dotnet6-baseline
   ```

4. **Validation After Rollback:**
   - Services build without errors: `dotnet build`
   - Services start locally: `dapr run ...`
   - Smoke test: Place order, verify makeline queue

**Note:** No Blue-Green deployment, no kubectl rollback needed - this is appropriate for teaching demonstrations where instructor controls environment.
```

---

## Gap 14: Missing Service-Specific Refactoring Checklists

**Severity:** LOW
**Impacts:** Implementation planning

**What's Missing:**

The upgrade analysis provides **detailed checklists for each service** (dotnet-upgrade-analysis.md:1906-2072):

- OrderService Refactoring Checklist (5-6 hours)
- MakeLineService Refactoring Checklist (5-6 hours)
- AccountingService Refactoring Checklist (7-8 hours)
- Plus 4 more services

**Current State in Testing Strategy:**

No reference to these implementation checklists.

**Recommendation:**

Add to testing-validation-strategy.md summary section:

```markdown
### Implementation Reference

For detailed service-by-service refactoring checklists, see:
- `docs/research/dotnet-upgrade-analysis.md` Section 4 (Service-Specific Refactoring Checklists) - lines 1906-2072

Each checklist includes:
- Tasks with effort estimates
- Files to modify
- Files to delete
- Testing validation steps
```

---

## Gap 15: No Reference to GO/NO-GO Decision Criteria

**Severity:** MEDIUM
**Impacts:** Upgrade approval process

**What's Missing:**

The upgrade analysis documents **specific GO/NO-GO criteria** (dotnet-upgrade-analysis.md:2233-2247):

**GO if:**
- All critical tests pass (E2E, DB Schema, API Compatibility)
- Performance metrics within 10% of .NET 6 baseline
- No data loss or corruption
- Health endpoints work correctly

**NO-GO if:**
- E2E order flow test fails
- Database schema incompatibility detected
- Breaking API changes discovered
- Performance degradation > 20%
- Critical Dapr integration broken

**Current State in Testing Strategy:**

testing-validation-strategy.md:986-1009 has GO/NO-GO section but:
- Doesn't reference upgrade analysis criteria
- Some criteria differ (e.g., performance thresholds)

**Recommendation:**

Update testing-validation-strategy.md:986-1009 to align with upgrade analysis:

```markdown
### GO/NO-GO Decision

**Reference:** `docs/research/dotnet-upgrade-analysis.md` Section 9 (GO/NO-GO Decision) - lines 2233-2247

**GO if:**
- ✅ All critical tests pass (E2E, DB Schema, API Compatibility) - per dotnet-upgrade-analysis.md:2236
- ✅ Performance metrics within 10% of .NET 6 baseline - per dotnet-upgrade-analysis.md:2237
- ✅ No data loss or corruption in test scenarios - per dotnet-upgrade-analysis.md:2238
- ✅ Health endpoints work correctly in Kubernetes - per dotnet-upgrade-analysis.md:2239
- ✅ All Dapr integrations functional

**NO-GO if:**
- ❌ E2E order flow test fails - per dotnet-upgrade-analysis.md:2242
- ❌ Database schema incompatibility detected - per dotnet-upgrade-analysis.md:2243
- ❌ Breaking API changes discovered - per dotnet-upgrade-analysis.md:2244
- ❌ Performance degradation > 20% - per dotnet-upgrade-analysis.md:2245
- ❌ Critical Dapr integration broken - per dotnet-upgrade-analysis.md:2246
```

---

## Summary of Recommendations

### Immediate Actions (Before Starting .NET 10 Upgrade)

1. **Add explicit references** to `docs/research/dotnet-upgrade-analysis.md` sections throughout `plan/testing-validation-strategy.md`
2. **Integrate Section 3.6 test scenarios** (26 hours effort) into testing strategy
3. **Add ETag concurrency tests** for MakeLineService and LoyaltyService
4. **Reference tooling artifact locations** (artifacts/upgrade-assistant/, artifacts/api-analyzer/)
5. **Add pub/sub "ordercompleted" flow** to integration tests
6. **Add deprecated API tests** (Dapr secret store, Microsoft.AspNetCore 2.2.0)

### Medium-Term Actions (During Upgrade)

7. **Capture .NET 6 performance baseline** before any upgrades
8. **Test all 18 REST endpoints** from upgrade analysis Section 3.1
9. **Validate database schema migrations** per Section 3.5
10. **Test service invocation patterns** per Section 3.4

### Documentation Updates

11. **Link rollback plan** to `.NET6-backup` branch strategy
12. **Reference service-specific checklists** from upgrade analysis Section 4
13. **Align GO/NO-GO criteria** with upgrade analysis Section 9
14. **Add sync-to-async validation** for AccountingService and VirtualCustomers

### Tools & Automation

15. **Integrate upgrade tooling** outputs into CI/CD pipeline
16. **Enforce artifact creation** (upgrade-assistant, api-analyzer, dependency audits)
17. **Add automated tests** for breaking changes (Program.cs refactoring, health endpoints, OpenAPI)

---

## Conclusion

The testing strategy is structurally sound but **lacks integration with specific upgrade analysis findings**. Without explicit references:

1. **Knowledge Loss Risk:** 26 hours of test scenario research in upgrade analysis could be forgotten
2. **Coverage Gaps:** Critical tests (ETag concurrency, deprecated APIs, service invocation) missing
3. **Tooling Gaps:** No integration with upgrade tooling outputs (artifacts/)
4. **Criteria Misalignment:** GO/NO-GO criteria differ between documents

**Recommendation:** Update `plan/testing-validation-strategy.md` to explicitly reference all 15 gaps identified in this analysis, ensuring comprehensive test coverage aligned with known upgrade risks.

---

**Analysis Complete**
**Total Gaps:** 15 (5 Critical, 6 High, 4 Medium)
**Estimated Effort to Close Gaps:** 8-12 hours (documentation updates + test creation)
