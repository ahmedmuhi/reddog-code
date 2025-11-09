---
title: "ADR-0005: Kubernetes Health Probe Endpoint Standardization"
status: "Accepted"
date: "2025-11-02"
authors: "Red Dog Modernization Team"
tags: ["architecture", "decision", "kubernetes", "health-probes", "reliability", "cloud-agnostic"]
supersedes: ""
superseded_by: ""
---

# ADR-0005: Kubernetes Health Probe Endpoint Standardization

## Status

**Accepted**

## Implementation Status

**Current State:** 🔵 Accepted (Not Fully Implemented)

**What's Working:**
- Decision documented with comprehensive implementation patterns for all languages
- Health check pattern specified for .NET, Go, Python, Node.js

**What's Not Working:**
- Current services implement `/health` endpoint (legacy pattern), not `/healthz`, `/livez`, `/readyz`
- No separation between startup, liveness, and readiness probes
- Kubernetes Deployment manifests not configured with standardized probe definitions
- Dependency checks (Dapr, database, configuration) not implemented in readiness probes

**Evidence:**
- MakeLineService likely implements `/health` (legacy pattern needs migration)
- AccountingService likely implements `/health` (legacy pattern needs migration)
- No Kubernetes manifests with startupProbe, livenessProbe, readinessProbe configurations found

**Dependencies:**
- **Depends On:** ADR-0001 (.NET 10 upgrade provides better health check APIs)
- **Depends On:** ADR-0009 (Helm charts will include probe configurations)
- **Blocks:** Production reliability, automatic failure recovery

**Next Steps:**
1. Implement `/healthz`, `/livez`, `/readyz` endpoints in OrderService and AccountingService (.NET)
2. Add dependency checks to `/readyz`: Dapr sidecar health, database connectivity
3. Migrate from `/health` to new endpoints in all polyglot services (Go, Python, Node.js)
4. Update Helm chart templates to include probe configurations with appropriate timeouts
5. Test probe behavior: simulate Dapr failure, database failure, slow startup scenarios

## Context

Red Dog's polyglot microservices architecture (8 services across 5 languages: .NET, Go, Python, Node.js, Vue.js) must deploy reliably to multiple container orchestration platforms (AKS, EKS, GKE, Azure Container Apps). Container orchestrators need mechanisms to determine service health and make automated decisions about traffic routing and container lifecycle management.

**Key Constraints:**
- Multi-cloud deployment targets (AKS, EKS, GKE, Azure Container Apps) require consistent health monitoring
- Services start at different speeds (OrderService: ~10 seconds, Go services: ~2 seconds, Python services: ~5 seconds)
- Dependencies must be ready before receiving traffic (Dapr sidecar, databases, configuration)
- Teaching/demo focus requires demonstrating production-grade reliability patterns
- REQ-004 (from `plan/orderservice-dotnet10-upgrade.md`) mandates health check endpoints for Kubernetes probes
- ADR-0002 establishes cloud-agnostic architecture principles

**Problem:**
Without standardized health endpoints:
- **Container orchestrators cannot detect failures**: Deadlocked services keep receiving traffic (bad user experience)
- **Slow-starting services receive premature traffic**: First requests fail because dependencies not ready (Dapr sidecar, database connections)
- **Failed containers linger**: Kubernetes cannot restart unhealthy containers (manual intervention required)
- **Inconsistent implementations**: Each service implements health checks differently (hard to teach, hard to debug)

**Kubernetes Health Probe Types:**
1. **Startup Probe**: "Has the application finished starting?" (prevents premature liveness checks for slow apps)
2. **Liveness Probe**: "Is the application still alive?" (restarts deadlocked/crashed containers)
3. **Readiness Probe**: "Is the application ready to receive traffic?" (removes unhealthy pods from load balancer)

**Available Options:**
1. **No Health Checks**: Rely on container orchestrator defaults (TCP port checks only, no application-level health)
2. **Custom Per-Service Paths**: Each service chooses own endpoint paths (`/health`, `/api/health`, `/status`, etc.)
3. **Kubernetes Standard Paths**: Use conventional paths (`/healthz`, `/livez`, `/readyz`) aligned with Kubernetes ecosystem

## Decision

**Adopt Kubernetes standard health probe endpoints (`/healthz`, `/livez`, `/readyz`) for ALL Red Dog microservices across ALL languages (.NET, Go, Python, Node.js).**

