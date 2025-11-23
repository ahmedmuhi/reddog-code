---
id: KI-HEALTH_CHECKS-001
title: Health Check Best Practices for Red Dog Services
tags:
  - red-dog
  - health-checks
  - kubernetes
  - dotnet
  - dapr
  - observability
last_updated: 2025-11-22
source_sessions: []
source_plans: []
confidence: high
status: Active
owner: Red Dog Modernization Team
notes: Promoted from ADR-0005 implementation notes; keep aligned with ADR-0005 when probe conventions change.
---

# Summary

This knowledge item defines how Red Dog services should implement health checks for Kubernetes liveness and readiness probes, with Dapr sidecar checks as the primary example. It captures stable patterns (IHealthCheck, IHttpClientFactory, async/await, cancellation, logging, testing) and anti-patterns (new HttpClient per probe, blocking calls) that apply to all services. It is intended to guide future services and refactors so that health checks are production-ready, testable, and operationally safe.

## Key Facts

- **FACT-001**: All Red Dog services expose Kubernetes-compatible health endpoints that distinguish between **liveness** (“is the process alive?”) and **readiness** (“can this instance serve traffic?”).

- **FACT-002**: For .NET services, the canonical pattern is to implement health checks via the `IHealthCheck` interface and register them through `builder.Services.AddHealthChecks()` with appropriate tags (e.g. `"live"`, `"ready"`).

- **FACT-003**: Dapr sidecar readiness is checked by calling the Dapr HTTP health endpoint at `http://localhost:{DAPR_HTTP_PORT}/v1.0/healthz`, where `DAPR_HTTP_PORT` defaults to `3500` if not set.

- **FACT-004**: `IHttpClientFactory` is the standard mechanism for HTTP-based health checks in .NET services to avoid socket exhaustion and to centralise HTTP client configuration.

- **FACT-005**: Health checks must be fully asynchronous using `async/await`; synchronous blocking on async calls (e.g. `.Result`, `.Wait()`) is considered an anti-pattern in Red Dog.

- **FACT-006**: Health checks are part of the production contract: Kubernetes repeatedly calls liveness/readiness probes at configured intervals (typically every 5–10 seconds), so their implementation directly impacts pod stability and rollout behaviour.

- **FACT-007**: At least one production-ready implementation exists (e.g. `DaprSidecarHealthCheck` in ReceiptGenerationService), and several services (OrderService, AccountingService, MakeLineService, LoyaltyService) have historically used anti-patterns that must be migrated.

## Constraints

- **CON-001**: Health checks in .NET MUST NOT create `new HttpClient()` inside inline lambdas or health check methods on every probe; all HTTP access MUST go through `IHttpClientFactory`.

- **CON-002**: Health checks MUST NOT block on async operations using `.Result`, `.Wait()`, or similar; they MUST use asynchronous APIs end-to-end.

- **CON-003**: `CheckHealthAsync` implementations MUST accept and propagate the `CancellationToken` they receive into downstream operations (e.g. `GetAsync(url, cancellationToken)`).

- **CON-004**: Health check timeouts in code (e.g. HTTP client timeout) MUST be strictly less than the configured Kubernetes probe timeout for the same endpoint, to avoid checks outliving probe windows.

- **CON-005**: Health checks MUST be testable in isolation; implementations must use dependency injection for collaborators (e.g. `IHttpClientFactory`, `ILogger<T>`) rather than directly constructing dependencies.

- **CON-006**: Health checks MUST provide meaningful descriptions and logging to support debugging (e.g. include target URL, status code, and exception messages in unhealthy results and logs).

- **CON-007**: Any service that uses Dapr as a critical dependency MUST have a readiness check that reflects Dapr sidecar availability; services MUST NOT report themselves “ready” if Dapr is unavailable when they depend on it for normal operation.

## Patterns & Recommendations

- **PAT-001**: For each external dependency that determines readiness (e.g. Dapr sidecar, database, message broker), implement a dedicated `IHealthCheck` class that:
  - Injects required collaborators via constructor (e.g. `IHttpClientFactory`, `ILogger<T>`),
  - Uses `IHttpClientFactory.CreateClient("Name")` for HTTP calls,
  - Uses `async/await` and respects `CancellationToken`,
  - Returns `HealthCheckResult.Healthy` or `HealthCheckResult.Unhealthy` with descriptive messages.

