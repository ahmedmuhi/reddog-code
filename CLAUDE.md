# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Communication Note

The user is using voice transcription for input. If you encounter any unusual phrasing or words that don't make sense in context, please ask for clarification rather than assuming the intended meaning.

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

### Modernization Goals:
1. **Polyglot Architecture** - Migrate from .NET-only to 5 languages (Go, Python, Node.js, .NET, Vue.js)
2. **Modern Tech Stack** - Upgrade all dependencies to latest LTS versions
3. **Cloud-First Deployment** - One-command deployment scripts for AKS, Container Apps, EKS, GKE
4. **Teaching Focus** - Optimize for instructor-led demonstrations, not self-guided learning

### Current State vs Target State:

| Component | Current (2021) | Target (2025) |
|-----------|---------------|---------------|
| .NET | 6.0 (EOL) | 8.0 or 9.0 (LTS) |
| Node.js | 14 (EOL) | 20 or 22 (LTS) |
| Vue.js | 2.6 (EOL) | 3.x (Current) |
| Dapr | 1.3.0 (Old) | 1.14+ (Latest) |
| KEDA | 2.2.0 (Old) | 2.16+ (Latest) |
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

**Red Dog Coffee** - A multi-location coffee chain demo application.

Red Dog is a microservices-based demo application showcasing Dapr (Distributed Application Runtime) in a retail order management scenario. The application simulates a multi-location coffee/restaurant chain (Red Dog Coffee) with services for order processing, loyalty management, receipt generation, and analytics.

**Business Domain:**
- Coffee shop chain with multiple store locations
- Customers order beverages (Americano, Latte, Caramel Macchiato, etc.)
- Orders processed through a queue (MakeLine)
- Workers (baristas) complete orders
- Loyalty points tracked per customer
- Corporate analytics across all stores

## Build Commands

### .NET Services
```bash
# Build entire solution
dotnet build

# Build specific service
dotnet build RedDog.OrderService
dotnet build RedDog.AccountingService
dotnet build RedDog.MakeLineService
dotnet build RedDog.LoyaltyService
dotnet build RedDog.ReceiptGenerationService
dotnet build RedDog.VirtualWorker
dotnet build RedDog.VirtualCustomers
dotnet build RedDog.Bootstrapper
```

### Vue.js UI
```bash
cd RedDog.UI
npm install          # Install dependencies
npm run serve        # Development server
npm run build        # Production build
npm run lint         # Lint code
```

### Database Setup
The Bootstrapper service initializes the SQL database using EF Core migrations:
```bash
# Run Bootstrapper to create database schema
# Requires DAPR_HTTP_PORT environment variable
DAPR_HTTP_PORT=5880 dotnet run --project RedDog.Bootstrapper
```

### Entity Framework Commands
```bash
# Optimize DbContext (generates compiled models)
dotnet ef dbcontext optimize -p RedDog.AccountingService -n RedDog.AccountingModel -o RedDog.AccountingModel/CompiledModels -c AccountingContext
```

## Running Services Locally

### Using VS Code Tasks
The repository includes pre-configured VS Code tasks in `.vscode/tasks.json`:
- **"Build Solution"** - Build all .NET services (default build task)
- **"Dapr (All Services)"** - Start all Dapr sidecars for all services
- Individual **"Dapr [ServiceName]"** tasks for each service

### Running Individual Services
Each service runs with Dapr sidecar. Services use ports 51XX-59XX for apps and 51XX-59XX for Dapr HTTP/gRPC ports.

Example:
```bash
# Start Dapr sidecar for OrderService
dapr run --app-id order-service \
  --components-path ./manifests/local/branch \
  --app-port 5100 \
  --dapr-grpc-port 5101 \
  --dapr-http-port 5180

# In another terminal, run the service
cd RedDog.OrderService
DAPR_HTTP_PORT=5180 DAPR_GRPC_PORT=5101 dotnet run
```

### VS Code Launch Configurations
Use `.vscode/launch.json` debug configurations:
- **"Debug All Services"** - Compound configuration to debug all services simultaneously
- Individual debug configurations for each service with proper Dapr environment variables

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

## Configuration

### Environment Variables

Each service requires Dapr ports:
```bash
DAPR_HTTP_PORT=5X80  # Dapr HTTP API port
DAPR_GRPC_PORT=5X01  # Dapr gRPC API port
ASPNETCORE_URLS=http://*:5X00  # App listening port (for ASP.NET services)
```

VirtualCustomers and VirtualWorker require:
```bash
STORE_ID=Redmond  # Store location identifier
```

### Local Development Setup

1. Ensure Dapr CLI is installed: `dapr init`
2. Start SQL Server container (configured in `.devcontainer/docker-compose.yml`)
3. Run Bootstrapper to initialize database
4. Start services using VS Code tasks or manual dapr run commands
5. For UI: Create `RedDog.UI/.env` with:
   ```
   VUE_APP_MAKELINE_BASE_URL=http://localhost:5200
   VUE_APP_ACCOUNTING_BASE_URL=http://localhost:5700
   ```

### Testing API Endpoints

REST files in `rest-samples/` directory:
- `order-service.rest` - Test order creation, product listing
- `makeline-service.rest` - Test order queue operations
- `accounting-service.rest` - Test analytics endpoints
- `ui.rest` - Test UI backend calls

Use VS Code REST Client extension or similar tools.

## Key Implementation Patterns

### Pub/Sub Subscription
Services subscribe to Dapr pub/sub topics using decorators:
```csharp
[Topic("reddog.pubsub", "orders")]
[HttpPost("orders")]
public async Task<ActionResult> HandleOrder([FromBody] CloudEvent<OrderSummary> cloudEvent)
```

### Dapr Middleware
Services must configure Dapr middleware in Startup.cs:
```csharp
app.UseCloudEvents();  // Enable CloudEvents format
endpoints.MapSubscribeHandler();  // Enable pub/sub subscriptions
services.AddControllers().AddDapr();  // Add Dapr to controllers
```

### State Management
Services use `DaprClient` for state operations:
```csharp
await _daprClient.SaveStateAsync("reddog.state.makeline", key, value);
var data = await _daprClient.GetStateAsync<T>("reddog.state.makeline", key);
```

### Service Scoping
Dapr components use `scopes` to limit which services can access them. Check YAML files for scope definitions.

## Deployment Scenarios

This codebase supports multiple deployment targets:
- **Local/Codespaces**: Uses `manifests/local/` with Redis and local storage
- **AKS**: See https://github.com/Azure/reddog-aks
- **Container Apps**: See https://github.com/Azure/reddog-containerapps
- **Hybrid/Arc**: See https://github.com/Azure/reddog-hybrid-arc (uses `manifests/corporate/` for corporate hub)

When modifying services, ensure code remains infrastructure-agnostic - Dapr handles environment differences through component configuration.
