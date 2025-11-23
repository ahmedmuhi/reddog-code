---
title: "ADR-0011: OpenTelemetry Observability Standard (Logs, Traces, Metrics)"
status: "Accepted"
date: "2025-11-09"
authors: "Red Dog Modernization Team"
tags: ["architecture", "decision", "observability", "logging", "tracing", "metrics", "opentelemetry", "cloud-agnostic"]
supersedes: ""
superseded_by: ""
---

# ADR-0011: OpenTelemetry Observability Standard (Logs, Traces, Metrics)

## Status

**Accepted**

Implementation is being rolled out incrementally. Detailed steps and task-level status are tracked in:

- `plan/feature-opentelemetry-observability-1.md` (implementation plan and migration status)

This ADR defines the *architectural standard*; it does not track day-to-day migration progress.

---

## Context

Red Dog is a polyglot, multi-cloud microservices system:

- Services are implemented in **.NET, Go, Python, and Node.js**.
- Target platforms include **AKS, EKS, GKE, and Azure Container Apps**.
- ADR-0002 establishes a **cloud-agnostic architecture** using Dapr components as the main abstraction for platform services.

Current observability is minimal and fragmented:

- Most services log to **console only**, with no consistent structure.
- There is **no distributed tracing** and **no central correlation** of requests that cross multiple services.
- There is **no single, vendor-neutral pipeline** for logs, traces, and metrics.
- Each language tends to adopt its own logging library and conventions.

This makes it difficult to:

- Diagnose issues in cross-service flows (e.g., `OrderService → MakeLineService → LoyaltyService`).
- Provide consistent teaching/demo experiences that show modern cloud-native observability.
- Keep the architecture portable across clouds without coupling to Azure Monitor, CloudWatch, or Stackdriver.

We need a single, cloud-agnostic observability standard that:

- Works across all Red Dog languages and services.
- Exposes logs, traces, and metrics in a unified way.
- Allows backends to change (Jaeger, Loki, Prometheus, commercial tools) without code changes.

---

## Decision

**Adopt OpenTelemetry (OTel) with OTLP as the canonical observability standard for all logs, traces, and metrics across all Red Dog services and languages.**

Concretely:

- All services emit **logs, traces, and metrics via OpenTelemetry SDKs** in their respective languages.
- All telemetry is exported using the **OpenTelemetry Protocol (OTLP)** to a shared **OpenTelemetry Collector**.
- The Collector fans out to one or more backends (e.g. Loki for logs, Prometheus for metrics, Jaeger for traces, or equivalent), chosen per environment.
- No service writes directly to cloud-specific logging APIs (Azure Monitor, CloudWatch, etc.) or proprietary agent SDKs.

This decision defines the **contract** for how services emit telemetry. It does **not** mandate a specific observability UI (Grafana vs. vendor tools) as long as they can consume OTLP/Collector outputs.

---

## Scope

Within the scope of this ADR:

- **Services**  
  - All core microservices: `OrderService`, `AccountingService`, `MakeLineService`, `VirtualWorker`, `ReceiptGenerationService`, `VirtualCustomers`, `LoyaltyService`, and future services.
  - Background workers and job processors that participate in business flows.

- **Signals**  
  - **Logs:** Structured, JSON-like logs exported via OTel.
  - **Traces:** Distributed traces across service boundaries, including HTTP and Dapr calls.
  - **Metrics:** Basic service metrics (requests, errors, latency) and key business metrics as needed.

- **Environments**  
  - Local development, shared dev/test clusters, and production-like environments on AKS/EKS/GKE/Container Apps.

Out of scope for this ADR:

- Choice of specific long-term backend vendors (Grafana vs. Datadog vs. Application Insights).
- Detailed SDK wiring and configuration examples for each language (captured in guides).

---

## Key Principles

The following principles define the architectural contract:

1. **Native OpenTelemetry SDKs**  
   Services use the **official OTel SDKs** for their language (e.g. `OpenTelemetry.*` for .NET) rather than vendor-specific or third-party logging sinks as the primary mechanism.

2. **OTLP as the single wire protocol**  
   All signals (logs, traces, metrics) are exported over **OTLP** (HTTP or gRPC) to an **OpenTelemetry Collector** endpoint. Services do not export directly to individual backends.

3. **Cloud-agnostic backends**  
   The Collector is responsible for exporting telemetry to environment-specific backends (Loki, Prometheus, Jaeger, or commercial tools). Swapping or adding backends is a **deployment decision**, not a code change.

4. **Consistent resource metadata**  
   All telemetry includes standard resource attributes such as:
   - `service.name`
   - `service.version`
   - `deployment.environment` (e.g. `local`, `dev`, `prod`)

5. **Trace-first design**  
   - Incoming external requests are traced from the edge (gateway/ingress) through downstream calls.
   - Application logs emitted within a trace context automatically include trace identifiers (TraceId/SpanId) for correlation.
   - Dapr and other infrastructure are configured to participate in the same trace context where reasonable.

6. **Structured logging standard**  
   - Logs follow a minimal, shared schema (e.g. timestamp, level, message template, service name, key domain IDs such as OrderId/CustomerId).
   - The detailed schema is maintained in a separate standard document:
     - `docs/standards/logging-schema.md`

7. **Configuration via environment / manifests**  
   - Service code uses configuration (e.g. environment variables like `OTEL_EXPORTER_OTLP_ENDPOINT`) rather than hard-coded endpoints.
   - Canonical Collector configuration for the project lives alongside manifests, not baked into service code.

