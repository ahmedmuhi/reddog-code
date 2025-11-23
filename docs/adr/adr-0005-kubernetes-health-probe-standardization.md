---
title: "ADR-0005: Kubernetes Health Probe Endpoint Standardization"
status: "Accepted"
date: "2025-11-02"
last_updated: "2025-11-23"
authors: "Red Dog Modernization Team"
tags: ["architecture", "decision", "kubernetes", "health-probes", "reliability", "cloud-agnostic"]
supersedes: ""
superseded_by: ""
---

# ADR-0005: Kubernetes Health Probe Endpoint Standardization

## Status

**Accepted**

As of 2025-11-23:

- New and modernised services are expected to follow this ADR.
- Some legacy services still expose only a single `/health` endpoint and are being migrated as part of ongoing modernization plans and health-check implementation work.

This ADR defines the canonical contract for health probe endpoints and their semantics. Concrete implementation patterns and per-language examples live in the Health Checks Knowledge Item (`knowledge/health-check-best-practices-ki.md`) and related standards/guides.

---

## Context

Red Dog is a polyglot microservices system deployed to multiple container platforms (AKS, EKS, GKE, Azure Container Apps). Container orchestrators need a consistent way to:

- Decide when a container has finished starting.
- Detect when a container is no longer healthy and should be restarted.
- Decide whether a container instance is currently safe to receive traffic.

Historically, services used a single `/health` endpoint with ad hoc semantics:

- No clear distinction between “alive” vs “ready for traffic”.
- No standard path across services.
- No standard contract for dependency checks (Dapr, database, configuration).

This caused:

- Unreliable failure detection (deadlocked processes still looked “healthy”).
- Slow-starting services receiving traffic before they were ready.
- Inconsistent patterns across languages and services, which is problematic for teaching and operations.

Kubernetes and the wider CNCF ecosystem use a conventional split between startup, liveness, and readiness probes and commonly use `/healthz`-style endpoints for HTTP health checks. We want to align with those conventions in a cloud-agnostic way.

---

## Decision

**Adopt a standard set of HTTP health endpoints for ALL Red Dog microservices:**

- `GET /healthz` — **startup** / “is the process up?”
- `GET /livez` — **liveness** / “is the process still alive and not deadlocked?”
- `GET /readyz` — **readiness** / “is this instance currently able to serve requests (dependencies OK)?”

### Endpoint contract

**HP-001 – Paths and methods**

All services (regardless of language) MUST expose the following endpoints:

| Endpoint   | Method | Purpose                         |
|-----------|--------|---------------------------------|
| `/healthz` | GET    | Startup / basic process health |
| `/livez`   | GET    | Liveness                       |
| `/readyz`  | GET    | Readiness                      |

**HP-002 – HTTP semantics**

- Success MUST be reported as **HTTP 200 OK**.
- Failure MUST be reported as **HTTP 503 Service Unavailable**  
  (other 5xx are allowed but 503 is strongly preferred).
- Response body content is not semantically significant for probes but MAY contain diagnostic text or JSON.

**HP-003 – Readiness dependencies**

The `/readyz` endpoint MUST ONLY report “ready” (200) when all *critical* dependencies for handling requests are usable. At a minimum:

- Dapr sidecar is reachable and healthy (where Dapr is used).
- Primary data store(s) (e.g. database) can be connected to.
- Any mandatory configuration required for the service to function is available.

Transient failures SHOULD be handled with reasonable timeouts and limited retries inside the readiness check; services MUST NOT block for longer than the probe timeout.

**HP-004 – Liveness behaviour**

The `/livez` endpoint MUST:

- Return 200 when the process is alive and able to make progress.
- Return 503 when the process is in a state where it is unlikely to recover without a restart (e.g. fatal internal state, unrecoverable errors, or explicit “poison pill” conditions if defined).

Liveness checks MUST NOT include brittle dependency checks that would cause unnecessary restarts during transient external outages; those belong in readiness.

**HP-005 – Startup behaviour**

The `/healthz` endpoint MUST:

- Return 200 once the application has started and is capable of running its main loop.
- Be suitable for use with Kubernetes `startupProbe` and equivalent features in other platforms.

It MAY share implementation with `/livez` for simple services, as long as startup timing and probe configuration are tuned correctly.

---

## Consequences

### Positive

- **POS-001 – Consistent operational model**  
  Operators and tools can rely on the same three endpoints for all services, regardless of language or cloud platform.

