---
goal: Upgrade AccountingModel to .NET 10 LTS with EF Core 10 alignment and tooling compliance
version: 1.0
date_created: 2025-11-06
last_updated: 2025-11-06
owner: Red Dog Modernization Team
status: Planned
tags: [upgrade, dotnet10, accountingmodel, efcore]
---

# Introduction

![Status: Planned](https://img.shields.io/badge/status-Planned-blue)

This implementation plan defines deterministic steps to upgrade `RedDog.AccountingModel` (shared EF Core library) from .NET 6.0 to .NET 10.0 LTS, ensuring compatibility with `RedDog.AccountingService` and Bootstrapper. The plan aligns with `plan/modernization-strategy.md`, `docs/research/dotnet-upgrade-analysis.md`, `plan/testing-validation-strategy.md`, and `plan/cicd-modernization-strategy.md`.

## Research References

- `docs/research/dotnet-upgrade-analysis.md` (Dependency inventory, EF Core breaking changes, Docker updates)
- `plan/testing-validation-strategy.md` (Tool installation requirements, validation artifacts, downstream build checks)
- `plan/cicd-modernization-strategy.md` (CI workflow expectations and tooling audits)

## 1. Requirements & Constraints

- **REQ-001**: Target framework must be `net10.0` with `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<LangVersion>14.0`.
- **REQ-002**: EF Core packages (`Microsoft.EntityFrameworkCore`, `Design`, `SqlServer`, `Tools`) upgraded to `10.0.x` in both AccountingModel and dependent projects.
- **REQ-003**: Remove obsolete package references (Serilog, Swashbuckle, etc.—none exist here, but verify).
- **SEC-001**: `dotnet list package --vulnerable` must report zero vulnerabilities (artifact stored under `artifacts/dependencies/`).
- **CON-001**: AccountingService and Bootstrapper must consume the updated library without binding redirects; coordinate upgrades.
- **GUD-001**: Enable nullable reference types and resolve all warnings.
- **PAT-001**: Use async EF Core APIs and modern C# features (file-scoped namespaces, collection expressions) throughout entity configurations.

## 2. Implementation Steps

### Implementation Phase 1

- **GOAL-001**: Apply tooling, update csproj/package references, and ensure clean builds.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-000** | Run `.NET Upgrade Assistant`: `upgrade-assistant upgrade RedDog.AccountingModel/RedDog.AccountingModel.csproj --entry-point RedDog.AccountingModel/RedDog.AccountingModel.csproj --non-interactive --skip-backup false > artifacts/upgrade-assistant/accountingmodel.md`. | | |
| **TASK-001** | Execute `dotnet workload restore` and `dotnet workload update`; append console output to `artifacts/dependencies/accountingmodel-workloads.txt`. | | |
| **TASK-002** | Capture dependency baselines: `dotnet list RedDog.AccountingModel/RedDog.AccountingModel.csproj package --outdated --include-transitive`, `--vulnerable`, and `dotnet list ... reference --graph`, saving files to `artifacts/dependencies/accountingmodel-{outdated|vulnerable|graph}.txt`. | | |
| **TASK-003** | Run API Analyzer: `dotnet tool run api-analyzer -f net10.0 -p RedDog.AccountingModel/RedDog.AccountingModel.csproj > artifacts/api-analyzer/accountingmodel.md`; resolve warnings ≥ Medium. | | |
| **TASK-004** | Update `RedDog.AccountingModel/RedDog.AccountingModel.csproj`: set `<TargetFramework>net10.0</TargetFramework>`, enable `<Nullable>`/`<ImplicitUsings>`, set `<LangVersion>14.0`. | | |
| **TASK-005** | Upgrade EF Core packages to 10.0.x (`Microsoft.EntityFrameworkCore`, `SqlServer`, `Design`), ensuring versions match AccountingService and Bootstrapper. Remove any legacy package references. | | |
| **TASK-006** | Re-run `dotnet restore` and dependency audits to confirm zero outdated/vulnerable packages; overwrite artifacts. | | |
| **TASK-007** | Execute `dotnet build RedDog.AccountingModel/RedDog.AccountingModel.csproj -warnaserror` to confirm zero warnings. | | |

**Completion Criteria (Phase 1):** Artifacts exist under `artifacts/upgrade-assistant/`, `artifacts/dependencies/`, `artifacts/api-analyzer/`; `dotnet build` succeeds without warnings.

### Implementation Phase 2

- **GOAL-002**: Modernize code, synchronize migrations, and validate downstream consumers.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| **TASK-008** | Apply modernization features: file-scoped namespaces, collection expressions, primary constructors (where applicable), removal of obsolete synchronous EF APIs. | | |
| **TASK-009** | Resolve nullable warnings across entity classes and DbContext configurations; treat warnings as errors. | | |
| **TASK-010** | Update EF Core annotations/Fluent APIs to 10.x equivalents (e.g., `HasPrecision`, `OwnsOne` patterns) ensuring compatibility with AccountingService migrations. | | |
| **TASK-011** | Coordinate with AccountingService to regenerate EF Core migrations if schema changes occur; store migration diff in `artifacts/accountingmodel-migration-impact.md`. | | |
| **TASK-012** | Update unit/integration tests (if any) referencing the model; run `dotnet test RedDog.AccountingModel.Tests` (create minimal tests if absent). | | |
| **TASK-013** | Validate downstream builds: run `dotnet build` for `RedDog.AccountingService` and `RedDog.Bootstrapper` to ensure no binding/version conflicts. Log results in `artifacts/accountingmodel-downstream-validation.md`. | | |

**Completion Criteria (Phase 2):** AccountingModel builds warning-free; nullable warnings resolved; downstream services build successfully against the upgraded library; migration impact documented.

## 3. Alternatives

- **ALT-001**: Keep AccountingModel on .NET 6 while upgrading AccountingService — rejected (shared EF packages would conflict; modernization strategy requires unified baseline).
- **ALT-002**: Replace AccountingModel with inline models in AccountingService — rejected (library reuse is still desired for Bootstrapper and future services).

## 4. Dependencies

- **DEP-001**: `docs/research/dotnet-upgrade-analysis.md` (EF Core upgrade guidance and breaking changes).
- **DEP-002**: `plan/testing-validation-strategy.md` (tooling artifacts and validation flows).
- **DEP-003**: `plan/cicd-modernization-strategy.md` (tooling audit job and CI enforcement).
- **DEP-004**: `RedDog.AccountingService/RedDog.AccountingService.csproj`, `RedDog.Bootstrapper/RedDog.Bootstrapper.csproj` (downstream consumers).

## 5. Files

- **FILE-001**: `RedDog.AccountingModel/RedDog.AccountingModel.csproj`
- **FILE-002**: `RedDog.AccountingModel/*.cs` entity/configuration files
- **FILE-003**: `artifacts/upgrade-assistant/accountingmodel.md`
- **FILE-004**: `artifacts/dependencies/accountingmodel-*.txt`
- **FILE-005**: `artifacts/api-analyzer/accountingmodel.md`
- **FILE-006**: `artifacts/accountingmodel-migration-impact.md`
- **FILE-007**: `artifacts/accountingmodel-downstream-validation.md`

## 6. Testing

- **TEST-001**: `dotnet test RedDog.AccountingModel.Tests/RedDog.AccountingModel.Tests.csproj` (create minimal tests if missing).
- **TEST-002**: Downstream build validation: `dotnet build RedDog.AccountingService/RedDog.AccountingService.csproj` and `dotnet build RedDog.Bootstrapper/RedDog.Bootstrapper.csproj` after library upgrade.
- **TEST-003**: Migration diff review (compare `dotnet ef migrations script` before/after) to ensure no unintended schema changes.

## 7. Risks & Assumptions

- **RISK-001**: EF Core 10 changes may introduce schema diffs impacting production. *Mitigation*: coordinate migration scripts with AccountingService/Bootstrapper and review `artifacts/accountingmodel-migration-impact.md`.
- **RISK-002**: Nullable enablement may produce numerous warnings. *Mitigation*: treat warnings as errors and schedule focused cleanup time.
- **ASSUMPTION-001**: AccountingService and Bootstrapper upgrades proceed in parallel so the shared library can be consumed immediately.

## 8. Related Specifications / Further Reading

- `plan/modernization-strategy.md`
- `docs/research/dotnet-upgrade-analysis.md`
- `plan/testing-validation-strategy.md`
- `plan/cicd-modernization-strategy.md`
- `docs/adr/adr-0001-dotnet10-lts-adoption.md`
