---
goal: Upgrade OrderService from .NET 6 to .NET 10 LTS with Modern Hosting Model
version: 2.0
date_created: 2025-11-02
last_updated: 2025-11-02
owner: Red Dog Modernization Team
status: Planned
tags: [upgrade, dotnet10, lts, modernization, orderservice, dapr, framework-upgrade]
---

# OrderService .NET 10 LTS Upgrade Implementation Plan

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

## Introduction

This implementation plan details the upgrade of the **OrderService** from .NET 6 (EOL November 12, 2024) to **.NET 10 LTS** (released November 11, 2025). The OrderService is one of two core .NET services being retained in the modernized Red Dog architecture (the other being AccountingService). This upgrade is part of the broader modernization initiative documented in `MODERNIZATION_PLAN.md`.

**Why .NET 10 LTS (Not .NET 9 STS):**
- **.NET 6:** Already EOL (November 12, 2024) - **security risk**
- **.NET 9 STS:** Ends November 10, 2026 - only 11 months of remaining support
- **.NET 10 LTS:** Ends November 11, 2028 - **3 years of support**
- **Strategic Decision:** Target LTS for long-term stability and future-proofing

**Key Objectives:**
- Upgrade target framework from `net6.0` to `net10.0`
- Update all NuGet packages to .NET 10 compatible versions
- Migrate from traditional Startup/Program pattern to minimal hosting model
- Ensure full compatibility with Dapr sidecar architecture
- Leverage .NET 10 performance improvements and C# 14 features
- Maintain API backward compatibility for existing consumers
- Migrate from Swashbuckle to Microsoft.AspNetCore.OpenAPI (recommended path)

**Expected Outcomes:**
- Modern, maintainable codebase using latest .NET LTS patterns
- Improved performance from .NET 10 runtime optimizations (stack allocation, JIT enhancements, 8-20% GC improvements)
- Enhanced security with updated dependencies
- Better developer experience with minimal hosting model and C# 14
- 3-year support window without another major upgrade
- Foundation for future .NET LTS upgrades (aligned with .NET release cadence)

---

## .NET 10 Key Features & Improvements

This section highlights the key features and improvements in .NET 10 that make this upgrade valuable.

### Runtime Performance Enhancements

1. **Stack Allocation for Arrays**
   - Small arrays of value types can be stack-allocated (reduces GC pressure)
   - Small arrays of reference types can be stack-allocated
   - Escape analysis for delegates and local struct fields
   - **Impact:** Significant reduction in heap allocations for hot paths

2. **JIT Compiler Improvements**
   - Enhanced code generation for struct arguments (physical promotion)
   - Improved loop inversion using graph-based loop recognition
   - Array interface method devirtualization
   - Better code layout optimization (3-opt TSP heuristic)
   - Improved inlining with profile data
   - **Impact:** 5-15% performance improvement in compute-intensive workloads

3. **Garbage Collection**
   - Arm64 write-barrier improvements: 8-20% GC pause improvements
   - Dynamic adaptation to application size (default for Server GC)
   - **Impact:** Lower latency for REST API responses

4. **SIMD & Vectorization**
   - AVX10.2 support for x64 processors (when hardware available)
   - Enhanced Arm64 vectorization
   - **Impact:** Faster data processing for order calculations

### C# 14 Language Features

1. **Field-Backed Properties**
   - New `field` keyword for semi-auto properties
   - Simplifies property implementation with custom logic
   ```csharp
   public string Name
   {
       get => field;
       set => field = value?.Trim();
   }
   ```

2. **Extension Blocks**
   - Static extension methods and properties
   - Better organization of extension functionality
   ```csharp
   extension StringExtensions for string
   {
       public static bool IsNullOrEmpty => string.IsNullOrEmpty(this);
   }
   ```

3. **Null-Conditional Assignment**
   - `?.=` operator for conditional assignment
   ```csharp
   order?.Total?.= CalculateTotal(); // Only assigns if order and Total are not null
   ```

4. **Enhanced Lambdas**
   - Parameter modifiers without type specification
   - Partial instance constructors and events

5. **`nameof` Enhancements**
   - Support for unbound generic types
   - More flexible compile-time reflection

### ASP.NET Core 10 Features

1. **Native OpenAPI Support**
   - `Microsoft.AspNetCore.OpenApi` built-in (replaces Swashbuckle)
   - `IOpenApiDocumentProvider` in DI container
   - Better performance and integration
   - **Impact:** OrderService will migrate from Swashbuckle to native OpenAPI

2. **Minimal APIs Enhancements**
   - Record type validation support
   - Integration with `IProblemDetailsService`
   - Server-Sent Events (SSE) support natively
   - **Impact:** Better developer experience for API endpoints

3. **JsonPatch Performance**
   - New `System.Text.Json` implementation
   - 171x faster than Newtonsoft.Json
   - 88% less memory allocation
   - **Impact:** Potential for future PATCH endpoint optimizations

