---
id: KI-OBSERVABILITY_OPENTELEMETRY-001
title: OpenTelemetry Observability Standard for Red Dog
tags:
  - red-dog
  - observability
  - opentelemetry
  - logging
  - tracing
  - metrics
  - cloud-agnostic
last_updated: 2025-11-22
source_sessions: []
source_plans: []
confidence: high
status: Active
owner: Red Dog Modernization Team
notes: Promoted from ADR-0011 implementation details into a reusable observability standard.
---

# Summary

This knowledge item captures the long-lived observability standard for the Red Dog system based on OpenTelemetry (OTel) and the OTLP protocol. It defines how services emit logs, traces, and metrics, how the OpenTelemetry Collector is used as the canonical pipeline, and what metadata and conventions are required for consistent, cloud-agnostic observability. It is intended to guide future ADRs, implementation plans, and new services so they do not have to rediscover observability patterns.

## Key Facts

- **FACT-001**: OpenTelemetry is the **canonical observability framework** for Red Dog; all services emit logs, traces, and metrics via OTel SDKs (per language) rather than vendor-specific logging APIs.

- **FACT-002**: **OTLP (OpenTelemetry Protocol)** is the single wire protocol for all observability signals; services export telemetry only to an OpenTelemetry Collector OTLP endpoint, not directly to backends.

- **FACT-003**: An **OpenTelemetry Collector** runs as part of the platform baseline (cluster-level component) and is responsible for receiving OTLP traffic, enriching/batching, and exporting to one or more configured backends.

- **FACT-004**: Telemetry is designed to be **cloud-agnostic**: backends (e.g. Loki, Prometheus, Jaeger, or commercial tools) are chosen and configured per environment on the Collector side; service code does not change when backends change.

- **FACT-005**: All telemetry includes standard resource attributes such as `service.name`, `service.version`, and `deployment.environment` to support cross-service correlation and multi-environment analysis.

- **FACT-006**: Logs are treated as **structured events** (JSON-like), not arbitrary text; they include at minimum: timestamp (UTC), level, message/template, service name, and correlation identifiers (TraceId/SpanId when available).

- **FACT-007**: Red Dog’s observability model is **polyglot**: .NET, Go, Python, and Node.js services all use language-native OTel SDKs or official bridges, so the same OTLP/Collector pipeline works across all languages.

- **FACT-008**: Observability for Red Dog is **trace-first**: distributed traces are the primary mechanism to understand cross-service flows; logs and metrics are correlated to traces via shared identifiers.

## Constraints

- **CON-001**: Services MUST NOT call cloud-specific logging or monitoring APIs (e.g. Azure Monitor, AWS CloudWatch, GCP Cloud Logging) directly for core application telemetry; all such telemetry MUST flow through OpenTelemetry and OTLP.

- **CON-002**: All new services and major rewrites MUST adopt OpenTelemetry-based logging and tracing as their primary observability mechanism; legacy console-only logging is not acceptable as the long-term pattern.

- **CON-003**: Telemetry MUST use **UTC timestamps** and structured logging; local timezones and unstructured concatenated strings are not acceptable as the primary format.

- **CON-004**: OTLP endpoints (Collector URLs, ports, and protocols) MUST be provided via configuration (e.g. environment variables such as `OTEL_EXPORTER_OTLP_ENDPOINT`) or manifests; they MUST NOT be hard-coded in service code.

- **CON-005**: The OpenTelemetry Collector is the **only supported ingress** for OTLP traffic from services; services MUST NOT export directly to storage backends or UIs (e.g. Loki, Jaeger, Grafana, Datadog) unless an explicit exception is recorded in a separate ADR.

- **CON-006**: Any environment that claims to be “production-like” for Red Dog MUST include a correctly configured Collector and at least one backend for logs and traces; it is not valid to call an environment “prod-like” if telemetry cannot be consumed.

## Patterns & Recommendations

