# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Communication Note

The user is using voice transcription for input. If you encounter any unusual phrasing or words that don't make sense in context, please ask for clarification rather than assuming the intended meaning.

## Start by Finding Out When Are We

Very important: The user's timezone is {datetime(.)now().strftime("%Z")}. The current date is {datetime(.)now().strftime("%Y-%m-%d")}. 

Any dates before this are in the past, and any dates after this are in the future. When the user asks for the 'latest', 'most recent', 'today's', etc. don't assume your knowledge is up to date;

## Current Development Status

**Actual State (as of 2025-11-11 10:58 NZDT):**
- ✅ Phase 0 cleanup completed (removed manifests/local, manifests/corporate, CorporateTransferService, .vscode)
- ✅ Phase 0 tooling installed (Go 1.25.4, kind 0.30.0, kubectl 1.34.1, Helm 3.19.0, upgrade-assistant, ApiCompat)
- ✅ **Phase 0.5 COMPLETE** - kind cluster operational, all services deployed via Helm, end-to-end smoke tests passing
- ✅ Phase 1 baseline complete (.NET 6 performance baseline established - see `tests/k6/BASELINE-RESULTS.md`)
- ✅ Dapr 1.16.2 running in-cluster (Kubernetes mode with Redis + SQL Server)
- ✅ .NET 6.0.36 + ASP.NET Core 6.0.36 runtimes installed
- 🟡 **Phase 1A IN PROGRESS** - 5/9 services upgraded to .NET 10 (56% complete as of 2025-11-12)
- ✅ OrderService, ReceiptGenerationService, AccountingService, AccountingModel, Bootstrapper upgraded to .NET 10
- ⏳ MakeLineService, LoyaltyService, VirtualWorker, VirtualCustomers still .NET 6.0

**📅 Upcoming Upgrade (November 11, 2025):**
- ⏰ **.NET 10 GA Release** - Upgrade from 10.0.100-rc.2 to 10.0.0 (GA)
- Update `global.json` to `10.0.0` after official release at .NET Conf 2025
- Verify with `dotnet --version` after upgrade

**What Works Now:**
- Building .NET 6.0 services with .NET 10 SDK
- **kind cluster deployment** - Operational with Dapr 1.16.2 in Kubernetes mode
- **Helm chart deployment** - All services deployed and healthy via `charts/reddog/` and `charts/infrastructure/`
- Running services locally with `dapr run` and Dapr 1.16.2 CLI (slim mode)
- OrderService validated and load tested (P95: 7.77ms, 46.47 req/s)
- Performance baseline established (k6 load testing framework)
- Dapr components configured (pub/sub, state stores, bindings, secret stores)
- Infrastructure running (Redis, SQL Server, Nginx Ingress)
- Receipt generation service with working localstorage binding (emptyDir + fsGroup)
- End-to-end pub/sub flow verified (OrderService → all subscribers)
- Vue.js 2 UI development (npm run serve)
- REST API testing via samples in `rest-samples/`

**What Doesn't Work Yet:**
- .NET 10 builds (projects not retargeted)
- Automated unit/integration tests (load testing only)
- Production-ready observability (Jaeger, Prometheus, Grafana - disabled for Phase 0.5)

## Documentation Map

This repository uses structured documentation to separate concerns and provide clear navigation:

### 📋 Quick Reference (You Are Here)
- **CLAUDE.md** (this file) - Development guide, current status, common commands

### 🏗️ Architectural Decisions
- **[ADR Overview & Navigation Hub](docs/adr/README.md)** - **Start here** to navigate all architectural decisions
  - Implementation status dashboard (🟢 Implemented, 🟡 In Progress, 🔵 Accepted, ⚪ Planned)
  - Configuration decision tree ("Where should I put this setting?")
  - Role-based reading guides (Developer, Operator, Decision Maker)
  - 11 ADRs organized by category (Core Platform, Configuration, Deployment, Operational, Multi-Cloud)
- **Individual ADRs** in `docs/adr/` - Detailed decision records with implementation status

