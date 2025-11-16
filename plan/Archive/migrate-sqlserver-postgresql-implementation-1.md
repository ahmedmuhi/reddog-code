```yaml
goal: "SUPERCEDED: Migrate SQL Server workloads to PostgreSQL 18"
version: 1.0
date_created: 2025-11-16
last_updated: 2025-11-17
owner: "Red Dog Modernization Team"
status: 'Superseded'
tags: [migration, database, ef-core, postgresql, sql-server, superseded]
```

# Introduction

This plan is **Superseded** and retained for historical context only.

During the architectural review on 2025-11-17, a simpler, more cost-effective, and lower-risk path was identified and approved.

### Rationale for Superseding

1.  **High Complexity:** The effort to migrate the .NET application data layer from EF Core (`UseSqlServer`) to Npgsql (`UseNpgsql`)—including remediating T-SQL, concurrency tokens (`rowversion` vs. `xmin`), and case sensitivity—was determined to be high-risk and provided limited value compared to the new plan.
2.  **Superior Architecture Identified:** The approved architecture is:
      * **Local:** SQLite (for Dapr State) + SQL Server Docker (for App Data)
      * **Cloud:** Azure Cosmos DB (for Dapr State) + Azure SQL (for App Data)
3.  **Better Cost Model:** The approved plan leverages the **$0 lifetime free tiers** for both Azure Cosmos DB and Azure SQL Database, a significant advantage for the educational goals of the project.
4.  **Simplicity:** The new plan requires **zero application database migration**, allowing the team to focus on the Dapr-native modernization.

All work will proceed based on the `Migrate State Stores from Redis...` and `Dapr Cloud Hardening` initiatives.

## 1. Requirements & Constraints

- **REQ-001**: Preserve data integrity and history for AccountingService and future transactional services (lossless migration, row counts + checksums validated before cutover).
- **REQ-002**: Applications must adopt `Npgsql.EntityFrameworkCore.PostgreSQL` 9.x+ (or 10.x RC when .NET 10 GA) to leverage EF Core 9/10 provider improvements and configure `UseNpgsql` with PostgreSQL 18 server hints.
- **SEC-001**: Secrets move to Kubernetes-managed secret stores/Workload Identity; no connection strings stored in code or Git.
- **CON-001**: PostgreSQL 18.x is the baseline in all managed clouds; enable default data checksums and new TLS/OAuth authentication paths.
- **CON-002**: Keep downtime under 30 minutes for production cutover; design CDC/dual-write window accordingly.
- **GUD-001**: Follow ADR-0007 cloud-agnostic guidance – no provider-specific SQL beyond portable PostgreSQL features.
- **PAT-001**: Use cloud-native Database Migration Service (or equivalent) with Gemini-assisted SQL remediation + continuous CDC to convert schema/data before application flip.
- **PAT-002**: Adopt asynchronous I/O (`io_method=io_uring` where available) and `pg_stat_io`/`pg_aios` monitoring once workloads reside on PostgreSQL 18.

## 2. Implementation Steps

### Implementation Phase 1 – Discovery & Readiness

- **GOAL-001**: Capture current SQL Server footprint, dependencies, and readiness gaps.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-101 | Inventory SQL Server objects (tables, procs, CLR, triggers) and classify compatibility blockers | | |
| TASK-102 | Benchmark current workload (baseline TPS, latency, index usage) for post-migration comparison | | |
| TASK-103 | Provision pilot PostgreSQL 18.1+ clusters in each cloud (Cloud SQL, RDS 18.1, Azure Flexible Server 17/18 preview) with IAM/Workload Identity wiring | | |
| TASK-104 | Document downtime budget, RPO/RTO, and stakeholder approvals | | |

### Implementation Phase 2 – Schema & Code Conversion

