# Web API Standards

Standards for building HTTP APIs across all Red Dog microservices.

**Applies to:** All services exposing HTTP APIs (OrderService, MakeLineService, AccountingService, LoyaltyService)
**Languages:** .NET, Go, Python, Node.js
**Last Updated:** 2025-11-09

⚠️ **Implementation Status:** These standards describe the **TARGET state** for modernized services. Current .NET 6.0 services do not yet comply. See `CLAUDE.md` → "Current Development Status" for implementation timeline (Phase 1A: .NET 10 upgrade required before adopting these standards).

---

## Red Dog Architecture Overview

⚠️ **Note:** The architecture diagram below shows the **TARGET state** after Phase 1B language migrations (planned). Currently, all services are .NET 6.0. See `CLAUDE.md` → "Current Development Status" for actual implementation state.

Red Dog Coffee demonstrates a polyglot microservices architecture using Dapr for service communication. Each service owns its data store (Database per Service pattern) and communicates via Dapr primitives.

### Data Flow Architecture (Target State)

```
┌─────────────────┐
│ OrderService    │
│ (Python/.NET)   │────┐
└─────────────────┘    │
                       │ Dapr Pub/Sub
┌─────────────────┐    │ "orders" topic
│ MakeLineService │────┤
│ (Go)            │    │
└─────────────────┘    │
                       │
┌─────────────────┐    │
│ LoyaltyService  │────┤
│ (Node.js)       │    │
└─────────────────┘    │
                       │
┌─────────────────┐    │
│ ReceiptService  │────┤
│ (Python)        │    │
└─────────────────┘    │
                       │
                       ▼
              ┌─────────────────────┐
              │ AccountingService   │◄────── REST API calls
              │ (.NET + EF Core)    │         from UI/Dashboard
              └──────────┬──────────┘
                         │
                         │ EF Core (private)
                         ▼
                  ┌──────────────┐
                  │  SQL Server  │
                  │  (Private DB)│
                  └──────────────┘
```

### Key Architectural Principles

1. **Database per Service**: Each service owns its data store
   - AccountingService: SQL Server (via Entity Framework Core)
   - MakeLineService: Redis state store (via Dapr State API)
   - LoyaltyService: Redis state store (via Dapr State API)
   - Other services: Stateless or use Dapr state stores

2. **Dapr-Based Communication**: Services never call databases directly across boundaries
   - Pub/Sub: Event-driven async communication
   - Service Invocation: Synchronous HTTP/gRPC calls
   - State Management: Key-value storage abstraction

3. **Polyglot Architecture**: Choose the right language for each service
   - .NET: SQL Server integration, complex business logic
   - Go: High-performance, concurrent workloads
   - Python: Data processing, scripting
   - Node.js: Event-driven, I/O-heavy operations

4. **No Cross-Service Database Access**: This is critical!
   - ❌ OrderService NEVER queries AccountingService's SQL database
   - ✅ OrderService publishes events → AccountingService subscribes
   - ✅ UI queries AccountingService's REST API for data

This polyglot approach is enabled by Dapr's language-agnostic APIs, allowing each service to use the best tool for its specific requirements.

---

## 1. OpenAPI / Scalar Documentation

All HTTP APIs **must** expose OpenAPI documentation with **Scalar UI** at `/scalar` endpoint.

**Why this is #1:** API specification is the foundation - it defines your API contract before implementing other concerns.