**Scope:**
- **All 8 services must implement**:
  - `GET /healthz` - Startup probe (basic process health)
  - `GET /livez` - Liveness probe (deadlock detection)
  - `GET /readyz` - Readiness probe (dependency health: Dapr, database, configuration)
- **Applies to**: OrderService, AccountingService, MakeLineService, VirtualWorker, ReceiptGenerationService, VirtualCustomers, LoyaltyService
- **UI (Vue.js build)**: Health endpoints in Node.js build container (if health checks needed during build)

**HTTP Response Codes:**
- **Success**: 200 OK (any response body acceptable, "Healthy", "OK", empty string)
- **Failure**: 503 Service Unavailable (or any 4xx/5xx status code)

**Rationale:**
- **HP-001**: **Cloud-Agnostic Standard**: `/healthz`, `/livez`, `/readyz` are **Kubernetes ecosystem conventions** (used by kube-apiserver, kubelet, GKE, EKS, AKS). Not proprietary to any cloud provider.
- **HP-002**: **Zero Platform-Specific Code**: Health endpoints are standard HTTP. No Azure Health Check SDK, AWS Health Check SDK, or GCP-specific code. Works with any load balancer or orchestrator.
- **HP-003**: **Identical Behavior Across Platforms**: Liveness failure → restart container (AKS, EKS, GKE, Container Apps all do this). Readiness failure → remove from load balancer (same everywhere).
- **HP-004**: **Production Reliability**: Prevents traffic to unhealthy services, automatic container restarts on deadlock, graceful handling of slow startup times.
- **HP-005**: **Polyglot Compatibility**: Standard HTTP endpoints work in any language. Same pattern for .NET, Go, Python, Node.js (language-agnostic).
- **HP-006**: **Teaching Clarity**: "All Red Dog services use `/healthz`, `/livez`, `/readyz`" - simple, memorable, consistent. Demonstrates Kubernetes best practices.
- **HP-007**: **Ecosystem Alignment**: Matches patterns used by Kubernetes components, Prometheus metrics endpoints, and cloud-native best practices.

## Consequences

### Positive

- **POS-001**: **Automated Failure Recovery**: Kubernetes automatically restarts deadlocked containers (liveness probe failure). Zero manual intervention for common failure modes.
- **POS-002**: **Traffic Protection**: Readiness probe prevents traffic to unhealthy pods. Users never hit services with failed dependencies (Dapr not ready, database down).
- **POS-003**: **Graceful Slow Startup**: Startup probe allows 60+ seconds for slow-starting apps (Java, .NET with large dependency graphs). Prevents premature restarts.
- **POS-004**: **Consistent Operational Model**: Operators know exactly where to find health endpoints across all 8 services. Debugging uses same pattern everywhere.
- **POS-005**: **Cloud Portability**: Same Kubernetes YAML works on AKS, EKS, GKE. Azure Container Apps implements same probe semantics (maintains compatibility).
- **POS-006**: **Zero Vendor Lock-In**: No dependency on Azure Health Check API, AWS health check features, or GCP-specific tooling. Pure HTTP endpoints.
- **POS-007**: **Observability Integration**: Health endpoints can be scraped by Prometheus, queried by monitoring dashboards, tested by integration tests.
- **POS-008**: **Polyglot Simplicity**: Same endpoint paths across .NET, Go, Python, Node.js. Easier to teach, easier to remember, easier to debug.

### Negative

- **NEG-001**: **Implementation Overhead**: Each service must implement 3 endpoints (`/healthz`, `/livez`, `/readyz`). Adds ~50-100 lines of code per service.
- **NEG-002**: **Dependency Checking Complexity**: Readiness probe must check Dapr sidecar, database, configuration store. Requires retry logic, timeout handling, error recovery.
- **NEG-003**: **False Positive Risk**: Overly aggressive health checks can mark healthy services as unhealthy (transient network issues, database connection pool exhaustion).
- **NEG-004**: **Probe Configuration Tuning**: `initialDelaySeconds`, `periodSeconds`, `failureThreshold` must be tuned per service. Incorrect values cause premature restarts or delayed failure detection.
- **NEG-005**: **Startup Probe Limitations**: If startup probe `failureThreshold` exceeded, Kubernetes kills container even if app would eventually start. Requires careful tuning for slow services.
- **NEG-006**: **Testing Burden**: Health endpoints must be tested in integration tests. Requires mocking Dapr, database, configuration dependencies.
- **NEG-007**: **Probe Overhead**: Kubernetes calls probes every `periodSeconds` (typically 5-10 seconds). Adds ~10-50 requests/minute per pod to service (minimal but measurable).

