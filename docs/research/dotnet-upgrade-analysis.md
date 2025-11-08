# .NET Upgrade Analysis - Red Dog Coffee

**Analysis Date:** 2025-11-06
**Current Framework:** .NET 6.0 (EOL: November 12, 2024)
**Target Framework:** .NET 10.0 (LTS)
**Phase Scope:** Phase 1A - .NET 10 Upgrade (All Services)
**Total Projects in Scope:** 9 (.NET 6 → .NET 10)

## Executive Summary

### Overview

The Red Dog Coffee solution contains **9 .NET projects**, all currently built with **.NET 6.0** (End of Life: November 12, 2024). This document analyzes the upgrade path to **.NET 10 LTS** and provides implementation guidance for Phase 1A of the modernization plan.

### Two-Phase Modernization Strategy

**Phase 1A: .NET 10 Upgrade (This Document's Scope)**
- **Goal:** Upgrade all .NET 6 projects to .NET 10 LTS
- **Projects:** ALL 9 projects (.NET 6 → .NET 10)
- **Timeline:** 1-2 weeks (37-55 hours estimated effort)
- **Deliverable:** Production-ready .NET 10 services, Dapr 1.16 validated, EOL risk eliminated

**Phase 1B: Language Migration (Future Work)**
- **Goal:** Migrate 5 services from .NET 10 to target languages
- **Projects:** MakeLineService, LoyaltyService, ReceiptGenerationService, VirtualWorker, VirtualCustomers
- **Target Languages:** Go 1.23+, Python 3.12+, Node.js 24
- **Timeline:** 2-3 weeks (AFTER Phase 1A completion)
- **Deliverable:** Polyglot architecture with 5 languages

**Final State:**
- **4 services stay in .NET 10**: OrderService, AccountingService, AccountingModel, Bootstrapper
- **5 services migrate to other languages**: MakeLineService (Go), LoyaltyService (Node.js), ReceiptGenerationService (Python), VirtualWorker (Go), VirtualCustomers (Python)

**Strategic Rationale:** The two-phase approach de-risks modernization by separating framework upgrades (Phase 1A) from language migrations (Phase 1B). This allows:
1. Dapr 1.16 validation once in .NET, reused for all language migrations
2. Production deployment of .NET 10 (removing EOL risk) before starting language migrations
3. Language migrations from a modern .NET 10 baseline (better reference than EOL .NET 6)

### Key Technical Changes

| Component | Current (2024) | Target (2025) | Complexity | Notes |
|-----------|---------------|---------------|------------|-------|
| .NET Runtime | 6.0 (EOL) | 10.0 (LTS) | **CRITICAL** | Major version upgrade, 2-generation jump |
| Dapr SDK | 1.5.0 | 1.16.0 | **CRITICAL** | 11 minor versions (May 2022 → Sep 2025), breaking changes |
| Logging | Serilog 4.1.0 | OpenTelemetry native | **HIGH** | Technology replacement (not upgrade), per web-api-standards.md |
| OpenAPI Generation | Swashbuckle 6.2.3 | Microsoft.AspNetCore.OpenApi | **HIGH** | Native .NET OpenAPI generation |
| OpenAPI UI | Swagger UI | Scalar UI | **MEDIUM** | Modern API documentation UI |
| EF Core | 6.0.4 / 5.0.5 | 10.0.x | **MEDIUM** | Major version upgrade, migration generation required |

### Risk Assessment

**Overall Risk Level:** **MEDIUM**

**Low Risk Factors:**
- Simple dependency graph (only 2 internal project references)
- Small package count (10 unique external packages)
- Modern SDK-style project format (100% of projects)
- No legacy `packages.config` or `web.config` files

**Medium Risk Factors:**
- .NET 6 → .NET 10 (skip 2 versions: .NET 7, .NET 8)
- Dapr version lag (11 minor versions, 3+ years behind)
- Technology replacements (Serilog → OpenTelemetry, Swashbuckle → Scalar)

**High Risk Factors:**
- **Deprecated package:** Microsoft.AspNetCore 2.2.0 (6 years EOL, security risk)
- **Framework EOL:** .NET 6 reached EOL November 2024 (no more patches)
- **Dapr breaking changes:** Actor reminders, state store serialization changes

### Effort Estimate

| Phase | Estimated Hours | Risk Level |
|-------|----------------|------------|
| Framework Targeting & Code Adjustments | 33-44h | MEDIUM |
| NuGet & Dependency Management | 37-55h | HIGH |
| CI/CD & Build Pipeline Updates | 8-12h | LOW |
| Testing & Validation | 12-16h | MEDIUM |
| **TOTAL (Phase 1A)** | **90-127h** | **MEDIUM** |

**Timeline:** 2-3 weeks with dedicated team

### Success Criteria

✅ All 9 projects build successfully on .NET 10
✅ All NuGet packages upgraded to .NET 10-compatible versions
✅ Dapr 1.16 SDK validated across all services
✅ Serilog fully replaced with OpenTelemetry native logging
✅ All CI/CD pipelines updated and tested
✅ Integration tests passing
✅ Production deployment successful (phased rollout)

---

## Project Inventory

This section provides a comprehensive catalog of all 9 .NET projects in the Red Dog Coffee solution, their current state, upgrade path, and final destination after both Phase 1A (.NET 10 upgrade) and Phase 1B (language migration) complete.

### All Projects at a Glance

| Project | Type | SDK | Migration Complexity | Phase 1A | Phase 1B | Final State |
|---------|------|-----|---------------------|----------|----------|-------------|
| AccountingModel | Library | Library | Low-Medium | ✅ .NET 6 → .NET 10 | ⛔ No migration | .NET 10 |
| AccountingService | Web API | Web | Medium-High | ✅ .NET 6 → .NET 10 | ⛔ No migration | .NET 10 |
| OrderService | Web API | Web | Medium | ✅ .NET 6 → .NET 10 | ⛔ No migration | .NET 10 |
| Bootstrapper | Console | Console | Medium | ✅ .NET 6 → .NET 10 | ⛔ No migration | .NET 10 |
| MakeLineService | Web API | Web | Medium | ✅ .NET 6 → .NET 10 | ➡️ .NET 10 → Go | Go 1.23+ |
| LoyaltyService | Web API | Web | Medium | ✅ .NET 6 → .NET 10 | ➡️ .NET 10 → Node.js | Node.js 24 |
| ReceiptGenerationService | Web API | Web | Medium | ✅ .NET 6 → .NET 10 | ➡️ .NET 10 → Python | Python 3.12+ |
| VirtualWorker | Web API | Web | Medium | ✅ .NET 6 → .NET 10 | ➡️ .NET 10 → Go | Go 1.23+ |
| VirtualCustomers | Console | Console | Medium | ✅ .NET 6 → .NET 10 | ➡️ .NET 10 → Python | Python 3.12+ |

---

### Current vs Target State Comparison

| Aspect | Current (2024) | Target (2025) | Gap |
|--------|---------------|---------------|-----|
| .NET Runtime | 6.0 (EOL) | 10.0 (LTS) | Major version upgrade required |
| Dapr SDK | 1.5.0 | 1.16+ | 11 minor versions behind |
| EF Core | 6.0.4 | 10.0.x | Major version upgrade required |
| Logging | Serilog 4.1.0 | OpenTelemetry native logging | **REMOVE and replace** (per web-api-standards.md) |
| OpenAPI Generation | Swashbuckle.AspNetCore 6.2.3 | Microsoft.AspNetCore.OpenApi | Replace with native .NET OpenAPI generation |
| OpenAPI UI | Swagger UI (via Swashbuckle) | Scalar UI | Replace Swagger UI with Scalar per web-api-standards.md |

---

### Complex Projects - Detailed Analysis

The following 3 projects require special attention due to unique dependencies, deprecated packages, or EF Core version mismatches.

#### AccountingService
**Path:** `/home/ahmedmuhi/code/reddog-code/RedDog.AccountingService/RedDog.AccountingService.csproj`
**Type:** ASP.NET Core Web API
**Current Framework:** net6.0
**Final State:** .NET 10 (staying in .NET)

**Dependencies:**
- `Dapr.AspNetCore` 1.5.0
- `Dapr.Extensions.Configuration` 1.5.0
- `Microsoft.EntityFrameworkCore.Design` 6.0.4
- `Serilog.AspNetCore` 4.1.0
- `Swashbuckle.AspNetCore` 6.2.3

**Project References:**
- RedDog.AccountingModel

**Special Considerations:**
- **EF Core Migration Required:** Uses EF Core 6.0.4, requires migration generation for 10.0.x upgrade
- **SQL Server Integration:** Only service accessing SQL database (via AccountingModel)
- **Blocking I/O Operations:** Contains synchronous EF Core queries (lines 38, 81 in AccountingController.cs) that should be converted to async
- **Dapr Configuration API:** Uses Dapr.Extensions.Configuration for app settings (stays in .NET 10)

**Upgrade Priority:** HIGH (database migrations, EF Core upgrade, sync-to-async conversions)

#### Bootstrapper
**Path:** `/home/ahmedmuhi/code/reddog-code/RedDog.Bootstrapper/RedDog.Bootstrapper.csproj`
**Type:** Console Application (Database Initialization)
**Current Framework:** net6.0
**Final State:** .NET 10 (staying in .NET)

**Dependencies:**
- `Dapr.Client` 1.5.0 (NOT Dapr.AspNetCore)
- `Microsoft.EntityFrameworkCore.Design` 5.0.5

**Project References:**
- RedDog.AccountingModel

**Special Considerations:**
- **Older EF Core Version:** Uses EF Core 5.0.5 (vs 6.0.4 in AccountingModel/AccountingService)
- **Dapr.Client Usage:** Uses Dapr.Client directly (not Dapr.AspNetCore like other services)
- **Database Migrations:** Runs EF Core migrations for AccountingService SQL database initialization
- **Deployment:** Runs as Kubernetes Job (not Deployment) - see manifests/branch/base/deployments/bootstrapper.yaml

**Upgrade Priority:** CRITICAL (EF Core version mismatch, must align with AccountingService/AccountingModel at 10.0.x)

#### VirtualCustomers
**Path:** `/home/ahmedmuhi/code/reddog-code/RedDog.VirtualCustomers/RedDog.VirtualCustomers.csproj`
**Type:** Console Application (Load Generator)
**Current Framework:** net6.0
**Final State:** Python 3.12+ (Phase 1B migration)

**Dependencies:**
- `Dapr.AspNetCore` 1.5.0
- **`Microsoft.AspNetCore` 2.2.0** (DEPRECATED - EOL Dec 2019)
- `Microsoft.Extensions.Hosting` 6.0.0
- `Microsoft.Extensions.Http` 6.0.0
- `Serilog.AspNetCore` 4.1.0
- `Serilog.Extensions.Hosting` 4.2.0
- `Serilog.Sinks.Console` 4.0.0

**Project References:** None

**Special Considerations:**
- **Deprecated Package:** Microsoft.AspNetCore 2.2.0 is 6 years EOL - MUST remove before upgrade
- **Most Complex Dependency Profile:** Uses 7 external packages (vs 2-5 for other projects)
- **Sync-Over-Async Anti-Pattern:** Contains `.Wait()` blocking call (line 263 in VirtualCustomers.cs)
- **Migration Target:** Will be migrated to Python 3.12+ in Phase 1B (FROM .NET 10 version)

**Upgrade Priority:** CRITICAL (remove deprecated Microsoft.AspNetCore 2.2.0 FIRST, then upgrade to .NET 10)

---

### Project Dependency Graph

```
RedDog Solution (9 projects)
├── RedDog.AccountingModel (foundation library)
│   └── No dependencies
│
├── RedDog.AccountingService (Web API)
│   └── Depends on: RedDog.AccountingModel
│
├── RedDog.Bootstrapper (Console App - Database Init)
│   └── Depends on: RedDog.AccountingModel
│
├── 6 other services (no project dependencies)
    └── MakeLineService, LoyaltyService, ReceiptGenerationService,
        OrderService, VirtualWorker, VirtualCustomers
```

**Dependency Depth:** Shallow (1 level deep)
**Coupling Level:** Minimal (only 2 project references: AccountingService → AccountingModel, Bootstrapper → AccountingModel)

---

## Dependency Analysis

### Executive Summary

The Red Dog Coffee solution consists of 8 .NET projects, all using .NET 6.0 (which reached End of Life on November 12, 2024). The dependency landscape is relatively clean with minimal external packages, but **all packages are outdated** and the current framework itself is EOL.

**Upgrade Complexity Assessment:** **MEDIUM**
- Simple dependency graph (only 1 internal project reference)
- Small number of external dependencies (6 unique packages)
- All packages have clear upgrade paths to .NET 10
- One deprecated package identified (Microsoft.AspNetCore 2.2.0)

---

### Complete NuGet Package Inventory

**Note:** This inventory covers all 9 .NET projects in the solution. All projects are in scope for Phase 1A (.NET 10 upgrade).

#### By Package (Unique Packages Across Solution)

| Package Name | Current Versions Used | Latest Stable (Approx) | Status | Notes |
|--------------|----------------------|------------------------|--------|-------|
| **Dapr.AspNetCore** | 1.5.0 (7 projects) | 1.16.0 | OUTDATED | Core dependency, major version updates available |
| **Dapr.Client** | 1.5.0 (1 project - Bootstrapper) | 1.16.0 | OUTDATED | Used by Bootstrapper instead of Dapr.AspNetCore |
| **Dapr.Extensions.Configuration** | 1.5.0 (1 project) | 1.16.0 | OUTDATED | Complements Dapr.AspNetCore |
| **Microsoft.EntityFrameworkCore.Design** | 6.0.4 (2 projects), 5.0.5 (1 project - Bootstrapper) | 10.0.x | OUTDATED | Bootstrapper uses older v5.0.5, others use v6.0.4 |
| **Microsoft.EntityFrameworkCore.SqlServer** | 6.0.4 (1 project) | 6.0.36 / 8.0.11 / 9.0.0 | OUTDATED | Patch updates available |
| **Serilog.AspNetCore** | 4.1.0 (7 projects) | **REMOVE** | **REPLACE** | Replace with OpenTelemetry.Exporter.OpenTelemetryProtocol (per web-api-standards.md) |
| **Serilog.Extensions.Hosting** | 4.2.0 (1 project) | **REMOVE** | **REPLACE** | Replace with OpenTelemetry native logging |
| **Serilog.Sinks.Console** | 4.0.0 (1 project) | **REMOVE** | **REPLACE** | Replace with OpenTelemetry native logging |
| **Swashbuckle.AspNetCore** | 6.2.3 (3 projects) | 7.2.0 | OUTDATED | Minor version updates available |
| **Microsoft.Extensions.Hosting** | 6.0.0 (1 project) | 6.0.2 / 8.0.1 / 9.0.0 | OUTDATED | Patch and major updates available |
| **Microsoft.Extensions.Http** | 6.0.0 (1 project) | 6.0.1 / 8.0.1 / 9.0.0 | OUTDATED | Patch and major updates available |
| **Microsoft.AspNetCore** | 2.2.0 (1 project) | N/A | DEPRECATED | .NET Core 2.2 EOL Dec 2019 |

---

### Project-by-Project Dependency Breakdown

#### 1. RedDog.AccountingModel
**Location:** `/home/ahmedmuhi/code/reddog-code/RedDog.AccountingModel/RedDog.AccountingModel.csproj`
**Type:** Class Library
**Current Framework:** net6.0

**External Dependencies:**
- Microsoft.EntityFrameworkCore.Design 6.0.4
- Microsoft.EntityFrameworkCore.SqlServer 6.0.4

**Project References:** None

**Notes:** Foundation project for EF Core data models. No dependencies on other internal projects.

---

#### 2. RedDog.AccountingService
**Location:** `/home/ahmedmuhi/code/reddog-code/RedDog.AccountingService/RedDog.AccountingService.csproj`
**Type:** Web API
**Current Framework:** net6.0

**External Dependencies:**
- Dapr.AspNetCore 1.5.0
- Dapr.Extensions.Configuration 1.5.0
- Microsoft.EntityFrameworkCore.Design 6.0.4
- Serilog.AspNetCore 4.1.0
- Swashbuckle.AspNetCore 6.2.3

**Project References:**
- RedDog.AccountingModel

**Notes:** Only service with a project reference dependency. Consumes EF Core models from AccountingModel project.

---

#### 3. RedDog.MakeLineService
**Location:** `/home/ahmedmuhi/code/reddog-code/RedDog.MakeLineService/RedDog.MakeLineService.csproj`
**Type:** Web API
**Current Framework:** net6.0

**External Dependencies:**
- Dapr.AspNetCore 1.5.0
- Serilog.AspNetCore 4.1.0
- Swashbuckle.AspNetCore 6.2.3

**Project References:** None

**Notes:** Planned for migration to Go in modernization plan.

---

#### 4. RedDog.LoyaltyService
**Location:** `/home/ahmedmuhi/code/reddog-code/RedDog.LoyaltyService/RedDog.LoyaltyService.csproj`
**Type:** Web API
**Current Framework:** net6.0

**External Dependencies:**
- Dapr.AspNetCore 1.5.0
- Serilog.AspNetCore 4.1.0

**Project References:** None

**Notes:** Minimal dependencies. Planned for migration to Node.js in modernization plan.

---

#### 5. RedDog.ReceiptGenerationService
**Location:** `/home/ahmedmuhi/code/reddog-code/RedDog.ReceiptGenerationService/RedDog.ReceiptGenerationService.csproj`
**Type:** Web API
**Current Framework:** net6.0

**External Dependencies:**
- Dapr.AspNetCore 1.5.0
- Serilog.AspNetCore 4.1.0

**Project References:** None

**Notes:** Minimal dependencies. Planned for migration to Python in modernization plan.

---

#### 6. RedDog.OrderService
**Location:** `/home/ahmedmuhi/code/reddog-code/RedDog.OrderService/RedDog.OrderService.csproj`
**Type:** Web API
**Current Framework:** net6.0

**External Dependencies:**
- Dapr.AspNetCore 1.5.0
- Serilog.AspNetCore 4.1.0
- Swashbuckle.AspNetCore 6.2.3

**Project References:** None

**Notes:** Core service staying in .NET. Clean dependency profile.

---

#### 7. RedDog.VirtualWorker
**Location:** `/home/ahmedmuhi/code/reddog-code/RedDog.VirtualWorker/RedDog.VirtualWorker.csproj`
**Type:** Web API
**Current Framework:** net6.0

**External Dependencies:**
- Dapr.AspNetCore 1.5.0
- Serilog.AspNetCore 4.1.0

**Project References:** None

**Notes:** Minimal dependencies. Planned for migration to Go in modernization plan.

---

#### 8. RedDog.VirtualCustomers
**Location:** `/home/ahmedmuhi/code/reddog-code/RedDog.VirtualCustomers/RedDog.VirtualCustomers.csproj`
**Type:** Console Application (Exe)
**Current Framework:** net6.0

**External Dependencies:**
- Dapr.AspNetCore 1.5.0
- Microsoft.AspNetCore 2.2.0 (DEPRECATED)
- Microsoft.Extensions.Hosting 6.0.0
- Microsoft.Extensions.Http 6.0.0
- Serilog.AspNetCore 4.1.0
- Serilog.Extensions.Hosting 4.2.0
- Serilog.Sinks.Console 4.0.0

**Project References:** None

**Notes:** Most complex dependency profile. Contains deprecated Microsoft.AspNetCore 2.2.0 package (EOL Dec 2019). Planned for migration to Python.

#### 9. RedDog.Bootstrapper
**Location:** `/home/ahmedmuhi/code/reddog-code/RedDog.Bootstrapper/RedDog.Bootstrapper.csproj`
**Type:** Console Application (Database Initialization)
**Current Framework:** net6.0

**External Dependencies:**
- Dapr.Client 1.5.0 (NOT Dapr.AspNetCore)
- Microsoft.EntityFrameworkCore.Design 5.0.5 (older version than AccountingModel/AccountingService)

**Project References:**
- RedDog.AccountingModel

**Notes:** Runs EF Core migrations for AccountingService database initialization. Uses older EF Core 5.0.5 (needs upgrade to 10.0.x). Staying in .NET permanently.

---

### Project Dependency Graph

```
RedDog Solution (9 projects)
├── RedDog.AccountingModel (foundation library)
│   └── No dependencies
│
├── RedDog.AccountingService (Web API)
│   └── Depends on: RedDog.AccountingModel
│
├── RedDog.Bootstrapper (Console App - Database Init)
│   └── Depends on: RedDog.AccountingModel
│
├── RedDog.MakeLineService (Web API)
│   └── No project dependencies
│
├── RedDog.LoyaltyService (Web API)
│   └── No project dependencies
│
├── RedDog.ReceiptGenerationService (Web API)
│   └── No project dependencies
│
├── RedDog.OrderService (Web API)
│   └── No project dependencies
│
├── RedDog.VirtualWorker (Web API)
│   └── No project dependencies
│
└── RedDog.VirtualCustomers (Console App)
    └── No project dependencies
```

**Depth:** Shallow (1 level deep)
**Coupling:** Minimal (only 2 project references: AccountingService → AccountingModel, Bootstrapper → AccountingModel)

---

### Upgrade Path Assessment

#### For .NET 10 Migration (Per Modernization Plan)

##### Phase 1A: ALL Projects Upgrade to .NET 10 (9 projects)

**Reference:** See "Phase 1A vs Phase 1B Scope Clarification" (earlier in document) for project list and phase definitions.

**Required Package Upgrades (All Projects):**

| Package | Current | Target (.NET 10) | Complexity |
|---------|---------|------------------|------------|
| Dapr.AspNetCore / Dapr.Client | 1.5.0 | 1.16.0 | LOW - Additive changes |
| Serilog.AspNetCore (ALL Serilog packages) | 4.1.0 | **REMOVE** → OpenTelemetry native logging | **HIGH** - Technology replacement (web-api-standards.md) |
| Swashbuckle.AspNetCore | 6.2.3 | Replace with Microsoft.AspNetCore.OpenApi + Scalar | MEDIUM - API migration |
| Microsoft.EntityFrameworkCore.* | 6.0.4 / 5.0.5 | 10.0.x | MEDIUM - Breaking changes across major versions |
| Microsoft.Extensions.* | 6.0.0 | 10.0.x | LOW - Mostly additive |

##### Phase 1B: Language Migration (5 projects AFTER .NET 10 upgrade)

After successful .NET 10 deployment, these projects will migrate to other languages:

**Go Migrations:**
- MakeLineService (.NET 10 → Go 1.23+)
- VirtualWorker (.NET 10 → Go 1.23+)

**Python Migrations:**
- ReceiptGenerationService (.NET 10 → Python 3.12+)
- VirtualCustomers (.NET 10 → Python 3.12+)

**Node.js Migration:**
- LoyaltyService (.NET 10 → Node.js 24)

**Strategic Note:** These services will be migrated FROM their .NET 10 versions (not from outdated .NET 6 code). The .NET 10 baseline ensures:
- Dapr 1.16 compatibility is already validated
- Modern framework patterns are used as reference
- Git history preserves .NET 10 versions for comparison

---

# NuGet & Dependency Management

**Context:** This section focuses on NuGet hygiene for the .NET 10 upgrade—package compatibility, dependency alignment, and clean project metadata so the platform changes land on a stable base.
**Analysis Date:** 2025-11-06
**Effort Estimate:** 37-55 hours
**Risk Level:** HIGH
**Scope:** All 9 .NET projects (.NET 6 → .NET 10 upgrade path)

## Package Compatibility Analysis

The dependency assessment concentrates on three areas: (1) direct package compatibility, (2) shared dependency alignment across services, and (3) transitive/legacy artifacts that could block the .NET 10 jump.

### Package Inventory Summary

**Total Unique Packages:** 10 direct dependencies across 9 projects

**Package Distribution:**
- Dapr.AspNetCore (1.5.0) - 7 projects
- Dapr.Client (1.5.0) - 1 project (Bootstrapper)
- Serilog.AspNetCore (4.1.0) - 7 projects
- Swashbuckle.AspNetCore (6.2.3) - 3 projects
- Microsoft.EntityFrameworkCore.Design (6.0.4, 5.0.5) - 3 projects
- Microsoft.EntityFrameworkCore.SqlServer (6.0.4) - 1 project
- Dapr.Extensions.Configuration (1.5.0) - 1 project
- Microsoft.AspNetCore (2.2.0) - 1 project (DEPRECATED)
- Microsoft.Extensions.Hosting (6.0.0) - 1 project
- Microsoft.Extensions.Http (6.0.0) - 1 project
- Serilog.Extensions.Hosting (4.2.0) - 1 project
- Serilog.Sinks.Console (4.0.0) - 1 project

### Compatibility & Project Format Review

| Project | Format | Status |
|---------|--------|--------|
| `/home/ahmedmuhi/code/reddog-code/RedDog.AccountingModel/RedDog.AccountingModel.csproj` | SDK-style + PackageReference | ✅ Modern |
| `/home/ahmedmuhi/code/reddog-code/RedDog.LoyaltyService/RedDog.LoyaltyService.csproj` | SDK-style + PackageReference | ✅ Modern |
| `/home/ahmedmuhi/code/reddog-code/RedDog.VirtualCustomers/RedDog.VirtualCustomers.csproj` | SDK-style + PackageReference | ✅ Modern |
| `/home/ahmedmuhi/code/reddog-code/RedDog.VirtualWorker/RedDog.VirtualWorker.csproj` | SDK-style + PackageReference | ✅ Modern |
| `/home/ahmedmuhi/code/reddog-code/RedDog.ReceiptGenerationService/RedDog.ReceiptGenerationService.csproj` | SDK-style + PackageReference | ✅ Modern |

- 100% of projects already use SDK-style `.csproj` files—no ToolsVersion/`<ProjectGuid>` cleanup required.
- No `packages.config`, `app.config`, `web.config`, or other legacy config artifacts were found anywhere in the repository.

### Deprecated Package Removal

**Issue:** `RedDog.VirtualCustomers` references an outdated ASP.NET Core package.

**File:** `/home/ahmedmuhi/code/reddog-code/RedDog.VirtualCustomers/RedDog.VirtualCustomers.csproj`

```xml
<PackageReference Include="Microsoft.AspNetCore" Version="2.2.0" />
```

**Problem:**
- `Microsoft.AspNetCore 2.2.0` is an EOL metapackage from .NET Core 2.2 (support ended December 23, 2019).
- Modern SDK-style projects already include ASP.NET Core assemblies; this metapackage causes restore noise and will not exist for .NET 10.

**Recommendation:** Remove the package reference entirely. If specific namespaces were needed, replace with targeted `Microsoft.Extensions.*` packages instead.

### Solution File Check

- `RedDog.sln` still carries standard Visual Studio GUID entries. This is expected and requires no action.
- All eight `.csproj` files referenced in the solution exist—no orphaned or missing projects.

## Modern NuGet Practices

### Central Package Management (Optional)

**Current State:** Each project pins package versions independently.

**Opportunity:** Introduce `Directory.Packages.props` (plus optional `Directory.Build.*`) to manage versions centrally.

```xml
<!-- Directory.Packages.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Dapr.AspNetCore" Version="1.16.0" />
    <PackageVersion Include="Serilog.AspNetCore" Version="10.0.0" />
  </ItemGroup>
</Project>
```

After adoption, individual projects drop the `Version` attribute from `<PackageReference>` entries. Implement this once the .NET 10 upgrade stabilizes to avoid churn.

### SDK Version Pinning (Recommended)

Although the Platform Upgrade section already specifies a `global.json`, reiterate here that NuGet workflows depend on consistent SDK resolution. Create `/home/ahmedmuhi/code/reddog-code/global.json` during the upgrade so restores/builds always use .NET SDK `10.0.100` with the desired roll-forward policy.

### NuGet Hygiene Checklist

- Run `dotnet list package --outdated` and `--include-transitive` during each milestone to surface dependency drift.
- Add `dotnet list package --vulnerable` to CI to catch known CVEs when the Serilog → OpenTelemetry change removes the older packages.
- Align Dapr packages (AspNetCore, Client, Extensions.Configuration) on 1.16.0 simultaneously to avoid cross-version runtime mismatches.

## Migration Recommendations

1. **Remove `Microsoft.AspNetCore 2.2.0` immediately** (VirtualCustomers).
2. **Create `global.json`** while retargeting to .NET 10 so developers/CI restore identical SDKs.
3. **Plan central package management** after the framework upgrade to simplify ongoing maintenance.

## Summary

| Category | Status | Details |
|----------|--------|---------|
| Package Management Format | ✅ Excellent | 100% modern `PackageReference` usage |
| Project File Format | ✅ Excellent | All SDK-style `.csproj` files |
| Legacy Config Files | ✅ None found | No `packages.config`, `web.config`, etc. |
| Legacy Packages | ⚠️ 1 issue | `Microsoft.AspNetCore 2.2.0` in VirtualCustomers |
| Solution File | ✅ Good | All projects referenced; no cleanup needed |
| Modern Practices | ⚠️ Opportunities | Central package management, SDK pinning, vulnerability scanning |

Overall the solution is well-positioned for .NET 10: modern project formats, minimal legacy baggage, and a single deprecated package to remove before the Dapr/OpenTelemetry refresh.

## Action Items

| Task | Priority | Effort | Timing |
|------|----------|--------|--------|
| Remove `Microsoft.AspNetCore 2.2.0` from VirtualCustomers | High | <15 minutes | Before .NET 10 retargeting |
| Add `global.json` for SDK pinning | Medium | <15 minutes | During platform upgrade |
| Enable nullable reference types & implicit usings | Medium | Per project | Tracked in Platform Upgrade guide |
| Implement central package management | Low | 1-2 hours | After .NET 10 upgrade stabilizes |
| Add NuGet vulnerability scanning to CI | Low | 0.5 hour | With CI/CD modernization |

## Report Metadata

**Generated:** 2025-11-06
**Analysis Tool:** Claude Code (Haiku 4.5)
**Projects Analyzed:** 8
**Assessment Stage:** Dependency analysis

---

# Platform Upgrade Implementation Guide

**Context:** This section walks through upgrading every service onto the .NET 10 platform—runtime targeting, hosting model, logging/observability plumbing, container images, and language features—so the modernization work has a consistent baseline.
**Analysis Date:** 2025-11-06
**Effort Estimate:** 33-44 hours
**Risk Level:** MEDIUM
**Strategy:** Direct .NET 6 → .NET 10 upgrade (skipping intermediate versions)
**Modern Features:** Nullable reference types, Implicit usings, File-scoped namespaces, Top-level statements

---

## Framework Targeting Setup

### Target Framework Selection

**Current Configuration (All 9 Projects):**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">  <!-- or Microsoft.NET.Sdk -->
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>
</Project>
```

**Recommended .NET 10 Configuration:**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">  <!-- or Microsoft.NET.Sdk -->
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>14.0</LangVersion>
  </PropertyGroup>
</Project>
```

### Dockerfile Base Image Updates

**Current Images:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
```

**Required .NET 10 Images:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
```

**Notes:**
- Ubuntu 24.04 LTS is the default base OS for .NET 10 images (complies with ADR-0003)
- VirtualCustomers and Bootstrapper should use `runtime:10.0` instead of `aspnet:10.0` (console apps, not web)

### global.json Recommendation

**Create new file:** `/home/ahmedmuhi/code/reddog-code/global.json`

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestMinor",
    "allowPrerelease": false
  }
}
```

**Benefits:**
- Ensures consistent builds across developer machines
- Prevents accidental use of preview/RC builds
- CI/CD reproducibility

---

## Modernization Checklist

### Hosting & Logging Modernization

- **Legacy hosting model:** All six web services rely on `IHostBuilder` + `Startup.cs`. Migrate to the minimal hosting model (`var builder = WebApplication.CreateBuilder(args);`) and remove the Startup files (~100-200 lines per service).
- **Serilog removal:** Replace Serilog with OpenTelemetry native logging + OTLP exporter per `web-api-standards.md`. Packages to add: `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Instrumentation.AspNetCore`. Detailed add/remove steps live in the NuGet & Dependency Management section.

### Health Check Standardization

All services currently expose custom `ProbesController` endpoints. Replace them with ASP.NET Core health checks and Kubernetes-standard endpoints:

```csharp
builder.Services.AddHealthChecks().AddDaprHealthCheck("dapr-sidecar");

