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

## Context

Red Dog's polyglot microservices architecture (8 services across 5 languages: .NET, Go, Python, Node.js, Vue.js) requires unified observability to debug distributed transactions, monitor system health, and troubleshoot production issues across multiple cloud platforms (AKS, EKS, GKE, Azure Container Apps).

**Key Constraints:**
- Multi-language architecture requires consistent observability patterns across .NET, Go, Python, Node.js
- Cloud-agnostic deployment targets (AKS, EKS, GKE, Container Apps) require vendor-neutral telemetry export
- Distributed tracing needed to correlate requests across 8 microservices
- Teaching/demo focus requires demonstrating industry-standard observability practices
- ADR-0002 establishes cloud-agnostic architecture principles
- Legacy implementation uses Serilog 4.1.0 with console logging (no distributed tracing, no centralized aggregation)

**Problem:**
Without standardized observability:
- **No distributed tracing**: Cannot correlate logs across service boundaries (OrderService → MakeLineService → LoyaltyService)
- **No centralized logging**: Logs scattered across container stdout (must SSH to each pod to debug)
- **No vendor-neutral export**: Locked into cloud-specific logging (Azure Monitor, CloudWatch, Stackdriver)
- **Inconsistent implementations**: Each language uses different logging framework (Serilog, winston, logrus, etc.)
- **No automatic trace correlation**: TraceId/SpanId not automatically injected into logs

**Available Options:**
1. **Continue with Serilog + Console Logging**: No changes, no distributed tracing, no centralized aggregation
2. **Serilog + OpenTelemetry Sink**: Add OTLP exporter to Serilog (hybrid approach, maintains Serilog API)
3. **Native OpenTelemetry**: Use native OTLP exporters for logs, traces, metrics (industry standard, vendor-neutral)

## Decision

**Adopt Native OpenTelemetry (OTLP) for ALL observability signals (logs, traces, metrics) across ALL Red Dog microservices and ALL languages (.NET, Go, Python, Node.js).**

**Scope:**
- **All 8 services must implement**:
  - Structured logging with native OTLP exporters (not Serilog sinks)
  - Distributed tracing via OpenTelemetry instrumentation
  - Metrics export via OTLP protocol
- **Applies to**: OrderService, AccountingService, MakeLineService, VirtualWorker, ReceiptGenerationService, VirtualCustomers, LoyaltyService
- **Export protocol**: OTLP (OpenTelemetry Protocol) via HTTP (port 4318) or gRPC (port 4317)
- **Backend**: OpenTelemetry Collector → Loki (logs), Prometheus (metrics), Jaeger (traces)

### Key Principles

- **Native OTLP exporters** (not third-party sinks like Serilog.Sinks.OpenTelemetry)
- **JSON format** for logs (not plain text)
- **UTC timestamps** (no local timezones)
- **Automatic trace context correlation** (TraceId, SpanId injected automatically)
- **Contextual properties** (OrderId, CustomerId, ServiceName)
- **Push to OpenTelemetry Collector** via OTLP protocol (HTTP or gRPC)

### Why OpenTelemetry?

**OpenTelemetry (OTEL)** is the industry standard for observability in 2025:
- **Polyglot support:** Works with .NET, Go, Python, Node.js, and 20+ languages
- **Unified backend:** Single pipeline for logs, traces, and metrics
- **Vendor-neutral:** Export to Jaeger, Grafana, Application Insights, Datadog, etc.
- **Native Dapr integration:** Dapr 1.16+ has built-in OTEL support
- **Logs stable:** OTLP Logs 1.0 specification released October 2024

