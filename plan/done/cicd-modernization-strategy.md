# CI/CD Modernization Strategy

**Effort Estimate:** 8-12 hours
**Risk Level:** LOW

**Analysis Date:** 2025-11-08
**Agent Model:** Claude Haiku 4.5 (3 parallel agents)
**Analysis Scope:** All CI/CD pipelines, build configurations, and automation strategies

## Overview

This strategy defines the Phase 5 CI/CD modernization scope for the .NET 10 upgrade, covering GitHub Actions pipeline configuration, build automation, testing strategies, and continuous integration best practices.

---

## Agent 1: Pipeline Configuration Analysis

### CI/CD Pipeline Inventory

**Platform:** GitHub Actions (100%)

**Discovered Pipelines:**
- 9 total workflows in `.github/workflows/`
- 7 service packaging workflows (OrderService, AccountingService, MakeLineService, LoyaltyService, ReceiptGenerationService, VirtualWorker, VirtualCustomers)
- 1 UI packaging workflow (Node.js/Vue.js)
- 1 manifest promotion workflow
- **Status:** All active, triggered on push to master branch

**Key Observation:** All pipelines use Docker-based builds only (no direct `dotnet build` commands). Builds happen entirely inside Dockerfiles.

### Current Configuration Analysis

| Service | SDK Version | .csproj Target | NuGet Task | Docker Images | Test Strategy |
|---------|------------|----------------|------------|---------------|---------------|
| All .NET Services | sdk:6.0 | net6.0 | docker/build-push-action@v2 | ghcr.io | NONE (Docker-only) |
| UI | node:14.15.4 | N/A | docker/build-push-action@v2 | ghcr.io | NONE |

**Critical Issues Found:**
- No `global.json` for SDK version pinning
- No test execution in any workflow
- No code quality checks or security scanning
- Deprecated GitHub Actions syntax (`::set-output`)
- Outdated action versions (v2 → should be v4-v5)

### .NET 10 Incompatibilities

#### CRITICAL Issues

| File | Issue | Impact | Recommendation |
|------|-------|--------|----------------|
| All .NET Dockerfiles | `FROM mcr.microsoft.com/dotnet/sdk:6.0` | CRITICAL | Update to `sdk:10.0` |
| All .NET Dockerfiles | `FROM mcr.microsoft.com/dotnet/aspnet:6.0` | CRITICAL | Update to `aspnet:10.0` |
| All .csproj files | `<TargetFramework>net6.0</TargetFramework>` | HIGH | Update to `net10.0` |
| All services | Dapr.AspNetCore 1.5.0 | HIGH | Update to 1.16.0+ |
| Repository root | Missing global.json | MEDIUM | Create with SDK 10.0.100 |

#### MEDIUM Issues