app.MapHealthChecks("/healthz");   // General health
app.MapHealthChecks("/livez");     // Liveness probe
app.MapHealthChecks("/readyz");    // Readiness probe
```

Removing the six `ProbesController.cs` files eliminates ~78 lines of duplicated code.

### Language Features & Project-Wide Settings

#### Nullable Reference Types

- **Current state:** Disabled in every project.
- **Estimated warnings:** 50-80 in models, 20-30 in controllers, 5-10 in infrastructure.
- **Approach:** Enable `<Nullable>enable</Nullable>`, fix high-priority warnings (models), then controllers, and finally treat warnings as errors via `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
- **Focus areas:** Uninitialized strings/collections, LINQ methods returning nullable results.

#### Implicit Usings

- **Current state:** Each file carries 8-12 explicit `using` directives.
- **Benefit:** Enabling `<ImplicitUsings>enable</ImplicitUsings>` cuts using statements by ~50%.
- **Auto-imported namespaces:** `System`, `System.Collections.Generic`, `System.Linq`, `System.Threading`, `System.Threading.Tasks`, `Microsoft.AspNetCore.*`, `Microsoft.Extensions.*`.
- **Still required:** Dapr namespaces and project-specific namespaces.

#### Modern C# Feature Opportunities

1. **Collection expressions (C# 12):** Replace `new List<T>()` with `[]` (9 occurrences across 7 files).
2. **Primary constructors (C# 12):** 13 controllers can convert constructor parameters directly to fields.
3. **File-scoped namespaces (C# 10):** Apply to all 74 source files.
4. **Required members (C# 11):** 18 model classes should mark mandatory properties as `required`.

---

## Async Pattern Conversion

### Overview

The codebase already follows async best practices—only three blocking scenarios remain. Strategy: convert all I/O and service-to-service calls to async while keeping trivial operations synchronous.

### High-Priority Fixes

#### RedDog.AccountingService — Synchronous EF Core Queries

**Files:** `Controllers/AccountingController.cs`

- **Line 38 – UpdateMetrics():**
  ```csharp
  // Before (BLOCKING)
  Customer customer = dbContext.Customers.SingleOrDefault(c => c.LoyaltyId == orderSummary.LoyaltyId);
  
  // After (ASYNC)
  Customer customer = await dbContext.Customers.SingleOrDefaultAsync(c => c.LoyaltyId == orderSummary.LoyaltyId);
  ```
- **Line 81 – MarkOrderComplete():**
  ```csharp
  // Before (BLOCKING)
  Order order = dbContext.Orders.SingleOrDefault<Order>(o => o.OrderId == orderSummary.OrderId);
  
  // After (ASYNC)
  Order order = await dbContext.Orders.SingleOrDefaultAsync(o => o.OrderId == orderSummary.OrderId);
  ```
- **Impact:** Eliminates thread-pool blocking on every pub/sub message.
- **Effort:** ~2 hours.

#### RedDog.VirtualCustomers — Sync-over-Async Shutdown

**File:** `VirtualCustomers.cs:263`

```csharp
// Before
stoppingToken.Register(() => {
    _ordersTask.Wait();  // BLOCKING!
});

// After
stoppingToken.Register(() => {
    _ordersTask.GetAwaiter().GetResult();  // Better exception propagation
});
```

- **Impact:** Prevents shutdown hangs; limited to termination path.
- **Effort:** ~1 hour.

### Low-Priority Optimizations

#### Unnecessary Task.FromResult (6 files)

All `ProbesController` files wrap simple responses in `Task.FromResult`. Remove the async wrappers after health-check modernization:

```csharp
// Before
[HttpGet("ready")]
public async Task<IActionResult> IsReady()
{
    return await Task.FromResult(Ok());
}

// After
[HttpGet("ready")]
public IActionResult IsReady()
{
    return Ok();
}
```

- **Impact:** Minimal (probes run infrequently).
- **Effort:** ~5 hours across six files.

### Current Async Usage Assessment

✅ **EXCELLENT patterns:** All Dapr APIs, EF Core queries, file I/O, HTTP client calls, and background services already use async correctly.
✅ **No `ConfigureAwait(false)` or `async void` abuses.
❌ **Two blocking EF Core queries** remain (addressed above).

| Project | Priority | Effort | Files | Key Changes |
|---------|----------|--------|-------|-------------|
| AccountingService | 🔴 HIGH | 2h | 1 | Convert 2 EF Core sync queries |
| VirtualCustomers | 🟡 MEDIUM | 1h | 1 | Replace `.Wait()` with `.GetAwaiter().GetResult()` |
| OrderService | 🟢 LOW | 1h | 1 | Remove `Task.FromResult` |
| MakeLineService | 🟢 LOW | 1h | 1 | Remove `Task.FromResult` |
| LoyaltyService | 🟢 LOW | 1h | 1 | Remove `Task.FromResult` |
| ReceiptGenerationService | 🟢 LOW | 1h | 1 | Remove `Task.FromResult` |
| VirtualWorker | 🟢 LOW | 1h | 1 | Remove `Task.FromResult` |
| AccountingModel | ⚪ N/A | 0h | 0 | Entity models only |

**Total async optimization effort:** ~7 hours.

---

## Implementation Summary

### Critical Findings

1. All 9 projects must retarget to `net10.0`.
2. Six web services still rely on the legacy hosting model.
3. One obsolete package reference (Microsoft.AspNetCore 2.2.0).
4. Two blocking EF Core queries in AccountingService.
5. 74 source files ready for file-scoped namespaces.
6. 13 controllers ready for primary constructors.
7. 18 model classes need nullable annotations.

### Modern Features Adoption

| Feature | Current State | Target State | Impact | Effort |
|---------|--------------|--------------|--------|--------|
| Framework Version | net6.0 | net10.0 | High | Low |
| Hosting Model | IHostBuilder + Startup.cs | WebApplication.CreateBuilder | High | Medium |
| Nullable Reference Types | Disabled | Enabled | High | High |
| Implicit Usings | Disabled | Enabled | Low | Low |
| File-Scoped Namespaces | Block-scoped | File-scoped | Low | Low |
| Primary Constructors | Traditional | Primary | Low | Low |
| Collection Expressions | `new List<T>()` | `[]` | Low | Low |
| Async I/O | 97% async | 100% async | Medium | Low |

### Estimated Total Effort

| Task Category | Effort (Hours) |
|---------------|----------------|
| Framework Upgrade | 0.5 |
| Hosting Model Migration | 8-12 |
| Nullable Reference Types | 12-16 |
| Modern C# Features | 5-8 |
| Async Conversions | 7 |
| **Total** | **33-44 hours** |

### Recommended Priority Order

1. **Critical:** Update TargetFramework to `net10.0`, migrate to minimal hosting, and fix blocking EF queries.
2. **High:** Enable nullable reference types, implicit usings, and file-scoped namespaces.
3. **Medium:** Adopt primary constructors, collection expressions, and remove `Task.FromResult`.
4. **Low:** Add required members and consolidate global usings.

---

# CI/CD & Build Pipeline Modernization

Detailed CI/CD pipeline analysis, modernization recommendations, and workflow templates now live in `docs/research/cicd-modernization.md`. Review that document when planning GitHub Actions or Azure DevOps updates for the .NET 10 rollout.

---

# Breaking Change Analysis (.NET 6 → .NET 10)

**Analysis Date:** 2025-11-08
**Objective:** Identify deprecated APIs, breaking changes, and required code refactorings for .NET 6 → .NET 10 upgrade by analyzing the actual Red Dog codebase.

**Methodology:** Three specialized agents analyzed the codebase:
1. **API Deprecation Detection** - Scanned all Program.cs, Startup.cs, and Controllers for current API usage patterns
2. **API Replacement Strategy** - Identified required refactorings and modern .NET 10 patterns
3. **Critical Flows Mapping** - Mapped all endpoints, pub/sub flows, state operations, and test scenarios

---

## Executive Summary

**Critical Discovery:** All 7 Red Dog services use the **deprecated .NET 6 IHostBuilder + Startup.cs pattern**. Migration to .NET 10 **WebApplicationBuilder minimal APIs** is required for all services.

**Good News:** All **Dapr SDK APIs are fully compatible** with .NET 10 - no breaking changes in Dapr.AspNetCore 1.5.0 → 1.16.0 upgrade path.

**Total Effort Estimate:** **41-44 hours** (5-6 developer days)

### Key Metrics

- **Services Analyzed:** 7 (AccountingService, OrderService, MakeLineService, LoyaltyService, ReceiptGenerationService, VirtualWorker, VirtualCustomers)
- **Program.cs Files:** 7 (all use deprecated pattern)
- **Startup.cs Files:** 6 (all must be eliminated)
- **API Endpoints:** 18 REST endpoints identified
- **Pub/Sub Topics:** 2 (orders, ordercompleted)
- **State Stores:** 2 (reddog.state.makeline, reddog.state.loyalty)
- **Service Invocations:** 4 Dapr service-to-service calls
- **Database Tables:** 3 (Customer, Order, OrderItem)

### Breaking Changes by Priority

| Priority | Change Required | Services Affected | Effort | Blocks Upgrade |
|----------|----------------|-------------------|--------|----------------|
| **HIGH** | Program.cs refactoring (IHostBuilder → WebApplicationBuilder) | 7 services | 20h | No (but strongly recommended) |
| **HIGH** | Health endpoints (/probes/* → /healthz, /livez, /readyz) | 6 services | 17-23h | No (ADR-0005 compliance) |
| **MEDIUM** | OpenAPI (Swashbuckle → Built-in + Scalar) | 3 services | 3h | No |
| **MEDIUM** | EF Core 10 upgrade + compiled model regeneration | AccountingService | 4h | No |
| **MEDIUM** | Dapr package updates (1.5.0 → 1.16.0) | 7 services | 3.5h | No |
| **MEDIUM** | Serilog configuration modernization | 7 services | 6.5h | No |

**Total:** 41-44 hours

---

## Section 1: API Deprecation Detection Report

### 1.1 Deprecated Hosting Pattern (ALL SERVICES)

**Finding:** All 7 services use the deprecated .NET 6 `IHostBuilder` + `Startup.cs` pattern.

**Current Pattern (Example from OrderService):**

**Program.cs:**
```csharp
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        });
```

**Startup.cs:**
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddHttpClient();
    services.AddControllers().AddDapr();
    services.AddSwaggerGen(/* ... */);
}

public void Configure(IApplicationBuilder app)
{
    app.UseSwagger();
    app.UseRouting();
    app.UseCloudEvents();
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapSubscribeHandler();
        endpoints.MapControllers();
    });
}
```

**Issue:** While still supported in .NET 10, this pattern is considered **legacy**. .NET 6+ introduced minimal hosting with `WebApplicationBuilder`, and .NET 10 templates no longer generate `Startup.cs`.

**Impact:** **All 7 services** must refactor to modern pattern

**Files Affected:**
- `/home/ahmedmuhi/code/reddog-code/RedDog.OrderService/Program.cs` (lines 40-46)
- `/home/ahmedmuhi/code/reddog-code/RedDog.OrderService/Startup.cs` (entire file)
- `/home/ahmedmuhi/code/reddog-code/RedDog.AccountingService/Program.cs` (lines 48-74)
- `/home/ahmedmuhi/code/reddog-code/RedDog.AccountingService/Startup.cs` (entire file)
- `/home/ahmedmuhi/code/reddog-code/RedDog.MakeLineService/Program.cs` (lines 40-46)
- `/home/ahmedmuhi/code/reddog-code/RedDog.MakeLineService/Startup.cs` (entire file)
- `/home/ahmedmuhi/code/reddog-code/RedDog.LoyaltyService/Program.cs` (lines 40-46)
- `/home/ahmedmuhi/code/reddog-code/RedDog.LoyaltyService/Startup.cs` (entire file)
- `/home/ahmedmuhi/code/reddog-code/RedDog.ReceiptGenerationService/Program.cs` (lines 39-45)
- `/home/ahmedmuhi/code/reddog-code/RedDog.ReceiptGenerationService/Startup.cs` (entire file)
- `/home/ahmedmuhi/code/reddog-code/RedDog.VirtualWorker/Program.cs` (lines 40-46)
- `/home/ahmedmuhi/code/reddog-code/RedDog.VirtualWorker/Startup.cs` (entire file)
- `/home/ahmedmuhi/code/reddog-code/RedDog.VirtualCustomers/Program.cs` (lines 28-50)

**Recommended Action:** Migrate all services to `WebApplicationBuilder` pattern during .NET 10 upgrade

---

### 1.2 Deprecated Dapr Secret Store Extension (AccountingService)

**Finding:** AccountingService uses deprecated `AddDaprSecretStore()` extension method.

**Current Code (AccountingService Program.cs:68):**
```csharp
var daprClient = new DaprClientBuilder().Build();
var secretDescriptors = new List<DaprSecretDescriptor>
{
    new DaprSecretDescriptor("reddog-sql")
};
config.AddDaprSecretStore(SecretStoreName, secretDescriptors, daprClient);
```

**Package:** `Dapr.Extensions.Configuration` v1.5.0

**Issue:** `AddDaprSecretStore()` is **deprecated in Dapr SDK 1.8+**. Dapr team recommends using Dapr Configuration API instead (ADR-0004).

**Impact:** AccountingService only

**Recommended Action:** Replace with Dapr Configuration API per ADR-0004 during Program.cs refactoring

---

### 1.3 Non-Standard Health Endpoint Paths (6 SERVICES)

**Finding:** All services with health checks use custom `/probes/healthz` and `/probes/ready` endpoints via ProbesController.

**Current Implementation (Example from OrderService ProbesController.cs):**
```csharp
[Route("[controller]")]  // Results in /probes/...
[ApiController]
public class ProbesController : ControllerBase
{
    [HttpGet("ready")]
    public async Task<IActionResult> IsReady()
    {
        return await Task.FromResult(Ok());
    }

    [HttpGet("healthz")]
    public async Task<IActionResult> IsHealthy()
    {
        var response = await _httpClient.GetAsync($"http://localhost:{DaprHttpPort}/v1.0/healthz");
        return new StatusCodeResult((int)response.StatusCode);
    }
}
```

**Issue:** Non-standard route prefix `/probes/`. **ADR-0005 mandates Kubernetes-standard health probe paths:**
- `/healthz` - Overall health
- `/livez` - Liveness probe
- `/readyz` - Readiness probe

**Impact:** All 6 HTTP services (OrderService, MakeLineService, LoyaltyService, AccountingService, VirtualWorker, ReceiptGenerationService)

**Files Affected:**
- `/home/ahmedmuhi/code/reddog-code/RedDog.OrderService/Controllers/ProbesController.cs`
- `/home/ahmedmuhi/code/reddog-code/RedDog.MakeLineService/Controllers/ProbesController.cs`
- `/home/ahmedmuhi/code/reddog-code/RedDog.LoyaltyService/Controllers/ProbesController.cs`
- `/home/ahmedmuhi/code/reddog-code/RedDog.AccountingService/Controllers/ProbesController.cs`
- `/home/ahmedmuhi/code/reddog-code/RedDog.VirtualWorker/Controllers/ProbesController.cs`
- `/home/ahmedmuhi/code/reddog-code/RedDog.ReceiptGenerationService/Controllers/ProbesController.cs` (if exists)

**Recommended Action:** Delete ProbesController.cs, use built-in ASP.NET Core health check middleware with ADR-0005 paths

---

### 1.4 Swashbuckle vs Built-in OpenAPI (3 SERVICES)

**Finding:** 3 services use Swashbuckle.AspNetCore 6.2.3 for OpenAPI/Swagger.

**Current Code (Example from OrderService Startup.cs:33-36):**
```csharp
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "RedDog.OrderService", Version = "v1" });
});