- **POS-002 – Improved reliability**  
  Kubernetes (and other orchestrators) can:
  - Restart containers that fail liveness checks.
  - Stop routing traffic to instances that fail readiness checks.
  - Avoid restarting slow-starting services prematurely via startup probes.

- **POS-003 – Cloud-agnostic behaviour**  
  The pattern uses plain HTTP and status codes only. The same manifests and semantics work on AKS, EKS, GKE, and Azure Container Apps without provider-specific health-check SDKs.

- **POS-004 – Teaching and demo clarity**  
  Workshops and documentation can use a single, memorable pattern: `/healthz`, `/livez`, `/readyz` with clear responsibilities.

### Negative

- **NEG-001 – Implementation overhead**  
  Each service must implement and maintain three endpoints, including dependency checks for `/readyz`.

- **NEG-002 – Tuning complexity**  
  Probe configuration (timeouts, periods, failure thresholds) must be tuned per service to avoid false positives or delayed failure detection.

- **NEG-003 – Testing cost**  
  Proper unit and integration tests are required to ensure health endpoints behave correctly under dependency failures and startup scenarios.

---

## Alternatives Considered

### ALT-001 – No application-level health checks (TCP only)

- **Description:** Rely on Kubernetes’ default TCP checks or simple port probing.
- **Reason rejected:** TCP-level checks only confirm that a process is listening on a socket; they cannot detect deadlocks, misconfiguration, or dependency failures. They also cannot express “not ready yet” without crashing the process.

### ALT-002 – Single `/health` endpoint for all probes

- **Description:** Use one endpoint (`/health` or similar) for startup, liveness, and readiness.
- **Reason rejected:** A single endpoint cannot cleanly express the distinction between “alive but not ready” vs “dead and must be restarted”. Using it for both liveness and readiness leads to either unnecessary restarts or insufficient protection from bad traffic.

### ALT-003 – Per-service custom paths

- **Description:** Allow each service to choose its own health endpoint paths (e.g. `/api/health`, `/status`).
- **Reason rejected:** Increases operational complexity and cognitive load. Makes Helm and deployment templates more error-prone and undermines standardisation and teaching goals.

### ALT-004 – gRPC or exec-based probes only

- **Description:** Use gRPC Health Checking Protocol or Kubernetes `exec` probes instead of HTTP endpoints.
- **Reason rejected:** Not all services expose gRPC; exec probes are not available on all target platforms (e.g. Azure Container Apps) and require extra tooling in images. HTTP probes are simpler, more portable, and adequate for our needs.

---

## Implementation Notes

This section is intentionally brief. It defines only the minimum implementation guidance that is tightly coupled to the decision; detailed examples live elsewhere.

**IN-001 – Language-specific patterns**

- Each language stack (.NET, Go, Python, Node.js, etc.) MUST implement the three endpoints using the idiomatic web framework for that service.
- Detailed patterns, anti-patterns (e.g. avoiding `new HttpClient()` per request), and unit-test examples are documented in:

  - `knowledge/health-check-best-practices-ki.md` (`KI-HEALTH_CHECKS-001`)

**IN-002 – Kubernetes / ACA probe configuration**

- Kubernetes Deployments and Azure Container Apps definitions MUST wire their probes to these endpoints:

  - `startupProbe` / `type: startup` → `GET /healthz`
  - `livenessProbe` / `type: liveness` → `GET /livez`
  - `readinessProbe` / `type: readiness` → `GET /readyz`

- Probe timing (initial delays, periods, thresholds) SHOULD follow the defaults and guidelines in the health-check KI and any relevant implementation plans, and MAY be tuned per service based on observed behaviour.

**IN-003 – Backwards compatibility**

- Legacy `/health` endpoints MAY be temporarily preserved for external tooling during migration, but new probes and manifests MUST use `/healthz`, `/livez`, `/readyz`.
- Any legacy health behaviour should be removed once all consumers have moved to the standard endpoints.

---

## References

- **Related ADRs**
  - `docs/adr/adr-0002-cloud-agnostic-configuration-via-dapr.md` — cloud-agnostic principles that motivate uniform health patterns.

- **Knowledge Items**
  - `knowledge/health-check-best-practices-ki.md` (`KI-HEALTH_CHECKS-001`) — detailed patterns, anti-patterns, and testing guidance for health checks.

- **External Documentation**
  - Kubernetes: Configure liveness, readiness, and startup probes.
  - Azure Container Apps: Health probes configuration.
