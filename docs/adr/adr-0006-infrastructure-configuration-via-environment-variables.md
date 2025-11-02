---
title: "ADR-0006: Infrastructure Configuration via Environment Variables"
status: "Accepted"
date: "2025-11-02"
authors: "Red Dog Modernization Team"
tags: ["architecture", "decision", "configuration", "environment-variables", "cloud-agnostic", "polyglot"]
supersedes: ""
superseded_by: ""
---

# ADR-0006: Infrastructure Configuration via Environment Variables

## Status

**Accepted**

## Context

Red Dog's polyglot microservices architecture (8 services across 5 languages: .NET, Go, Python, Node.js, Vue.js) requires a clear strategy for managing two distinct types of configuration:

1. **Infrastructure/Runtime Configuration**: Container ports, network addresses, Dapr sidecar ports, runtime modes
2. **Application Configuration**: Business logic settings, feature flags, operational parameters (covered by ADR-0004)

**Key Constraints:**
- Services must deploy to multiple platforms (AKS, EKS, GKE, Azure Container Apps) with different port conventions
- Same Docker image must run in different environments without rebuilding
- Infrastructure settings must be known **before application startup** (cannot wait for Dapr Configuration API calls)
- Container orchestrators (Kubernetes, Azure Container Apps) need to know listening ports for traffic routing
- Multi-language architecture requires consistent pattern across .NET, Go, Python, Node.js
- ADR-0002 establishes cloud-agnostic architecture principles
- ADR-0004 establishes Dapr Configuration API for application settings

**Problem:**
Without clear guidelines, developers might:
- **Hard-code infrastructure settings** in application code (ports, addresses) → Cannot change without rebuilding Docker image
- **Mix infrastructure and application config** → Unclear which settings use environment variables vs Dapr Configuration API
- **Platform detection logic** → Application code checks "Am I on Azure? Use port 8080. Am I on AWS? Use port 80." (violates cloud-agnostic principles)

**Real-World Scenarios Requiring Different Infrastructure Config:**

| Platform | HTTP Port | Dapr HTTP Port | Why Different |
|----------|-----------|----------------|---------------|
| **Local Development** | 5100 | 5180 | Avoid port conflicts between multiple services |
| **AKS (Kubernetes)** | 80 | 3500 | Standard HTTP port, standard Dapr port |
| **Azure Container Apps** | 8080 | 3500 | Non-privileged port convention |
| **EKS (AWS)** | 8080 | 3500 | Same as Container Apps |
| **GKE (Google)** | 8080 | 3500 | Same as Container Apps |

**Available Options:**
1. **Hard-code in application code**: `app.listen(5100)` - Requires rebuild per environment
2. **Environment variables**: Read `PORT` or `ASPNETCORE_URLS` from environment - Same image, different config
3. **Configuration files**: Bundle `appsettings.json`, `config.yaml` in Docker image - Still requires rebuild per environment
4. **Dapr Configuration API**: Retrieve from centralized store - Too late (need port before app starts)

## Decision

**Adopt environment variables as the EXCLUSIVE mechanism for infrastructure/runtime configuration across ALL Red Dog microservices.**

**Scope - What MUST Use Environment Variables:**

| Category | Examples | Why Environment Variables |
|----------|----------|---------------------------|
| **HTTP/gRPC Listening Ports** | `ASPNETCORE_URLS` (.NET), `PORT` (Go/Python/Node.js) | Container orchestrator needs to route traffic to correct port |
| **Dapr Sidecar Ports** | `DAPR_HTTP_PORT`, `DAPR_GRPC_PORT` | Application needs Dapr address before startup |
| **Network Addresses** | `HOST`, `BIND_ADDRESS` | Container binding configuration |
| **Runtime Modes** | `ASPNETCORE_ENVIRONMENT`, `NODE_ENV`, `GO_ENV` | Development vs Production behavior |
| **Logging Levels** | `LOG_LEVEL` | Infrastructure-level observability |

**Scope - What MUST NOT Use Environment Variables (Use Dapr Configuration API per ADR-0004):**