**Rationale:**
- **OBS-001**: **Cloud-Agnostic Standard**: OpenTelemetry is vendor-neutral CNCF project. Not locked into Azure Monitor, AWS CloudWatch, or GCP Stackdriver. Same OTLP export works on any cloud.
- **OBS-002**: **Zero Platform-Specific Code**: OTLP exporters are standard libraries. No Azure SDK, AWS SDK, or GCP SDK for logging. Works with any observability backend.
- **OBS-003**: **Identical Behavior Across Platforms**: Same telemetry collection works on AKS, EKS, GKE, Container Apps. Backend choice (Jaeger, Grafana, Datadog) is deployment-time decision.
- **OBS-004**: **Polyglot Compatibility**: Single OTLP protocol works across .NET, Go, Python, Node.js. Language-agnostic observability.
- **OBS-005**: **Teaching Clarity**: "All Red Dog services use OpenTelemetry OTLP" - simple, memorable, industry-standard. Demonstrates modern observability best practices.
- **OBS-006**: **Ecosystem Alignment**: Matches patterns used by Kubernetes, Prometheus, Jaeger, and cloud-native best practices.
- **OBS-007**: **Automatic Trace Correlation**: TraceId/SpanId automatically injected into logs when logging within traced operations. No manual correlation needed.

## Implementation by Language

### .NET (Microsoft.Extensions.Logging + Native OTLP Exporter)

**Why Native:** No third-party dependencies, Microsoft-supported, automatic trace correlation

**Installation:**
```bash
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
```

**Configuration (Program.cs):**
```csharp
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

// Configure OpenTelemetry logging
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

// Configure OpenTelemetry SDK
var otel = builder.Services.AddOpenTelemetry();

// Add resource attributes (service name, version)
otel.ConfigureResource(resource => resource
    .AddService("OrderService", serviceVersion: "1.0.0"));

// Add tracing for automatic trace context correlation
otel.WithTracing(tracing =>
{
    tracing.AddAspNetCoreInstrumentation();
    tracing.AddHttpClientInstrumentation();
});

// Add metrics (optional)
otel.WithMetrics(metrics =>
{
    metrics.AddAspNetCoreInstrumentation();
    metrics.AddMeter("Microsoft.AspNetCore.Hosting");
});

// Export to OTLP collector
otel.UseOtlpExporter();

var app = builder.Build();

// Example usage with contextual properties
app.MapPost("/order", (Order order, ILogger<Program> logger) =>
{
    logger.LogInformation(
        "Order created: OrderId={OrderId}, CustomerId={CustomerId}, Quantity={Quantity}",
        order.OrderId, order.CustomerId, order.Quantity);

    return Results.Ok();
});

app.Run();
```

**Configuration (appsettings.json or environment variables):**
```json
{
  "OTEL_EXPORTER_OTLP_ENDPOINT": "http://otel-collector:4318",
  "OTEL_EXPORTER_OTLP_PROTOCOL": "http/protobuf"
}
```

**Key Features:**
- **Automatic trace correlation:** TraceId/SpanId injected when logging within traced operations
- **Structured logging:** Use message templates with named parameters
- **Environment configuration:** Endpoint configurable via `OTEL_EXPORTER_OTLP_ENDPOINT`
- **JSON format:** Logs exported in OTLP JSON/Protobuf format

---

### Go (slog + OpenTelemetry Bridge)

**Why slog:** Standard library since Go 1.21, zero external dependencies, official OTEL bridge support

**Installation:**
```bash
go get go.opentelemetry.io/contrib/bridges/otelslog
go get go.opentelemetry.io/otel/exporters/otlp/otlplog/otlploghttp
go get go.opentelemetry.io/otel/sdk/log
```