**Why Scalar:** Modern UI with code examples in 6 languages (C#, Go, Python, JavaScript, curl, etc.), dark mode, better search, and Microsoft-recommended for .NET 9+.

### Standard by Language

**.NET (Use Microsoft.AspNetCore.OpenApi + Scalar.AspNetCore per ADR-0001):**
```csharp
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
```

**Installation:** `dotnet add package Scalar.AspNetCore`
**Access:** `http://localhost:5100/scalar`

---

**Go (scalar-go):**
```go
package main

import (
    "fmt"
    "net/http"
    scalargo "github.com/bdpiprava/scalar-go"
)

func main() {
    http.HandleFunc("/scalar", func(w http.ResponseWriter, r *http.Request) {
        html, err := scalargo.NewV2(
            scalargo.WithSpecURL("/openapi.json"),
            scalargo.WithDarkMode(),
        )
        if err != nil {
            http.Error(w, err.Error(), 500)
            return
        }
        w.Header().Set("Content-Type", "text/html; charset=utf-8")
        fmt.Fprint(w, html)
    })

    http.ListenAndServe(":8080", nil)
}
```

**Installation:** `go get github.com/bdpiprava/scalar-go`
**Access:** `http://localhost:8080/scalar`

---

**Python (FastAPI with scalar-fastapi):**
```python
from fastapi import FastAPI
from scalar_fastapi import get_scalar_api_reference

app = FastAPI(
    title="Red Dog OrderService API",
    version="1.0.0",
    description="Order management API"
)

@app.get("/scalar", include_in_schema=False)
async def scalar_html():
    return get_scalar_api_reference(
        openapi_url=app.openapi_url,
        title=app.title
    )
```

**Installation:** `pip install scalar-fastapi`
**Access:** `http://localhost:5100/scalar`

---

**Node.js (Fastify with @scalar/fastify-api-reference):**
```javascript
import Fastify from 'fastify'
import scalarApiReference from '@scalar/fastify-api-reference'

const fastify = Fastify()

await fastify.register(scalarApiReference, {
  routePrefix: '/scalar',
})

await fastify.listen({ port: 3000 })
```

**Installation:** `npm install @scalar/fastify-api-reference`
**Access:** `http://localhost:3000/scalar`

---

### Why Scalar?

- **Industry Standard:** OpenAPI 3.2 remains the dominant standard for REST APIs in 2025
- **Microsoft Endorsed:** Recommended for .NET 9+ (Swashbuckle removed from defaults)
- **Teaching Value:** Code examples in 6 languages align with Red Dog's polyglot architecture
- **Modern UX:** Dark mode, advanced search, better developer experience
- **Cross-Language:** Official packages for .NET, Go, Python, Node.js
- **Zero Lock-In:** Open-source (MIT), reads standard OpenAPI JSON
- **Contract-First Development:** Define API contract before implementation

**Research:** See `docs/research/scalar-api-research.md` for detailed integration guides and package information.

---

## 2. CORS Configuration

All HTTP APIs that are called from the Vue.js UI **must** configure CORS (Cross-Origin Resource Sharing) to allow browser requests.

### Standard

- **Use application-level CORS middleware** (not cloud provider CORS features)
- **Allowed origins** configured via **Dapr Configuration API** (see ADR-0004)
- **Configuration key:** `allowedOrigins` (comma-separated list)

### Implementation by Language

**.NET (ASP.NET Core):**
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
var daprClient = new DaprClientBuilder().Build();

// Read allowed origins from Dapr Configuration API
var config = await daprClient.GetConfiguration("reddog.config", new[] { "allowedOrigins" });
var allowedOrigins = config["allowedOrigins"].Value.Split(',');

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUI", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();  // If using cookies/auth
    });
});

var app = builder.Build();

// IMPORTANT: UseCors MUST come before UseRouting
app.UseCors("AllowUI");
app.UseRouting();
app.MapControllers();

app.Run();
```

**Go (net/http with rs/cors):**
```go
package main

import (
    "context"
    "net/http"
    "strings"
    dapr "github.com/dapr/go-sdk/client"
    "github.com/rs/cors"
)

func main() {
    // Read allowed origins from Dapr Configuration API
    daprClient, _ := dapr.NewClient()
    config, _ := daprClient.GetConfigurationItem(context.Background(),
        "reddog.config", "allowedOrigins")
    allowedOrigins := strings.Split(config.Value, ",")

    // Configure CORS
    c := cors.New(cors.Options{
        AllowedOrigins:   allowedOrigins,
        AllowedMethods:   []string{"GET", "POST", "PUT", "DELETE", "OPTIONS"},
        AllowedHeaders:   []string{"*"},
        AllowCredentials: true,
    })

    mux := http.NewServeMux()
    mux.HandleFunc("/order", orderHandler)

    handler := c.Handler(mux)
    http.ListenAndServe(":8080", handler)
}
```

**Python (FastAPI):**
```python
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from dapr.clients import DaprClient

app = FastAPI()
dapr_client = DaprClient()

# Read allowed origins from Dapr Configuration API
config = dapr_client.get_configuration(store_name="reddog.config", keys=["allowedOrigins"])
allowed_origins = config.items["allowedOrigins"].value.split(",")

app.add_middleware(
    CORSMiddleware,
    allow_origins=allowed_origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)
```

**Node.js (Express):**
```javascript
const express = require('express');
const cors = require('cors');
const { DaprClient } = require('@dapr/dapr');

