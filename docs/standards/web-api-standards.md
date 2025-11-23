# HTTP Web API Standard

Normative standard for building HTTP APIs across all Red Dog microservices.

**Applies to:** All services exposing HTTP APIs  
(OrderService, MakeLineService, AccountingService, LoyaltyService, ReceiptGenerationService, UI backends)

**Languages:** .NET, Go, Python, Node.js

This document defines the required behaviour and contracts for HTTP APIs in Red Dog.  
It is intentionally independent of current implementation status or migration phase.  
Implementation progress and modernization timelines are tracked in sessions and implementation
plans, not in this standard.

For related architectural knowledge and deeper rationale, see:

- `knowledge/ki-red-dog-architecture-001.md` – service boundaries and data ownership  
- `knowledge/ki-http-api-standard-core-001.md` – core HTTP API contract (JSON, errors, health, versioning)  
- `knowledge/ki-http-api-platform-integration-001.md` – CORS, Dapr integration, API keys, rate limiting  
- `knowledge/ki-observability-opentelemetry-001.md` – logging, tracing, metrics with OpenTelemetry  

---

## 0. Architectural Context (Informative)

Red Dog is a polyglot microservices system where each service owns its data (Database-per-Service)
and communicates across boundaries via Dapr primitives (pub/sub, service invocation, state, bindings).
The UI talks to backends via HTTP APIs only; no component is allowed to access another service’s
database directly.

This standard describes how those HTTP APIs must behave at the edge of each service.  
For architectural boundaries and data ownership, see `knowledge/ki-red-dog-architecture-001.md`.

---

## 1. OpenAPI & Scalar Documentation

All HTTP APIs **MUST** expose an OpenAPI specification and a Scalar UI at the `/scalar` endpoint.

### 1.1 Requirements

1. An OpenAPI document **MUST** describe all public HTTP endpoints of the service.
2. A Scalar API reference UI **MUST** be available at `/scalar` and **MUST** render the OpenAPI document.
3. The OpenAPI document **MUST** use OpenAPI 3.x.
4. The `/scalar` endpoint:
   - **MUST** be enabled in at least one environment used for development and testing.
   - **MAY** be disabled or access-controlled in production if required by security policy.
5. The OpenAPI document **SHOULD** include:
   - Operation summaries and descriptions.
   - Request/response schemas and examples.
   - Standardised error responses using RFC 7807 (see Section 3).

### 1.2 Examples (Non-Normative)

#### Example (.NET, non-normative)

```csharp
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference(); // exposes /scalar
````

#### Example (FastAPI, non-normative)

```python
from fastapi import FastAPI
from scalar_fastapi import get_scalar_api_reference

app = FastAPI()

@app.get("/scalar", include_in_schema=False)
async def scalar_html():
    return get_scalar_api_reference(
        openapi_url=app.openapi_url,
        title=app.title,
    )