### Library & Framework Improvements

1. **JSON Serialization**
   - Duplicate property disallowing (security)
   - Strict settings for production
   - `PipeReader` support for streaming
   - **Impact:** Better OrderSummary serialization performance

2. **Post-Quantum Cryptography**
   - ML-DSA, HashML-DSA support
   - Composite ML-DSA algorithms
   - **Impact:** Future-proofing for quantum computing threats

3. **Networking**
   - `WebSocketStream` for better WebSocket handling
   - TLS 1.3 support on macOS
   - **Impact:** Better Dapr sidecar communication

4. **Diagnostics & Observability**
   - Enhanced tracing and metrics
   - Better OpenTelemetry integration
   - **Impact:** Improved monitoring with Prometheus/Grafana/Jaeger

### Why These Features Matter for OrderService

| Feature | OrderService Benefit |
|---------|---------------------|
| Stack allocation | Reduced GC pressure when processing order batches |
| JIT improvements | Faster order total calculations |
| GC enhancements | Lower P95/P99 latency for `/order` endpoint |
| Native OpenAPI | Simpler dependency management, better performance |
| C# 14 features | Cleaner, more maintainable controller/model code |
| JSON improvements | Faster OrderSummary serialization for pub/sub |
| Diagnostics | Better troubleshooting with enhanced telemetry |

**Performance Expectations:**
- **5-15%** faster API response times (JIT + GC improvements)
- **10-20%** reduced memory allocation (stack allocation + JSON optimizations)
- **8-20%** lower GC pause times (Arm64 write-barrier improvements)

---

## 1. Requirements & Constraints

### Functional Requirements

- **REQ-001**: OrderService must maintain existing REST API contract (`POST /order`, `GET /product`)
- **REQ-002**: Dapr pub/sub integration must continue to publish `OrderSummary` to `orders` topic
- **REQ-003**: DaprClient integration must work with both .NET 10 and Dapr 1.16+ runtime
- **REQ-004**: Service must support deployment to Kubernetes (AKS, EKS, GKE) and Azure Container Apps
  - **What this means:**
    - Service must be containerized (Docker) with Ubuntu 24.04-based images (default .NET 10 base image)
    - **Application configuration must use Dapr Configuration API** (no hardcoded values, no direct cloud SDK calls). See ADR-0004.
    - Environment variables only for Dapr sidecar settings (`DAPR_HTTP_PORT`, `DAPR_GRPC_PORT`) and ASP.NET runtime (`ASPNETCORE_URLS`)
    - No dependencies on cloud provider-specific features in application code (Dapr components handle platform differences)
    - Health check endpoints (`/healthz`, `/livez`, `/readyz`) for Kubernetes probes
    - Dapr sidecars work across all platforms (Dapr is platform-agnostic)
    - Secrets retrieved via Dapr secret store (backend can be Azure Key Vault, AWS Secrets Manager, or Kubernetes Secrets depending on deployment target)
    - Configuration retrieved via Dapr Configuration API (backend can be Azure App Configuration, Redis, or PostgreSQL depending on deployment target)
  - **Why this matters:**
    - Red Dog is a teaching demo - instructors deploy to different clouds (Azure, AWS, GCP)
    - Azure Container Apps uses managed Dapr, Kubernetes uses Dapr extension or self-hosted
    - Code must remain infrastructure-agnostic (Dapr handles platform differences via component configuration)
  - **Note:** As of October 30, 2025, Microsoft changed default .NET images from Debian to Ubuntu 24.04 "Noble Numbat". Ubuntu's longer support periods align better with .NET release cycles.
- **REQ-005**: CORS policy must remain functional for UI integration
- **REQ-006**: Swagger/OpenAPI documentation must be preserved

### Technical Requirements

- **TEC-001**: Target .NET 10.0 framework (latest LTS)
- **TEC-002**: Use Dapr.AspNetCore SDK 1.16.0 or later (.NET 10 compatible)
- **TEC-003**: Use Serilog.AspNetCore 10.0.0 or later (matches .NET version)
- **TEC-004**: **Migrate to Microsoft.AspNetCore.OpenApi** (recommended)
- **TEC-005**: Adopt minimal hosting model (WebApplication builder pattern)
- **TEC-006**: Enable nullable reference types (NRT) for improved code safety
- **TEC-007**: Use modern C# 14 features where applicable (field-backed properties, extension blocks, null-conditional assignment)
- **TEC-008**: Maintain Serilog structured logging with UTC timestamps

### Security Requirements

- **SEC-001**: No security vulnerabilities in NuGet dependencies (audit via `dotnet list package --vulnerable`)
- **SEC-002**: CORS policy must be explicitly configured (no overly permissive defaults)
- **SEC-003**: HTTP-only deployment acceptable (HTTPS termination handled by ingress/load balancer)
- **SEC-004**: Secrets managed via Dapr secret store (no hardcoded credentials)

