# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Communication Note

The user is using voice transcription for input. If you encounter any unusual phrasing or words that don't make sense in context, please ask for clarification rather than assuming the intended meaning.

## When Are We

Very important: The user's timezone is {datetime(.)now().strftime("%Z")}. The current date is {datetime(.)now().strftime("%Y-%m-%d")}. 

Any dates before this are in the past, and any dates after this are in the future. When the user asks for the 'latest', 'most recent', 'today's', etc. don't assume your knowledge is up to date;

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

## Modernization Status

🚧 **This project is undergoing active modernization** (Started: 2025-11-01)

**Current Phase:** Phase 0 - Foundation Cleanup

### Key Documents:
- **`plan/MODERNIZATION_PLAN.md`** - Comprehensive 8-phase modernization roadmap
- **`plan/SAFE_CLEANUP.md`** - Step-by-step guide for removing outdated components
- **`.claude/sessions/`** - Detailed session logs of all decisions and progress

### Architectural Decisions:
- **`docs/adr/`** - Architectural Decision Records documenting key technical choices
  - `adr-0001-dotnet10-lts-adoption.md` - .NET 10 LTS adoption rationale
  - `adr-0002-cloud-agnostic-configuration-via-dapr.md` - Dapr abstraction for multi-cloud portability
  - `adr-0003-ubuntu-2404-base-image-standardization.md` - Ubuntu 24.04 for all container base images
  - `adr-0004-dapr-configuration-api-standardization.md` - Dapr Configuration API for application settings
  - `adr-0005-kubernetes-health-probe-standardization.md` - Kubernetes health probes (/healthz, /livez, /readyz)
  - `adr-0006-infrastructure-configuration-via-environment-variables.md` - Environment variables for infrastructure config

### Technical Standards:
- **`docs/standards/`** - Implementation standards for consistent development practices
  - `web-api-standards.md` - HTTP API standards (CORS, errors, versioning, health checks)

### Modernization Goals:
1. **Polyglot Architecture** - Migrate from .NET-only to 5 languages (Go, Python, Node.js, .NET, Vue.js)
2. **Modern Tech Stack** - Upgrade all dependencies to latest LTS versions
3. **Cloud-First Deployment** - One-command deployment scripts for AKS, Container Apps, EKS, GKE
4. **Teaching Focus** - Optimize for instructor-led demonstrations, not self-guided learning

### Current State vs Target State:

| Component | Current (2021) | Target (2025) |
|-----------|---------------|---------------|
| .NET | 6.0 (EOL) | 10.0 (LTS) |
| Node.js | 14 (EOL) | 24 (LTS) |
| Vue.js | 2.6 (EOL) | 3.5 |
| Dapr | 1.3.0 (Old) | 1.16 |
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

### Dapr Components (Local Development)

Located in `manifests/local/branch/`:
- `reddog.pubsub.yaml` - Redis pub/sub component
- `reddog.state.makeline.yaml` - Redis state for MakeLineService
- `reddog.state.loyalty.yaml` - Redis state for LoyaltyService
- `reddog.binding.receipt.yaml` - Output binding for receipts
- `reddog.binding.virtualworker.yaml` - Output binding for order completion
- `reddog.secretstore.yaml` - Local secret store
- `secrets.json` - Local secrets file

### Database Model

`RedDog.AccountingModel` contains EF Core entities:
- Compiled models in `RedDog.AccountingModel/CompiledModels/` (generated)
- Migrations in `RedDog.Bootstrapper/Migrations/`
- AccountingContext connects to SQL Server via connection string from Dapr secret store