// ...

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "RedDog.OrderService v1"));
```

**Issue:** .NET 9+ includes **built-in OpenAPI support** via `Microsoft.AspNetCore.OpenApi`. Swashbuckle is now optional.

**Impact:** OrderService, MakeLineService, AccountingService

**Recommended Action:** Consider migrating to built-in OpenAPI + Scalar UI (modern alternative to Swagger UI)

---

### 1.5 Dapr SDK API Compatibility Analysis

**Finding:** All Dapr APIs are **fully compatible** with .NET 10. No breaking changes identified.

**APIs Verified:**

| API | Current Usage | .NET 10 Compatible | Action Required |
|-----|--------------|-------------------|-----------------|
| `PublishEventAsync<T>()` | OrderService, MakeLineService | ✅ Yes | Update package only (1.5.0 → 1.16.0) |
| `[Topic(pubsub, topic)]` attribute | 5 services | ✅ Yes | Update package only |
| `GetStateEntryAsync<T>()` | MakeLineService, LoyaltyService | ✅ Yes | Update package only |
| `TrySaveAsync()` (StateEntry) | MakeLineService, LoyaltyService | ✅ Yes | Update package only |
| `InvokeMethodAsync<T>()` | VirtualWorker, VirtualCustomers | ✅ Yes | Update package only |
| `CreateInvokeMethodRequest<T>()` | VirtualCustomers | ✅ Yes | Update package only |
| `InvokeBindingAsync<T>()` | ReceiptGenerationService | ✅ Yes | Update package only |
| `StateOptions` | MakeLineService, LoyaltyService | ✅ Yes | No changes needed |
| `MapSubscribeHandler()` | All pub/sub services | ✅ Yes | Syntax update for minimal APIs |
| `UseCloudEvents()` | All Dapr services | ✅ Yes | Middleware order verification |

**ETag Concurrency Pattern (Verified Compatible):**

MakeLineService and LoyaltyService both use optimistic concurrency with ETags:

```csharp
StateOptions _stateOptions = new StateOptions()
{
    Concurrency = ConcurrencyMode.FirstWrite,
    Consistency = ConsistencyMode.Eventual
};

