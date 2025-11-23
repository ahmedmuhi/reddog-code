---
id: KI_OBSERVABILITY_OPENTELEMETRY_001
title: Observability Standards (OpenTelemetry for Red Dog Services)
tags:
  - observability
  - logging
  - tracing
  - metrics
  - opentelemetry
  - otlp
last_updated: 2025-11-09
source_sessions:
  - .claude/sessions/2025-11-01-0838.md
source_plans:
  - plan/modernization-strategy.md
confidence: high
status: active
owner: "Ahmed Muhi"
notes: >
  Captures the target observability model for Red Dog services: OpenTelemetry
  for logs, traces, and metrics with OTLP export via the OpenTelemetry Collector.
  Implementation is gated on .NET 10 and ADR-0011.
---

# Summary

This Knowledge Item defines the target observability standard for Red Dog
services, including HTTP APIs. It describes how logging, distributed tracing,
and metrics should be implemented using OpenTelemetry and exported via OTLP
to a central OpenTelemetry Collector.

It applies to all Red Dog services (.NET, Go, Python, Node.js) once the
migration described in ADR-0011 is complete.

## Key Facts

- **FACT-301**: All HTTP APIs and background services must adopt OpenTelemetry
  for logs, traces, and metrics as the standard observability stack.
- **FACT-302**: Telemetry must be exported using OTLP (OpenTelemetry Protocol)
  to the OpenTelemetry Collector, not directly to vendor-specific backends.
- **FACT-303**: Logs must be structured (JSON) and include correlation fields
  such as `traceId` and `serviceName` so that logs, traces, and metrics can
  be joined.
- **FACT-304**: Dapr (1.16+) provides automatic W3C Trace Context propagation
  across service-to-service calls, and Red Dog services must integrate with
  that model rather than inventing a separate tracing mechanism.
- **FACT-305**: The OpenTelemetry Collector is responsible for forwarding
  data to downstream systems (e.g. Loki, Prometheus, Jaeger or equivalent),
  decoupling application code from storage/vendors.

## Constraints

- **CON-301**: New services and modernized services must use native
  OpenTelemetry SDKs and OTLP exporters. They must not introduce new direct
  sinks (e.g. direct Serilog-to-Seq, direct winston-to-Elastic) that bypass
  OTLP and the collector.
- **CON-302**: Application logs must be structured JSON with at least:
  - `traceId` (if within a traced request),
  - `serviceName`,
  - `level` (log severity),
  - plus relevant domain context (e.g. `orderId` where appropriate).
- **CON-303**: The OTLP endpoints must use the agreed collector addresses:
  - OTLP gRPC: `otel-collector:4317`
  - OTLP HTTP: `otel-collector:4318`
  Application code must not hard-code alternative endpoints except via
  configuration explicitly aligned with ADR-0011.
- **CON-304**: Observability configuration (endpoints, sampling, log levels)
  must be driven by config (e.g. environment variables, Helm values, Dapr
  configuration) rather than hard-coded values.
- **CON-305**: Telemetry must not log sensitive secrets (API keys, passwords,
  tokens) or unnecessary PII.

## Patterns & Recommendations

- **PAT-301**: Logging with OpenTelemetry
  - Use the language’s native logging integration with OpenTelemetry:
    - .NET: `Microsoft.Extensions.Logging` bridged to OpenTelemetry.
    - Go: `log/slog` with `go.opentelemetry.io/contrib/bridges/otelslog`.
    - Python: structured logging (e.g. `structlog`) integrated with OTLP.
    - Node.js: `pino` or similar with OpenTelemetry instrumentation.
  - Always include `serviceName` and relevant IDs (e.g. `orderId`) in log
    scope/context rather than manually concatenating into messages.

- **PAT-302**: Tracing
  - Start a new trace or span at API boundaries (HTTP handlers) and let Dapr
    propagate context for downstream calls.
  - For significant internal operations (database calls, external services),
    create child spans to make performance and failure reasons visible.
  - Use sampling configured centrally (e.g. via env/config) rather than
  hard-coding.

- **PAT-303**: Metrics
  - Use OpenTelemetry Metrics for:
    - Request counts and latencies per endpoint,
    - Error rates,
    - Resource-specific metrics (e.g. orders processed, loyalty events).
  - Export metrics to the collector and let it forward to Prometheus or
    another metrics backend.

- **PAT-304**: Environment-specific configuration
  - Configure OTLP endpoints, sampling, and log levels via deployment
    configuration (Helm, environment variables, Dapr configuration).
  - Use sensible defaults for local development (e.g. logging to console plus
    OTLP to a local collector) while keeping the same code path.

- **PAT-305**: Migration from existing logging
  - Existing Serilog (and other library) setups should be migrated to use
    OpenTelemetry sinks/bridges rather than parallel, competing pipelines.
  - During transition, keep the configuration simple and temporary; the end
    state should be a single OpenTelemetry-first pipeline.

## Risks & Open Questions

- **RISK-301**: Running multiple, unrelated logging/tracing stacks side by
  side (e.g. Serilog to one backend, OpenTelemetry to another) will fragment
  observability and increase operational complexity.
- **RISK-302**: Misconfigured trace propagation or missing `traceId` in logs
  will make it difficult to follow a request path across services.
- **RISK-303**: Overly verbose logging or aggressive metrics without sampling
  can increase costs and degrade performance.

- **OPEN-301**: The choice of concrete downstream observability stack
  (e.g. Loki+Prometheus+Tempo vs other vendors) may evolve; this KI focuses
  on the OpenTelemetry/OTLP contract, not the final storage.
- **OPEN-302**: The detailed set of required domain-specific metrics may grow
  over time; this KI sets principles, not an exhaustive metric catalogue.

## Sources & Provenance

- **SRC-301**: `docs/standards/web-api-standards.md`
  - Section: Observability (Logging, Tracing, Metrics)
- **SRC-302**: `adr/adr-0011-opentelemetry-observability-standard.md`
- **SRC-303**: `adr/adr-0001-dotnet10-lts-adoption.md` (pre-requisite for .NET
  OpenTelemetry adoption)
