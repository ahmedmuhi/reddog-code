# Local Development Gap - Research Findings

**Date:** 2025-11-09
**Status:** Critical gap identified in testing strategy

## Problem Statement

Testing and validation strategy requires establishing .NET 6 baseline performance measurements, but all local development infrastructure was removed on November 2, 2025 during Phase 0 cleanup as part of cloud-first modernization strategy.

**Conflict:**
- ✅ **Architectural Goal:** Cloud-agnostic architecture via Dapr abstraction (no cloud lock-in)
- ❌ **Testing Gap:** Cannot establish performance baselines without local dev environment
- ❌ **Iteration Speed:** Cloud deployment too expensive/slow for benchmark iteration

## What Was Removed (November 2, 2025)

### Infrastructure
- `.devcontainer/` - GitHub Codespaces setup (Docker Compose, SQL Server 2019, Redis, Dapr CLI)
- `manifests/local/branch/` - Local Dapr component configurations (pub/sub, state, bindings, secrets)
- `.vscode/` - VS Code tasks (launch.json 192 lines, tasks.json 558 lines)
- `docs/local-dev.md` - Comprehensive local development guide

### Timeline
- **2021-2022:** Active local development via GitHub Codespaces + devcontainers
- **November 2, 2025:** Everything removed in Phase 0 cleanup (commits `ecca0f5`, `04d8b7f`)
- **Rationale:** "Cloud-first approach (remove local dev complexity)"

## Critical Documentation Bugs

1. **CLAUDE.md:178-187** - References deleted `manifests/local/branch/` directory
2. **README.md:15** - Broken link to deleted `docs/local-dev.md`
3. **.gitignore:641-642** - References to deleted manifest directories

## What Would Be Needed for Local Development

### Option 1: Docker Compose (Modern Approach)
- Redis container (pub/sub + state stores)
- SQL Server 2022 container
- Dapr sidecar orchestration per service
- Recreate Dapr component configs (templates exist in git history)

### Option 2: .NET Aspire
- Mentioned in `plan/testing-validation-strategy.md` but not implemented
- Would provide modern orchestration for multi-service local development

### Option 3: Lightweight Cloud (Container Apps)
- Deploy to Azure Container Apps for testing
- Trade-off: Cost and iteration speed vs setup complexity

## Dependencies Required

**Infrastructure:**
- Redis (Dapr pub/sub, MakeLine state, Loyalty state)
- SQL Server (AccountingService database)
- Dapr runtime (sidecars per service)

**Services:**
- OrderService (port 5100)
- MakeLineService (port 5200)
- ReceiptGenerationService (port 5300)
- LoyaltyService (port 5400)
- VirtualWorker (port 5500)
- AccountingService (port 5700)
- RedDog.UI (port 8080)

## Testing Strategy Impact

**Phase 1.1: Performance Baseline** requires:
1. Establish .NET 6 baseline metrics (BEFORE upgrade)
2. Run k6 load tests against current services
3. Measure P95 latency, throughput, memory usage
4. Save baseline artifacts for comparison

**Blocked:** Cannot execute Phase 1.1 without local or cloud environment

## Architectural Consideration

This repository demonstrates **cloud-agnostic architecture** via Dapr abstraction, enabling deployment to:
- Azure Kubernetes Service (AKS)
- Azure Container Apps
- AWS Elastic Kubernetes Service (EKS)
- Google Kubernetes Engine (GKE)

Local development is orthogonal to cloud-agnostic goal - Dapr works identically locally and in cloud.

## Next Steps (TBD)

Options to evaluate:
1. Recreate Docker Compose setup (minimal, benchmark-focused)
2. Implement .NET Aspire (modern, full-featured)
3. Use ephemeral cloud deployments (Container Apps + scripts)
4. Hybrid: Local services + cloud infrastructure (Redis/SQL in cloud)

## References

- `plan/testing-validation-strategy.md` - Requires local baseline establishment
- `plan/modernization-strategy.md:99-104` - Documents removal of local dev tooling
- Git commits: `ecca0f5` (removed devcontainer), `04d8b7f` (removed .vscode)
