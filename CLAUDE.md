# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Communication Note

The user is using voice transcription for input. If you encounter any unusual phrasing or words that don't make sense in context, please ask for clarification rather than assuming the intended meaning.

## Start by Finding Out When Are We

Very important: The user's timezone is {datetime(.)now().strftime("%Z")}. The current date is {datetime(.)now().strftime("%Y-%m-%d")}. 

Any dates before this are in the past, and any dates after this are in the future. When the user asks for the 'latest', 'most recent', 'today's', etc. don't assume your knowledge is up to date;

## Current Development Status

**Actual State (as of 2025-11-10):**
- ✅ Phase 0 cleanup completed (removed manifests/local, manifests/corporate, CorporateTransferService, .vscode)
- ✅ Phase 0 tooling installed (Go 1.25.4, kind 0.30.0, kubectl 1.34.1, Helm 3.19.0, upgrade-assistant, ApiCompat)
- ⚠️ All services still .NET 6.0 with Dapr 1.5.0 (Phase 1A .NET 10 upgrade not started)
- ⚠️ No automated tests exist (prerequisite for Phase 1A)
- ⚠️ kind/Helm local dev not implemented (ADR-0008 planned but not built)
- ⚠️ global.json specifies .NET 10 SDK RC2, but .csproj files target net6.0

**📅 Upcoming Upgrade (November 11, 2025):**
- ⏰ **.NET 10 GA Release** - Upgrade from 10.0.100-rc.2 to 10.0.0 (GA)
- Update `global.json` to `10.0.0` after official release at .NET Conf 2025
- Verify with `dotnet --version` after upgrade

**What Works Now:**
- Building .NET 6.0 services with .NET 10 SDK
- Running services locally with `dapr run` and Dapr 1.5.0 CLI
- Vue.js 2 UI development (npm run serve)
- REST API testing via samples in `rest-samples/`

**What Doesn't Work Yet:**
- kind cluster deployment (kind-config.yaml not created)
- Helm chart deployment (charts/ directory doesn't exist)
- .NET 10 builds (projects not retargeted)
- Automated testing (no test projects)

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
- Dapr CLI 1.5.0+ for local service execution
- Node.js 14+ and npm for Vue.js UI

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
```bash
# OrderService
dapr run --app-id orderservice --app-port 5100 --dapr-http-port 5180 \
  -- dotnet run --project RedDog.OrderService

# MakeLineService
dapr run --app-id makelineservice --app-port 5200 --dapr-http-port 5280 \
  -- dotnet run --project RedDog.MakeLineService

# AccountingService
dapr run --app-id accountingservice --app-port 5700 --dapr-http-port 5780 \
  -- dotnet run --project RedDog.AccountingService

# LoyaltyService
dapr run --app-id loyaltyservice --app-port 5400 --dapr-http-port 5480 \
  -- dotnet run --project RedDog.LoyaltyService
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

- **OrderService** (port 5100): REST API for order CRUD, publishes orders to pub/sub
- **MakeLineService** (port 5200): Queue management using Redis state, exposes order status API
- **LoyaltyService** (port 5400): Manages customer loyalty points in Redis state
- **ReceiptGenerationService** (port 5300): Generates receipts via output binding
- **AccountingService** (port 5700): Aggregates order data in SQL Server, exposes analytics API
- **VirtualCustomers**: Simulates order creation (no app port, Dapr-only)
- **VirtualWorker** (port 5500): Simulates order completion
- **Bootstrapper**: One-time database initialization via EF migrations
- **RedDog.UI** (port 8080): Vue.js dashboard consuming MakeLineService and AccountingService APIs

### Local Development Setup

⚠️ **PLANNED - NOT YET IMPLEMENTED**

The following describes the **target state** per ADR-0008, but is not yet built:

**Planned Setup (kind + Helm):**
1. Create kind cluster: `kind create cluster --config kind-config.yaml`
2. Install Dapr: `dapr init --kubernetes`
3. Deploy infrastructure: `helm install reddog-infra ./charts/infrastructure -f values/values-local.yaml`
4. Deploy application: `helm install reddog ./charts/reddog -f values/values-local.yaml`

**Status:**
- ❌ kind-config.yaml doesn't exist yet
- ❌ charts/ directory doesn't exist yet
- ❌ values/values-local.yaml doesn't exist yet
- ✅ `manifests/local/` was removed November 2, 2025 (cleanup completed)

**Current Local Development:**
Use `dapr run` commands (see "Common Development Commands" section above) to run services individually with Dapr sidecars.

### Database Model

`RedDog.AccountingModel` contains EF Core entities:
- Compiled models in `RedDog.AccountingModel/CompiledModels/` (generated)
- Migrations in `RedDog.Bootstrapper/Migrations/`
- AccountingContext connects to SQL Server via connection string from Dapr secret store