## Alternatives Considered

### No Health Checks (TCP Port Checks Only)

- **ALT-001**: **Description**: Rely on Kubernetes default TCP port checks. No custom HTTP health endpoints. Kubernetes considers pod healthy if TCP connection succeeds on container port.
- **ALT-002**: **Rejection Reason**: TCP checks only verify process is listening, not that application is healthy. Deadlocked app with open TCP socket appears healthy. Cannot check dependencies (Dapr, database). No way to signal "not ready" without crashing entire container. Unacceptable for production reliability.

### Custom Per-Service Endpoint Paths

- **ALT-003**: **Description**: Each service chooses own health endpoint paths. OrderService uses `/api/health`, MakeLineService uses `/status`, LoyaltyService uses `/health`, etc.
- **ALT-004**: **Rejection Reason**: Inconsistent operational model. Operators must remember different paths per service. Kubernetes YAML differs per service (error-prone). Teaching/demo confusion ("Why different paths?"). Does not align with Kubernetes ecosystem conventions.

### Single `/health` Endpoint (No Separate Liveness/Readiness)

- **ALT-005**: **Description**: Implement single `/health` endpoint for all probe types. Use same endpoint for startup, liveness, and readiness checks.
- **ALT-006**: **Rejection Reason**: Cannot differentiate between "app is alive" (liveness) and "app is ready for traffic" (readiness). Example: database temporarily unavailable → `/health` returns 503 → Kubernetes restarts container (wrong action, should just remove from load balancer). Coarse-grained failure recovery (restart) when fine-grained (stop traffic) would suffice.

### gRPC Health Checking Protocol

- **ALT-007**: **Description**: Use gRPC Health Checking Protocol (`grpc.health.v1.Health` service). Kubernetes supports gRPC probes (native gRPC health checks).
- **ALT-008**: **Rejection Reason**: Not all services use gRPC (OrderService uses HTTP REST). Requires gRPC server in every service (added complexity). Azure Container Apps does not support gRPC probes (only HTTP/TCP). Teaching overhead (explain gRPC health protocol). HTTP probes simpler and more universal.

### Exec Command Probes

- **ALT-009**: **Description**: Use Kubernetes `exec` probes (run shell command inside container, check exit code). Example: `exec: { command: ["cat", "/tmp/healthy"] }`.
- **ALT-010**: **Rejection Reason**: Azure Container Apps does not support `exec` probes (only HTTP/TCP). Requires shell utilities in container image (violates minimal container principle). Cannot check dependencies (Dapr, database) without complex shell scripts. HTTP probes cleaner and more testable.

## Implementation Notes

- **IMP-001**: **Endpoint Path Standards**:

| Endpoint | HTTP Method | Purpose | Success Code | Failure Code |
|----------|-------------|---------|--------------|--------------|
| `/healthz` | GET | Startup probe - basic process health | 200 OK | 503 Service Unavailable |
| `/livez` | GET | Liveness probe - deadlock detection | 200 OK | 503 Service Unavailable |
| `/readyz` | GET | Readiness probe - dependency health | 200 OK | 503 Service Unavailable |

- **IMP-002**: **Health Check Implementation Patterns by Language**:

**ASP.NET Core (.NET 10)**:
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDaprClient();
var app = builder.Build();

// Startup probe - basic health
app.MapGet("/healthz", () => Results.Ok("Healthy"));

// Liveness probe - deadlock detection
app.MapGet("/livez", () => Results.Ok("Alive"));

// Readiness probe - dependency checks
app.MapGet("/readyz", async (DaprClient daprClient, OrderDbContext db) =>
{
    try
    {
        // Check Dapr sidecar health
        await daprClient.CheckHealthAsync();

        // Check database connectivity
        await db.Database.CanConnectAsync();

        // Check configuration loaded
        var config = await daprClient.GetConfiguration("reddog.config", new[] { "storeId" });
        if (string.IsNullOrEmpty(config["storeId"].Value))
            return Results.StatusCode(503);

        return Results.Ok("Ready");
    }
    catch (Exception ex)
    {
        return Results.StatusCode(503);
    }
});