**Configuration:**
```go
package main

import (
    "context"
    "log/slog"
    "os"

    "go.opentelemetry.io/contrib/bridges/otelslog"
    "go.opentelemetry.io/otel/exporters/otlp/otlplog/otlploghttp"
    "go.opentelemetry.io/otel/log/global"
    sdklog "go.opentelemetry.io/otel/sdk/log"
    "go.opentelemetry.io/otel/sdk/resource"
    semconv "go.opentelemetry.io/otel/semconv/v1.26.0"
)

func main() {
    ctx := context.Background()

    // Create resource with service information
    res, err := resource.New(ctx,
        resource.WithAttributes(
            semconv.ServiceName("makeline-service"),
            semconv.ServiceVersion("1.0.0"),
        ),
    )
    if err != nil {
        panic(err)
    }

    // Create OTLP log exporter (HTTP)
    exporter, err := otlploghttp.New(ctx,
        otlploghttp.WithEndpoint("otel-collector:4318"),
        otlploghttp.WithInsecure(),
    )
    if err != nil {
        panic(err)
    }

    // Create logger provider with batch processor
    loggerProvider := sdklog.NewLoggerProvider(
        sdklog.WithResource(res),
        sdklog.WithProcessor(sdklog.NewBatchProcessor(exporter)),
    )
    defer loggerProvider.Shutdown(ctx)

    // Set global logger provider
    global.SetLoggerProvider(loggerProvider)

    // Create slog logger with OpenTelemetry handler
    logger := otelslog.NewLogger("makeline-service",
        otelslog.WithLoggerProvider(loggerProvider),
    )

    // Use slog normally - trace context automatically included
    logger.Info("order created",
        slog.String("orderId", "12345"),
        slog.String("customerId", "abc-123"),
        slog.Int("quantity", 2),
    )
}
```

**Key Features:**
- **Standard library:** No external logging dependencies
- **Automatic trace correlation:** TraceId/SpanId injected when logging within traced operations
- **Batch processing:** Efficient batching before export
- **Resource attributes:** Service name/version attached to all logs

---

### Python (structlog + OTLPLogExporter)

**Why structlog:** Industry-standard structured logging for Python (Dropbox, Stripe)

**Installation:**
```bash
pip install structlog
pip install opentelemetry-api
pip install opentelemetry-sdk
pip install opentelemetry-exporter-otlp-proto-grpc
```

**Configuration:**
```python
import logging
import structlog
from opentelemetry import trace
from opentelemetry._logs import set_logger_provider
from opentelemetry.exporter.otlp.proto.grpc._log_exporter import OTLPLogExporter
from opentelemetry.sdk._logs import LoggerProvider, LoggingHandler
from opentelemetry.sdk._logs.export import BatchLogRecordProcessor
from opentelemetry.sdk.resources import Resource

# Create logger provider with service information
logger_provider = LoggerProvider(
    resource=Resource.create({
        "service.name": "receipt-service",
        "service.version": "1.0.0",
    })
)
set_logger_provider(logger_provider)

# Create OTLP log exporter
exporter = OTLPLogExporter(endpoint="http://otel-collector:4317", insecure=True)
logger_provider.add_log_record_processor(BatchLogRecordProcessor(exporter))

# Attach OTLP handler to root logger
handler = LoggingHandler(level=logging.INFO, logger_provider=logger_provider)
logging.getLogger().addHandler(handler)
logging.getLogger().setLevel(logging.INFO)

# Add trace context processor for structlog
def add_trace_context(logger, method_name, event_dict):
    """Add OpenTelemetry trace context to structlog events."""
    span = trace.get_current_span()
    if span:
        span_context = span.get_span_context()
        event_dict["trace_id"] = format(span_context.trace_id, "032x")
        event_dict["span_id"] = format(span_context.span_id, "016x")
    return event_dict

# Configure structlog to use stdlib logging (which has OTLP handler)
structlog.configure(
    processors=[
        structlog.stdlib.filter_by_level,
        add_trace_context,
        structlog.stdlib.add_logger_name,
        structlog.stdlib.add_log_level,
        structlog.processors.TimeStamper(fmt="iso"),
        structlog.stdlib.ProcessorFormatter.wrap_for_formatter,
    ],
    logger_factory=structlog.stdlib.LoggerFactory(),
    cache_logger_on_first_use=True,
)

# Use structlog normally
logger = structlog.get_logger()
logger.info("receipt_generated", order_id="12345", customer_id="abc-123", amount=45.50)

# Shutdown before exit
logger_provider.shutdown()
```