| Category | Examples | Why Dapr Configuration API |
|----------|----------|---------------------------|
| **Business Logic Settings** | `storeId`, `maxOrderSize`, `orderTimeout` | Application-level, can change at runtime |
| **Feature Flags** | `enableLoyalty`, `enableReceipts` | Dynamic toggle without redeployment |
| **Operational Parameters** | `maxRetries`, `requestTimeout` | Tune behavior without restart |

**Rationale:**
- **INF-001**: **Cloud-Agnostic Standard**: Environment variables are **universal across all platforms** (Docker, Kubernetes, Container Apps, systemd, etc.). Not proprietary to Azure, AWS, or GCP.
- **INF-002**: **Polyglot Compatibility**: All languages support environment variables (POSIX standard). Works identically in .NET, Go, Python, Node.js, shell scripts.
- **INF-003**: **Container Orchestrator Integration**: Kubernetes Deployments, Container Apps configurations, Docker Compose all provide native environment variable injection. No custom tooling required.
- **INF-004**: **Same Image, Multiple Environments**: Build Docker image **once**, deploy to dev/staging/production with different environment variables. No code changes, no rebuilds.
- **INF-005**: **Startup Dependency**: Infrastructure settings (ports, addresses) must be known **before application starts**. Cannot wait for async Dapr Configuration API call during startup.
- **INF-006**: **Clear Separation of Concerns**: Infrastructure config (how container runs) vs application config (what app does). Environment variables for "how", Dapr Config API for "what".
- **INF-007**: **12-Factor App Principle**: Follows [12-Factor App methodology](https://12factor.net/config) - "Store config in the environment" for infrastructure settings.

## Consequences

### Positive

- **POS-001**: **Zero Rebuilds for Port Changes**: Change listening port from 5100 → 8080 by updating Kubernetes YAML or Container Apps config. No Docker image rebuild.
- **POS-002**: **Platform-Agnostic Application Code**: No `if (Azure) { port = 8080 } else if (AWS) { port = 80 }` logic. Same code runs everywhere.
- **POS-003**: **Polyglot Consistency**: .NET uses `ASPNETCORE_URLS`, Go/Python/Node.js use `PORT`. Different variables, same pattern (environment-driven configuration).
- **POS-004**: **Container Orchestrator Native**: Kubernetes `env:` blocks, Container Apps `env:` arrays, Docker Compose `environment:` sections work without custom configuration loaders.
- **POS-005**: **Deployment Simplicity**: Deployment manifests (Kubernetes YAML, Bicep, Terraform) directly set environment variables. No application code changes needed.
- **POS-006**: **Testing Isolation**: Integration tests set environment variables per test (`PORT=9999`). No global state pollution, no configuration file conflicts.
- **POS-007**: **Clear Documentation**: Operators know infrastructure settings are in deployment manifests, application settings in Dapr Configuration stores. No confusion about "where is this setting?".
- **POS-008**: **Fail-Fast Validation**: Missing required environment variables (e.g., `ASPNETCORE_URLS`) cause immediate startup failure. Easy to diagnose.

### Negative

- **NEG-001**: **Environment Variable Proliferation**: Large number of environment variables across all services. Kubernetes Deployments can become verbose.
- **NEG-002**: **No Centralized Management UI**: Unlike Dapr Configuration API (Azure App Configuration UI, AWS Parameter Store UI), environment variables edited in YAML/Bicep files.
- **NEG-003**: **No Runtime Updates**: Changing environment variable requires pod restart (Kubernetes) or revision deployment (Container Apps). Cannot update without downtime.
- **NEG-004**: **Type Safety Loss**: Environment variables are strings. Must parse/validate (`int.Parse(PORT)`, `bool(ENABLE_DEBUG)`). Runtime errors if invalid values.
- **NEG-005**: **Secret Exposure Risk**: If developers mistakenly put secrets in environment variables (vs Dapr secret store), visible in `kubectl describe pod`. Requires developer education.
- **NEG-006**: **Documentation Overhead**: Must document which environment variables each service requires. Deployment guides need comprehensive env var tables.
- **NEG-007**: **Local Development Configuration**: Developers need `.env` files, shell export scripts, or IDE launch configurations to set environment variables locally.

## Alternatives Considered

### Hard-Coded Infrastructure Settings

- **ALT-001**: **Description**: Hard-code listening ports, Dapr addresses in application code. `app.listen(5100)`, `daprClient = new DaprClient("localhost:3500")`.
- **ALT-002**: **Rejection Reason**: Requires Docker image rebuild for every environment (dev: 5100, staging: 8080, production: 80). Cannot use same image across platforms. Violates immutable infrastructure principle. Deployment becomes error-prone (did we rebuild the right image?).

### Configuration Files Bundled in Docker Image

- **ALT-003**: **Description**: Bundle `appsettings.json` (. NET), `config.yaml` (Go/Python/Node.js) in Docker image with environment-specific values. Build separate images: `orderservice:dev`, `orderservice:staging`, `orderservice:prod`.
- **ALT-004**: **Rejection Reason**: Violates "build once, deploy many" principle. Must rebuild Docker image for configuration changes. Configuration drift risk (dev image accidentally deployed to production). Increases CI/CD complexity (multiple image tags to manage).

### Platform Detection Logic

- **ALT-005**: **Description**: Application code detects platform at runtime (`if (Azure) { port = 8080 } else if (AWS) { port = 80 }`). Use metadata endpoints (Azure Instance Metadata Service, AWS EC2 metadata) to identify platform.
- **ALT-006**: **Rejection Reason**: Violates cloud-agnostic principles (ADR-0002). Application code coupled to platform detection logic. Brittle (metadata endpoints change, new platforms require code updates). Testing requires mocking platform metadata. Not polyglot-friendly (Go, Python, Node.js all need platform detection libraries).

### Dapr Configuration API for All Settings

- **ALT-007**: **Description**: Use Dapr Configuration API (ADR-0004) for both application settings AND infrastructure settings. Retrieve `listeningPort`, `daprHttpPort` from Dapr Configuration store at startup.
- **ALT-008**: **Rejection Reason**: Chicken-and-egg problem. Application needs to know Dapr HTTP port (`DAPR_HTTP_PORT`) to call Dapr Configuration API. Cannot retrieve Dapr port from Dapr itself. Also, container orchestrator needs to know listening port for traffic routing BEFORE app starts (cannot wait for async Configuration API call).

## Implementation Notes

- **IMP-001**: **Required Environment Variables by Service Type**:

**All Services (.NET, Go, Python, Node.js):**
```bash
# Dapr sidecar communication
DAPR_HTTP_PORT=3500      # Dapr HTTP API endpoint
DAPR_GRPC_PORT=50001     # Dapr gRPC API endpoint (optional, HTTP sufficient)

# Runtime mode
<LANGUAGE>_ENV=production  # ASPNETCORE_ENVIRONMENT, NODE_ENV, GO_ENV, PYTHON_ENV
```

**.NET Services (OrderService, AccountingService):**
```bash
# HTTP listening configuration
ASPNETCORE_URLS=http://+:80   # ASP.NET Core binding (+ = all interfaces 0.0.0.0)

# Runtime environment
ASPNETCORE_ENVIRONMENT=Production   # Development, Staging, Production
```

**Go Services (MakeLineService, VirtualWorker):**
```bash
# HTTP listening configuration
PORT=8080           # Go standard convention (or HOST_PORT, HTTP_PORT)
HOST=0.0.0.0        # Bind address (0.0.0.0 = all interfaces)

# Runtime environment
GO_ENV=production   # development, production
```

**Python Services (ReceiptGenerationService, VirtualCustomers):**
```bash
# HTTP listening configuration
PORT=8080           # Python web framework convention (Flask, FastAPI)
HOST=0.0.0.0        # Bind address

# Runtime environment
PYTHON_ENV=production   # development, production
```

**Node.js Services (LoyaltyService):**
```bash
# HTTP listening configuration
PORT=8080           # Node.js standard convention (process.env.PORT)
HOST=0.0.0.0        # Bind address

# Runtime environment
NODE_ENV=production   # development, production
```

- **IMP-002**: **Language-Specific Implementation Patterns**:

**.NET (ASP.NET Core):**
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// ASP.NET Core automatically reads ASPNETCORE_URLS
// No explicit configuration needed!

var app = builder.Build();

// Optional: Validate required Dapr environment variables
var daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT")
    ?? throw new InvalidOperationException("DAPR_HTTP_PORT not set");

app.Run();
// Application listens on ASPNETCORE_URLS value (e.g., http://+:80)
```

**Go (net/http):**
```go
package main

import (
    "fmt"
    "net/http"
    "os"
)

func main() {
    // Read infrastructure config from environment
    port := os.Getenv("PORT")
    if port == "" {
        port = "8080" // Default fallback
    }

    host := os.Getenv("HOST")
    if host == "" {
        host = "0.0.0.0" // Default: all interfaces
    }

    daprHttpPort := os.Getenv("DAPR_HTTP_PORT")
    if daprHttpPort == "" {
        panic("DAPR_HTTP_PORT not set")
    }

    addr := fmt.Sprintf("%s:%s", host, port)
    fmt.Printf("Listening on %s\n", addr)
    http.ListenAndServe(addr, nil)
}
```

**Python (FastAPI):**
```python
import os
import uvicorn
from fastapi import FastAPI

app = FastAPI()

# Read infrastructure config from environment
port = int(os.getenv("PORT", "8080"))  # Default 8080
host = os.getenv("HOST", "0.0.0.0")    # Default all interfaces

dapr_http_port = os.getenv("DAPR_HTTP_PORT")
if not dapr_http_port:
    raise ValueError("DAPR_HTTP_PORT not set")

if __name__ == "__main__":
    uvicorn.run(app, host=host, port=port)
```

**Node.js (Express):**
```javascript
const express = require('express');
const app = express();

// Read infrastructure config from environment
const port = process.env.PORT || 8080;  // Default 8080
const host = process.env.HOST || '0.0.0.0';  // Default all interfaces

const daprHttpPort = process.env.DAPR_HTTP_PORT;
if (!daprHttpPort) {
    throw new Error('DAPR_HTTP_PORT not set');
}

app.listen(port, host, () => {
    console.log(`Listening on ${host}:${port}`);
});
```

- **IMP-003**: **Kubernetes Deployment Example (AKS/EKS/GKE)**:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: orderservice
spec:
  template:
    metadata:
      annotations:
        dapr.io/enabled: "true"
        dapr.io/app-id: "order-service"
        dapr.io/app-port: "80"
    spec:
      containers:
      - name: orderservice
        image: ghcr.io/azure/reddog-orderservice:latest
        env:
        # Infrastructure configuration (THIS ADR)
        - name: ASPNETCORE_URLS
          value: "http://+:80"
        - name: DAPR_HTTP_PORT
          value: "3500"
        - name: DAPR_GRPC_PORT
          value: "50001"
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"

        # Application configuration retrieved from Dapr Config API (ADR-0004)
        # NOT set as environment variables!
        # App calls: daprClient.GetConfiguration("reddog.config", ["storeId", "maxOrderSize"])

        ports:
        - containerPort: 80
```

- **IMP-004**: **Azure Container Apps Example**:

```yaml
properties:
  template:
    containers:
    - name: orderservice
      image: ghcr.io/azure/reddog-orderservice:latest
      env:
      # Infrastructure configuration
      - name: ASPNETCORE_URLS
        value: "http://+:8080"
      - name: DAPR_HTTP_PORT
        value: "3500"
      - name: ASPNETCORE_ENVIRONMENT
        value: "Production"

      # Application configuration from Dapr Config API (NOT env vars)

    dapr:
      enabled: true
      appId: order-service
      appPort: 8080
```

- **IMP-005**: **Docker Compose (Local Development)**:

```yaml
version: '3.8'
services:
  orderservice:
    image: reddog-orderservice
    environment:
      # Infrastructure configuration
      - ASPNETCORE_URLS=http://+:5100
      - DAPR_HTTP_PORT=5180
      - DAPR_GRPC_PORT=5181
      - ASPNETCORE_ENVIRONMENT=Development
    ports:
      - "5100:5100"
    depends_on:
      - redis
      - sqlserver
```

- **IMP-006**: **Local Development (.env file)**:

```bash
# .env (for local development - NOT committed to Git)
ASPNETCORE_URLS=http://localhost:5100
DAPR_HTTP_PORT=5180
DAPR_GRPC_PORT=5181
ASPNETCORE_ENVIRONMENT=Development
LOG_LEVEL=Debug
```

```bash
# Load .env and run
dotnet run --project RedDog.OrderService
```

- **IMP-007**: **Environment Variable Validation Pattern**:

**.NET Validation:**
```csharp
// Startup validation helper
public static class EnvironmentValidator
{
    public static void ValidateInfrastructureConfig()
    {
        var required = new[] { "ASPNETCORE_URLS", "DAPR_HTTP_PORT" };
        var missing = required.Where(v => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(v)));

        if (missing.Any())
            throw new InvalidOperationException(
                $"Missing required environment variables: {string.Join(", ", missing)}");
    }
}

// Program.cs
EnvironmentValidator.ValidateInfrastructureConfig();
var app = builder.Build();
```

- **IMP-008**: **Documentation Requirements**:
  - Each service README.md must include "Required Environment Variables" table
  - Deployment guides must specify environment variables per platform (AKS, Container Apps, EKS, GKE)
  - Example Kubernetes YAML manifests with all required environment variables
  - Local development setup instructions with `.env` file examples

- **IMP-009**: **Testing Strategy**:
  - **Unit Tests**: Mock environment variables using test frameworks (`Environment.SetEnvironmentVariable` in .NET, `os.environ` in Python)
  - **Integration Tests**: Docker Compose with test-specific environment variables (`PORT=9999` for test isolation)
  - **E2E Tests**: Verify correct port binding (`curl http://orderservice:80/healthz`)

- **IMP-010**: **Migration Strategy**:
  1. **Audit**: Identify all hard-coded infrastructure settings (ports, addresses)
  2. **Refactor**: Replace with environment variable reads
  3. **Default Values**: Provide sensible defaults for local development (`PORT ?? 5100`)
  4. **Validate**: Add startup validation for required environment variables
  5. **Document**: Update deployment manifests and README files

## References

- **REF-001**: Related ADR: `docs/adr/adr-0004-dapr-configuration-api-standardization.md` (application settings use Dapr Config API, NOT environment variables)
- **REF-002**: Related ADR: `docs/adr/adr-0002-cloud-agnostic-configuration-via-dapr.md` (cloud-agnostic architecture principles)
- **REF-003**: Related Plan: `plan/orderservice-dotnet10-upgrade.md` REQ-004 (configurable port via ASPNETCORE_URLS)
- **REF-004**: Related Plan: `plan/MODERNIZATION_PLAN.md` (applies to all service migrations: Go, Python, Node.js, .NET)
- **REF-005**: 12-Factor App: [III. Config - Store config in the environment](https://12factor.net/config)
- **REF-006**: Microsoft Docs: [ASP.NET Core Configuration - Environment Variables](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/#environment-variables)
- **REF-007**: Kubernetes Docs: [Define Environment Variables for a Container](https://kubernetes.io/docs/tasks/inject-data-application/define-environment-variable-container/)
- **REF-008**: Azure Docs: [Manage environment variables in Azure Container Apps](https://learn.microsoft.com/azure/container-apps/environment-variables)
- **REF-009**: Session Log: `.claude/sessions/2025-11-02-1105-orderservice-dotnet10-refinement.md` (ASPNETCORE_URLS discussion)