app.Run();
```

**Go (net/http)**:
```go
package main

import (
    "context"
    "database/sql"
    "net/http"
    dapr "github.com/dapr/go-sdk/client"
)

var daprClient dapr.Client
var db *sql.DB

func healthzHandler(w http.ResponseWriter, r *http.Request) {
    w.WriteHeader(http.StatusOK)
    w.Write([]byte("Healthy"))
}

func livezHandler(w http.ResponseWriter, r *http.Request) {
    w.WriteHeader(http.StatusOK)
    w.Write([]byte("Alive"))
}

func readyzHandler(w http.ResponseWriter, r *http.Request) {
    ctx := context.Background()

    // Check Dapr health
    if err := daprClient.CheckHealthCtx(ctx); err != nil {
        w.WriteHeader(http.StatusServiceUnavailable)
        return
    }

    // Check database
    if err := db.PingContext(ctx); err != nil {
        w.WriteHeader(http.StatusServiceUnavailable)
        return
    }

    w.WriteHeader(http.StatusOK)
    w.Write([]byte("Ready"))
}

func main() {
    http.HandleFunc("/healthz", healthzHandler)
    http.HandleFunc("/livez", livezHandler)
    http.HandleFunc("/readyz", readyzHandler)
    http.ListenAndServe(":5200", nil)
}
```

**Python (Flask/FastAPI)**:
```python
from fastapi import FastAPI, Response
from dapr.clients import DaprClient
import asyncpg

app = FastAPI()
dapr_client = DaprClient()
db_pool = None

@app.get("/healthz")
async def healthz():
    return {"status": "Healthy"}

@app.get("/livez")
async def livez():
    return {"status": "Alive"}

@app.get("/readyz")
async def readyz():
    try:
        # Check Dapr health
        dapr_client.check_health()

        # Check database connection
        async with db_pool.acquire() as conn:
            await conn.fetchval("SELECT 1")

        return {"status": "Ready"}
    except Exception as e:
        return Response(status_code=503)
```

**Node.js (Express)**:
```javascript
const express = require('express');
const { DaprClient } = require('@dapr/dapr');

const app = express();
const daprClient = new DaprClient();

app.get('/healthz', (req, res) => {
    res.status(200).send('Healthy');
});

app.get('/livez', (req, res) => {
    res.status(200).send('Alive');
});

app.get('/readyz', async (req, res) => {
    try {
        // Check Dapr health
        await daprClient.health.check();

        // Check configuration loaded
        const config = await daprClient.configuration.get('reddog.config', ['storeId']);
        if (!config.items.storeId) {
            return res.status(503).send('Configuration not loaded');
        }

        res.status(200).send('Ready');
    } catch (err) {
        res.status(503).send('Not ready');
    }
});

app.listen(5400);
```

- **IMP-003**: **Kubernetes Probe Configuration Template**:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: {{ .ServiceName }}
spec:
  template:
    spec:
      containers:
      - name: {{ .ServiceName }}
        image: ghcr.io/azure/reddog-{{ .ServiceName }}:{{ .Version }}
        ports:
        - containerPort: {{ .Port }}

        # Startup probe - allows slow startup (60 seconds max)
        startupProbe:
          httpGet:
            path: /healthz
            port: {{ .Port }}
          failureThreshold: 60    # 60 failures * 1 second = 60 seconds max startup
          periodSeconds: 1        # Check every 1 second

        # Liveness probe - restarts deadlocked containers
        livenessProbe:
          httpGet:
            path: /livez
            port: {{ .Port }}
          initialDelaySeconds: 10 # Wait 10s after startup probe succeeds
          periodSeconds: 10       # Check every 10 seconds
          failureThreshold: 3     # 3 consecutive failures triggers restart
          timeoutSeconds: 5       # 5 second timeout per probe

        # Readiness probe - removes unhealthy pods from service
        readinessProbe:
          httpGet:
            path: /readyz
            port: {{ .Port }}
          initialDelaySeconds: 5  # Wait 5s after startup probe succeeds
          periodSeconds: 5        # Check every 5 seconds
          failureThreshold: 3     # 3 consecutive failures removes from LB
          timeoutSeconds: 3       # 3 second timeout per probe
```