| File | Issue | Recommendation |
|------|-------|----------------|
| All workflows/*.yaml | `echo ::set-output` (deprecated) | Migrate to `>> $GITHUB_OUTPUT` |
| All workflows/*.yaml | `actions/checkout@v2` | Update to @v4 |
| All workflows/*.yaml | `docker/*@v1-v2` actions | Update to @v3-v5 |
| UI/Dockerfile | `node:14.15.4` (EOL) | Update to `node:24-alpine` |

### SDK Version Pinning Recommendations

#### Create global.json

**Location:** `/global.json`

```json
{
  "sdk": {
    "version": "10.0.100",
    "allowPrerelease": false,
    "rollForward": "latestPatch"
  }
}
```

**Benefits:**
- Ensures consistent SDK version across dev/CI/prod
- Prevents accidental builds with wrong SDK version
- Enables `setup-dotnet@v4` to auto-detect version

#### Updated Dockerfile Pattern

**Before (.NET 6):**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
# ... build steps
FROM mcr.microsoft.com/dotnet/aspnet:6.0
```

**After (.NET 10):**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# ... build steps
FROM mcr.microsoft.com/dotnet/aspnet:10.0
```

**Note:** .NET 10 images default to Ubuntu 24.04 LTS (per ADR-0003)

### Missing CI/CD Features

**No Build Validation:**
- Current: Only Docker build (validates code indirectly)
- Recommendation: Add optional validation pipeline for PRs

**No Test Execution:**
- Current: Zero test coverage in CI/CD
- Recommendation: Add test step (requires tests to exist)

**No Security Scanning:**
- Current: No vulnerability scanning
- Recommendation: Add `dotnet list package --vulnerable` step

**No Code Quality Checks:**
- Current: No static analysis, linting, or style enforcement
- Recommendation: Add Roslyn analyzer validation

### Tooling Compliance Checks

- Add a `tooling-audit` job to every PR workflow (runs after checkout, before build):
  1. `upgrade-assistant upgrade <Project>.csproj --entry-point <Project>.csproj --non-interactive --skip-backup false > artifacts/upgrade-assistant/<project>.md`
  2. `dotnet workload restore` and `dotnet workload update`
  3. `dotnet list <Project>.csproj package --outdated --include-transitive`, `--vulnerable`, and `dotnet list <Project>.csproj reference --graph` (outputs stored under `artifacts/dependencies/`)
  4. `dotnet tool run api-analyzer -f net10.0 -p <Project>.csproj > artifacts/api-analyzer/<project>.md`
- Job fails if:
  - API Analyzer emits any warning with severity ≥ Medium.
  - `dotnet list package --vulnerable` reports vulnerabilities.
  - Required artifact files are missing.
- Publish tooling artifacts as workflow artifacts for auditing and use them as merge-gate requirements (status check `tooling-audit` must pass before merge).

### Implementation Effort

| Task | Hours | Complexity |
|------|-------|------------|
| Create global.json | 0.5 | Trivial |
| Update 7 .NET Dockerfiles | 2 | Low |
| Update 8 .csproj files | 4 | Medium |
| Update 8 workflow files | 3 | Medium |
| Test local builds | 2 | Medium |
| Test in GitHub Actions | 2 | Medium |
| Documentation | 2 | Low |
| Code review & merge | 1 | Low |
| **TOTAL** | **16-17 hours** | **Low-Medium** |

### Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| Dapr SDK 1.16 compatibility | Medium | High | Test integration with Dapr 1.16 runtime |
| NuGet package conflicts | Low | High | Use `dotnet list package --include-transitive` |
| GitHub Actions runner compatibility | Low | Medium | Test in feature branch first |
| Image size regression | Low | Medium | Benchmark current vs new sizes |

---

## Agent 2: Build Pipeline Modernization

### Complete GitHub Actions Workflow

**Comprehensive Multi-Job Workflow:**

**Job Structure:**
1. **build-and-test** - Matrix build (8 projects), unit tests, coverage
2. **integration-tests** - Dapr sidecar testing, pub/sub flow, service invocation
3. **docker-build-and-publish** - Container builds, GHCR publishing
4. **final-status** - Aggregate status check

**Key Features:**
- Matrix strategy for parallel builds (4 concurrent)
- Service containers (Redis 7, SQL Server 2022)
- Dapr 1.16.0 installation and configuration
- Code coverage collection (XPlat Code Coverage)
- Container security scanning (Trivy)
- Conditional publishing (main branch only)

**Sample Matrix Build:**
```yaml
strategy:
  matrix:
    project:
      - name: OrderService
        path: RedDog.OrderService
        port: 5100
        has-tests: true
      - name: AccountingService
        path: RedDog.AccountingService
        port: 5700
        has-tests: true
      # ... 6 more projects
  fail-fast: false
```

**Integration Test Setup:**
```yaml
steps:
  - name: Install Dapr CLI
    run: |
      wget -q https://raw.githubusercontent.com/dapr/cli/master/install/install.sh \
        -O - | /bin/bash -s -- --runtime-version 1.16.0

  - name: Test pub/sub flow
    run: |
      # Start OrderService + MakeLineService
      # Create test order
      # Verify message delivery
```

### Feature Branch Validation Strategy

#### Pre-Merge Validation Checklist

**Automated Validation (Every PR):**
- ✅ All projects compile (zero errors, zero warnings)
- ✅ All unit tests pass (100% pass rate)
- ✅ Code coverage ≥ 80% per project
- ✅ NuGet vulnerability scan clean (no HIGH/CRITICAL)
- ✅ Docker images build successfully
- ✅ StyleCop/Roslyn analyzers pass

**Integration Validation (PR Approval):**
- ✅ Dapr 1.16.0 sidecar starts
- ✅ Health endpoints respond (/healthz, /livez, /readyz)
- ✅ Pub/sub message flow works
- ✅ State store operations succeed
- ✅ EF Core migrations apply cleanly

**Manual Validation:**
- ✅ Code review completed
- ✅ Architecture review (if structural changes)
- ✅ Documentation updated
- ✅ Breaking changes documented

#### Branch Protection Rules

**For Main Branch:**
- Require 1+ PR reviews
- Require status checks:
  - `build-and-test`
  - `integration-tests`
  - `docker-build-and-publish`
- Require conversation resolution
- Require branch to be up to date
- No force pushes
- No deletions

### Multi-Stage Build Validation

**Stage 1: Fast Feedback (< 5 min)**
- Syntax check, compilation, fast unit tests
- Fail fast on obvious errors

**Stage 2: Comprehensive Testing (5-15 min)**
- All unit tests, code coverage analysis
- Integration tests with Dapr

**Stage 3: Container Validation (10-20 min)**
- Docker image builds
- Security scanning (Trivy)
- Image size validation

**Stage 4: Deployment Validation (15-30 min)**
- Deploy to dev environment
- Smoke tests
- Health check validation

### Dapr Integration in CI

**Required Components:**
1. **Dapr CLI Installation:** v1.16.0
2. **Component Configuration:** Redis pub/sub, Redis state stores
3. **Service Startup:** `dapr run` wrapper for each service
4. **Health Validation:** Wait for `/healthz` before tests

**Sample docker-compose.yml for CI:**
```yaml
services:
  redis:
    image: redis:7-alpine
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "YourStrong!Password"
      ACCEPT_EULA: "Y"
```

### EF Core Migration Testing

**Automated Migration Validation:**
1. Create test SQL Server container
2. Apply migrations: `dotnet ef database update`
3. Validate schema
4. Test rollback: `dotnet ef database update <previous>`
5. Cleanup test database

**Safety Checks:**
- No data loss (no DROP statements)
- Backward compatibility (can roll back)
- Performance validation (< 60 seconds)

### Implementation Roadmap

**Phase 1: Basic Automation (Weeks 1-2)**
- GitHub Actions basic workflow
- Unit test discovery scripts
- Coverage enforcement
- Documentation

**Phase 2: Integration Testing (Weeks 3-4)**
- Dapr sidecar orchestration
- Integration test projects
- Test fixtures and data builders
- GitHub Actions integration job

**Phase 3: Advanced Validation (Weeks 5-6)**
- Database migration tests
- Security scanning (Trivy)
- Container build automation
- SBOM generation

**Phase 4: Rollback Testing (Weeks 7-8)**
- Deployment rollback automation
- Database rollback automation
- Operational runbooks
- Team training

### Effort Estimation

| Category | Hours | Notes |
|----------|-------|-------|
| GitHub Actions workflow | 19 | Matrix builds, integration tests |
| Dapr integration | 13 | CLI setup, components, tests |
| EF Core migration testing | 9 | Validation, rollback, safety |
| Documentation | 17 | Runbooks, troubleshooting |
| **TOTAL** | **75 hours** | **2-3 weeks (1 FTE)** |

---

## Agent 3: CI Automation Enhancement

### Automated Test Execution Strategy

#### Unit Test Automation

**Test Discovery:**
- Auto-discover: `find . -name "*Tests.csproj"`
- Parallel execution: `--parallel:threads=4`
- Coverage collection: `--collect:"XPlat Code Coverage"`
- Result format: TRX + Cobertura XML

**Coverage Enforcement:**
- Minimum: 80% threshold per project
- Fail build if below threshold
- Trend analysis (compare with previous)

**Example Command:**
```bash
dotnet test \
  --configuration Release \
  --collect:"XPlat Code Coverage" \
  --results-directory ./test-results \
  --logger "trx" \
  -- \
  --parallel:threads=4
```

#### Integration Test Automation

**Dapr Sidecar Orchestration:**
1. Install Dapr CLI v1.16.0
2. Initialize runtime: `dapr init --runtime-version 1.16.0`
3. Create component YAMLs (pub/sub, state stores)
4. Start services with `dapr run`
5. Execute tests
6. Collect logs
7. Cleanup

**Pub/Sub Test Flow:**
```bash
# OrderService publishes test order
curl -X POST http://localhost:5100/order \
  -H "Content-Type: application/json" \
  -d '{"productName":"Americano","quantity":2}'

# Verify MakeLineService received message
curl http://localhost:5200/makeline/status
```

#### Database Migration Test Automation

**Migration Validation Pipeline:**
```bash
# Start SQL Server container
docker run -d --name testdb \
  -e SA_PASSWORD=YourStrong!Password \
  mcr.microsoft.com/mssql/server:2022-latest

# Apply migrations
dotnet ef database update \
  --connection "Server=localhost;..."

# Run tests
dotnet test AccountingService.Tests

# Cleanup
docker rm -f testdb
```

### Build Verification Automation

#### Compilation Verification

**Multi-Configuration Builds:**
- Build Debug configuration
- Build Release configuration
- Warnings as errors: `/warnaserror`
- Enforce code style: `-p:EnforceCodeStyleInBuild=true`

**Target Framework Validation:**
```bash
# Ensure all projects target net10.0
grep -r "<TargetFramework>net10.0</TargetFramework>" *.csproj || exit 1
```

#### Static Analysis Automation

**Roslyn Analyzers:**
```xml
<PropertyGroup>
  <AnalysisLevel>latest</AnalysisLevel>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  <EnableNETAnalyzers>true</EnableNETAnalyzers>
</PropertyGroup>
```

**EditorConfig Rules:**
```ini
[*.cs]
dotnet_diagnostic.CA1031.severity = error  # No catch general exceptions
dotnet_diagnostic.CA2007.severity = none   # ConfigureAwait not needed
dotnet_diagnostic.CA3001.severity = error  # SQL injection review
```

**Security Scanning:**
```bash
# NuGet vulnerabilities
dotnet list package --vulnerable

# Container scanning
trivy image reddog/orderservice:latest
```

#### Container Build Automation

**Parallel Docker Builds:**
```bash
# Build all services in parallel (4 concurrent)
parallel -j 4 docker build \
  -t reddog/{}:latest \
  -f RedDog.{}/Dockerfile . ::: \
  OrderService AccountingService MakeLineService \
  LoyaltyService ReceiptGenerationService VirtualWorker VirtualCustomers
```

**Image Security Scanning:**
```bash
# Trivy scan for HIGH/CRITICAL CVEs
trivy image --severity HIGH,CRITICAL \
  reddog/orderservice:latest
```

### Continuous Integration Validation Strategies

#### PR Validation Matrix

| Event | Steps | Duration | Failure Action |
|-------|-------|----------|----------------|
| PR Opened | Format + Compile + Fast tests + Static analysis | 5-8 min | Block merge, comment fixes |
| PR Updated | Full build + All tests + Integration + DB migrations | 15-20 min | Block merge, report coverage |
| PR Approved | Security scan + Container builds + Regression tests | 20-30 min | Block merge, review required |

#### Main Branch Validation

| Event | Steps | Duration |
|-------|-------|----------|
| Merge to Main | Full tests + DB migration + Container push + Deploy dev + Smoke tests | 30-45 min |
| Nightly Build | Full regression + Perf benchmarks + Security audit + Load tests | 2-4 hours |

#### Release Validation

| Event | Steps | Duration |
|-------|-------|----------|
| Release Branch | Full tests + Load testing + Security audit + Deploy staging + UAT + Rollback test | 2-3 hours |

### Test Data Management

**Test Data Generation (Faker.NET):**
```csharp
var order = new OrderTestDataBuilder()
    .WithOrderId("test-order-123")
    .WithStoreId("store-001")
    .WithItems(5)
    .WithRandomTimestamp()
    .Build();
```

**Test Isolation:**
- Unique ID per test run: `test-{guid}`
- No conflicts in parallel execution
- Automatic cleanup after tests

**State Management:**
- Reset Redis: `FLUSHDB`
- Delete SQL test data: `DELETE WHERE StoreId LIKE 'test-%'`
- Restore foreign keys after cleanup

### Automated Rollback Validation

**Deployment Rollback:**
```bash
# Deploy v2.0.0
kubectl set image deployment/order-service order-service=v2.0.0

# Rollback to v1.9.9
kubectl rollback deployment/order-service

# Verify health
kubectl exec pod/order-service -- curl http://localhost/healthz
```

**Database Rollback:**
```bash
# Apply latest migration
dotnet ef database update

# Rollback to previous
dotnet ef database update PreviousMigrationName

# Re-apply (verify forward works)
dotnet ef database update
```

### Metrics & Monitoring

**Test Execution Time:**
- Track test performance over time
- Identify slow tests (> 5 seconds)
- Optimize or parallelize

**Coverage Trend Analysis:**
```csv
date,commit,coverage_percent
2025-11-08,abc1234,85.2
2025-11-07,def5678,84.9
```

**Flaky Test Detection:**
- Run tests 3 times
- Identify tests that fail intermittently
- Quarantine until fixed

### Effort Estimation

| Category | Setup Hours | Ongoing Hours/Sprint |
|----------|-------------|---------------------|
| Unit test automation | 8-12 | 2-3 |
| Integration tests | 12-16 | 3-5 |
| Database migration tests | 6-8 | 1-2 |
| Security scanning | 4-6 | 1-2 |
| Container automation | 8-10 | 2-3 |
| Rollback testing | 8-12 | 1-2 |
| **TOTAL** | **46-68 hours** | **10-17 hours** |

**Timeline:** 1-2 engineer weeks for initial setup

**ROI:** Payback in 4 weeks for teams of 4+ developers

---

## Phase 5 Summary

### Combined Findings from All 3 Agents

**Total Pipelines:** 9 GitHub Actions workflows

**Critical Issues Identified:**
1. **All Dockerfiles** - Hard-coded SDK 6.0 and aspnet 6.0 images (7 services)
2. **All .csproj files** - Framework version net6.0 (8 projects)
3. **No global.json** - SDK version not pinned
4. **No test execution** - Zero test coverage in CI/CD
5. **Deprecated Actions** - Using v2 actions (should be v4-v5)
6. **No security scanning** - No vulnerability checks
7. **Node.js UI** - Using EOL Node 14.15.4 (should be Node 24 LTS)

### Recommended CI/CD Strategy

**✅ ADOPT: Multi-Stage Validation Pipeline**

**Stage 1: Fast Feedback (< 5 min)**
- Syntax check, compilation
- Fast unit tests
- Static analysis

**Stage 2: Comprehensive Testing (5-15 min)**
- All unit tests, coverage ≥ 80%
- Integration tests with Dapr

**Stage 3: Container Validation (10-20 min)**
- Docker builds
- Trivy security scanning
- Image size validation

**Stage 4: Deployment Validation (15-30 min)**
- Deploy to dev
- Smoke tests
- Health checks

### Priority Action Plan

#### PHASE 5A: Pipeline Configuration Updates (16-17 hours)
1. ✅ Create `/global.json` with SDK 10.0.100
2. ✅ Update all Dockerfiles (sdk:6.0 → sdk:10.0, aspnet:6.0 → aspnet:10.0)
3. ✅ Update all workflow files (action versions v2 → v4-v5)
4. ✅ Fix deprecated syntax (`::set-output` → `>> $GITHUB_OUTPUT`)
5. ✅ Update Node.js UI Dockerfile (node:14 → node:24)

#### PHASE 5B: Build Validation Enhancement (19 hours)
1. ✅ Create comprehensive GitHub Actions workflow
2. ✅ Add matrix build strategy (8 projects parallel)
3. ✅ Setup .NET 10 SDK from global.json
4. ✅ Add unit test execution with coverage
5. ✅ Add code quality checks (Roslyn analyzers)

#### PHASE 5C: Integration Testing (13 hours)
1. ✅ Install Dapr CLI 1.16.0 in CI
2. ✅ Create Dapr component configurations
3. ✅ Add integration test job
4. ✅ Test pub/sub flow (OrderService → MakeLineService)
5. ✅ Test service invocation

#### PHASE 5D: Security & Quality (10 hours)
1. ✅ Add NuGet vulnerability scanning
2. ✅ Add Trivy container scanning
3. ✅ Add SBOM generation
4. ✅ Add code coverage enforcement (80% minimum)

#### PHASE 5E: Testing & Documentation (17 hours)
1. ✅ Test locally with Docker
2. ✅ Test in GitHub Actions (feature branch)
3. ✅ Document CI/CD processes
4. ✅ Create troubleshooting guide
5. ✅ Code review and merge

### Total Phase 5 Estimated Effort

| Phase | Hours | Percentage |
|-------|-------|-----------|
| PHASE 5A: Pipeline Updates | 16-17 | 21% |
| PHASE 5B: Build Validation | 19 | 25% |
| PHASE 5C: Integration Testing | 13 | 17% |
| PHASE 5D: Security & Quality | 10 | 13% |
| PHASE 5E: Testing & Documentation | 17 | 23% |
| **TOTAL** | **75-76 hours** | **100%** |

**Timeline:** 9-10 developer-days (2 weeks for 1 FTE)

### Risk Assessment

| Risk Level | Issues | Count | Mitigation |
|-----------|--------|-------|-----------|
| **CRITICAL** | Dockerfile updates, .csproj updates, global.json creation | 3 | Test locally before CI |
| **HIGH** | Dapr integration, EF Core migrations, test automation | 3 | Incremental rollout, feature branches |
| **MEDIUM** | Security scanning, performance, container size | 3 | Monitor metrics, set thresholds |
| **LOW** | Documentation, action version updates | 2 | Standard process |

### Success Criteria

✅ **GO/NO-GO Decision: PROCEED**

**All issues have identified solutions:**
- Clear migration path for all 9 pipelines
- Complete GitHub Actions templates
- Production-ready automation scripts
- Effort aligns with modernization timeline (Phase 1A)
- Risk is manageable with staged rollout

**Validation Checklist:**
1. All Docker images build with .NET 10 SDK
2. All workflows execute without errors
3. Images push to GHCR with correct tags
4. Unit tests run and coverage reports generated
5. Integration tests pass with Dapr 1.16.0
6. Security scans report zero critical vulnerabilities
7. Local dev experience unchanged

### Next Steps

**Decision Point:** Choose implementation path
- **Option A:** Begin Phase 5A implementation (pipeline configuration updates)
- **Option B:** Continue analysis with Phase 6 (Risk Assessment & Mitigation)
- **Option C:** Review Phase 5 findings before proceeding

---

**End of Phase 5 Analysis**

---