### Constraints

- **CON-001**: Must work with Dapr sidecar architecture (cannot use .NET Native AOT)
- **CON-002**: Cannot break existing Dapr component contracts (pub/sub, state, bindings)
- **CON-003**: Must remain compatible with other services during staged rollout
- **CON-004**: Docker image must use Ubuntu 24.04 (default .NET 10 base image as of Oct 30, 2025)
- **CON-005**: Build must work with GitHub Actions CI/CD pipeline
- **CON-006**: Deployment manifests must support Helm chart templating

### Guidelines

- **GUD-001**: Follow ASP.NET Core minimal hosting model best practices
- **GUD-002**: Use top-level statements and global usings for cleaner code
- **GUD-003**: Leverage dependency injection throughout the application
- **GUD-004**: Maintain structured logging with contextual information
- **GUD-005**: Keep controllers thin; business logic in domain models
- **GUD-006**: Document breaking changes in session log and commit messages

### Patterns to Follow

- **PAT-001**: Minimal hosting model (`WebApplication.CreateBuilder()`)
- **PAT-002**: CloudEvents format for Dapr pub/sub messages
- **PAT-003**: Health check endpoints (`/healthz`, `/livez`, `/readyz`)
- **PAT-004**: Dependency injection for DaprClient and ILogger
- **PAT-005**: Async/await throughout (no blocking I/O)

---

## 2. Implementation Steps

### Phase 1: Pre-Upgrade Analysis & Preparation

**Goal:** Understand current state, identify risks, prepare environment

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-001 | Document current OrderService API surface (endpoints, contracts) | | |
| TASK-002 | Create integration test suite for existing functionality | | |
| TASK-003 | Verify Dapr runtime version compatibility (1.16+ with SDK 1.16) | | |
| TASK-004 | Audit current NuGet packages for .NET 10 compatibility | | |
| TASK-005 | Review breaking changes documentation (.NET 6 → 7 → 8 → 9 → 10) | | |
| TASK-006 | Create feature branch `upgrade/orderservice-dotnet10` | | |
| TASK-007 | Backup current working state (tag as `orderservice-net6-baseline`) | | |

**Deliverables:**
- API contract documentation (OpenAPI spec)
- Integration test suite (baseline passing tests)
- Compatibility matrix spreadsheet
- Git feature branch and baseline tag

---

### Phase 2: Framework & SDK Upgrade

**Goal:** Update project files and core dependencies

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-101 | Update `RedDog.OrderService.csproj` TargetFramework to `net10.0` | | |
| TASK-102 | Update Dapr.AspNetCore from 1.5.0 → 1.16.0 (verify .NET 10 support) | | |
| TASK-103 | Update Serilog.AspNetCore from 4.1.0 → 10.0.0 (when available) | | |
| TASK-104 | **Migrate to Microsoft.AspNetCore.OpenApi** | | |
| TASK-105 | Enable nullable reference types (`<Nullable>enable</Nullable>`) | | |
| TASK-106 | Run `dotnet restore` and resolve any package conflicts | | |
| TASK-107 | Run `dotnet build` and address compilation errors | | |
| TASK-108 | Fix nullable reference type warnings in Models and Controllers | | |

**Expected Challenges:**
- NRT warnings for uninitialized properties in models
- Potential API changes in Dapr SDK 1.16
- Deprecated Serilog configuration methods

**Success Criteria:**
- Clean build with zero errors
- Warnings reduced to acceptable level (< 5 warnings)
- All packages restored without conflicts

---

### Phase 3: Minimal Hosting Model Migration

**Goal:** Refactor from Startup.cs/Program.cs to minimal hosting pattern

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-201 | Create new `Program.cs` with WebApplication builder pattern | | |
| TASK-202 | Migrate service registrations from `ConfigureServices()` | | |
| TASK-203 | Migrate middleware pipeline from `Configure()` | | |
| TASK-204 | Configure Serilog with `UseSerilog()` in builder | | |
| TASK-205 | Register Dapr with `AddControllers().AddDapr()` | | |
| TASK-206 | Configure CORS policy in builder | | |
| TASK-207 | Configure Swagger/OpenAPI in builder | | |
| TASK-208 | Register health check endpoints | | |
| TASK-209 | Configure CloudEvents middleware (`UseCloudEvents()`) | | |
| TASK-210 | Map subscribe handler (`MapSubscribeHandler()`) | | |
| TASK-211 | Map controllers and run application | | |
| TASK-212 | Delete old `Startup.cs` file | | |
| TASK-213 | Clean up unused using statements | | |

**Refactoring Pattern:**