const app = express();
const daprClient = new DaprClient();

// Read allowed origins from Dapr Configuration API
const config = await daprClient.configuration.get('reddog.config', ['allowedOrigins']);
const allowedOrigins = config.items.allowedOrigins.value.split(',');

app.use(cors({
    origin: allowedOrigins,
    credentials: true
}));
```

### Dapr Configuration Store (Example)

**Key:** `allowedOrigins`
**Value (Local):** `http://localhost:8080`
**Value (Production):** `https://reddog-ui-aks.eastus.cloudapp.azure.com,https://reddog-ui.azurecontainerapps.io`

---

## 3. Error Response Format

All APIs **must** return errors using **RFC 7807 Problem Details** format.

### Standard Error Schema

```json
{
  "type": "https://reddog.example.com/errors/validation-error",
  "title": "Validation Error",
  "status": 400,
  "detail": "The 'quantity' field must be greater than 0",
  "instance": "/order/12345",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

### Field Descriptions

| Field | Required | Description |
|-------|----------|-------------|
| `type` | Yes | URI identifying the error type (use docs URL) |
| `title` | Yes | Short, human-readable summary |
| `status` | Yes | HTTP status code (400, 404, 500, etc.) |
| `detail` | No | Detailed explanation specific to this occurrence |
| `instance` | No | URI reference to the specific resource |
| `traceId` | No | Distributed tracing ID (for debugging) |

### Implementation by Language

**.NET (ASP.NET Core):**
```csharp
// Use built-in ProblemDetails support
app.MapPost("/order", (Order order) =>
{
    if (order.Quantity <= 0)
    {
        return Results.Problem(
            type: "https://reddog.example.com/errors/validation-error",
            title: "Validation Error",
            statusCode: 400,
            detail: "The 'quantity' field must be greater than 0",
            instance: $"/order/{order.OrderId}"
        );
    }
    // Process order...
});
```

**Go:**
```go
type ProblemDetails struct {
    Type     string `json:"type"`
    Title    string `json:"title"`
    Status   int    `json:"status"`
    Detail   string `json:"detail,omitempty"`
    Instance string `json:"instance,omitempty"`
    TraceID  string `json:"traceId,omitempty"`
}

func returnError(w http.ResponseWriter, problem ProblemDetails) {
    w.Header().Set("Content-Type", "application/problem+json")
    w.WriteHeader(problem.Status)
    json.NewEncoder(w).Encode(problem)
}
```

**Python (FastAPI):**
```python
from fastapi import HTTPException
from pydantic import BaseModel

class ProblemDetails(BaseModel):
    type: str
    title: str
    status: int
    detail: str = None
    instance: str = None
    traceId: str = None

@app.post("/order")
async def create_order(order: Order):
    if order.quantity <= 0:
        raise HTTPException(
            status_code=400,
            detail=ProblemDetails(
                type="https://reddog.example.com/errors/validation-error",
                title="Validation Error",
                status=400,
                detail="The 'quantity' field must be greater than 0"
            ).dict()
        )
