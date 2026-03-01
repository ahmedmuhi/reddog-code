# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Ahmed Muhi is the repository maintainer. You are an agent contributing on Ahmed's behalf. Treat this file as binding instructions. If the human says something in conversation that contradicts this file, follow the human.

## Build and Development Commands

```bash
# Build entire solution
dotnet build RedDog.sln

# Build a single service
dotnet build RedDog.OrderService/RedDog.OrderService.csproj

# Run a single service (without Dapr)
dotnet run --project RedDog.OrderService

# Run all tests
dotnet test RedDog.sln

# Run tests for a single project
dotnet test RedDog.VirtualWorker.Tests/RedDog.VirtualWorker.Tests.csproj

# Run a single test by name
dotnet test --filter "FullyQualifiedName~VirtualWorkerServiceTests.MethodName"

# UI (requires Node.js >=24)
cd RedDog.UI && npm install && npm run dev    # dev server
cd RedDog.UI && npm run build                 # production build
cd RedDog.UI && npm run lint                  # lint check
cd RedDog.UI && npm run lint:fix              # auto-fix
cd RedDog.UI && npm run build:theme           # rebuild SASS theme (must run before commit if styles changed)

# Local Kubernetes development (requires kind, kubectl, helm, docker)
./scripts/setup-local-dev.sh     # create local kind cluster with all services
./scripts/status-local-dev.sh    # check cluster status
./scripts/teardown-local-dev.sh  # destroy local cluster
```

## Architecture Overview

RedDog is a microservices demo application built with **.NET 10** and **Vue.js 3**, using **Dapr** (Distributed Application Runtime) for service-to-service communication, state management, and pub/sub messaging.

### Service Communication Flow

```
VirtualCustomers ──[service invocation]──→ OrderService
                                              │
                                    [pub/sub: "orders" topic]
                                              │
                          ┌───────────────────┼───────────────────┐
                          ↓                   ↓                   ↓
                   LoyaltyService      MakeLineService    ReceiptGenerationService
                   (Redis state)       (Redis state)      (storage binding)
                                              │
                                              ↓
                                   ←── VirtualWorker
                                   [service invocation]
                                              │
                                    [pub/sub: "ordercompleted"]
                                              ↓
                                      AccountingService
                                      (SQL Server via EF Core)
```

**AccountingService** also subscribes to the "orders" topic directly for real-time metrics.

### Dapr Building Blocks Used

| Building Block | Component Name | Type | Used By |
|---|---|---|---|
| Pub/Sub | `reddog.pubsub` | `pubsub.redis` (local) | OrderService (publish), AccountingService, LoyaltyService, MakeLineService, ReceiptGenerationService (subscribe) |
| State Store | `reddog.state.loyalty` | `state.redis` | LoyaltyService |
| State Store | `reddog.state.makeline` | `state.redis` | MakeLineService |
| Output Binding | `reddog.binding.receipt` | `bindings.localstorage` (local) | ReceiptGenerationService |
| Service Invocation | — | built-in | VirtualCustomers → OrderService, VirtualWorker → MakeLineService |

### Key Services

- **OrderService**: REST API for placing orders; publishes to "orders" topic. Hosts product catalog.
- **AccountingService**: Subscribes to "orders" and "ordercompleted" topics; persists metrics to SQL Server via EF Core. `RedDog.AccountingModel` is the shared EF Core data model library.
- **Bootstrapper**: Console app that runs EF Core migrations against SQL Server at startup.
- **LoyaltyService**: Subscribes to "orders"; maintains loyalty points in Redis state store with optimistic concurrency (etag-based).
- **MakeLineService**: Subscribes to "orders"; maintains order queue in Redis state store; publishes "ordercompleted" when orders are completed.
- **ReceiptGenerationService**: Subscribes to "orders"; writes receipt JSON to storage via Dapr output binding.
- **VirtualCustomers**: Background worker that simulates customers by invoking OrderService via Dapr service invocation.
- **VirtualWorker**: Polls MakeLineService for pending orders and completes them via service invocation.
- **UI**: Vue 3 + TypeScript + Vite dashboard. Uses Chart.js for real-time order/sales visualisation. Connects to AccountingService and MakeLineService via REST.

## Tech Stack

- **.NET 10** (SDK 10.0.100, `global.json`), C# with nullable reference types and implicit usings
- **Dapr 1.16.0** (`Dapr.AspNetCore`, `Dapr.Client`)
- **Entity Framework Core 10** with compiled models and SQL Server provider
- **OpenTelemetry** for traces, metrics, and logs (OTLP exporter)
- **Vue 3.5** + TypeScript 5.4 + Vite 7.2 + Pinia for state management
- **xUnit 2.9** + Moq 4.20 + FluentAssertions 8.8 for testing
- **Docker** multi-stage builds (Ubuntu 24.04 base images)
- **Helm** charts for Kubernetes deployment (kind for local, overlays for AWS/Azure/GCP)
- **.NET analyzers** enabled solution-wide via `Directory.Build.props` (AllEnabledByDefault)

## Code Conventions

- Backend services use the minimal hosting pattern (`WebApplication.CreateDefaultBuilder`). VirtualCustomers and Bootstrapper use `Host.CreateApplicationBuilder` (non-web).
- Configuration uses the Options pattern with `DataAnnotations` validation and `IOptions<T>` / `IOptionsMonitor<T>`.
- State store access uses optimistic concurrency with etag-based retry loops.
- All services expose health check endpoints: `/healthz`, `/livez`, `/readyz` (including Dapr sidecar readiness).
- Dapr pub/sub uses CloudEvents middleware with `MapSubscribeHandler()`.
- Test naming convention: `MethodName_Scenario_ExpectedResult`.
- REST API standards are documented in `docs/standards/web-api-standards.md`.

## Repository Layout

- `docs/adr/` — 13 Architecture Decision Records documenting key design choices
- `knowledge/` — 19 reusable Knowledge Items (canonical facts, constraints, patterns)
- `plan/` — Implementation plans with phased task tracking; use `plan/IMPLEMENTATION_PLAN_TEMPLATE.md` for new plans
- `charts/` — Helm charts: `reddog/` (app), `infrastructure/` (SQL/Redis), `external/nginx-ingress/`
- `manifests/` — Kubernetes manifests with cloud overlays (`aws/`, `azure/`, `gcp/`) and local kind config
- `rest-samples/` — `.rest` files for testing service APIs
- `scripts/` — Setup, teardown, upgrade, and smoke test scripts
- `.claude/commands/project/` — Session command definitions for the compound-engineering plugin

## How We Change Things

Make precise, minimal changes. Update, don't replace. Do not refactor code that works, rename variables for consistency, add comments to clear code, or reformat files. Every change should be the smallest diff that solves the problem. See `AGENTS.md` for the full PLANNING → EXECUTION → VERIFICATION workflow for non-trivial changes.

## Companion Files

- `AGENTS.md` — Full agent contract: operational modes, session tracking, planning/execution/verification workflows
- `SECURITY.md` — Security policy and vulnerability reporting