StateEntry<List<OrderSummary>> state;
bool isSuccess;

do
{
    state = await _daprClient.GetStateEntryAsync<List<OrderSummary>>(storeName, key);
    state.Value ??= new List<OrderSummary>();
    state.Value.Add(orderSummary);
    isSuccess = await state.TrySaveAsync(_stateOptions);
} while(!isSuccess);
```

**Analysis:** This pattern is **fully supported** in Dapr SDK 1.16 for .NET 10. No code changes required.

---

### 1.6 EF Core API Compatibility Analysis

**Finding:** EF Core APIs are compatible with .NET 10, but packages must be upgraded.

**Current Usage (AccountingService Startup.cs:36-40):**
```csharp
services.AddDbContext<AccountingContext>(options =>
{
    options.UseModel(RedDog.AccountingModel.AccountingContextModel.Instance);  // Compiled model
    options.UseSqlServer(Configuration["reddog-sql"]);
});
```

**Package Versions:**
- Current: `Microsoft.EntityFrameworkCore.SqlServer` v6.0.4
- Target: `Microsoft.EntityFrameworkCore.SqlServer` v10.0.0

**APIs Verified Compatible:**
- `AddDbContext<T>()` - ✅ Compatible
- `UseModel()` - ✅ Compatible (compiled models supported in EF Core 10)
- `UseSqlServer()` - ✅ Compatible
- `SaveChangesAsync()` - ✅ Compatible
- `DbSet<T>` operations - ✅ Compatible
- EF Functions (`EF.Functions.DateDiffSecond()`, `DateFromParts()`) - ✅ Compatible

**Action Required:**
1. Update EF Core packages: 6.0.4 → 10.0.0
2. Regenerate compiled models: `dotnet ef dbcontext optimize`

---

## Section 2: API Replacement Strategy Report

### 2.1 Program.cs Minimal API Refactoring

**Pattern:** All 7 services must migrate from `IHostBuilder` + `Startup.cs` to `WebApplicationBuilder` minimal API pattern.

**BEFORE (.NET 6 Pattern):**

**Program.cs:**
```csharp
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        });
```

**Startup.cs:**
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddControllers().AddDapr();
        services.AddSwaggerGen(/* ... */);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(/* ... */);
        app.UseSerilogRequestLogging();
        app.UseRouting();
        app.UseCloudEvents();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapSubscribeHandler();
            endpoints.MapControllers();
        });
    }
}
```