```

Other languages (Go, Node.js) may follow their respective Scalar integrations as long as the
behaviour above is respected.

---

## 2. CORS (Cross-Origin Resource Sharing)

This section applies to HTTP APIs invoked from the browser (e.g. Vue.js UI).

### 2.1 Requirements

1. CORS **MUST** be implemented using application-level middleware in each service:

   * e.g. ASP.NET Core CORS middleware, FastAPI CORSMiddleware, Express `cors`, etc.
2. CORS configuration **MUST NOT** rely solely on cloud ingress/load balancer CORS features.
   Platform-level CORS **MAY** be used as an additional defence, but not as the primary mechanism.
3. Allowed origins **MUST** be provided via configuration, not hard-coded in source:

   * preferred: Dapr Configuration API (e.g. store `reddog.config`, key `allowedOrigins`),
   * acceptable fallback: environment variables or equivalent config system.
4. All environments (local, test, staging, production) **MUST** use the same mechanism for
   specifying allowed origins (even if the values differ).
5. The configured origins **SHOULD** be as narrow as practical (exact UI origins, not `*`).

For more detail and patterns, see `knowledge/ki-http-api-platform-integration-001.md`
and ADR-0002 / ADR-0004.

---

## 3. Error Response Format (RFC 7807 Problem Details)

All HTTP APIs **MUST** return errors using RFC 7807 *Problem Details for HTTP APIs*.

### 3.1 Standard Error Schema

Error responses **MUST** conform to the following structure:

```json
{
  "type": "https://reddog.example.com/errors/validation-error",
  "title": "Validation Error",
  "status": 400,
  "detail": "The 'quantity' field must be greater than 0",
  "instance": "/v1/order/12345",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

### 3.2 Required Fields

| Field      | Required | Description                                                          |
| ---------- | -------- | -------------------------------------------------------------------- |
| `type`     | Yes      | Stable URI identifying the error type (Red Dog error catalogue URL). |
| `title`    | Yes      | Short, human-readable summary of the error type.                     |
| `status`   | Yes      | HTTP status code of the response (e.g. 400, 404, 500).               |
| `detail`   | No       | Human-readable explanation specific to this occurrence.              |
| `instance` | No       | URI reference to the specific resource or request path.              |
| `traceId`  | No       | Distributed trace identifier (if available) for correlation.         |

### 3.3 Behaviour

1. Error responses **MUST** use the `application/problem+json` content type.
2. The `status` field **MUST** match the actual HTTP response status code.
3. The `type` URI **SHOULD** be stable and documented (not a per-request URL).
4. Services **MUST NOT** return ad-hoc JSON error shapes to UI or external clients;
   Problem Details is the canonical error format.

### 3.4 HTTP Status Code Usage

The following table standardises semantics. Examples below are illustrative; the actual payloads
**MUST** follow Problem Details format.

| Status | Usage                                                     |
| ------ | --------------------------------------------------------- |
| 400    | Malformed request, validation failures.                   |
| 401    | Missing or invalid authentication.                        |
| 403    | Authenticated but insufficient permissions.               |
| 404    | Resource not found.                                       |
| 422    | Business rule violation (semantic error).                 |
| 429    | Rate limit exceeded.                                      |
| 500    | Unexpected server error.                                  |
| 503    | Temporarily unavailable (maintenance, dependencies down). |

---

## 4. API Versioning

### 4.1 Requirements

1. Public HTTP APIs **SHOULD** include an explicit version prefix in the URL path:

   * e.g. `/v1/order`, `/v2/order`.
2. A new major version (e.g. `v2`) **MUST** be introduced for breaking changes to contracts.
3. Old versions **MAY** be maintained for a deprecation window as defined by product/ops policies.
4. Internal / infrastructure endpoints (e.g. `/scalar`, `/healthz`, `/livez`, `/readyz`)
   **MAY** be unversioned.

### 4.2 Examples (Informative)

```http
GET /v1/product         # Version 1
GET /v2/product         # Version 2 (breaking changes)
POST /v1/order          # Create order (v1 contract)
POST /v2/order          # Create order (v2 contract)
```

### 4.3 Deprecation Headers (Recommended)

APIs that are being deprecated **SHOULD** advertise their status via HTTP headers:

```http
Deprecation: Sun, 01 Jun 2025 00:00:00 GMT
Sunset: Sun, 01 Sep 2025 00:00:00 GMT
Link: </v2/product>; rel="successor-version"
```

The exact deprecation window is an operational decision and **MUST** be documented per API.

---

## 5. Health Endpoints

All HTTP services **MUST** expose Kubernetes-friendly health endpoints.

### 5.1 Required Endpoints

1. `GET /healthz` – startup / basic process health.
2. `GET /livez` – liveness probe (process not deadlocked or crashed).
3. `GET /readyz` – readiness probe (service and critical dependencies ready).

### 5.2 Behaviour

1. Healthy responses **MUST** return `200 OK`.
2. Unhealthy / not ready responses **MUST** return `503 Service Unavailable`.
3. Bodies **MAY** be simple (`"OK"`, `"Healthy"`, `{}`), but:

   * **MUST NOT** expose sensitive details or stack traces.
4. `/readyz` **SHOULD** check critical dependencies:

   * database connectivity where applicable,
   * essential Dapr components for this service (e.g. state store, pub/sub, bindings).

Detailed probe configuration is defined in ADR-0005.

---

## 6. Request / Response Patterns

### 6.1 JSON Naming

1. All JSON request and response bodies **MUST** use `camelCase` for field names.
2. `snake_case`, `PascalCase`, and `kebab-case` **MUST NOT** be introduced in public JSON contracts.
3. If underlying models use different naming, mapping **MUST** be applied at the HTTP boundary.

**Example:**

```json
{
  "orderId": 12345,
  "productName": "Americano",
  "unitPrice": 3.5,
  "quantity": 2,
  "totalPrice": 7.0,
  "customerId": "abc-123"
}
```

### 6.2 Pagination

#### 6.2.1 Limit/Offset (Simple Lists)

1. Simple list endpoints **SHOULD** support `limit` and `offset` query parameters.

**Request:**

```http
GET /v1/product?limit=20&offset=40
```

**Response shape (informative):**

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

#### 6.2.2 Cursor-Based (Large or Unbounded Lists)

1. Endpoints returning large or unbounded result sets **SHOULD** use cursor-based pagination.

**Request:**

```http
GET /v1/order?limit=20&cursor=eyJpZCI6MTIzNDV9
```

**Response shape (informative):**

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

### 6.3 Filtering and Sorting

1. Filtering **SHOULD** use query parameters that match field names or well-documented filters:

   ```http
   GET /v1/product?category=coffee&priceMin=3.00&priceMax=5.00
   ```

2. Sorting **SHOULD** use a `sort` parameter with `field:direction` pairs:

   ```http
   GET /v1/product?sort=price:asc
   GET /v1/product?sort=name:desc,price:asc
   ```

3. The set of supported filters and sort fields **MUST** be documented in OpenAPI.

---

## 7. HTTP Method Usage

### 7.1 Semantics

| Method | Usage                        | Idempotent |  Safe |
| ------ | ---------------------------- | ---------: | ----: |
| GET    | Retrieve resource(s)         |      ✅ Yes | ✅ Yes |
| POST   | Create new resource / action |       ❌ No |  ❌ No |
| PUT    | Replace entire resource      |      ✅ Yes |  ❌ No |
| PATCH  | Partially update a resource  |       ❌ No |  ❌ No |
| DELETE | Remove resource              |      ✅ Yes |  ❌ No |

* **Idempotent:** Multiple identical requests have the same effect as one.
* **Safe:** Does not modify server state.

### 7.2 Examples (Informative)

```http
GET    /v1/product           # List all products
GET    /v1/product/123       # Get product 123
POST   /v1/order             # Create new order
PUT    /v1/product/123       # Replace product 123
PATCH  /v1/product/123       # Partially update product 123
DELETE /v1/order/456         # Delete order 456
```

---

## 8. Authentication & Authorization

### 8.1 Internal Service-to-Service Calls

1. Internal service-to-service HTTP calls **MUST** use Dapr Service Invocation.
2. Internal calls **MUST** rely on Dapr’s mutual TLS (mTLS) for transport security.
3. Services **MUST NOT** call each other directly via:

   * pod IPs, node ports, cluster-internal load balancers,
   * or other ad-hoc HTTP mechanisms for normal internal use.

**Informative example (.NET):**

```csharp
var httpClient = DaprClient.CreateInvokeHttpClient("makeline-service");
var response = await httpClient.GetAsync("/v1/order/status/123");
```

### 8.2 External Clients (API Keys)

1. External access to HTTP APIs **MUST** be authenticated.
2. API keys for external clients **MUST** be stored in a Dapr secret store (e.g. `reddog.secretstore`)
   or equivalent secret management mechanism defined by ADR-0002.
3. API keys **MUST NOT** be:

   * hard-coded in source,
   * stored in public configuration,
   * logged in plaintext.
4. Requests from external clients **MUST** be validated using a documented mechanism
   (e.g. `X-API-Key` header).
5. Invalid or missing credentials **MUST** result in appropriate Problem Details errors:

   * `401 Unauthorized` for missing/invalid credentials,
   * `403 Forbidden` for insufficient permissions.

### 8.3 Rate Limiting

1. Internet-exposed HTTP APIs **SHOULD** implement rate limiting.
2. Rate limiting **MAY** be implemented via:

   * platform mechanisms (e.g. KEDA, API gateways, ingress),
   * application-level middleware.
3. Policies **MUST** be documented (limits, windows, retry guidance).
4. Exceeded limits **SHOULD** return `429 Too Many Requests` with a Problem Details body and
   optional `Retry-After` header.

Further patterns are described in `knowledge/ki-http-api-platform-integration-001.md`.

---

## 9. Observability (Logging, Tracing, Metrics)

This section defines the target observability behaviour for Red Dog HTTP APIs.
Detailed implementation guidance is in ADR-0011 and `knowledge/ki-observability-opentelemetry-001.md`.

### 9.1 Requirements

1. All services **MUST** use OpenTelemetry as the primary mechanism for:

   * logs,
   * traces,
   * metrics.
2. Telemetry **MUST** be exported via OTLP (OpenTelemetry Protocol) to an OpenTelemetry Collector,
   not directly to vendor-specific endpoints.
3. Application logs **MUST** be structured (machine-parseable) and **SHOULD** use JSON.
4. Logs related to traced requests **MUST** include:

   * `traceId` (if part of an active trace),
   * `serviceName`,
   * log level,
   * relevant domain identifiers (e.g. `orderId`) where applicable.
5. Dapr’s W3C Trace Context propagation **MUST** be honoured; services **MUST NOT**
   invent incompatible trace mechanisms.
6. Observability configuration (endpoints, sampling, log level) **MUST** be driven by configuration
   (environment, Helm values, Dapr config) and **MUST NOT** be hard-coded.

### 9.2 Data Flow (Informative)

* Services emit OTLP telemetry → OpenTelemetry Collector → downstream systems (e.g. Loki, Prometheus, Jaeger, Tempo, or vendor-specific backends).
* The choice of downstream stack may change without modifying application code.

### 9.3 Safety

1. Telemetry **MUST NOT** include:

   * secrets (API keys, passwords, tokens),
   * unnecessary PII.
2. Verbose logging and high-cardinality metrics **SHOULD** be controlled via sampling and configuration
   to avoid cost and performance issues.

---

## Related Architectural Decisions (Informative)

The following ADRs provide deeper context for this standard:

* **ADR-0001: .NET 10 LTS Adoption**
  – Modern .NET baseline for services and tooling.

* **ADR-0002: Cloud-Agnostic Configuration via Dapr**
  – Secret and configuration management via Dapr stores.

* **ADR-0004: Dapr Configuration API Standardization**
  – Standard mechanism for application configuration (CORS, feature flags, etc.).

* **ADR-0005: Kubernetes Health Probe Standardization**
  – Detailed health endpoint and probe configuration.

* **ADR-0006: Infrastructure Configuration via Environment Variables**
  – Infrastructure settings (ports, endpoints) as env vars.

* **ADR-0007: Cloud-Agnostic Deployment Strategy**
  – Deployment to AKS, EKS, GKE with a consistent architecture.

* **ADR-0009: Helm Multi-Environment Deployment**
  – Multi-environment Helm configuration (values files, templates).

* **ADR-0011: OpenTelemetry Observability Standard**
  – Logging, tracing, metrics via OpenTelemetry and OTLP.

---

## External References (Informative)

* RFC 7807: Problem Details for HTTP APIs
* Microsoft REST API Guidelines
* Google API Design Guide
* Zalando RESTful API Guidelines
* OpenAPI Specification
* W3C Trace Context