- **IMP-004**: **Azure Container Apps Probe Configuration**:

```yaml
properties:
  template:
    containers:
    - name: {{ .ServiceName }}
      image: ghcr.io/azure/reddog-{{ .ServiceName }}:{{ .Version }}
      probes:
      - type: startup
        httpGet:
          path: /healthz
          port: {{ .Port }}
        failureThreshold: 60
        periodSeconds: 1

      - type: liveness
        httpGet:
          path: /livez
          port: {{ .Port }}
        initialDelaySeconds: 10
        periodSeconds: 10
        failureThreshold: 3

      - type: readiness
        httpGet:
          path: /readyz
          port: {{ .Port }}
        initialDelaySeconds: 5
        periodSeconds: 5
        failureThreshold: 3
```

- **IMP-005**: **Readiness Probe Dependency Checks** (Recommended):
  1. **Dapr Sidecar Health**: `daprClient.CheckHealthAsync()` or HTTP GET `http://localhost:3500/v1.0/healthz`
  2. **Database Connectivity**: `db.CanConnectAsync()` or `SELECT 1` query
  3. **Configuration Loaded**: `daprClient.GetConfiguration()` returns expected keys
  4. **Optional**: State store health, pub/sub broker health (if critical dependencies)

- **IMP-006**: **Probe Tuning Guidelines**:

| Service Type | Startup `failureThreshold` | Liveness `periodSeconds` | Readiness `periodSeconds` |
|--------------|----------------------------|--------------------------|---------------------------|
| **Fast Startup** (Go, Python scripts) | 20 (20 seconds max) | 10s | 5s |
| **Medium Startup** (.NET, Node.js) | 60 (60 seconds max) | 10s | 5s |
| **Slow Startup** (Java, large .NET apps) | 120 (120 seconds max) | 15s | 10s |

- **IMP-007**: **Testing Strategy**:
  - **Unit Tests**: Mock dependencies (Dapr, database), verify health endpoints return 200/503
  - **Integration Tests**: Start service + Dapr sidecar + Redis, call `/readyz`, verify 200 OK
  - **E2E Tests**: Deploy to Kubernetes, simulate failures (kill Dapr, stop database), verify pod restarts/removed from LB
  - **Chaos Tests**: Use Chaos Mesh to inject failures, verify health probes trigger correct recovery

- **IMP-008**: **Migration Strategy**:
  1. **Phase 1**: Add `/healthz`, `/livez`, `/readyz` endpoints to all services (basic implementations)
  2. **Phase 2**: Update Kubernetes Deployments to use new probes (test in staging)
  3. **Phase 3**: Add dependency checks to `/readyz` (Dapr, database, configuration)
  4. **Phase 4**: Tune probe timing based on observed startup/failure patterns

- **IMP-009**: **Success Criteria**:
  - All 8 services expose `/healthz`, `/livez`, `/readyz` endpoints
  - Kubernetes Deployments configured with startup, liveness, readiness probes
  - Simulated failures (kill Dapr, database) trigger correct behavior (restart or remove from LB)
  - Zero premature restarts during normal startup
  - Health endpoints return within 100ms (fast probe responses)

## References

- **REF-001**: Related ADR: `docs/adr/adr-0002-cloud-agnostic-configuration-via-dapr.md` (cloud-agnostic architecture principles)
- **REF-002**: Related Plan: `plan/orderservice-dotnet10-upgrade.md` REQ-004 (health check endpoint requirement)
- **REF-003**: Related Plan: `plan/modernization-strategy.md` (applies to all service migrations: Go, Python, Node.js, .NET)
- **REF-004**: Kubernetes Docs: [Configure Liveness, Readiness, and Startup Probes](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/)
- **REF-005**: Azure Docs: [Health probes in Azure Container Apps](https://learn.microsoft.com/azure/container-apps/health-probes)
- **REF-006**: Kubernetes API: [Probe v1 core specification](https://kubernetes.io/docs/reference/generated/kubernetes-api/v1.28/#probe-v1-core)
- **REF-007**: Convention: Kubernetes components use `/healthz` (kube-apiserver, kubelet, kube-controller-manager)
- **REF-008**: Session Log: `.claude/sessions/2025-11-02-1105-orderservice-dotnet10-refinement.md` (health probe discussion)