---

## Architectural Shape

At a high level, observability in Red Dog follows this pattern:

1. **Application services**  
   - Emit logs, traces, and metrics via language-specific OTel SDKs.
   - Export them to the local cluster endpoint exposed by the OpenTelemetry Collector using OTLP.

2. **OpenTelemetry Collector**  
   - Runs as a shared component in the cluster (e.g. Deployment/DaemonSet).
   - Receives OTLP traffic on standard ports (4317/4318).
   - Applies resource/tag enrichment and batching.
   - Exports to configured backends (Loki, Prometheus, Jaeger, or equivalents).

3. **Backends and UI**  
   - Backends are environment-specific and can be swapped without changing service code.
   - Typical baseline: Loki (logs), Prometheus (metrics), Jaeger (traces) visualized via Grafana.

Canonical, repo-specific configuration is kept in:

- `manifests/observability/otel-collector-config.yaml` (Collector config)
- `docs/guides/opentelemetry-collector-setup.md` (Collector deployment guide)
- `docs/guides/opentelemetry-setup-<language>.md` (per-language wiring guides)

---

## Consequences

### Positive

- **Unified observability model**  
  All services and languages emit telemetry in a consistent way, through the same protocol and Collector.

- **Cloud and vendor portability**  
  Backends and vendors can change by reconfiguring the Collector; service code and SDK usage remain the same.

- **Distributed tracing and correlation**  
  Cross-service flows can be traced end-to-end, and logs can be correlated via TraceId/SpanId.

- **Teaching and demo value**  
  The project demonstrates current CNCF best practice (OpenTelemetry) instead of fragmented, service-specific logging.

- **Alignment with Dapr and ADR-0002**  
  Observability follows the same “cloud-agnostic, component-based” philosophy as Dapr for platform services.

### Negative

- **Migration effort**  
  Existing logging code (e.g. Serilog-only console logging) must be migrated or adapted to emit via OpenTelemetry.

- **Additional infrastructure**  
  The OpenTelemetry Collector (and any default backends like Loki/Prometheus/Jaeger) must be deployed and operated as part of the baseline platform.

- **Learning curve**  
  Teams must learn OpenTelemetry concepts (resources, spans, exporters, Collector pipelines) in addition to existing platform knowledge.

- **Configuration complexity**  
  Collector configuration and environment-specific exporters add YAML and operational complexity that must be managed carefully.

---

## Alternatives Considered

### 1. Continue with Serilog + Console (status quo)

- **Description:** Keep per-service logging frameworks (e.g. Serilog) writing to console; rely on cluster log aggregation or cloud-native logging.
- **Reason Rejected:** No first-class distributed tracing, no unified cross-language standard, and strong coupling to per-cloud logging products.

### 2. Serilog + OpenTelemetry Sink (hybrid)

- **Description:** Keep Serilog as the primary logging API in .NET services and add an OTel/OTLP sink for export.
- **Pros:** Smaller code changes for .NET; gradual migration.
- **Reason Rejected as target state:** Adds an extra abstraction layer, is not polyglot across languages, and does not align as cleanly with official OpenTelemetry SDKs as the long-term standard. Acceptable as a **transition tactic**, not a final architecture.

### 3. Cloud-specific logging stacks (Azure Monitor, CloudWatch, Stackdriver)

- **Description:** Use each cloud’s native logging and tracing solution directly from services.
- **Reason Rejected:** Violates Red Dog’s cloud-agnostic goals and ADR-0002; increases cognitive load; makes teaching and multi-cloud demos harder.

---

## Implementation Notes

- New services MUST use OpenTelemetry from the beginning, following the per-language guides.
- Existing services SHOULD be migrated in batches, according to the tasks and phases defined in:
  - `plan/feature-opentelemetry-observability-1.md`
- The presence and configuration of the OpenTelemetry Collector is treated as **part of the platform baseline**, similar to ingress and Dapr.

---

## Relationship to Other ADRs

- **ADR-0002 — Cloud-Agnostic Configuration via Dapr**  
  - This ADR applies the same cloud-agnostic principle to observability; backend choice is handled by configuration and the Collector, not by code.

- **ADR-0005 — Kubernetes Health Probe Standardization**  
  - Health probes complement this ADR by ensuring basic liveness/readiness signals; OpenTelemetry provides deeper diagnostics.

- **ADR-0010 — Nginx Ingress Controller for Cloud-Agnostic Traffic Routing**  
  - Ingress sits at the edge of request flows; OpenTelemetry traces should begin at or near this boundary for end-to-end visibility.

---

## Related Documentation

The detailed “how-to” material for wiring and running OpenTelemetry lives outside this ADR:

- `docs/standards/logging-schema.md` — Standard logging fields and levels.
- `docs/guides/opentelemetry-collector-setup.md` — How to deploy and configure the Collector.
- `docs/guides/opentelemetry-setup-dotnet.md` — .NET specific setup.
- `docs/guides/opentelemetry-setup-go.md` — Go specific setup.
- `docs/guides/opentelemetry-setup-python.md` — Python specific setup.
- `docs/guides/opentelemetry-setup-nodejs.md` — Node.js specific setup.

External references:

- [OpenTelemetry Specification](https://opentelemetry.io/docs/specs/otel/)
- [OTLP Protocol](https://opentelemetry.io/docs/specs/otlp/)
- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/languages/net/)
- [CNCF OpenTelemetry Project](https://www.cncf.io/projects/opentelemetry/)