```

### HTTP Status Code Usage

| Status | Usage | Example |
|--------|-------|---------|
| **400 Bad Request** | Malformed request, invalid JSON | `{"error": "Invalid JSON syntax"}` |
| **401 Unauthorized** | Missing or invalid authentication | `{"error": "API key required"}` |
| **403 Forbidden** | Valid auth, but insufficient permissions | `{"error": "Admin access required"}` |
| **404 Not Found** | Resource does not exist | `{"error": "Order 12345 not found"}` |
| **422 Unprocessable Entity** | Valid request, but business logic violation | `{"error": "Quantity must be > 0"}` |
| **429 Too Many Requests** | Rate limit exceeded | `{"error": "Rate limit: 100 req/min"}` |
| **500 Internal Server Error** | Unexpected server error | `{"error": "Database connection failed"}` |
| **503 Service Unavailable** | Service temporarily unavailable | `{"error": "Maintenance mode"}` |

---

## 4. API Versioning

APIs **should** support versioning to allow backward-compatible changes.

### Standard: URL Path Versioning

**Preferred Method:** Include version in URL path (`/v1/order`, `/v2/order`)

**Why:** Simple, explicit, works with all HTTP clients (including browsers, curl, Postman)

### Examples

```
GET /v1/product           # Version 1
GET /v2/product           # Version 2 (breaking changes)
POST /v1/order            # Version 1
POST /v2/order            # Version 2
```

### Implementation Pattern

**.NET:**
```csharp
app.MapGet("/v1/product", () => { /* V1 logic */ });
app.MapGet("/v2/product", () => { /* V2 logic */ });
```

**Go:**
```go
mux.HandleFunc("/v1/product", v1ProductHandler)
mux.HandleFunc("/v2/product", v2ProductHandler)
```

### Deprecation Strategy

1. **Announce deprecation** (6 months notice): Update API docs, add `Deprecation` header
2. **Sunset period** (3 months): Return `410 Gone` with migration guide link
3. **Remove endpoint**: After sunset period, remove v1 entirely

**Deprecation Header Example:**
```http
HTTP/1.1 200 OK
Deprecation: Sun, 01 Jun 2025 00:00:00 GMT
Sunset: Sun, 01 Sep 2025 00:00:00 GMT
Link: </v2/product>; rel="successor-version"
```

---

## 5. Health Endpoints

All HTTP APIs **must** implement health check endpoints for Kubernetes probes.

### Standard: Kubernetes Health Probes

**See ADR-0005 for comprehensive guidance.**

**Required Endpoints:**
- `GET /healthz` - Startup probe (basic process health)
- `GET /livez` - Liveness probe (deadlock detection)
- `GET /readyz` - Readiness probe (dependency health)

**Success Response:** `200 OK` with any body (`"Healthy"`, `"OK"`, `{}`)
**Failure Response:** `503 Service Unavailable`

### Quick Reference

```csharp
// .NET
app.MapGet("/healthz", () => Results.Ok("Healthy"));
app.MapGet("/livez", () => Results.Ok("Alive"));
app.MapGet("/readyz", async (DaprClient dapr, DbContext db) =>
{
    try
    {
        await dapr.CheckHealthAsync();
        await db.Database.CanConnectAsync();
        return Results.Ok("Ready");
    }
    catch { return Results.StatusCode(503); }
});
```

---

## 6. Request/Response Patterns

### JSON Naming Convention

**Standard:** Use **camelCase** for all JSON fields (not snake_case, PascalCase, or kebab-case).

**Example:**
```json
{
  "orderId": 12345,
  "productName": "Americano",
  "unitPrice": 3.50,
  "quantity": 2,
  "totalPrice": 7.00,
  "customerId": "abc-123"
}
```

### Pagination

**Standard:** Use **limit/offset** pagination for simple use cases, **cursor-based** for large datasets.

**Limit/Offset Example:**
```http
GET /v1/product?limit=20&offset=40
```

**Response:**
```json
{
  "data": [...],
  "pagination": {
    "limit": 20,
    "offset": 40,
    "total": 500,
    "hasMore": true
  }
}
```

**Cursor-Based Example (for large datasets):**
```http
GET /v1/order?limit=20&cursor=eyJpZCI6MTIzNDV9
```

**Response:**
```json
{
  "data": [...],
  "pagination": {
    "limit": 20,
    "nextCursor": "eyJpZCI6MTIzNjV9",
    "hasMore": true
  }
}
```

### Filtering and Sorting

**Filtering:** Use query parameters matching field names
```http
GET /v1/product?category=coffee&priceMin=3.00&priceMax=5.00
```

**Sorting:** Use `sort` parameter with field name and direction
```http
GET /v1/product?sort=price:asc
GET /v1/product?sort=name:desc,price:asc  # Multiple fields
```

---

## 7. HTTP Method Usage

| Method | Usage | Idempotent | Safe |
|--------|-------|------------|------|
| **GET** | Retrieve resource(s) | ✅ Yes | ✅ Yes |
| **POST** | Create new resource | ❌ No | ❌ No |
| **PUT** | Replace entire resource | ✅ Yes | ❌ No |
| **PATCH** | Update part of resource | ❌ No | ❌ No |
| **DELETE** | Remove resource | ✅ Yes | ❌ No |

**Idempotent:** Multiple identical requests have the same effect as a single request
**Safe:** Does not modify server state (read-only)

### Examples

```http
GET    /v1/product          # List all products
GET    /v1/product/123      # Get product 123
POST   /v1/order            # Create new order
PUT    /v1/product/123      # Replace product 123
PATCH  /v1/product/123      # Update product 123 fields
DELETE /v1/order/456        # Delete order 456
```

---

## 8. Authentication & Authorization

### Standard: Dapr Service Invocation (mTLS)

For **service-to-service** communication, use **Dapr service invocation** with built-in mTLS.

**Example (.NET):**
```csharp
// Call MakeLineService from OrderService
var httpClient = DaprClient.CreateInvokeHttpClient("makeline-service");
var response = await httpClient.GetAsync("/order/status/123");
```

**Security:** Dapr handles mTLS automatically (no manual certificate management).

### API Keys (External Clients)

For **external API access**, use API keys stored in **Dapr secret store** (see ADR-0004).

**.NET Example:**
```csharp
// Retrieve API key from Dapr secret store
var secrets = await daprClient.GetSecretAsync("reddog.secretstore", "api-key");
var apiKey = secrets["api-key"];