**Key Features:**
- **OTLPLogExporter:** Sends logs to OTLP collector (not just traces)
- **LoggingHandler bridge:** Structlog → stdlib → OTLP
- **Trace correlation:** Manual processor adds trace_id/span_id to logs
- **Batch processing:** Efficient batching before export

---

### Node.js (pino + Instrumentation + Transport)

**Why pino:** Fastest JSON logger for Node.js (5-10x faster than winston/bunyan)

**Installation:**
```bash
npm install pino
npm install @opentelemetry/instrumentation-pino
npm install pino-opentelemetry-transport
```

**Configuration (Two-Part Setup):**

**Part 1: instrumentation.js** (trace correlation):
```javascript
const { NodeSDK } = require('@opentelemetry/sdk-node');
const { OTLPTraceExporter } = require('@opentelemetry/exporter-trace-otlp-grpc');
const { OTLPLogExporter } = require('@opentelemetry/exporter-logs-otlp-grpc');
const { Resource } = require('@opentelemetry/resources');
const { SEMRESATTRS_SERVICE_NAME, SEMRESATTRS_SERVICE_VERSION } = require('@opentelemetry/semantic-conventions');
const { PinoInstrumentation } = require('@opentelemetry/instrumentation-pino');

const sdk = new NodeSDK({
  resource: new Resource({
    [SEMRESATTRS_SERVICE_NAME]: 'loyalty-service',
    [SEMRESATTRS_SERVICE_VERSION]: '1.0.0',
  }),
  traceExporter: new OTLPTraceExporter({ url: 'http://otel-collector:4317' }),
  logExporter: new OTLPLogExporter({ url: 'http://otel-collector:4317' }),
  instrumentations: [
    new PinoInstrumentation({
      logSending: true,
      logKeys: {
        traceId: 'trace_id',
        spanId: 'span_id',
        traceFlags: 'trace_flags',
      },
    }),
  ],
});

sdk.start();

process.on('SIGTERM', () => {
  sdk.shutdown()
    .then(() => console.log('OpenTelemetry terminated'))
    .finally(() => process.exit(0));
});

module.exports = sdk;
```

**Part 2: app.js** (application code):
```javascript
require('./instrumentation'); // MUST be first!

const express = require('express');
const pino = require('pino');

const logger = pino({ level: 'info' });

const app = express();

app.post('/award', (req, res) => {
  logger.info({ orderId: '12345', points: 50 }, 'loyalty points awarded');
  res.json({ success: true });
});

app.listen(5400, () => {
  logger.info('loyalty service started on port 5400');
});
```

**Key Features:**
- **Two packages work together:**
  - `@opentelemetry/instrumentation-pino`: Adds trace_id/span_id to logs
  - `pino-opentelemetry-transport`: Sends logs to OTLP collector
- **Automatic trace correlation:** TraceId/SpanId injected into every log
- **Log sending:** `logSending: true` exports logs to OTLP
- **Must load first:** Instrumentation must be required before any other code

---

## Required Contextual Properties

All log entries **must** include these properties (when available):

| Property | Type | Description | Example |
|----------|------|-------------|---------|
| `@t` | ISO 8601 UTC | Timestamp in UTC | `2025-11-06T10:30:45.123Z` |
| `@mt` | string | Message template | `Order created: {Quantity} items` |
| `serviceName` | string | Service identifier | `OrderService`, `MakeLineService` |
| `orderId` | string | Order ID (if applicable) | `12345` |
| `customerId` | string | Customer ID (if applicable) | `abc-123` |
| `traceId` | string | Distributed trace ID | `00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01` |
| `level` | string | Log level | `Information`, `Warning`, `Error` |

## OpenTelemetry Collector Configuration

**Deployment:** Run OpenTelemetry Collector as a sidecar or DaemonSet