- **PAT-001**: For **new services**, start from a per-language OpenTelemetry setup guide (e.g. `docs/guides/opentelemetry-setup-dotnet.md`) and wire logging and tracing via official OTel SDKs from the beginning; do not add Serilog/logrus/winston first and retrofit OTel later.

- **PAT-002**: Use a **single shared Collector per cluster** (or a small number of Collectors with clear routing) with:
  - OTLP receivers on standard ports (4317 gRPC, 4318 HTTP),
  - batch and resource processors,
  - exporters configured per environment for logs, metrics, and traces.

- **PAT-003**: Treat **backends as pluggable**: the default teaching/demo stack can be Loki (logs), Prometheus (metrics), and Jaeger (traces) behind Grafana, but other backends MAY be added or swapped by changing Collector exporters only.

- **PAT-004**: Design observability as **trace-first**:
  - enable HTTP/Dapr instrumentation so each inbound request creates a root span;
  - ensure outgoing calls propagate trace context;
  - log within active spans so logs automatically include TraceId/SpanId.

- **PAT-005**: Apply a **minimal shared logging schema** across languages (maintained in `docs/standards/logging-schema.md`) including fields such as service name, environment, log level, message template, and key domain identifiers (e.g. `orderId`, `customerId`).

- **PAT-006**: For **local development**, run the Collector and at least one lightweight backend (e.g. Jaeger + Prometheus + file/debug exporter) using the same OTLP/Collector pattern as higher environments to avoid environment-specific hacks.

- **PAT-007**: When migrating existing services, treat observability as a **separate, incremental concern**: first introduce OTel side-by-side with existing logging, then phase out legacy patterns once equivalent or better coverage is confirmed.

## Risks & Open Questions

### Risks

- **RISK-001**: **Partial adoption** across services leads to “broken traces” where only some hops are visible, making debugging more confusing than having no tracing at all.

- **RISK-002**: **Misconfigured Collector** (e.g. missing exporters, incorrect endpoints, overly aggressive sampling) can silently drop telemetry, giving a false sense of observability.

- **RISK-003**: **Overly verbose logging** at `Information` or higher levels can produce high storage and egress costs, especially when correlated with traces; careful level and field selection is required.

- **RISK-004**: **Operational complexity** of managing Collector, backends, and dashboards may overwhelm smaller teaching/demo environments if not scripted and automated.

### Open Questions

- **OPEN-001**: What should be the **canonical baseline backend stack** for teaching and demos (e.g. Loki/Prometheus/Tempo vs. Loki/Prometheus/Jaeger), and how many backends are needed by default?

- **OPEN-002**: How should **front-end observability** (e.g. browser/Vue.js traces and logs) be integrated into the same trace graph and standards used by the backend services?

- **OPEN-003**: What **sampling strategies and default rates** should be applied per environment (local, dev, staging, production) to balance cost, performance, and debugging needs?

- **OPEN-004**: To what extent should legacy logging frameworks (e.g. Serilog, winston) be supported via OTel bridges vs. encouraging full migration to native OTel APIs?

## Source & Provenance

- Derived from:
  - `docs/adr/adr-0011-opentelemetry-observability-standard.md` (decision-level ADR).
- Related or expected implementation artifacts:
  - `manifests/observability/otel-collector-config.yaml` — canonical Collector configuration for Red Dog.
  - `docs/guides/opentelemetry-collector-setup.md` — Collector deployment and configuration guide.
  - `docs/guides/opentelemetry-setup-dotnet.md` / `opentelemetry-setup-go.md` / `opentelemetry-setup-python.md` / `opentelemetry-setup-nodejs.md` — per-language wiring guides.
  - `docs/standards/logging-schema.md` — shared logging schema and field definitions.
- Future implementation plans:
  - An implementation plan such as `plan/feature-opentelemetry-observability-1.md` SHOULD track rollout and migration status but is not part of this KI.