### 📐 Implementation Standards
- **[Web API Standards](docs/standards/web-api-standards.md)** - HTTP API conventions for all services
  - OpenAPI/Scalar documentation, CORS, error handling, health endpoints, observability
  - Cross-references to supporting ADRs (ADR-0002, 0004, 0005, 0006, 0011)
  - **Target state** for modernized services (current .NET 6.0 services don't yet comply)

### 📝 Planning Documents
- **[Modernization Strategy](plan/modernization-strategy.md)** - 8-phase transformation roadmap
- **[Testing & Validation Strategy](plan/testing-validation-strategy.md)** - Testing baseline (prerequisite for Phase 1A)
- **[Documentation Improvement Plan](plan/documentation-structure-improvement-plan.md)** - This documentation structure plan

### 🔍 Navigation Tips

**I need to understand a decision:** → [ADR Overview](docs/adr/README.md)

**I need to implement an API:** → [Web API Standards](docs/standards/web-api-standards.md)

**I need to configure something:** → [Configuration Decision Tree](docs/adr/README.md#configuration-decision-tree)

**I need to know what's implemented:** → [ADR Overview: Implementation Status](docs/adr/README.md#implementation-status-legend)

**I need to know what's next:** → [Modernization Strategy](plan/modernization-strategy.md)

**I'm confused about configuration:** → [ADR README Section: Configuration Architecture](docs/adr/README.md#configuration-architecture-overview)

---

## Common Development Commands

### Prerequisites
- .NET 10 SDK (per global.json) - builds .NET 6.0 projects
- .NET 6.0.36 runtime + ASP.NET Core 6.0.36 runtime (for running .NET 6 services)
- Dapr CLI 1.16.2+ for local service execution (slim mode)
- Node.js 24+ and npm 11+ for Vue.js UI
- k6 v0.54.0+ for load testing (installed to ~/bin/)
- Copy `.env/local.sample` → `.env/local`, set `SQLSERVER_SA_PASSWORD`, and keep the real file untracked.
- Copy `values/values-local.yaml.sample` → `values/values-local.yaml` before running kind/Helm scripts.

### Build & Restore
```bash
# Restore packages
dotnet restore RedDog.sln

# Build all services
dotnet build RedDog.sln -c Release

# Build individual service
dotnet build RedDog.OrderService/RedDog.OrderService.csproj -c Release
```

### Run Services Locally (with Dapr)

**Note:** Requires Dapr components in `.dapr/components/` and infrastructure running (Redis + SQL Server).

```bash
# OrderService (validated and load tested - P95: 7.77ms)
dapr run --app-id orderservice --app-port 5100 --dapr-http-port 5180 \
  --resources-path .dapr/components \
  -- dotnet run --project RedDog.OrderService/RedDog.OrderService.csproj

# MakeLineService
dapr run --app-id makelineservice --app-port 5200 --dapr-http-port 5280 \
  --resources-path .dapr/components \
  -- dotnet run --project RedDog.MakeLineService/RedDog.MakeLineService.csproj

# AccountingService
dapr run --app-id accountingservice --app-port 5700 --dapr-http-port 5780 \
  --resources-path .dapr/components \
  -- dotnet run --project RedDog.AccountingService/RedDog.AccountingService.csproj

# LoyaltyService
dapr run --app-id loyaltyservice --app-port 5400 --dapr-http-port 5480 \
  --resources-path .dapr/components \
  -- dotnet run --project RedDog.LoyaltyService/RedDog.LoyaltyService.csproj
```

### Vue.js UI Development
```bash
cd RedDog.UI
npm install
npm run serve    # Dev server at http://localhost:8080
npm run build    # Production build
npm run lint     # ESLint
```

### API Testing
- REST samples available in `rest-samples/` directory
- Use VS Code REST Client extension or similar tools
- Files: `order-service.rest`, `makeline-service.rest`, `accounting-service.rest`, `ui.rest`

### Load Testing
```bash
# Run k6 baseline test (requires OrderService running with Dapr)
k6 run tests/k6/orderservice-baseline.js

# View baseline results
cat tests/k6/BASELINE-RESULTS.md
```

### Infrastructure Management
```bash
# Load local env vars once per shell
set -a; source .env/local; set +a

# Start Redis and SQL Server (required for Dapr components)
docker run --name reddog-redis -d -p 6379:6379 redis:6.2-alpine
docker run --name reddog-sql -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD='"${SQLSERVER_SA_PASSWORD}"' \
  -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest

# Check infrastructure status
docker ps --filter name=reddog

# Stop infrastructure
docker stop reddog-redis reddog-sql
```

## Development Sessions

This project uses session tracking to maintain a history of development work. Sessions are stored in `.claude/sessions/` and provide context about past changes, decisions, and progress.

### Understanding Sessions

- **Location**: `.claude/sessions/`
- **Format**: Markdown files named `YYYY-MM-DD-HHMM.md` (e.g., `2025-11-01-0838.md`)
- **Current Session**: Tracked in `.claude/sessions/.current-session`

### What Sessions Contain

Each session file includes:
- Session goals and objectives
- Progress updates with timestamps
- Git changes made during the session
- Key findings and decisions
- Next steps and todos
- Notes about challenges and solutions

### Using Sessions

To understand past development work:
1. Check `.claude/sessions/.current-session` to find the active session
2. Read recent session files to understand what has been done
3. Review session goals to understand the project direction
4. Look at progress updates to see detailed changes

### Session Commands

- `/project:session-start` - Start a new development session
- `/project:session-current` - View current session status
- `/project:session-update` - Update session with progress
- `/project:session-end` - End current session

## Modernization Strategy (Target State)

🚧 **This project is undergoing active modernization** (Started: 2025-11-01)

**Progress:** Phase 0 cleanup completed. Phase 1A (.NET 10 upgrade) not yet started.

**Note:** The sections below describe the **target state** after modernization, not the current state. See "Current Development Status" above for what's actually implemented.

### Key Documents:
- **`plan/modernization-strategy.md`** - Comprehensive 8-phase modernization roadmap
- **`plan/testing-validation-strategy.md`** - Testing and validation baseline (prerequisite for Phase 1A)
- **`docs/research/`** - Research documents (upgrade analysis, gap analysis, breaking changes)
- **`.claude/sessions/`** - Detailed session logs of all decisions and progress

### Architectural Decisions:
- **`docs/adr/`** - Architectural Decision Records documenting key technical choices
  - `adr-0001-dotnet10-lts-adoption.md` - .NET 10 LTS adoption rationale
  - `adr-0002-cloud-agnostic-configuration-via-dapr.md` - Dapr abstraction for multi-cloud portability
  - `adr-0003-ubuntu-2404-base-image-standardization.md` - Ubuntu 24.04 for application containers
  - `adr-0004-dapr-configuration-api-standardization.md` - Dapr Configuration API for application settings
  - `adr-0005-kubernetes-health-probe-standardization.md` - Kubernetes health probes (/healthz, /livez, /readyz)
  - `adr-0006-infrastructure-configuration-via-environment-variables.md` - Environment variables for infrastructure config
  - `adr-0007-cloud-agnostic-deployment-strategy.md` - Containerized infrastructure for multi-cloud portability
  - `adr-0008-kind-local-development-environment.md` - kind (Kubernetes-in-Docker) for local development
  - `adr-0009-helm-multi-environment-deployment.md` - Helm charts with environment-specific values for multi-cloud deployment
  - `adr-0010-nginx-ingress-controller-cloud-agnostic.md` - Nginx Ingress Controller for cloud-agnostic HTTP routing
  - `adr-0012-dapr-bindings-object-storage.md` - Cloud-native blob storage for production (Azure Blob/S3/GCS), emptyDir for local dev

### Technical Standards:
- **`docs/standards/`** - Implementation standards for consistent development practices
  - `web-api-standards.md` - HTTP API standards (CORS, errors, versioning, health checks)

### Modernization Goals:
1. **Polyglot Architecture** - Migrate from .NET-only to 5 languages (Go, Python, Node.js, .NET, Vue.js)
2. **Modern Tech Stack** - Upgrade all dependencies to latest LTS versions
3. **Cloud-Agnostic Architecture** - Dapr abstraction enables deployment to any platform (AKS, Container Apps, EKS, GKE), showcasing infrastructure independence
4. **Teaching Focus** - Optimize for instructor-led demonstrations, not self-guided learning

### Current State vs Target State:

| Component | Current (2021) | Target (2025) |
|-----------|---------------|---------------|
| .NET | 6.0 (EOL) | 10.0 (LTS) |
| Node.js | 14 (EOL) | 24 (LTS) |
| Vue.js | 2.6 (EOL) | 3.5 |
| Dapr | 1.5.0 (Old) | 1.16 |
| KEDA | 2.2.0 (Old) | 2.17 |
| Languages | .NET only (10 services) | 5 languages (8 services) |

### Service Migration Plan:

**Keeping in .NET:**
- OrderService (core business logic)
- AccountingService (SQL Server + EF Core)

**Migrating to Go:**
- MakeLineService (queue management, concurrency)
- VirtualWorker (worker pool, performance)

**Migrating to Python:**
- ReceiptGenerationService (document generation)
- VirtualCustomers (load generation)

**Migrating to Node.js:**
- LoyaltyService (event-driven, pub/sub)

**Removing:**
- Bootstrapper (replace with init containers)
- CorporateTransferService (Arc scenarios not needed)

**Upgrading:**
- UI (Vue 2 → Vue 3)

---

## Project Overview

**Red Dog Coffee** - A microservices-based demo application showcasing Dapr (Distributed Application Runtime) in a retail order management scenario. The application simulates a multi-location coffee chain where customers order beverages, orders are processed through a queue (MakeLine), workers complete orders, loyalty points are tracked, and corporate analytics aggregate data across all stores.

## Architecture

### Microservices Communication via Dapr

**Service-to-Service Invocation:**
- Services use `DaprClient` for direct service calls
- Example: OrderService can invoke other services via Dapr service invocation API

**Pub/Sub Messaging:**
- Topic: `orders` (on `reddog.pubsub` component)
- Publisher: OrderService publishes `OrderSummary` messages
- Subscribers:
  - MakeLineService - Manages order queue
  - LoyaltyService - Updates customer loyalty points
  - ReceiptGenerationService - Generates receipts
  - AccountingService - Aggregates sales data

**State Management:**
- `reddog.state.makeline` - Redis state store for MakeLineService (order queue state)
- `reddog.state.loyalty` - Redis state store for LoyaltyService (loyalty points)
- AccountingService uses SQL Server via Entity Framework (not Dapr state)

**Output Bindings:**
- `reddog.binding.receipt` - Used by ReceiptGenerationService to store receipts
- `reddog.binding.virtualworker` - Used by VirtualWorker to complete orders

### Data Flow

1. **Order Placement:**
   - VirtualCustomers → OrderService (POST /order)
   - OrderService validates order, creates OrderSummary
   - OrderService publishes OrderSummary to `orders` topic

2. **Order Processing:**
   - MakeLineService subscribes to `orders`, adds to queue (state store)
   - LoyaltyService subscribes to `orders`, updates loyalty points
   - ReceiptGenerationService subscribes to `orders`, generates receipt
   - AccountingService subscribes to `orders`, stores in SQL DB

3. **Order Completion:**
   - VirtualWorker polls MakeLineService for orders
   - VirtualWorker completes orders via binding
   - MakeLineService updates order status in state

### Service Responsibilities

**Note:** All services listen on **port 80** inside containers (Kubernetes/Helm deployment). The ports shown below are for standalone `dapr run` CLI mode only.

- **OrderService** (standalone: 5100, container: 80): REST API for order CRUD, publishes orders to pub/sub
- **MakeLineService** (standalone: 5200, container: 80): Queue management using Redis state, exposes order status API
- **LoyaltyService** (standalone: 5400, container: 80): Manages customer loyalty points in Redis state
- **ReceiptGenerationService** (standalone: 5300, container: 80): Generates receipts via output binding
- **AccountingService** (standalone: 5700, container: 80): Aggregates order data in SQL Server, exposes analytics API
- **VirtualCustomers** (container: 80): Simulates order creation (Dapr pub/sub client)
- **VirtualWorker** (standalone: 5500, container: 80): Simulates order completion
- **Bootstrapper** (container: 80): One-time database initialization via EF migrations (console app, no HTTP listener)
- **RedDog.UI** (standalone: 8080, container: 80): Vue.js dashboard consuming MakeLineService and AccountingService APIs

### Local Development Setup

✅ **IMPLEMENTED - Phase 0.5 Complete**

**Quick Start (kind + Helm):**

1. **Prerequisites:**
   ```bash
   # Verify tooling installed
   kind version        # v0.30.0+
   kubectl version     # v1.34.1+
   helm version        # v3.19.0+
   dapr version        # v1.16.2+
   ```

2. **Configure local values:**
   ```bash
   # Copy sample config and customize
   cp values/values-local.yaml.sample values/values-local.yaml
   cp .env/local.sample .env/local
   # Edit values/values-local.yaml and .env/local with your settings
   ```

3. **Run setup script:**
   ```bash
   ./scripts/setup-local-dev.sh
   ```

   This script will:
   - Create kind cluster with Nginx Ingress and port mappings
   - Install Dapr 1.16.2 in Kubernetes mode
   - Deploy Redis and SQL Server infrastructure via Helm
   - Deploy all application services via Helm
   - Run bootstrapper job for database migrations
   - Verify all pods are healthy

4. **Access services:**
   ```bash
   # Port-forward to services
   kubectl port-forward svc/orderservice 5100:80
   kubectl port-forward svc/ui 8080:80

   # Or use ingress (if configured)
   curl http://localhost/api/orders
   ```

5. **View logs:**
   ```bash
   # Application logs
   kubectl logs -l app=orderservice -c orderservice

   # Dapr sidecar logs
   kubectl logs -l app=orderservice -c daprd
   ```

6. **Teardown:**
   ```bash
   helm uninstall reddog
   helm uninstall reddog-infra
   kind delete cluster
   ```

**Deployment Status:**
- ✅ kind cluster with Nginx Ingress
- ✅ Dapr 1.16.2 in Kubernetes mode
- ✅ Helm charts: `charts/infrastructure/` and `charts/reddog/`
- ✅ Environment-specific values: `values/values-local.yaml`
- ✅ All services deployed and healthy (2/2 Running with Dapr sidecars)
- ✅ Receipt binding working (localstorage with emptyDir + fsGroup)
- ✅ End-to-end smoke tests passing

**Alternative: Standalone Dapr (Legacy):**
If you prefer running services individually outside Kubernetes, use `dapr run` commands (see "Run Services Locally" section above).

### Database Model

`RedDog.AccountingModel` contains EF Core entities:
- Compiled models in `RedDog.AccountingModel/CompiledModels/` (generated)
- Migrations in `RedDog.Bootstrapper/Migrations/`
- AccountingContext connects to SQL Server via connection string from Dapr secret store