**AFTER (.NET 10 Minimal API Pattern):**

**Program.cs (all-in-one):**
```csharp
using Dapr.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

// Service registration
builder.Services.AddHttpClient();
builder.Services.AddControllers().AddDapr();
builder.Services.AddOpenApi();  // Built-in OpenAPI (replaces Swashbuckle)
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddCheck("dapr", async () =>
    {
        var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:3500/v1.0/healthz");
        return response.IsSuccessStatusCode
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Dapr sidecar not ready");
    }, tags: new[] { "ready" });

var app = builder.Build();

// Middleware
app.UseSerilogRequestLogging();
app.UseRouting();
app.UseCloudEvents();
app.UseAuthorization();

// Health endpoints (ADR-0005)
app.MapHealthChecks("/healthz");
app.MapHealthChecks("/livez", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
app.MapHealthChecks("/readyz", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// OpenAPI
app.MapOpenApi();  // Serves /openapi/v1.json
app.MapScalarApiReference();  // Serves /scalar/v1

// Dapr
app.MapSubscribeHandler();
app.MapControllers();

app.Run();
```

**Files to Modify:** All 7 Program.cs files
**Files to Delete:** All 6 Startup.cs files (VirtualCustomers doesn't have Startup.cs)
**Effort:** 3-4 hours per service (20 hours total)

---

### 2.2 Health Endpoint Refactoring (ADR-0005 Compliance)

**Current Implementation:** Custom `ProbesController` with `/probes/healthz` and `/probes/ready` endpoints.

**Target Implementation:** Built-in ASP.NET Core health check middleware with ADR-0005 paths.

**Example - AccountingService (with Database Check):**

**BEFORE (.NET 6):**

**ProbesController.cs:**
```csharp
[HttpGet("ready")]
public async Task<IActionResult> IsReady([FromServices] AccountingContext dbContext)
{
    if(!isReady)
    {
        try
        {
            if(await dbContext.Orders.CountAsync() >= 0 &&
               await dbContext.OrderItems.CountAsync() >= 0 &&
               await dbContext.Customers.CountAsync() >= 0)
            {
                isReady = true;
            }
        }
        catch(Exception e)
        {
            _logger.LogWarning(e, "Readiness probe failure.");
        }
        return new StatusCodeResult(503);
    }
    return Ok();
}
```

**AFTER (.NET 10):**

**Program.cs:**
```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddCheck("dapr", async () =>
    {
        var client = new HttpClient();
        var response = await client.GetAsync("http://localhost:3500/v1.0/healthz");
        return response.IsSuccessStatusCode
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Dapr sidecar not ready");
    }, tags: new[] { "ready" })
    .AddDbContextCheck<AccountingContext>("database", tags: new[] { "ready" });

app.MapHealthChecks("/healthz");
app.MapHealthChecks("/livez", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
app.MapHealthChecks("/readyz", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

**Benefits:**
- Standardized Kubernetes health probe paths
- Built-in health check framework (no custom controllers)
- Tag-based health check filtering (liveness vs readiness)
- EF Core DbContext health check included

**Files to Delete:** All ProbesController.cs files (6 services)
**Effort:** Included in Program.cs refactoring (17-23 hours total for all services)

---

### 2.3 OpenAPI Configuration Refactoring

**Current Pattern:** Swashbuckle.AspNetCore 6.2.3 (3 services)
**Target Pattern:** Microsoft.AspNetCore.OpenApi + Scalar.AspNetCore

**BEFORE (.NET 6 - Swashbuckle):**

**Startup.cs:**
```csharp
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "RedDog.OrderService", Version = "v1" });
});

