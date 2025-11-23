---
id: KI-HTTP-API-STANDARDS-CORE-001
title: Core HTTP API Standards for Red Dog Services
tags:
  - http
  - api
  - standards
  - web
  - contracts
last_updated: 2025-11-09
source_sessions:
  - .claude/sessions/2025-11-01-0838.md
source_plans:
  - plan/modernization-strategy.md
confidence: high
status: active
owner: "Ahmed Muhi"
notes: >
  Captures the stable HTTP API contract for all Red Dog services. Language-
  specific examples live in docs/standards/web-api-standards.md.
---

# Summary

This Knowledge Item defines the core HTTP API contract for all Red Dog
microservices. It standardises documentation endpoints, JSON conventions,
error formats, versioning, health endpoints, and basic request/response
patterns across languages (.NET, Go, Python, Node.js).

It applies to all services exposing HTTP APIs, including OrderService,
MakeLineService, AccountingService, and LoyaltyService.

## Key Facts

- **FACT-001**: All HTTP APIs must expose an OpenAPI specification and Scalar
  UI at the `/scalar` endpoint.
- **FACT-002**: JSON payloads must use `camelCase` for all field names in
  request and response bodies.
- **FACT-003**: Error responses exposed to clients must follow RFC 7807
  Problem Details format, with at least `type`, `title`, and `status` fields.
- **FACT-004**: Public HTTP APIs must be versioned using URL path prefixes
  such as `/v1/...` and `/v2/...` for breaking changes.
- **FACT-005**: All services must implement Kubernetes-friendly health
  endpoints:
  - `GET /healthz` – startup probe,
  - `GET /livez` – liveness probe,
  - `GET /readyz` – readiness probe.
- **FACT-006**: Pagination must support `limit/offset` for simple use cases,
  and cursor-based pagination for large datasets or unbounded lists.
- **FACT-007**: Standard query parameters must be used for filtering and
  sorting to maintain consistent UX across services.

## Constraints

- **CON-101**: No new HTTP endpoint may be added without an OpenAPI definition
  included in the service’s specification.
- **CON-102**: Error payloads returned to UI or external clients must conform
  to Problem Details semantics:
  - `type`: stable URI identifying the error type,
  - `title`: human-readable summary,
  - `status`: HTTP status code that matches the actual response status.
- **CON-103**: Public API routes must include a version prefix in their URL
  path, except for infrastructure endpoints such as `/scalar`, `/healthz`,
  `/livez`, `/readyz`.
- **CON-104**: JSON field naming must be `camelCase` end-to-end. Introducing
  snake_case, PascalCase, or kebab-case fields in JSON contracts is not
  allowed.
- **CON-105**: Health endpoints must return `200 OK` when healthy and
  `503 Service Unavailable` when not ready or not live; they must not expose
  sensitive internal exception details.

## Patterns & Recommendations

- **PAT-101**: Use URL path versioning as the default versioning mechanism:
  - `GET /v1/product`, `GET /v2/product`,
  - `POST /v1/order`, `POST /v2/order`.
- **PAT-102**: For limit/offset pagination:
  - Request pattern: `GET /v1/resource?limit=20&offset=40`
  - Response pattern:
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
- **PAT-103**: For cursor-based pagination:
  - Request pattern: `GET /v1/resource?limit=20&cursor=...`
  - Response pattern:
    ```json
    {
      "data": [...],
      "pagination": {
        "limit": 20,
        "nextCursor": "opaque-string",
        "hasMore": true
      }
    }
    ```
- **PAT-104**: Filtering and sorting:
  - Filtering via field-aligned query parameters, e.g.
    `GET /v1/product?category=coffee&priceMin=3.00&priceMax=5.00`.
  - Sorting via `sort` parameter with `field:direction`, e.g.
    `?sort=price:asc` or `?sort=name:desc,price:asc`.
- **PAT-105**: For health checks:
  - Implement `/healthz` as a shallow process check.
  - Implement `/livez` to detect deadlocks or critical internal failure.
  - Implement `/readyz` to validate dependencies (DB, Dapr, external services)
    and only return `200` when the service can accept traffic.

## Risks & Open Questions

- **RISK-101**: Divergence between OpenAPI specs and actual implemented
  endpoints will break client generation, testing, and documentation accuracy.
- **RISK-102**: Services that do not follow Problem Details will make error
  handling inconsistent in the UI and harder to reason about.
- **RISK-103**: Inconsistent pagination or filtering patterns across services
  will increase cognitive load for consumers and complicate reuse of UI
  components.
- **OPEN-101**: The exact deprecation window for older API versions (e.g.
  how long `/v1` remains available after `/v2` is introduced) depends on
  product and operations decisions and may vary by environment.
- **OPEN-102**: Whether some internal-only endpoints may omit path versioning
  is not fully decided; if allowed, they must be clearly documented and not
  exposed to external clients.

## Sources & Provenance

- **SRC-001**: `docs/standards/web-api-standards.md` – “Web API Standards”
- **SRC-002**: `adr/adr-0005-kubernetes-health-probe-standardization.md`
- **SRC-003**: RFC 7807 – Problem Details for HTTP APIs
- **SRC-004**: Microsoft REST API Guidelines and OpenAPI specification