**Before (.NET 6 - Startup.cs):**
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddHttpClient();
    services.AddControllers().AddDapr();
    services.AddCors(options => { ... });
    services.AddSwaggerGen(c => { ... });
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment()) { app.UseDeveloperExceptionPage(); }
    app.UseSwagger();
    app.UseSwaggerUI(...);
    app.UseSerilogRequestLogging();
    app.UseRouting();
    app.UseCloudEvents();
    app.UseCors();
    app.UseAuthorization();
    app.UseEndpoints(endpoints => { ... });
}
```

**After (.NET 10 - Program.cs):**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services
builder.Services.AddHttpClient();
builder.Services.AddControllers().AddDapr();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RedDog.OrderService",
        Version = "v1"
    });
});

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseCloudEvents();
app.UseCors();
app.UseAuthorization();

app.MapControllers();
app.MapSubscribeHandler();

app.Run();
```

**Success Criteria:**
- Application starts successfully
- Swagger UI accessible at `/swagger`
- Health endpoints respond correctly
- Serilog logging outputs to console in expected format

---

### Phase 4: Code Modernization

**Goal:** Leverage .NET 10 and C# 14 features for cleaner code

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-301 | Add `global using` statements for common namespaces | | |
| TASK-302 | Replace `DateTime.UtcNow` with `TimeProvider` (if needed for testing) | | |
| TASK-303 | Use collection expressions for list initialization (C# 12) | | |
| TASK-304 | Apply primary constructors to controllers (C# 12) | | |
| TASK-305 | Review and update XML documentation comments | | |
| TASK-306 | Validate nullable annotations on all model properties | | |
| TASK-307 | Use file-scoped namespaces (C# 10+) if not already applied | | |
| TASK-308 | **NEW:** Consider field-backed properties with `field` keyword (C# 14) | | |
| TASK-309 | **NEW:** Use null-conditional assignment `?.=` where appropriate (C# 14) | | |

**Optional Enhancements:**
- Implement `IValidatableObject` on models for better validation
- Add health check dependencies for Dapr sidecar readiness
- Implement structured exception handling middleware

**Success Criteria:**
- Code follows modern C# conventions
- IDE warnings minimized
- Code readability improved

---

### Phase 5: Testing & Validation

**Goal:** Verify all functionality works correctly with .NET 10

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-401 | Run unit tests (if available) | | |
| TASK-402 | Run integration tests against local Dapr runtime | | |
| TASK-403 | Test `POST /order` endpoint with sample payloads | | |
| TASK-404 | Test `GET /product` endpoint returns product catalog | | |
| TASK-405 | Verify Dapr pub/sub publishes messages correctly | | |
| TASK-406 | Verify Swagger UI documentation is accurate | | |
| TASK-407 | Test CORS headers from different origins | | |
| TASK-408 | Verify Serilog structured logging output | | |
| TASK-409 | Load test with `k6` or `hey` tool (performance baseline) | | |
| TASK-410 | Test with Dapr dashboard to verify service discovery | | |
| TASK-411 | Verify health check endpoints (`/healthz`, `/livez`) | | |

**Test Scenarios:**
1. **Happy Path**: Submit valid order → verify OrderSummary published to `orders` topic
2. **Invalid Product**: Submit order with non-existent ProductId → verify graceful handling
3. **Pub/Sub Failure**: Simulate Dapr sidecar down → verify error logging and 500 response
4. **High Load**: Submit 100 orders/sec for 60 seconds → verify no crashes or memory leaks

**Success Criteria:**
- All tests pass
- No regressions in functionality
- Performance equal or better than .NET 6 baseline
- Logs are structured and readable

---

### Phase 6: Docker & Deployment Updates

**Goal:** Update Docker images and Kubernetes manifests for .NET 10

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-501 | Update Dockerfile to use `mcr.microsoft.com/dotnet/sdk:10.0` for build | | |
| TASK-502 | Update Dockerfile to use `mcr.microsoft.com/dotnet/aspnet:10.0` for runtime (Ubuntu 24.04 Noble Numbat) | | |
| TASK-503 | Build Docker image locally and test | | |
| TASK-504 | Run container locally with Dapr sidecar | | |
| TASK-505 | Update Kubernetes deployment manifests (if framework version annotated) | | |
| TASK-506 | Update Helm chart values/templates (if applicable) | | |
| TASK-507 | Verify GitHub Actions workflow builds .NET 10 image | | |
| TASK-508 | Push test image to GHCR (GitHub Container Registry) | | |

**Dockerfile Pattern:**
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["RedDog.OrderService/RedDog.OrderService.csproj", "RedDog.OrderService/"]
RUN dotnet restore "RedDog.OrderService/RedDog.OrderService.csproj"
COPY . .
WORKDIR "/src/RedDog.OrderService"
RUN dotnet build "RedDog.OrderService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "RedDog.OrderService.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RedDog.OrderService.dll"]
```

**Note:** As of October 30, 2025, `mcr.microsoft.com/dotnet/aspnet:10.0` defaults to **Ubuntu 24.04 "Noble Numbat"**. Ubuntu's extended support periods (longer than .NET release cycles) provide better long-term stability.

**Success Criteria:**
- Docker image builds successfully
- Image size reasonable (< 250 MB)
- Container runs and responds to health checks
- Dapr sidecar injection works correctly

---

### Phase 7: Documentation & Knowledge Transfer

**Goal:** Document changes and update guidance

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-601 | Update CLAUDE.md with .NET 10 build instructions | | |
| TASK-602 | Update README.md with new prerequisites (.NET 10 SDK) | | |
| TASK-603 | Document breaking changes in session log | | |
| TASK-604 | Update VS Code tasks.json with .NET 10 paths (if needed) | | |
| TASK-605 | Update VS Code launch.json debug configurations | | |
| TASK-606 | Create migration guide for other services (template) | | |
| TASK-607 | Update modernization plan progress tracker | | |

**Documentation Updates:**
- Prerequisites: .NET 10 SDK required
- Build commands: Still `dotnet build` (no change)
- Migration notes: Minimal hosting model changes, OpenAPI migration
- Troubleshooting: Common .NET 10 upgrade issues
- Timeline recommendation: Wait until January 2026 for ecosystem maturity

---

### Phase 8: Release & Rollback Plan

**Goal:** Safely deploy to production with rollback capability

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-701 | Create pull request with detailed description | | |
| TASK-702 | Code review with at least one other developer | | |
| TASK-703 | Merge to main branch after approval | | |
| TASK-704 | Tag release as `orderservice-v2.0.0-dotnet10` | | |
| TASK-705 | Deploy to dev/test environment | | |
| TASK-706 | Run smoke tests in dev/test | | |
| TASK-707 | Deploy to staging environment (if available) | | |
| TASK-708 | Deploy to production (canary or blue/green) | | |
| TASK-709 | Monitor logs and metrics for 24 hours | | |
| TASK-710 | Document rollback procedure (revert to .NET 6 tag) | | |

**Rollback Plan:**
1. If critical issues detected, revert Kubernetes deployment to previous image
2. Use Helm rollback: `helm rollback reddog-orderservice <previous-revision>`
3. If container registry issue, pull previous image tag
4. Git revert merge commit if code rollback needed
5. Document incident and root cause analysis

**Success Criteria:**
- OrderService running in production on .NET 10 LTS
- No increase in error rate or latency
- Monitoring dashboards show healthy metrics
- Rollback procedure tested and documented

---

## 3. Alternatives

### ALT-001: Keep .NET 6 and Only Update Dependencies
**Description:** Upgrade NuGet packages to latest .NET 6 compatible versions without framework upgrade.

**Pros:**
- Minimal risk
- No code changes required
- Faster implementation

**Cons:**
- .NET 6 reached EOL November 12, 2024 (no security patches)
- Missing .NET 10 performance improvements (JIT, GC, stack allocation)
- Technical debt continues to accumulate
- Not aligned with modernization goals

**Decision:** ❌ Rejected - Does not address EOL risk or modernization objectives

---

### ALT-002: Upgrade to .NET 9 STS Instead of .NET 10 LTS
**Description:** Target .NET 9 (STS) instead of waiting for .NET 10 (LTS).

**Pros:**
- .NET 9 ecosystem more mature (released Nov 2024)
- Faster timeline to get off EOL .NET 6
- Package compatibility better established

**Cons:**
- .NET 9 ends support Nov 2026 (only 11 months away)
- Would require another upgrade to .NET 10 or .NET 11 soon
- Double migration effort and risk
- STS not recommended for long-term production systems

**Decision:** ❌ Rejected - Not strategically sound; better to wait for LTS

---

### ALT-003: Skip Minimal Hosting Model Migration
**Description:** Upgrade to .NET 10 but keep traditional Startup.cs/Program.cs pattern.

**Pros:**
- Less code churn
- Familiar pattern for existing developers
- Fully supported in .NET 10

**Cons:**
- Not aligned with modern .NET practices
- More boilerplate code
- Harder to read for new developers
- Future LTS upgrades will require migration eventually

**Decision:** ✅ Considered but not recommended - Minimal hosting model is the future

---

### ALT-004: Migrate to .NET Aspire Instead of Standalone Dapr
**Description:** Use .NET Aspire orchestration for local development with Dapr integration.

**Pros:**
- Modern .NET cloud-native development experience
- Built-in service discovery and configuration
- Better local development story

**Cons:**
- Major architectural change (out of scope for this task)
- Adds new dependency (.NET Aspire)
- Not aligned with teaching Dapr standalone
- Requires broader team discussion

**Decision:** ❌ Rejected - Out of scope; separate initiative if desired

---

### ALT-005: Staged Migration (.NET 6 → .NET 8 → .NET 10)
**Description:** Migrate to .NET 8 LTS first, then upgrade to .NET 10 later.

**Pros:**
- Lower risk per step
- .NET 8 LTS has mature ecosystem
- Faster first migration (can start immediately)
- .NET 8 supported until November 2026

**Cons:**
- Double migration effort and cost
- .NET 8 EOL only 1 year away (Nov 2026)
- Team fatigue from multiple upgrades
- Less cost-effective overall

**Decision:** ❌ Rejected - Direct .NET 6 → .NET 10 more strategic

---

## 4. Dependencies

### External Dependencies

- **DEP-001**: .NET 10 SDK (version 10.0.100 or later) installed on developer machines
- **DEP-002**: Visual Studio 2022 17.13+ or VS Code with C# Dev Kit for .NET 10 support
- **DEP-003**: Dapr CLI 1.16+ for local testing
- **DEP-004**: Dapr runtime 1.16+ in Kubernetes clusters
- **DEP-005**: Docker Desktop or Podman for container builds
- **DEP-006**: Kubernetes cluster (AKS, EKS, GKE, or local kind/minikube)

### NuGet Package Dependencies

- **DEP-101**: Dapr.AspNetCore 1.16.0+ (.NET 10 compatible - verify)
- **DEP-102**: Serilog.AspNetCore 10.0.0+ (version matches .NET 10 - when available)
- **DEP-103**: Microsoft.AspNetCore.OpenApi 10.0.0+ (recommended) OR Swashbuckle.AspNetCore 9.0.6+ (.NET 10 compatible - verify)
- **DEP-104**: Microsoft.AspNetCore.App framework (implicit with .NET 10 SDK)

### Internal Dependencies

- **DEP-201**: Dapr components (`reddog.pubsub`, `reddog.state.*`) must remain unchanged
- **DEP-202**: Redis pub/sub broker must be running for integration tests
- **DEP-203**: Product catalog JSON files in `ProductDefinitions/` directory
- **DEP-204**: Other services (MakeLineService, LoyaltyService, etc.) must accept OrderSummary v1 contract

### Infrastructure Dependencies

- **DEP-301**: GitHub Container Registry (GHCR) for image storage
- **DEP-302**: GitHub Actions for CI/CD pipelines
- **DEP-303**: Kubernetes ingress controller for HTTP routing
- **DEP-304**: Observability stack (Prometheus, Grafana, Jaeger) for monitoring

---

## 5. Files

### Modified Files

- **FILE-001**: `RedDog.OrderService/RedDog.OrderService.csproj` - Update TargetFramework and NuGet packages
- **FILE-002**: `RedDog.OrderService/Program.cs` - Complete rewrite for minimal hosting model
- **FILE-003**: `RedDog.OrderService/Controllers/OrderController.cs` - Add nullable annotations, possible primary constructor
- **FILE-004**: `RedDog.OrderService/Controllers/ProductController.cs` - Add nullable annotations
- **FILE-005**: `RedDog.OrderService/Controllers/ProbesController.cs` - Add nullable annotations
- **FILE-006**: `RedDog.OrderService/Models/*.cs` - Add nullable reference type annotations
- **FILE-007**: `RedDog.OrderService/Dockerfile` - Update base images to .NET 10
- **FILE-008**: `.github/workflows/order-service.yml` - Update build actions for .NET 10
- **FILE-009**: `manifests/**/order-service.yaml` - Update image tags (may be automated)
- **FILE-010**: `.vscode/tasks.json` - Update build task for .NET 10
- **FILE-011**: `.vscode/launch.json` - Verify debug configurations
- **FILE-012**: `CLAUDE.md` - Document .NET 10 build requirements
- **FILE-013**: `README.md` - Update prerequisites and build instructions
- **FILE-014**: `.claude/sessions/2025-11-02-0805-order-service-dotnet10-upgrade.md` - Session log (renamed)

### Deleted Files

- **FILE-101**: `RedDog.OrderService/Startup.cs` - Consolidated into Program.cs (minimal hosting)

### New Files

- **FILE-201**: `plan/orderservice-dotnet10-upgrade.md` - This implementation plan (you are here!)
- **FILE-202**: `tests/OrderService.IntegrationTests/` - New integration test project (if created)

---

## 6. Testing

### Unit Tests

- **TEST-001**: **Product.GetAllAsync()** - Verify product catalog loading from JSON
- **TEST-002**: **OrderController.CreateOrderSummaryAsync()** - Verify order total calculation
- **TEST-003**: **OrderSummary model** - Verify all properties serialize correctly
- **TEST-004**: **Nullable reference type compliance** - No NRT warnings in test project

### Integration Tests

- **TEST-101**: **POST /order with valid payload** - Returns 200 OK, publishes to Dapr
- **TEST-102**: **POST /order with invalid product** - Gracefully handles missing product
- **TEST-103**: **GET /product** - Returns product catalog
- **TEST-104**: **Health check endpoints** - Return 200 OK when healthy
- **TEST-105**: **Dapr pub/sub publish** - Verify message published to Redis topic
- **TEST-106**: **Swagger UI** - Accessible at `/swagger` in development mode
- **TEST-107**: **CORS headers** - Verify `Access-Control-Allow-Origin: *` present

### Performance Tests

- **TEST-201**: **Baseline latency** - P50, P95, P99 latency for POST /order
- **TEST-202**: **Throughput** - Orders/second sustained for 60 seconds
- **TEST-203**: **Memory consumption** - Heap size under load
- **TEST-204**: **Cold start time** - Time from container start to first successful request

### Acceptance Tests

- **TEST-301**: **End-to-end order flow** - Order → Publish → MakeLine receives
- **TEST-302**: **Multi-service integration** - Verify Loyalty and Receipt services process order
- **TEST-303**: **Swagger contract** - OpenAPI spec matches actual API behavior
- **TEST-304**: **Log output** - Structured logs contain expected fields

### Regression Tests

- **TEST-401**: **Backward compatibility** - Old clients can still call API
- **TEST-402**: **Dapr component compatibility** - Works with existing Dapr YAML configs
- **TEST-403**: **Message format** - OrderSummary JSON schema unchanged

**Test Environments:**
- **Local**: Dapr standalone mode, developer machine
- **Dev**: Kubernetes namespace with Dapr runtime
- **Staging**: Production-like environment (if available)
- **Production**: Canary deployment for gradual rollout

---

## 7. Risks & Assumptions

### Risks

- **RISK-001**: **Dapr SDK breaking changes** - Dapr SDK 1.16 may have API changes from 1.5
  - *Mitigation*: Review Dapr SDK release notes, test thoroughly in dev environment
  - *Likelihood*: Medium | *Impact*: High

- **RISK-002**: **NuGet package conflicts** - Transitive dependency conflicts between packages
  - *Mitigation*: Use `dotnet list package --include-transitive`, resolve conflicts early
  - *Likelihood*: Low | *Impact*: Medium

- **RISK-003**: **Performance regression** - .NET 10 could introduce unexpected performance issues
  - *Mitigation*: Establish performance baseline, run load tests before/after
  - *Likelihood*: Low | *Impact*: High

- **RISK-004**: **Minimal hosting model bugs** - New hosting model may have subtle behavioral differences
  - *Mitigation*: Thorough integration testing, compare logs with .NET 6 version
  - *Likelihood*: Low | *Impact*: Medium

- **RISK-005**: **Production deployment issues** - Kubernetes cluster may not support .NET 10 images
  - *Mitigation*: Test in dev/staging first, use default Ubuntu 24.04 base images (widely supported across all cloud providers)
  - *Likelihood*: Very Low | *Impact*: High

- **RISK-006**: **Other services incompatibility** - Changes in OrderSummary serialization could break consumers
  - *Mitigation*: Integration tests with downstream services, contract testing
  - *Likelihood*: Low | *Impact*: High

- **RISK-007**: **Developer environment setup** - Team members may not have .NET 10 SDK installed
  - *Mitigation*: Update documentation early, provide setup scripts
  - *Likelihood*: High | *Impact*: Low

- **RISK-008**: **GitHub Actions runner compatibility** - CI/CD may need runner updates for .NET 10
  - *Mitigation*: Use `actions/setup-dotnet@v4` with .NET 10, test in branch first
  - *Likelihood*: Low | *Impact*: Medium

### Assumptions

- **ASSUMPTION-001**: Dapr runtime 1.16+ is deployed in all target Kubernetes clusters
- **ASSUMPTION-002**: No changes required to Dapr component YAML files (pub/sub, state stores)
- **ASSUMPTION-003**: OrderSummary message contract remains backward compatible
- **ASSUMPTION-004**: Redis pub/sub broker supports current message formats
- **ASSUMPTION-005**: Container registry (GHCR) supports multi-arch .NET 10 images
- **ASSUMPTION-006**: Team has capacity to test thoroughly before production deployment
- **ASSUMPTION-007**: .NET 10 SDK is generally available and stable (RC 2 as of Nov 2025)
- **ASSUMPTION-008**: No major architectural changes planned for OrderService in parallel
- **ASSUMPTION-009**: Other services (MakeLine, Loyalty, Receipt) do not require simultaneous upgrade
- **ASSUMPTION-010**: Monitoring/observability stack supports .NET 10 telemetry
- **ASSUMPTION-011**: Package ecosystem will be mature by January 2026 (GA+2 months)

---

## 8. Related Specifications / Further Reading

### Internal Documentation

- [Red Dog Modernization Plan](./MODERNIZATION_PLAN.md) - Overall modernization roadmap
- [Safe Cleanup Guide](./SAFE_CLEANUP.md) - Phase 0 cleanup completed before this upgrade
- [CLAUDE.md](../CLAUDE.md) - Project guidance and development instructions
- [Session Log: 2025-11-02 OrderService .NET 10 Upgrade](../.claude/sessions/2025-11-02-0805-order-service-dotnet10-upgrade.md)

### Microsoft Documentation

- [What's new in .NET 10](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)
- [Breaking changes in .NET 10](https://learn.microsoft.com/en-us/dotnet/core/compatibility/10.0)
- [Breaking changes in .NET 6](https://learn.microsoft.com/en-us/dotnet/core/compatibility/6.0)
- [Breaking changes in .NET 7](https://learn.microsoft.com/en-us/dotnet/core/compatibility/7.0)
- [Breaking changes in .NET 8](https://learn.microsoft.com/en-us/dotnet/core/compatibility/8.0)
- [Breaking changes in .NET 9](https://learn.microsoft.com/en-us/dotnet/core/compatibility/9.0)
- [Migrate from ASP.NET Core 3.1 to .NET 6+ (Minimal Hosting)](https://learn.microsoft.com/en-us/aspnet/core/migration/50-to-60?view=aspnetcore-9.0)
- [ASP.NET Core Minimal Hosting Model](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/overview)
- [.NET 10 SDK Version Requirements](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/version-requirements)
- [.NET Runtime Performance Improvements (.NET 10)](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-10/)

### Dapr Documentation

- [Dapr .NET SDK Documentation](https://docs.dapr.io/developing-applications/sdks/dotnet/)
- [Dapr SDK Releases](https://github.com/dapr/dotnet-sdk/releases)
- [Dapr Supported Runtime Versions](https://docs.dapr.io/operations/support/support-release-policy/)
- [Dapr Pub/Sub with .NET](https://docs.dapr.io/developing-applications/building-blocks/pubsub/howto-publish-subscribe/)

### NuGet Package Documentation

- [Serilog.AspNetCore 9.0.0](https://github.com/serilog/serilog-aspnetcore)
- [Swashbuckle.AspNetCore 9.0.6](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [Dapr.AspNetCore 1.16.0](https://www.nuget.org/packages/Dapr.AspNetCore)

### Community Resources

- [.NET 10 Announcement Blog](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/)
- [C# 12 Features](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12)
- [C# 13 Features](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-13)
- [C# 14 Features](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)
- [Nullable Reference Types Best Practices](https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references)

---

## Appendix A: Quick Reference Commands

### Build and Run Locally

```bash
# Restore packages
dotnet restore RedDog.OrderService

# Build project
dotnet build RedDog.OrderService

# Run with Dapr
dapr run --app-id order-service \
  --app-port 5100 \
  --dapr-http-port 5180 \
  --dapr-grpc-port 5101 \
  --components-path ./manifests/local/branch \
  -- dotnet run --project RedDog.OrderService
```

### Testing

```bash
# Run tests (if available)
dotnet test

# Check for vulnerable packages
dotnet list package --vulnerable

# Publish for deployment
dotnet publish RedDog.OrderService -c Release -o ./publish
```

### Docker

```bash
# Build Docker image
docker build -t reddog-orderservice:net10 -f RedDog.OrderService/Dockerfile .

# Run container locally
docker run -p 5100:8080 -e DAPR_HTTP_PORT=5180 reddog-orderservice:net10

# Push to GHCR
docker tag reddog-orderservice:net10 ghcr.io/<org>/reddog-orderservice:net10
docker push ghcr.io/<org>/reddog-orderservice:net10
```

---

## Appendix B: Estimated Timeline

| Phase | Duration | Dependencies |
|-------|----------|--------------|
| Phase 1: Pre-Upgrade Analysis | 1 day | None |
| Phase 2: Framework & SDK Upgrade | 1 day | Phase 1 |
| Phase 3: Minimal Hosting Model Migration | 2 days | Phase 2 |
| Phase 4: Code Modernization | 1 day | Phase 3 |
| Phase 5: Testing & Validation | 2 days | Phase 4 |
| Phase 6: Docker & Deployment Updates | 1 day | Phase 5 |
| Phase 7: Documentation | 1 day | Phase 6 |
| Phase 8: Release & Monitoring | 1 day | Phase 7 |
| **Total** | **10 working days** | |

**Note:** Timeline assumes one developer working full-time. Can be parallelized with multiple team members.

---

## Appendix C: Success Metrics

| Metric | Baseline (.NET 6) | Target (.NET 10) | Measurement Method |
|--------|-------------------|-----------------|-------------------|
| Build time | TBD | ≤ Baseline | `time dotnet build` |
| Startup time | TBD | ≤ Baseline | Container logs (first request) |
| P95 latency (POST /order) | TBD | ≤ Baseline | Load testing tool (k6, hey) |
| Memory usage (idle) | TBD | ≤ Baseline | Prometheus metrics |
| Memory usage (load) | TBD | ≤ Baseline | Prometheus metrics |
| Container image size | TBD | ≤ Baseline + 10% | `docker images` |
| Lines of code | TBD | ≤ Baseline - 20% | `cloc` or GitHub stats |
| NuGet vulnerabilities | TBD | 0 critical/high | `dotnet list package --vulnerable` |

---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-11-02 | Claude Code | Initial implementation plan created |

---

**End of Implementation Plan**