// ...

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "RedDog.OrderService v1"));
```

**.csproj:**
```xml
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.2.3" />
```

**AFTER (.NET 10 - Built-in OpenAPI + Scalar):**

**Program.cs:**
```csharp
builder.Services.AddOpenApi();

// ...

app.MapOpenApi();  // Serves OpenAPI spec at /openapi/v1.json
app.MapScalarApiReference();  // Serves interactive UI at /scalar/v1
```

**.csproj:**
```xml
<!-- Remove Swashbuckle -->
<!-- <PackageReference Include="Swashbuckle.AspNetCore" Version="6.2.3" /> -->

<!-- Add Scalar -->
<PackageReference Include="Scalar.AspNetCore" Version="1.2.42" />
<!-- Microsoft.AspNetCore.OpenApi is included in .NET 10 SDK -->
```

**Services Affected:** OrderService, MakeLineService, AccountingService
**Effort:** 1 hour per service (included in Program.cs refactoring)

---

### 2.4 Dapr Configuration Refactoring (AccountingService)

**Current Pattern:** Dapr Secret Store via deprecated extension

**BEFORE (.NET 6):**

**Program.cs:**
```csharp
Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        var connectionString = Environment.GetEnvironmentVariable("reddog-sql");
        if (connectionString != null)
        {
            config.AddInMemoryCollection(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("reddog-sql", connectionString)
            });
        }
        else
        {
            var daprClient = new DaprClientBuilder().Build();
            var secretDescriptors = new List<DaprSecretDescriptor>
            {
                new DaprSecretDescriptor("reddog-sql")
            };
            config.AddDaprSecretStore(SecretStoreName, secretDescriptors, daprClient);  // DEPRECATED
        }
    })
```

**AFTER (.NET 10):**

**Program.cs:**
```csharp
var builder = WebApplication.CreateBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("reddog-sql");
if (connectionString != null)
{
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
    {
        ["reddog-sql"] = connectionString
    });
}
else
{
    var daprClient = new DaprClientBuilder().Build();
    var secretDescriptors = new List<DaprSecretDescriptor>
    {
        new DaprSecretDescriptor("reddog-sql")
    };
    builder.Configuration.AddDaprSecretStore("reddog.secretstore", secretDescriptors, daprClient);
}
```

**Note:** `AddDaprSecretStore()` API is unchanged in Dapr.Extensions.Configuration 1.16, but the hosting pattern changes.

**Alternative (ADR-0004):** Use Dapr Configuration API instead of secret store extension (future enhancement).

---

### 2.5 EF Core Compiled Model Regeneration

**Current Implementation:** RedDog.AccountingModel uses EF Core 6 compiled models

**Files:**
- `RedDog.AccountingModel/CompiledModels/AccountingContextModel.cs`
- `RedDog.AccountingModel/CompiledModels/AccountingContextModelBuilder.cs`
- `RedDog.AccountingModel/CompiledModels/CustomerEntityType.cs`
- `RedDog.AccountingModel/CompiledModels/OrderEntityType.cs`
- `RedDog.AccountingModel/CompiledModels/OrderItemEntityType.cs`
- `RedDog.AccountingModel/CompiledModels/StoreLocationEntityType.cs`

**Action Required:**

1. **Update Packages:**
   ```xml
   <!-- BEFORE -->
   <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="6.0.4" />
   <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="6.0.4" />

   <!-- AFTER -->
   <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.0" />
   <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />
   ```

2. **Regenerate Compiled Models:**
   ```bash
   cd /home/ahmedmuhi/code/reddog-code/RedDog.AccountingModel
   dotnet ef dbcontext optimize --project RedDog.AccountingModel.csproj
   ```

3. **Verify Usage in AccountingService:**
   ```csharp
   builder.Services.AddDbContext<AccountingContext>(options =>
   {
       options.UseModel(RedDog.AccountingModel.AccountingContextModel.Instance);
       options.UseSqlServer(builder.Configuration["reddog-sql"]);
   });
   ```

**Effort:** 2 hours (package update, regeneration, testing)

---

## Section 3: Critical Flows Mapping for Regression Testing

### 3.1 REST API Endpoint Inventory

**Total Endpoints:** 18 across 6 services

#### OrderService (4 endpoints)

| Endpoint | Method | Request | Response | Priority | File:Line |
|----------|--------|---------|----------|----------|-----------|
| /order | POST | CustomerOrder | - | **CRITICAL** | OrderController.cs:31 |
| /product | GET | - | List\<Product\> | **HIGH** | ProductController.cs:22 |
| /probes/ready | GET | - | 200 OK | HIGH | ProbesController.cs:24 |
| /probes/healthz | GET | - | 200/503 | HIGH | ProbesController.cs:30 |

#### MakeLineService (5 endpoints)

| Endpoint | Method | Request | Response | Priority | File:Line |
|----------|--------|---------|----------|----------|-----------|
| /orders | POST | OrderSummary | 200 OK | **CRITICAL** | MakelineController.cs:34 |
| /orders/{storeId} | GET | - | List\<OrderSummary\> | **HIGH** | MakelineController.cs:63 |
| /orders/{storeId}/{orderId} | DELETE | - | 200 OK | **CRITICAL** | MakelineController.cs:70 |
| /probes/ready | GET | - | 200 OK | MEDIUM | ProbesController.cs:24 |
| /probes/healthz | GET | - | 200/503 | MEDIUM | ProbesController.cs:29 |

#### AccountingService (9 endpoints)

| Endpoint | Method | Request | Response | Priority | File:Line |
|----------|--------|---------|----------|----------|-----------|
| /accounting/orders | POST | OrderSummary | 200 OK | **CRITICAL** | AccountingController.cs:33 |
| /accounting/ordercompleted | POST | OrderSummary | 200 OK | **HIGH** | AccountingController.cs:76 |
| /Orders/{period}/{timeSpan} | GET | storeId | OrdersTimeSeries | MEDIUM | AccountingController.cs:96 |
| /Corp/Stores | GET | - | List\<string\> | MEDIUM | AccountingController.cs:136 |
| /Corp/SalesProfit/PerStore | GET | - | List\<SalesProfitMetric\> | MEDIUM | AccountingController.cs:149 |
| /Corp/SalesProfit/Total | GET | - | List\<SalesProfitMetric\> | MEDIUM | AccountingController.cs:226 |
| /OrderMetrics | GET | storeId | List\<OrderMetric\> | MEDIUM | AccountingController.cs:296 |
| /probes/ready | GET | - | 200/503 | HIGH | ProbesController.cs:26 |
| /probes/healthz | GET | - | 200/503 | MEDIUM | ProbesController.cs:51 |

**Total Critical Endpoints:** 4 (POST /order, POST /orders, DELETE /orders/{storeId}/{orderId}, POST /accounting/orders)

---

### 3.2 Pub/Sub Message Flow Mapping

**Flow Diagram:**

```
VirtualCustomers (Background Service)
    ↓ Dapr Service Invocation: POST /order
OrderService
    ↓ PublishEventAsync: "reddog.pubsub" / "orders"
Topic: "orders"
    ↓ ↓ ↓ ↓
    MakeLineService        LoyaltyService         AccountingService       ReceiptGenerationService
    POST /orders           POST /loyalty/orders   POST /accounting/orders POST /orders
    ↓                      ↓                      ↓                       ↓
    SaveStateAsync         SaveStateAsync         SaveChangesAsync        InvokeBindingAsync
    (reddog.state.makeline)(reddog.state.loyalty) (SQL Server)           (blob storage)

VirtualWorker (Background Service)
    ↓ Dapr Service Invocation: GET /orders/{storeId}
MakeLineService
    ↓ Returns List<OrderSummary>
VirtualWorker
    ↓ Simulates work (Task.Delay), DELETE /orders/{storeId}/{orderId}
MakeLineService
    ↓ PublishEventAsync: "reddog.pubsub" / "ordercompleted"
Topic: "ordercompleted"
    ↓
    AccountingService
    POST /accounting/ordercompleted
    ↓
    SaveChangesAsync (updates CompletedDate)
```

#### Publisher 1: OrderService → "orders" topic

**Code:** OrderController.cs:40
```csharp
await _daprClient.PublishEventAsync<OrderSummary>(PubSubName, OrderTopic, orderSummary);
```

**Message Schema (OrderSummary):**
```csharp
public class OrderSummary
{
    public Guid OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? OrderCompletedDate { get; set; }
    public string StoreId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string LoyaltyId { get; set; }
    public List<OrderItemSummary> OrderItems { get; set; }
    public decimal OrderTotal { get; set; }
}

public class OrderItemSummary
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal UnitPrice { get; set; }
    public string ImageUrl { get; set; }
}
```

#### Publisher 2: MakeLineService → "ordercompleted" topic

**Code:** MakelineController.cs:87
```csharp
await _daprClient.PublishEventAsync<OrderSummary>(PubSubName, OrderCompletedTopic, order);
```

**Message:** Same OrderSummary schema, with `OrderCompletedDate` populated

#### Subscribers (4 services)

| Service | Topic | Code Location | Processing |
|---------|-------|---------------|------------|
| MakeLineService | orders | MakelineController.cs:33 | SaveStateAsync to reddog.state.makeline |
| LoyaltyService | orders | LoyaltyController.cs:28 | Calculate points, SaveStateAsync to reddog.state.loyalty |
| AccountingService | orders | AccountingController.cs:32 | Insert Order + OrderItems to SQL Server |
| ReceiptGenerationService | orders | ReceiptGenerationController.cs:26 | InvokeBindingAsync to blob storage |
| AccountingService | ordercompleted | AccountingController.cs:75 | Update Order.CompletedDate in SQL |

---

### 3.3 State Store Operations

#### MakeLineService (reddog.state.makeline)

| Operation | Key Pattern | Value Type | Code Location |
|-----------|------------|------------|---------------|
| GetStateEntryAsync | `storeId` (e.g., "Redmond") | List\<OrderSummary\> | MakelineController.cs:132 |
| TrySaveAsync | `storeId` | List\<OrderSummary\> | MakelineController.cs:49, 104 |

**ETag Concurrency:**
```csharp
StateOptions _stateOptions = new StateOptions()
{
    Concurrency = ConcurrencyMode.FirstWrite,  // Optimistic concurrency
    Consistency = ConsistencyMode.Eventual
};

StateEntry<List<OrderSummary>> state;
bool isSuccess;