**Complete Collector Config (otel-collector-config.yaml):**
```yaml
# OpenTelemetry Collector Configuration for Red Dog
# Receives logs, metrics, and traces from all services
# Exports to Loki (logs), Prometheus (metrics), and Jaeger (traces)

receivers:
  # OTLP receiver for all signals
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318

processors:
  # Batch processor for efficient export
  batch:
    timeout: 10s
    send_batch_size: 1024

  # Memory limiter to prevent OOM
  memory_limiter:
    check_interval: 1s
    limit_mib: 512

  # Resource processor to add environment tags
  resource:
    attributes:
      - key: deployment.environment
        value: production
        action: insert

exporters:
  # Loki exporter for logs (native OTLP endpoint)
  otlphttp/logs:
    endpoint: http://loki:3100/otlp
    tls:
      insecure: true
    retry_on_failure:
      enabled: true
      initial_interval: 5s
      max_interval: 30s

  # Prometheus exporter for metrics (scrape endpoint)
  prometheus:
    endpoint: 0.0.0.0:8889
    namespace: reddog
    const_labels:
      environment: production
    resource_to_telemetry_conversion:
      enabled: true

  # Jaeger exporter for traces (using OTLP)
  otlp/jaeger:
    endpoint: jaeger:4317
    tls:
      insecure: true
    retry_on_failure:
      enabled: true

  # Debug exporter for troubleshooting (optional)
  debug:
    verbosity: detailed
    sampling_initial: 5
    sampling_thereafter: 200

service:
  # Telemetry for collector self-monitoring
  telemetry:
    logs:
      level: info
    metrics:
      address: 0.0.0.0:8888

  # Define pipelines for each signal type
  pipelines:
    # Logs: Applications → OTLP → Batch → Loki
    logs:
      receivers: [otlp]
      processors: [memory_limiter, batch, resource]
      exporters: [otlphttp/logs, debug]

    # Metrics: Applications + Dapr → OTLP → Batch → Prometheus
    metrics:
      receivers: [otlp]
      processors: [memory_limiter, batch, resource]
      exporters: [prometheus]

    # Traces: Applications + Dapr → OTLP → Batch → Jaeger
    traces:
      receivers: [otlp]
      processors: [memory_limiter, batch, resource]
      exporters: [otlp/jaeger, debug]
```

**Architecture Flow:**
```
┌─────────────────────────────────────────────────────┐
│         Red Dog Services (.NET, Go, Python, Node)   │
│                                                      │
│  Logs ───┐                                          │
│  Metrics ┼──► OTLP (gRPC/HTTP) ──► Collector       │
│  Traces ─┘                                          │
└─────────────────────────────────────────────────────┘
                       │
        ┌──────────────┼──────────────┐
        │              │              │
        ▼              ▼              ▼
     LOGS          METRICS        TRACES
      │              │              │
      ▼              ▼              ▼
    LOKI        PROMETHEUS       JAEGER
 (Port 3100)    (Port 8889)   (Port 4317)
      │              │              │
      └──────────────┼──────────────┘
                     ▼
                 GRAFANA
              (Unified UI)
```

**Key Features:**
- **Loki native OTLP:** Uses `/otlp` endpoint (not Promtail)
- **Jaeger OTLP:** Modern OTLP protocol (not legacy Jaeger protocol)
- **Prometheus scraping:** Exposes metrics at `:8889/metrics`
- **Batch processing:** Groups telemetry for efficiency
- **Memory protection:** Limits collector memory usage
- **Retry logic:** Automatic retry on export failures

## Log Levels

Use these standard log levels consistently:

| Level | Usage | Example |
|-------|-------|---------|
| **Trace** | Very detailed debugging | `Entering method: CalculateTotal()` |
| **Debug** | Debugging information | `Order validation passed: orderId=12345` |
| **Information** | General operational messages | `Order created: 12345` |
| **Warning** | Unusual but handled situations | `Retry attempt 2/3 for Dapr call` |
| **Error** | Errors that need attention | `Failed to publish order to pub/sub` |
| **Critical** | Service-level failures | `Database connection pool exhausted` |

**Production Recommendation:** Set minimum level to **Information** (not Debug/Trace)

## Testing Logs