- **GOAL-002**: Convert SQL constructs and update EF Core provider packages.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-201 | Run conversion workspace (Gemini/DMS or Schema Conversion Tool) to translate schema + T-SQL to PL/pgSQL and capture remediation items | | |
| TASK-202 | Update shared data access libraries to target `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.3+ (or 10.0 RC when .NET 10 ships) and configure `UseNpgsql(..., o => o.SetPostgresVersion(18,0))` where needed | | |
| TASK-203 | Refactor SQL Server-specific features (IDENTITY, `GETDATE()`, temporal tables, cross-database joins) to PostgreSQL features (IDENTITY ALWAYS, `now()`, generated columns, FDWs) or EF Core interceptors | | |
| TASK-204 | Add migration scripts for enum types, JSONB columns, and sequences; check into `RedDog.AccountingService/Migrations` | | |

### Implementation Phase 3 – Data Migration & Dual Write

- **GOAL-003**: Move data with minimal downtime.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-301 | Configure continuous replication/CDC pipeline (Google DMS, AWS DMS Blue/Green, Azure DMS) from SQL Server to PostgreSQL with support for UUIDv7 + data checksums | | |
| TASK-302 | Execute full load, validate counts/checksums, and rehearse cutover in staging | | |
| TASK-303 | Implement dual-write/feature flag in services to toggle between SQL Server and PostgreSQL connections, and capture telemetry via `pg_stat_io`/App Insights for asynchronous I/O tuning | | |
| TASK-304 | Capture runbooks for switchover (freeze writes, finalize CDC, promote target) | | |

### Implementation Phase 4 – Cutover & Validation

- **GOAL-004**: Switch production traffic and certify system health.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-401 | Execute production cutover window; monitor CDC lag and confirm final snapshot | | |
| TASK-402 | Run automated smoke + regression suite (API, Dapr workflows) against PostgreSQL 18 with async I/O enabled (`io_method=io_uring` where supported) | | |
| TASK-403 | Tune indexes, autovacuum, asynchronous I/O workers (`io_workers`), and `work_mem`/`shared_buffers` per observability data | | |
| TASK-404 | Decommission legacy SQL Server assets after retention hold | | |

### Implementation Phase 5 – Post-Migration Optimization

- **GOAL-005**: Leverage PostgreSQL-native capabilities.

| Task | Description | Completed | Date |
|------|-------------|-----------|------|
| TASK-501 | Introduce partitioning/JSONB indexes for high-volume tables | | |
| TASK-502 | Enable pgAudit/pg_stat_statements dashboards in observability stack | | |
| TASK-503 | Update developer docs and onboarding scripts to default to PostgreSQL tooling (psql, pg_dump) | | |

## 3. Alternatives

- **ALT-001**: Re-platform to Azure SQL Managed Instance only (reject – retains SQL Server licensing/lock-in).
- **ALT-002**: Use direct pgloader migration without CDC (reject – cannot meet <30 min downtime requirement).

## 4. Dependencies

- **DEP-001**: Dapr sidecars must support PostgreSQL connection secrets via existing secret store.
- **DEP-002**: Observability stack (Grafana/Prometheus) needs PostgreSQL exporters deployed.
- **DEP-003**: Application feature flags infrastructure for dual-write toggle.

## 5. Files

- **FILE-001**: `RedDog.AccountingService/RedDog.AccountingService.csproj` (EF Core provider reference + migrations targeting PostgreSQL 18).
- **FILE-002**: `plan/upgrade-phase0-platform-foundation-implementation-1.md` (update dependencies/status once migration completes).
- **FILE-003**: `scripts/migrations/` (new automation scripts for pg_dump/pg_restore + CDC setup).

## 6. Testing

- **TEST-001**: Schema diff + data checksum comparison between SQL Server source and PostgreSQL target per release.
- **TEST-002**: End-to-end order flow + accounting aggregation using PostgreSQL in staging.
- **TEST-003**: Load test (100+ concurrent accounting writes) to validate vacuum/autovacuum settings.

## 7. Risks & Assumptions

- **RISK-001**: Unsupported T-SQL constructs (CLR, cross-db transactions) could delay migration – mitigated via early conversion analysis.
- **RISK-002**: Long-running CDC may lag under peak load – mitigate with capacity planning and throttled batch windows.
- **ASSUMPTION-001**: PostgreSQL 16 managed offerings are available in all target clouds with HA/DR features.

## 8. Related Specifications / Further Reading

- `plan/upgrade-phase0-platform-foundation-implementation-1.md`
- `plan/migrate-state-stores-cloud-native-implementation-1.md`
- `docs/research/infrastructure-versions-verification.md`