- **PAT-002**: Register health checks in `Program.cs` using `AddHealthChecks()` with clear tags:
  - A simple inline check for liveness (e.g. `"liveness"` → always healthy if process is running),
  - One or more `IHealthCheck` implementations tagged `"ready"` for dependencies such as Dapr.

- **PAT-003**: Maintain a **service-level health matrix** (table) that tracks which services use the correct pattern, which still have anti-patterns, and which have unknown status; use this as input to migration plans.

- **PAT-004**: For each `IHealthCheck` implementation, create unit tests that cover at least:
  - Healthy response (e.g. HTTP 200),
  - Non-success response (e.g. HTTP 503),
  - Network failure (e.g. `HttpRequestException`),
  - Cancellation (e.g. `OperationCanceledException`),
  - Configuration behaviour (e.g. reading `DAPR_HTTP_PORT` environment variable).

- **PAT-005**: Run targeted integration tests that validate probe semantics end-to-end:
  - With dependency up: readiness endpoint returns 200,
  - With dependency down: readiness endpoint returns a failing status (e.g. 503),
  - Verify that Kubernetes reacts accordingly (e.g. marks pod unready, removes from service endpoints).

- **PAT-006**: Use structured logging within health checks via `ILogger<T>` and log at:
  - Debug for successful checks,
  - Warning for expected failures (e.g. non-success status from dependency),
  - Error for exceptions and unexpected failures, including relevant context.

- **PAT-007**: Treat health check configuration (probe intervals, timeouts, initial delays) as part of the service’s SLOs; store recommended probe settings alongside deployment manifests and keep them aligned with code-level timeouts.

## Risks & Open Questions

### Risks

- **RISK-001**: Using `new HttpClient()` per check can cause **socket exhaustion** under normal Kubernetes probing, leading to intermittent failures and difficulty connecting to dependencies.

- **RISK-002**: Blocking calls (e.g. `.Result`) in health checks can contribute to **thread pool starvation**, especially under load, causing health endpoints to become unresponsive and pods to be incorrectly marked unhealthy.

- **RISK-003**: Misaligned timeouts (health check timeout longer than probe timeout) can lead to **zombie checks** continuing after Kubernetes has already considered the probe failed.

- **RISK-004**: Lack of tests for health checks increases the risk that seemingly small changes (timeouts, URLs, ports) cause silent regressions that only surface during production rollouts.

- **RISK-005**: Overly aggressive readiness checks (e.g. failing on non-critical dependencies) may cause pods to remain unready for longer than necessary, reducing effective capacity.

### Open Questions

- **OPEN-001**: What should be the **canonical timeout and interval settings** for liveness and readiness probes across environments (dev, test, production), and should they be standardised in a shared manifest fragment?

- **OPEN-002**: How should **health check metrics** (e.g. latency, failure counts) be exposed and integrated with the broader OpenTelemetry observability stack (e.g. Prometheus, Grafana)?

- **OPEN-003**: To what extent should non-Dapr dependencies (databases, message brokers) be included in readiness vs. separate diagnostic endpoints to avoid making readiness too “fragile”?

- **OPEN-004**: Should we enforce a **repository-wide health check style guide** (including naming, tags, and log schema) and, if so, where should that standard live (e.g. `docs/standards/health-checks.md`)?

## Source & Provenance

- Derived from:
  - `docs/adr/adr-0005-kubernetes-health-probe-standardization.md` (decision-level ADR for probe endpoints and conventions).
  - `docs/adr/adr-0005-implementation-notes-health-check-best-practices.md` (implementation notes with example `DaprSidecarHealthCheck` and anti-patterns).
- Related implementation examples:
  - ReceiptGenerationService health check implementation (`DaprSidecarHealthCheck` class) as the canonical .NET pattern.
- Related documentation:
  - Microsoft documentation on ASP.NET Core health checks and `IHttpClientFactory`.
  - Kubernetes documentation on liveness, readiness, and startup probes.