do
{
    state = await GetAllOrdersAsync(orderSummary.StoreId);
    state.Value ??= new List<OrderSummary>();
    state.Value.Add(orderSummary);
    isSuccess = await state.TrySaveAsync(_stateOptions);
} while(!isSuccess);  // Retry on ETag mismatch
```

#### LoyaltyService (reddog.state.loyalty)

| Operation | Key Pattern | Value Type | Code Location |
|-----------|------------|------------|---------------|
| GetStateEntryAsync | `loyaltyId` (e.g., "1") | LoyaltySummary | LoyaltyController.cs:44 |
| TrySaveAsync | `loyaltyId` | LoyaltySummary | LoyaltyController.cs:54 |

**LoyaltySummary Schema:**
```csharp
public class LoyaltySummary
{
    public string LoyaltyId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int PointsEarned { get; set; }
    public int PointTotal { get; set; }
}
```

**Points Calculation:** `(int)Math.Round(orderSummary.OrderTotal * 10, 0)`

---

### 3.4 Service Invocation Patterns

#### VirtualCustomers → OrderService

**Invocations:**
1. GET /product (retrieve product catalog)
   - Code: VirtualCustomers.cs:273
   ```csharp
   _products = await _daprClient.InvokeMethodAsync<List<Product>>(HttpMethod.Get, "order-service", "product", stoppingToken);
   ```

2. POST /order (place order)
   - Code: VirtualCustomers.cs:332-333
   ```csharp
   var request = _daprClient.CreateInvokeMethodRequest<CustomerOrder>("order-service", "order", order);
   var response = await _daprClient.InvokeMethodWithResponseAsync(request);
   ```

#### VirtualWorker → MakeLineService

**Invocations:**
1. GET /orders/{storeId} (retrieve orders to complete)
   - Code: VirtualWorkerController.cs:99
   ```csharp
   return await _daprClient.InvokeMethodAsync<List<OrderSummary>>(HttpMethod.Get, "make-line-service", $"orders/{StoreId}");
   ```

2. DELETE /orders/{storeId}/{orderId} (mark order complete)
   - Code: VirtualWorkerController.cs:112
   ```csharp
   await _daprClient.InvokeMethodAsync<OrderSummary>(HttpMethod.Delete, "make-line-service", $"orders/{orderSummary.StoreId}/{orderSummary.OrderId}", orderSummary);
   ```

---

### 3.5 Database Operations (AccountingService)

**DbContext:** AccountingContext
**Connection String Source:** `Configuration["reddog-sql"]` (from Dapr secret store or env var)

**Entities/Tables:**

1. **Customer** (LoyaltyId PK, FirstName, LastName)
2. **Order** (OrderId PK, StoreId, PlacedDate, CompletedDate?, Customer FK, OrderTotal)
3. **OrderItem** (OrderItemId PK, OrderId FK, ProductId, ProductName, Quantity, UnitCost, UnitPrice, ImageUrl)
4. **StoreLocation** (defined but not actively used)

**Operations:**

| Operation | Method | Code Location | Description |
|-----------|--------|---------------|-------------|
| Upsert Customer | SingleOrDefault + Add | AccountingController.cs:38-44 | Find or create customer by LoyaltyId |
| Insert Order | Add | AccountingController.cs:68 | Add new order with items |
| Insert OrderItems | Add (via Order.OrderItems) | AccountingController.cs:55-66 | Add items as part of order |
| Update Order.CompletedDate | SingleOrDefault + SaveChangesAsync | AccountingController.cs:81-90 | Mark order complete |
| Query Orders (time series) | LINQ grouping | AccountingController.cs:103-127 | Order counts by minute |
| Query Orders (stores) | Distinct | AccountingController.cs:139-143 | Unique store IDs |
| Query OrderItems (sales/profit) | LINQ join + grouping | AccountingController.cs:152-223 | Sales and profit by store/hour |
| Query Orders (metrics) | LINQ join + grouping | AccountingController.cs:299-370 | Order metrics with fulfillment time |

**Compiled Model Usage:**
```csharp
options.UseModel(RedDog.AccountingModel.AccountingContextModel.Instance);
```

---

### 3.6 Critical Test Scenarios

#### Scenario 1: End-to-End Order Flow (HIGHEST PRIORITY)

**Test:** Complete order placement and processing through all services

**Steps:**
1. VirtualCustomers → GET /product (OrderService) → receive product list
2. VirtualCustomers → POST /order (OrderService) → create order
3. OrderService → PublishEventAsync to "orders" topic
4. MakeLineService receives message → SaveStateAsync (reddog.state.makeline)
5. LoyaltyService receives message → calculate points, SaveStateAsync (reddog.state.loyalty)
6. AccountingService receives message → Insert Order + OrderItems to SQL
7. ReceiptGenerationService receives message → InvokeBindingAsync to blob storage
8. VirtualWorker → GET /orders/{storeId} (MakeLineService)
9. VirtualWorker simulates work → DELETE /orders/{storeId}/{orderId}
10. MakeLineService → PublishEventAsync to "ordercompleted" topic
11. AccountingService receives message → Update Order.CompletedDate

**Validation Points:**
- OrderService returns 200 OK
- All 4 subscribers receive message within 5 seconds
- Order exists in MakeLineService Redis state
- Loyalty points updated in Redis state
- Order + OrderItems stored in SQL Server
- Receipt blob created in storage
- After completion: order removed from MakeLine state, CompletedDate populated in SQL

**Success Criteria:**
- All steps complete without errors
- POST /order response time < 500ms (p95)
- Message delivery latency < 5 seconds (p99)
- Data consistency: OrderTotal in SQL matches sum of OrderItems

**Effort:** 12 hours

---

#### Scenario 2: State Store Concurrency (HIGH PRIORITY)

**Test:** Concurrent loyalty point updates with ETag optimistic concurrency

**Steps:**
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
- Logs show retry

**Effort:** 4 hours

---

#### Scenario 3: Database Schema Validation (HIGH PRIORITY)

**Test:** Verify EF Core schema matches expected structure

**Steps:**
1. Drop database RedDogAccounting (if exists)
2. Start AccountingService (will create schema via EF Core)
3. Query INFORMATION_SCHEMA.TABLES → verify Customer, Order, OrderItem, StoreLocation exist
4. Query INFORMATION_SCHEMA.COLUMNS → verify Order table columns
5. Query foreign keys → verify OrderItem.OrderId → Order.OrderId, Order.LoyaltyId → Customer.LoyaltyId

**Validation:**
- All 4 tables exist
- Customer.LoyaltyId is PK (nvarchar(36))
- Order.OrderId is PK (uniqueidentifier)
- OrderItem.OrderItemId is PK (int, IDENTITY)
- Decimal columns have (18,2) precision

**Effort:** 2 hours

---

#### Scenario 4: Service Invocation (MEDIUM PRIORITY)

**Test:** VirtualWorker → MakeLineService communication via Dapr

**Validation:**
- Service invocation succeeds (no Dapr errors)
- Request/response serialization correct (List\<OrderSummary\> marshaled properly)
- Dapr mTLS working (if enabled)
- HTTP status codes propagated correctly (200, 404, 500)

**Effort:** 3 hours

---

#### Scenario 5: API Backward Compatibility (MEDIUM PRIORITY)

**Test:** Ensure API contracts unchanged after .NET 10 upgrade

**Steps:**
1. Before upgrade: Export OpenAPI schemas from all services
2. After upgrade: Export OpenAPI schemas again
3. Compare schemas using JSON diff tool
4. Verify no breaking changes

**Validation:**
- All endpoints present in .NET 6 also present in .NET 10
- No additional required properties in request DTOs
- No removed properties in response DTOs
- Property types unchanged

**Effort:** 2 hours

---

### 3.7 Regression Test Priority Matrix

| Scenario | Priority | Complexity | Effort (hours) | Blocks Upgrade |
|----------|----------|------------|----------------|----------------|
| E2E Order Flow | CRITICAL | High | 12 | Yes |
| State Store Concurrency | HIGH | Medium | 4 | No |
| Database Schema Validation | HIGH | Low | 2 | Yes |
| Service Invocation | MEDIUM | Low | 3 | No |
| API Backward Compatibility | MEDIUM | Low | 2 | No |
| Health Endpoints | MEDIUM | Low | 3 | No |
| **Total** | - | - | **26** | - |

**Minimum Viable Testing (MVP - 16 hours):**
1. E2E Order Flow (12 hours)
2. Database Schema Validation (2 hours)
3. API Backward Compatibility (2 hours)

---

## Section 4: Service-Specific Refactoring Checklists

### 4.1 OrderService Refactoring Checklist

**Total Effort:** 5-6 hours

**Tasks:**

- [ ] **Program.cs Refactoring** (3h)
  - Convert from `IHostBuilder` + `Startup.cs` to `WebApplicationBuilder` minimal API
  - Migrate service registrations from Startup.ConfigureServices to builder.Services
  - Migrate middleware from Startup.Configure to app pipeline

- [ ] **Health Endpoints** (included above)
  - Delete ProbesController.cs
  - Add `AddHealthChecks()` with "self" (live) and "dapr" (ready) checks
  - Map `/healthz`, `/livez`, `/readyz` endpoints

- [ ] **OpenAPI Configuration** (included above)
  - Remove Swashbuckle.AspNetCore package (6.2.3)
  - Add Scalar.AspNetCore package (1.2.42)
  - Replace AddSwaggerGen/UseSwagger/UseSwaggerUI with AddOpenApi/MapOpenApi/MapScalarApiReference

- [ ] **Dapr Integration** (0.5h)
  - Update Dapr.AspNetCore package: 1.5.0 → 1.16.0
  - Verify PublishEventAsync still works (no code changes needed)

- [ ] **Serilog Configuration** (1h)
  - Move from global `Log.Logger` to `builder.Services.AddSerilog()`

- [ ] **CORS Configuration** (included)
  - Move from Startup.cs to Program.cs
  - Verify AllowAnyOrigin/AllowAnyHeader/AllowAnyMethod still appropriate

- [ ] **Testing** (1h)
  - Test health endpoints: /healthz, /livez, /readyz
  - Test OpenAPI spec: /openapi/v1.json
  - Test Scalar UI: /scalar/v1
  - Test order creation via POST /order
  - Test Dapr pub/sub publishing

**Files to Modify:**
- `/home/ahmedmuhi/code/reddog-code/RedDog.OrderService/Program.cs` - Complete rewrite
- `/home/ahmedmuhi/code/reddog-code/RedDog.OrderService/RedDog.OrderService.csproj` - Package updates

**Files to Delete:**
- `/home/ahmedmuhi/code/reddog-code/RedDog.OrderService/Startup.cs`
- `/home/ahmedmuhi/code/reddog-code/RedDog.OrderService/Controllers/ProbesController.cs`

---

### 4.2 MakeLineService Refactoring Checklist

**Total Effort:** 5-6 hours

**Tasks:**

- [ ] **Program.cs Refactoring** (3h)
- [ ] **Health Endpoints** (included)
- [ ] **OpenAPI Configuration** (included)
- [ ] **Dapr Integration** (0.5h)
  - Update Dapr.AspNetCore: 1.5.0 → 1.16.0
  - Verify [Topic] subscription (MakelineController.cs:33)
  - Verify GetStateEntryAsync + TrySaveAsync (optimistic concurrency)
  - Verify PublishEventAsync (OrderCompletedTopic)
- [ ] **Serilog Configuration** (1h)
- [ ] **Testing** (1h)
  - Test health endpoints
  - Test OpenAPI/Scalar
  - Test GET /orders/{storeId}
  - Test [Topic] subscription (publish to "orders", verify state updated)
  - Test DELETE /orders/{storeId}/{orderId} (verify OrderCompleted published)
  - Test state store concurrency

**Files to Modify:**
- `/home/ahmedmuhi/code/reddog-code/RedDog.MakeLineService/Program.cs`
- `/home/ahmedmuhi/code/reddog-code/RedDog.MakeLineService/RedDog.MakeLineService.csproj`

**Files to Delete:**
- `/home/ahmedmuhi/code/reddog-code/RedDog.MakeLineService/Startup.cs`
- `/home/ahmedmuhi/code/reddog-code/RedDog.MakeLineService/Controllers/ProbesController.cs`

---

### 4.3 AccountingService Refactoring Checklist

**Total Effort:** 7-8 hours

**Tasks:**

- [ ] **Program.cs Refactoring** (3h)
  - Convert from `IHostBuilder` + `Startup.cs` to `WebApplicationBuilder`
  - Migrate Dapr secret store configuration
  - Move AddDbContext from Startup to Program.cs

- [ ] **Health Endpoints** (included)
  - Delete ProbesController.cs
  - Add health checks with Dapr + Database checks

- [ ] **OpenAPI Configuration** (included)

- [ ] **Dapr Integration** (0.5h)
  - Update Dapr.AspNetCore: 1.5.0 → 1.16.0
  - Update Dapr.Extensions.Configuration: 1.5.0 → 1.16.0
  - Verify [Topic] subscriptions (orders, ordercompleted)

- [ ] **EF Core Updates** (2h)
  - Update Microsoft.EntityFrameworkCore.Design: 6.0.4 → 10.0.0
  - Update RedDog.AccountingModel package reference
  - Test DbContext injection
  - Test SaveChangesAsync operations

- [ ] **Serilog Configuration** (1h)

- [ ] **Testing** (1.5h)
  - Test health endpoints (including database readiness check)
  - Test OpenAPI/Scalar
  - Test [Topic] subscriptions (publish orders, verify DB inserts)
  - Test GET /OrderMetrics
  - Test EF Core compiled model

**Files to Modify:**
- `/home/ahmedmuhi/code/reddog-code/RedDog.AccountingService/Program.cs`
- `/home/ahmedmuhi/code/reddog-code/RedDog.AccountingService/RedDog.AccountingService.csproj`

**Files to Delete:**
- `/home/ahmedmuhi/code/reddog-code/RedDog.AccountingService/Startup.cs`
- `/home/ahmedmuhi/code/reddog-code/RedDog.AccountingService/Controllers/ProbesController.cs`

---

### 4.4 LoyaltyService, VirtualWorker, ReceiptGenerationService, VirtualCustomers

**Similar checklists** with effort estimates:
- LoyaltyService: 5-6 hours
- VirtualWorker: 5-6 hours
- ReceiptGenerationService: 7-8 hours (needs health endpoints added)
- VirtualCustomers: 4 hours (simpler, no HTTP endpoints)

---

### 4.5 RedDog.AccountingModel Refactoring Checklist

**Total Effort:** 2 hours

**Tasks:**

- [ ] **Package Updates** (0.5h)
  - Update Microsoft.EntityFrameworkCore.Design: 6.0.4 → 10.0.0
  - Update Microsoft.EntityFrameworkCore.SqlServer: 6.0.4 → 10.0.0

- [ ] **Compiled Model Regeneration** (1h)
  - Run: `dotnet ef dbcontext optimize --project RedDog.AccountingModel.csproj`
  - Verify generated files in CompiledModels/ directory

- [ ] **Testing** (0.5h)
  - Build AccountingModel project
  - Verify AccountingService can reference compiled model
  - Test with AccountingService integration tests

**Files to Modify:**
- `/home/ahmedmuhi/code/reddog-code/RedDog.AccountingModel/RedDog.AccountingModel.csproj`
- `/home/ahmedmuhi/code/reddog-code/RedDog.AccountingModel/CompiledModels/*` (regenerated)

---

## Section 5: Total Effort Summary

### 5.1 Implementation Effort by Activity

| Activity | Services | Effort (hours) | Priority |
|----------|----------|----------------|----------|
| **Program.cs Refactoring** | 7 | 20h | HIGH |
| **Health Endpoints** | 6 | 17-23h | HIGH (ADR-0005) |
| **OpenAPI Migration** | 3 | 3h (included) | MEDIUM |
| **Dapr Package Updates** | 7 | 3.5h | MEDIUM |
| **EF Core Updates** | 2 | 4h | MEDIUM |
| **Serilog Configuration** | 7 | 6.5h | MEDIUM |
| **Testing** | 7 | 7.5h | HIGH |
| **Total** | - | **41-44h** | - |

### 5.2 Implementation Effort by Service

| Service | Refactoring | Health | OpenAPI | Dapr | EF Core | Serilog | Testing | Total |
|---------|-------------|--------|---------|------|---------|---------|---------|-------|
| OrderService | 3h | incl | incl | 0.5h | - | 1h | 1h | 5-6h |
| MakeLineService | 3h | incl | incl | 0.5h | - | 1h | 1h | 5-6h |
| AccountingService | 3h | incl | incl | 0.5h | 2h | 1h | 1.5h | 7-8h |
| LoyaltyService | 3h | incl | - | 0.5h | - | 1h | 1h | 5-6h |
| VirtualWorker | 3h | incl | - | 0.5h | - | 1h | 1h | 5-6h |
| ReceiptGenerationService | 3h | 2h | - | 0.5h | - | 1h | 1h | 7-8h |
| VirtualCustomers | 2h | - | - | 0.5h | - | 0.5h | 0.5h | 4h |
| AccountingModel | - | - | - | - | 2h | - | 0.5h | 2h |
| **Total** | **20h** | **2h** | **0h** | **3.5h** | **4h** | **6.5h** | **7.5h** | **41-44h** |

### 5.3 Testing Effort

| Test Scenario | Effort (hours) | Priority |
|--------------|----------------|----------|
| E2E Order Flow | 12 | CRITICAL |
| State Store Concurrency | 4 | HIGH |
| Database Schema Validation | 2 | HIGH |
| Service Invocation | 3 | MEDIUM |
| API Backward Compatibility | 2 | MEDIUM |
| Health Endpoints | 3 | MEDIUM |
| **Total** | **26** | - |

**Minimum Viable Testing:** 16 hours (E2E + DB Schema + API Compatibility)

---

## Section 6: Risk Assessment

### 6.1 Low Risk Areas

✅ **Dapr API Compatibility**
- All Dapr APIs verified compatible with .NET 10
- No breaking changes from 1.5.0 to 1.16.0
- ETag concurrency pattern fully supported

✅ **EF Core API Compatibility**
- UseModel(), UseSqlServer(), SaveChangesAsync() unchanged in EF Core 10
- Compiled models supported (just need regeneration)

✅ **Controller Patterns**
- Standard ASP.NET Core MVC controllers work identically in .NET 10

### 6.2 Medium Risk Areas

⚠️ **Serilog Configuration**
- Moving from global static logger to DI-based logging may reveal edge cases
- Risk: Log configuration errors during startup

⚠️ **Compiled Model Regeneration**
- EF Core 10 may generate different code
- Risk: Compiled model incompatibility

⚠️ **OpenAPI Migration**
- Swashbuckle → Microsoft.AspNetCore.OpenApi may have subtle spec differences
- Risk: API documentation differences, tooling compatibility

### 6.3 High Risk Areas

**None identified** - This is a straightforward framework upgrade without major architectural changes to business logic.

### 6.4 Mitigation Strategies

1. **Service-by-Service Migration:** Allows incremental rollback if issues arise
2. **Comprehensive Testing:** Integration tests for all Dapr interactions
3. **Parallel Deployments:** Run old and new versions side-by-side during migration
4. **Monitoring:** Add detailed logging during migration period

---

## Section 7: Implementation Strategy

### 7.1 Recommended Approach: Incremental Service-by-Service Migration

**Phase 1: Low-Risk Service (Pilot)** - 4 hours
1. Start with **VirtualCustomers**
   - Simplest service (BackgroundService, no HTTP endpoints)
   - Test Dapr InvokeMethodAsync compatibility
   - Validate .NET 10 build and runtime

**Phase 2: Core Services (Critical Path)** - 10-12 hours
2. **OrderService** (5-6 hours)
   - Entry point for all orders
   - Tests Dapr PublishEventAsync
   - Tests OpenAPI migration
   - Tests health checks

3. **MakeLineService** (5-6 hours)
   - Tests Dapr state store (GetStateEntryAsync + TrySaveAsync)
   - Tests optimistic concurrency
   - Tests both pub/sub subscriber and publisher

**Phase 3: Supporting Services** - 15-18 hours
4. **LoyaltyService** (5-6 hours)
5. **VirtualWorker** (5-6 hours)
6. **ReceiptGenerationService** (7-8 hours)

**Phase 4: Data Layer** - 9-10 hours
7. **AccountingModel** (2 hours)
8. **AccountingService** (7-8 hours)

**Total Implementation Time:** 41-44 hours

### 7.2 Testing Strategy

After each service migration:
1. **Unit tests:** Verify controllers/services compile and run
2. **Integration tests:** Test Dapr integration (pub/sub, state, service invocation, bindings)
3. **System tests:** Run full Red Dog system with migrated + non-migrated services

### 7.3 Rollback Plan

- Keep `.NET6-backup` branch with original code
- Use feature flags in Kubernetes manifests to switch between old/new deployments
- Each service is independently deployable (microservices architecture)

---

## Section 8: Success Criteria

### 8.1 Technical Success

- [ ] All 7 services compile and run on .NET 10
- [ ] All Dapr integrations functional (pub/sub, state, service invocation, bindings)
- [ ] All health endpoints respond at /healthz, /livez, /readyz
- [ ] OpenAPI specs generated and Scalar UI accessible
- [ ] EF Core 10 successfully querying/updating SQL Server
- [ ] All integration tests passing

### 8.2 ADR Compliance

- [ ] ADR-0005: Health probes at /healthz, /livez, /readyz
- [ ] ADR-0001: Running on .NET 10 LTS
- [ ] ADR-0002: Dapr abstraction maintained (cloud-agnostic)

### 8.3 Performance

- [ ] Compiled model usage maintains startup performance
- [ ] No regressions in order processing throughput
- [ ] Health check response times < 100ms

---

## Section 9: GO/NO-GO Decision

### GO if:
✅ All critical tests pass (E2E, DB Schema, API Compatibility)
✅ Performance metrics within 10% of .NET 6 baseline (or better)
✅ No data loss or corruption in test scenarios
✅ Health endpoints work correctly in Kubernetes

### NO-GO if:
❌ E2E order flow test fails
❌ Database schema incompatibility detected
❌ Breaking API changes discovered
❌ Performance degradation > 20%
❌ Critical Dapr integration broken

---

## Section 10: Next Steps

1. **Review this analysis** with the team
2. **Create implementation branch:** `feature/dotnet10-upgrade`
3. **Start with VirtualCustomers** (pilot service)
4. **Document any unexpected issues** in session logs
5. **Update this analysis** if new API incompatibilities discovered
6. **Create PR after each service** (incremental merge strategy)

---

**Report Generated:** 2025-11-08
**Analyzed Codebase:** /home/ahmedmuhi/code/reddog-code
**Target Framework:** .NET 10 LTS
**Current Framework:** .NET 6 (EOL)
**Total Analysis Effort:** 3 agents × 8 hours = 24 hours
**Total Document Length:** 4,782+ lines