// Validate incoming request
app.Use(async (context, next) =>
{
    var requestKey = context.Request.Headers["X-API-Key"];
    if (requestKey != apiKey)
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Invalid API key");
        return;
    }
    await next();
});
```

### Rate Limiting

**Standard:** Use **KEDA** for autoscaling based on request rate, or implement application-level rate limiting.

**Simple Rate Limiting (.NET with AspNetCoreRateLimit):**
```csharp
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule { Endpoint = "*", Limit = 100, Period = "1m" }
    };
});
```

---

## 9. Distributed Tracing

All HTTP APIs **should** propagate distributed tracing headers for observability.

### Standard: OpenTelemetry via Dapr

Dapr 1.16+ provides **built-in OpenTelemetry support**. No manual instrumentation needed.

**Automatic Trace Propagation:**
- Dapr automatically adds `traceparent` header (W3C Trace Context)
- Trace IDs propagate across service-to-service calls
- Integrates with Jaeger, Zipkin, Application Insights

**Manual Instrumentation (if needed):**
```csharp
// .NET with OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());
```

---

## 10. Observability (Logging, Tracing, Metrics)

All HTTP APIs **must** implement structured logging, distributed tracing, and metrics using OpenTelemetry.

**See ADR-0011 for complete implementation guidance:** `docs/adr/adr-0011-opentelemetry-observability-standard.md`

### Quick Reference

**Standard:** Native OpenTelemetry OTLP exporters for logs, traces, and metrics

**Key Principles:**
- Native OTLP exporters (NOT Serilog, winston, logrus, or third-party sinks)
- JSON format with automatic trace correlation (TraceId, SpanId)
- Export to OpenTelemetry Collector → Loki/Prometheus/Jaeger
- Structured logging with contextual properties (OrderId, CustomerId, ServiceName)

**Implementations:**
- **.NET**: `Microsoft.Extensions.Logging` + `OpenTelemetry.Exporter.OpenTelemetryProtocol`
- **Go**: `log/slog` + `go.opentelemetry.io/contrib/bridges/otelslog`
- **Python**: `structlog` + `opentelemetry-exporter-otlp-proto-grpc`
- **Node.js**: `pino` + `@opentelemetry/instrumentation-pino`

**Required Log Properties:**
| Property | Description | Example |
|----------|-------------|---------|
| `traceId` | Distributed trace ID | `00-4bf92f3577...` |
| `serviceName` | Service identifier | `OrderService` |
| `orderId` | Order ID (if applicable) | `12345` |
| `level` | Log level | `Information`, `Error` |

**Collector Endpoints:**
- OTLP gRPC: `otel-collector:4317`
- OTLP HTTP: `otel-collector:4318`

**Full implementation details, code examples, and collector configuration:** See ADR-0011

---

## Related Documentation

- **ADR-0002:** Cloud-Agnostic Configuration via Dapr (secret management, state management)
- **ADR-0004:** Dapr Configuration API Standardization (application settings, CORS origins)
- **ADR-0005:** Kubernetes Health Probe Standardization (`/healthz`, `/livez`, `/readyz`)
- **ADR-0006:** Infrastructure Configuration via Environment Variables (ports, Dapr settings)
- **ADR-0011:** OpenTelemetry Observability Standard (logs, traces, metrics)

---

## References

- [RFC 7807: Problem Details for HTTP APIs](https://www.rfc-editor.org/rfc/rfc7807)
- [Microsoft REST API Guidelines](https://github.com/microsoft/api-guidelines)
- [Google API Design Guide](https://cloud.google.com/apis/design)
- [Zalando RESTful API Guidelines](https://opensource.zalando.com/restful-api-guidelines/)
- [OpenAPI Specification](https://spec.openapis.org/oas/latest.html)
- [W3C Trace Context](https://www.w3.org/TR/trace-context/)