**Verify JSON format:**
```bash
# .NET
curl http://localhost:5100/order | jq '.@t, .@mt, .orderId'

# Check OTEL collector
curl http://otel-collector:13133/  # Health endpoint
```

**Verify Jaeger traces:**
```bash
# Open Jaeger UI
http://localhost:16686

# Search for serviceName=OrderService
```

## Consequences

### Positive

- **POS-001**: **Unified Observability**: Single OTLP protocol for logs, traces, metrics across all services
- **POS-002**: **Automatic Trace Correlation**: TraceId/SpanId automatically injected into logs during traced operations
- **POS-003**: **Cloud Portability**: Same OTLP export works on AKS, EKS, GKE, Container Apps
- **POS-004**: **Vendor Neutral**: Not locked into Azure Monitor, CloudWatch, or Stackdriver
- **POS-005**: **Polyglot Consistency**: Same OTLP pattern across .NET, Go, Python, Node.js
- **POS-006**: **Production-Grade Debugging**: Distributed tracing enables debugging complex multi-service transactions
- **POS-007**: **Teaching Value**: Demonstrates industry-standard observability (CNCF OpenTelemetry)
- **POS-008**: **Flexible Backend**: Can switch from Jaeger to Datadog to Application Insights without code changes

### Negative

- **NEG-001**: **Migration Effort**: Must replace Serilog with native OTLP exporters in all .NET services
- **NEG-002**: **Collector Dependency**: Requires deploying OpenTelemetry Collector (additional infrastructure)
- **NEG-003**: **Learning Curve**: Teams must learn OpenTelemetry SDK APIs (different from Serilog)
- **NEG-004**: **Configuration Complexity**: OTEL Collector config (receivers, processors, exporters) adds complexity
- **NEG-005**: **Performance Overhead**: OTLP export adds ~5-10ms latency per request (batch processing mitigates)
- **NEG-006**: **Backend Setup**: Requires deploying Loki, Prometheus, Jaeger for local development

## Alternatives Considered

### Alternative 1: Continue with Serilog + Console Logging

- **Description**: Keep existing Serilog 4.1.0 implementation, log to console, rely on container orchestrator log aggregation
- **Rejection Reason**: No distributed tracing, no automatic trace correlation, no centralized aggregation, vendor-locked to cloud-specific log viewers. Unacceptable for production observability.

### Alternative 2: Serilog + OpenTelemetry Sink

- **Description**: Add `Serilog.Sinks.OpenTelemetry` package to existing Serilog implementations, export to OTLP collector
- **Pros**: Maintains Serilog API, less code changes, gradual migration path
- **Cons**: Adds third-party dependency, not Microsoft-supported, limited trace correlation features, hybrid approach adds complexity
- **Rejection Reason**: Native OpenTelemetry provides better trace correlation, vendor support, and polyglot consistency. Serilog sink is a transitional pattern, not target architecture.

### Alternative 3: Cloud-Specific Logging (Azure Monitor, CloudWatch, Stackdriver)

- **Description**: Use Azure Monitor for AKS, CloudWatch for EKS, Stackdriver for GKE
- **Rejection Reason**: Violates ADR-0002 cloud-agnostic principles. Vendor lock-in. Different APIs per cloud. Unacceptable.

## Related Documentation

- **ADR-0002:** Cloud-Agnostic Configuration via Dapr (establishes vendor-neutral architecture principles)
- **ADR-0005:** Kubernetes Health Probe Standardization (complementary observability for liveness/readiness)
- **docs/standards/web-api-standards.md:** Web API standards (references this ADR for logging)

## References

- [OpenTelemetry Specification](https://opentelemetry.io/docs/specs/otel/)
- [OTLP Logs 1.0 Specification](https://opentelemetry.io/docs/specs/otlp/#otlplog)
- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/languages/net/)
- [W3C Trace Context](https://www.w3.org/TR/trace-context/)
- [CNCF OpenTelemetry Project](https://www.cncf.io/projects/opentelemetry/)
