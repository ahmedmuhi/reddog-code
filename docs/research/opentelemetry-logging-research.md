---
title: OpenTelemetry Logging Research for Red Dog Polyglot Microservices
date: 2025-11-06
author: Red Dog Modernization Team
scope: Cross-Language Observability (.NET, Go, Python, Node.js)
status: Research Complete
---

# OpenTelemetry Logging Research for Red Dog Polyglot Microservices

## Executive Summary

This research investigates the best practices for implementing structured logging with OpenTelemetry across Red Dog's polyglot microservices architecture (.NET, Go, Python, Node.js with Dapr).

**Key Findings:**
1. **OpenTelemetry vs Prometheus**: OpenTelemetry handles logs/traces/metrics; Prometheus is metrics-only storage
2. **Serilog + OTEL is the .NET standard** in 2025 (production-ready, 865 dependent projects)
3. **Language-specific recommendations**: Zerolog/slog (Go), structlog (Python), Winston/Pino (Node.js)
4. **Why OTEL for logs**: Prometheus can't store logs at all (metrics only)
5. **Why language libraries**: Dapr operates at network level, can't see application code execution

---

## Question 1: OpenTelemetry vs Prometheus - What's the Difference?

### Direct Answer

**OpenTelemetry** is a vendor-neutral, open-source framework that collects all three signal types (metrics, traces, and logs) from applications using SDKs and exports them via a standardized protocol (OTLP). **Prometheus** is a complete, self-contained monitoring system that only handles metrics, using a pull-based collection model, with built-in storage, querying (PromQL), alerting, and visualization.

OpenTelemetry is instrumentation and collection; Prometheus is a metrics storage and query system. They are complementary—OpenTelemetry SDKs can export metrics in Prometheus format, and Prometheus often receives OpenTelemetry data through an intermediary.

### Evidence

**Official Sources:**
- OpenTelemetry Specification: https://opentelemetry.io/docs/specs/otel/overview/
- Prometheus Documentation: https://prometheus.io/
- OpenTelemetry Collector: https://opentelemetry.io/docs/collector/architecture/

**Key Comparisons:**
- SigNoz: https://signoz.io/blog/opentelemetry-vs-prometheus/
- Uptrace: https://uptrace.dev/comparisons/opentelemetry-vs-prometheus
- Last9: https://last9.io/blog/opentelemetry-vs-prometheus/

### Signal Type Breakdown

| Signal | OpenTelemetry | Prometheus |
|--------|---------------|-----------|
| **Metrics** | Yes (counters, gauges, histograms) | Yes (only) |
| **Traces** | Yes (distributed tracing) | No |
| **Logs** | Yes (1.0 stable as of 2024) | No |
| **Storage** | No (delegates to backends) | Yes (built-in TSDB) |
| **Protocol** | OTLP (gRPC/HTTP) | Prometheus Scrape Protocol |
| **Collection Model** | Push or Pull | Pull only |

### Industry Consensus (2025)

- **48.5%** of organizations using OpenTelemetry in production (EMA survey)
- **41%** production usage with 25.3% planning soon
- **81%** of IT professionals consider OpenTelemetry mature for production
- **85%** of OpenTelemetry users ALSO use Prometheus for metrics (Grafana survey)

The industry standard is **hybrid**: Use OpenTelemetry SDKs in applications for comprehensive instrumentation, export metrics to Prometheus for scraping, export traces/logs to specialized backends.

### Recommendation for Red Dog

**Use OpenTelemetry as the primary instrumentation framework** across all languages (.NET, Go, Python, Node.js). Configure OpenTelemetry to export:
- **Metrics** → Prometheus (via OTLP Metrics exporter)
- **Traces** → Jaeger/Tempo (via OTLP exporter)
- **Logs** → Loki or ELK (via OTLP exporter)

This gives you unified instrumentation with best-of-breed backends for each signal type.

---

## Question 2: Serilog + OpenTelemetry in .NET (2025)

### Direct Answer

**Yes, Serilog with OpenTelemetry sink is the industry-standard approach in 2025** for structured logging in .NET. The official `Serilog.Sinks.OpenTelemetry` package (v4.2.0, last updated May 31, 2025) transforms Serilog events into OpenTelemetry LogRecords and sends them via OTLP protocol. It's production-ready with 865 projects depending on it.

However, **Microsoft.Extensions.Logging with direct OTLP is now a viable alternative**, especially with .NET 9+ and the simplified `UseOtlpExporter()` helper introduced in OpenTelemetry SDK 1.8.0.

### Evidence

**Production Status:**
- GitHub: https://github.com/serilog/serilog-sinks-opentelemetry (159 stars, 15 contributors, active maintenance)
- NuGet: https://www.nuget.org/packages/Serilog.Sinks.OpenTelemetry/ (v4.2.0, May 2025)

**Microsoft Official Guidance:**
- .NET Observability with OpenTelemetry: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel
- OTLP Example: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-otlp-example
- UseOtlpExporter: Introduced in OpenTelemetry SDK 1.8.0-beta.1

**Industry Adoption:**
- Last9 (2025): https://last9.io/blog/serilog-and-opentelemetry/
- Medium (Sept 2025): "Logging Like a Pro — Serilog + OpenTelemetry in .NET"

### Package Maturity: Serilog.Sinks.OpenTelemetry

| Feature | Status | Notes |
|---------|--------|-------|
| OTLP Protocol Support | Stable | gRPC and HTTP Protobuf |
| Trace Context Correlation | Stable | Automatic trace/span ID injection |
| Resource Attributes | Stable | Supports primitive values |
| Message Templates | Supported | With optional MD5 hashing |
| Environment Variables | Supported | OTEL_OTLP_EXPORTER_* standard |
| Structured Properties | 1:1 Mapping | No flattening by default |
| Activity.Current Support | Yes | Compatible with SerilogTracing |

### Two Approaches in 2025

**Approach 1: Serilog + OpenTelemetry Sink (Recommended for existing Serilog users)**
```
Serilog → Serilog.Sinks.OpenTelemetry → OTLP Collector → Backend
```
Advantages: Backwards compatible, rich enrichment ecosystem, mature pattern
Complexity: Additional sink package dependency

**Approach 2: Microsoft.Extensions.Logging + OTLP (Recommended for new projects)**
```
ILogger → OpenTelemetry.Extensions.Hosting → OTLP → Backend
```
Advantages: No external dependencies, Microsoft-supported, simplified with UseOtlpExporter()
Complexity: Less community ecosystem than Serilog

### Best Practice in 2025

Most teams use **Serilog as the logging provider with Microsoft.Extensions.Logging abstraction** via `UseSerilog()`. This gives you:
- Serilog's structured logging power
- Microsoft.Extensions.Logging API compatibility
- Full OpenTelemetry integration
- Easy trace context correlation

### Recommendation for Red Dog

**Use Serilog.Sinks.OpenTelemetry** for OrderService and AccountingService. It's the most proven pattern with maximum community support and most similar to existing Red Dog logging patterns. However, **new services should evaluate Microsoft.Extensions.Logging + OTLP** to reduce dependencies.

---

## Question 3: Logging Libraries for Go, Python, Node.js → OpenTelemetry

### GO LOGGING LIBRARIES (2025)

#### Summary Table

| Library | OpenTelemetry Integration | Performance | Recommendation |
|---------|--------------------------|-------------|-----------------|
| **slog** (stdlib) | Yes, via slog bridge | Excellent (40 B/op) | NEW PROJECTS |
| **Zerolog** | Yes, via bridge | Fastest (0-allocation) | HIGH-PERF SERVICES |
| **Zap** | Yes, via plugin | Very Fast (similar to Zerolog) | EXISTING PROJECTS |
| **Logrus** | Yes, via plugin | Good (mature) | LEGACY SUPPORT |

**Direct Answer:** For Red Dog's Go services (MakeLineService, VirtualWorker), **use Zerolog for performance-critical services or slog for new services**. Both have excellent OpenTelemetry integration via Logger Bridge APIs.

**Evidence:**
- Uptrace: https://uptrace.dev/blog/golang-logging
- Dash0 (2025): https://www.dash0.com/faq/best-go-logging-tools-in-2025-a-comprehensive-guide
- Go 1.21+ slog: https://pkg.go.dev/log/slog

**Detailed Recommendations:**

1. **For MakeLineService (queue management, concurrency):** Use **Zerolog**
   - Reason: Queue operations are latency-sensitive, zero-allocation logging critical
   - Integration: via `samber/slog-zerolog` (slog handler wrapper)
   - OpenTelemetry: Full trace context correlation available

2. **For VirtualWorker (worker pool):** Use **Zerolog** or **slog**
   - If performance critical: Zerolog
   - If simplicity preferred: slog (no external deps)

3. **New Go services:** Default to **slog** (stdlib)
   - Standard library since Go 1.21
   - Zero external dependencies
   - 18% more developer participation in OpenTelemetry in 2025

**Integration Pattern:**
```go
// With slog and OpenTelemetry
import "log/slog"
import "go.opentelemetry.io/otel"

// Logs automatically correlate with trace context
slog.InfoContext(ctx, "order processed", "order_id", orderId)
// Trace ID and Span ID automatically injected if in active trace
```

---

### PYTHON LOGGING LIBRARIES (2025)

#### Summary Table

| Library | OpenTelemetry Integration | Maturity | Recommendation |
|---------|--------------------------|----------|-----------------|
| **structlog** | Yes (official handler) | Production | PRIMARY CHOICE |
| **python-json-logger** | Partial | Good | ALTERNATIVE |
| **loguru** | Manual (no first-party) | Popular | NOT RECOMMENDED |

**Direct Answer:** For Red Dog's Python services (ReceiptGenerationService, VirtualCustomers), **use structlog**. It has official OpenTelemetry handler support, strong trace correlation, and is the modern standard for Python structured logging.

**Evidence:**
- OpenTelemetry Python Contrib: https://github.com/open-telemetry/opentelemetry-python-contrib/pull/2492
- structlog Docs: https://www.structlog.org/en/stable/frameworks.html
- Last9: https://last9.io/blog/python-logging-with-structlog/

**Detailed Recommendations:**

1. **For ReceiptGenerationService (document generation):** Use **structlog**
   - Reason: Needs trace correlation for document generation tracking
   - Integration: OpenTelemetry handler provided in opentelemetry-python-contrib
   - Benefit: Automatic trace context injection

2. **For VirtualCustomers (load generation):** Use **structlog**
   - Reason: Want consistent logging across all Python services
   - Simplest approach: structlog writing JSON → OpenTelemetry Collector converts to OTLP

3. **Why NOT loguru?**
   - No official first-party OpenTelemetry integration
   - Requires manual trace context injection
   - Popular but less suitable for observability-first architectures

**Integration Pattern:**
```python
# With structlog and OpenTelemetry
import structlog
from opentelemetry import trace

logger = structlog.get_logger()

# Trace context automatically bound if using OTel processors
logger.info("receipt_generated", receipt_id=receipt_id, customer_id=customer_id)
```

**Best Practice (2025):**
Keep structlog writing JSON to stdout, let OpenTelemetry Collector transform to OTLP before sending to backend. This keeps application simple and allows flexible backend changes.

---

### NODE.JS LOGGING LIBRARIES (2025)

#### Summary Table

| Library | OpenTelemetry Integration | Performance | Recommendation |
|---------|--------------------------|-------------|-----------------|
| **Pino** | Yes (instrumentation pkg) | Fastest (5-10x faster) | PERFORMANCE-CRITICAL |
| **Winston** | Yes (instrumentation pkg) | Good | PRODUCTION DEFAULT |
| **Bunyan** | Yes (instrumentation pkg) | Good | JSON-FIRST APPROACH |

**Direct Answer:** For Red Dog's Node.js services (LoyaltyService), **use Winston for flexibility or Pino for high-throughput scenarios**. Both have official OpenTelemetry instrumentation packages maintained by the OTEL community.

**Evidence:**
- Official OTEL Instrumentation: https://opentelemetry.io/docs/instrumentation/js/libraries/
- SigNoz (2025): https://signoz.io/guides/pino-logger/
- Better Stack (2025): https://betterstack.com/community/guides/logging/best-nodejs-logging-libraries/

**Detailed Recommendations:**

1. **For LoyaltyService (event-driven, pub/sub):** Use **Winston** (default) or **Pino** (if throughput high)
   - Winston: 12M+ weekly downloads, most flexible, largest ecosystem
   - Pino: 5-10x faster, better for high-volume event systems
   - Both: Official OTEL instrumentation packages

2. **Integration Option A: Winston (Recommended for LoyaltyService)**
   - Package: `@opentelemetry/instrumentation-winston`
   - Benefit: Mature, rich output targets, community ecosystem
   - Trace correlation: Automatic via instrumentation

3. **Integration Option B: Pino (For high-throughput scenarios)**
   - Package: `@opentelemetry/instrumentation-pino`
   - Benefit: 5-10x faster than Winston, excellent for event processing
   - Trace correlation: Automatic via instrumentation

**Integration Pattern (Winston):**
```javascript
// With Winston and OpenTelemetry
import winston from 'winston';
import { registerInstrumentations } from '@opentelemetry/auto-instrumentations-node';

const logger = winston.createLogger({
  format: winston.format.json(),
  transports: [new winston.transports.Console()]
});

// OTEL instrumentation handles trace context injection
logger.info('loyalty_points_updated', { customerId, points });
```

---

## Question 4: Why OpenTelemetry for Logs? Why Not Prometheus?

### Direct Answer

**Prometheus only handles metrics** (numerical time-series data like CPU, request counts, error rates). It cannot store or query logs at all. **OpenTelemetry Logs is specifically for text records** (application events, errors, debug info), which are a different data type.

The typical architecture is: **Logs → OpenTelemetry/Loki | Metrics → Prometheus | Traces → Jaeger/Tempo**. Each has specialized storage optimized for its data type.

### Evidence

**Why Prometheus Doesn't Handle Logs:**
- Prometheus Documentation: https://prometheus.io/ (metrics-only system)
- Comparison (Last9): https://last9.io/blog/loki-vs-prometheus/
- SigNoz: https://signoz.io/blog/loki-vs-prometheus/

**OpenTelemetry Logs 1.0 (Stable Oct 2024):**
- Specification: https://opentelemetry.io/docs/concepts/signals/logs/
- Data Model: https://opentelemetry.io/docs/specs/otel/logs/data-model/
- Dash0: https://www.dash0.com/knowledge/opentelemetry-logging-explained

### Data Architecture Comparison

```
┌─────────────────────────────────────────────────────┐
│              Application Instrumentation             │
└─────────────────┬──────────────────────────────────┘
                  │
      ┌───────────┼───────────┐
      │           │           │
      ▼           ▼           ▼
   METRICS     TRACES      LOGS
   (numbers)  (spans)    (text)
      │           │           │
      ▼           ▼           ▼
 PROMETHEUS   JAEGER/TEMPO  LOKI/ELK
   (TSDB)    (trace DB)   (log DB)
```

### Why Logs Need Different Infrastructure Than Metrics

| Aspect | Metrics (Prometheus) | Logs (Loki/ELK) |
|--------|---------------------|-----------------|
| **Data Type** | Numerical time-series | Text/structured records |
| **Storage** | Compressed TSDB | Full-text searchable indexes |
| **Query Language** | PromQL (aggregation) | LogQL (pattern matching) |
| **Cardinality** | Low (hundreds of unique label combos) | High (unlimited unique values) |
| **Volume** | Low (scrape every 15s) | High (millions of entries/sec) |
| **Typical Retention** | 2-4 weeks | Hours to days |

### 2025 Best Practice Stack

Most organizations use **Grafana's full observability stack**:
1. **Prometheus** → Metrics (via Prometheus Scrape protocol)
2. **Loki** → Logs (via OpenTelemetry Collector or Fluentd)
3. **Tempo** → Traces (via OpenTelemetry OTLP)
4. **Grafana** → Unified dashboard across all three

According to Grafana's 2025 Observability Survey:
- 85% of OpenTelemetry users also use Prometheus
- 80%+ adoption in organizations >1000 employees
- Primary trend: Unified backends via OpenTelemetry

### Grafana Loki vs OpenTelemetry for Logs

| Aspect | Loki | OpenTelemetry Logs |
|--------|------|------------------|
| **Purpose** | Log storage and querying backend | Log collection and export standard |
| **Role** | Destination system | Transport/format standard |
| **Collection** | Via Grafana Alloy or Fluentd | Via OTLP protocol |
| **Query Language** | LogQL | Vendor-specific (depends on backend) |
| **In Practice** | Loki is the backend that receives logs from OpenTelemetry |

**They work together:** Applications emit logs via OpenTelemetry SDKs → OTLP protocol → Grafana Alloy (OTEL Collector) → Loki (storage)

### Recommendation for Red Dog

**Use OpenTelemetry for log collection** (standardized, vendor-neutral, multi-language support) and **send to Loki** (cost-effective log storage, Grafana-native). This aligns with the broader Dapr ecosystem which already uses OpenTelemetry for traces and metrics.

---

## Question 5: Why Language Libraries for Logs, But Dapr for Traces/Metrics?

### Direct Answer

**Dapr handles service-to-service observability** (traces across services, sidecar metrics). **Application logs are application responsibility** because:

1. Dapr operates at the network/runtime level, not application level
2. Application logging requires language-specific knowledge and libraries
3. Dapr cannot know what your application wants to log
4. Logs must correlate with application code execution, not just Dapr operations

**Simple rule:** Dapr handles "how your services talk to each other" (traces, service metrics). Applications handle "what's happening inside services" (logs, business metrics).

### Evidence

**Official Dapr Documentation:**
- Observability Concept: https://docs.dapr.io/concepts/observability-concept/
- Logging Guide: https://docs.dapr.io/operations/observability/logging/logs/
- Dapr for .NET Developers: https://learn.microsoft.com/en-us/dotnet/architecture/dapr-for-net-developers/observability

**Key Quote from Dapr Docs:**
"Dapr generates logs from the sidecar and control plane services to provide visibility into sidecar operation. Applications must implement their own logging separately from Dapr's observability features."

### Dapr Observability Architecture (2025)

```
┌─────────────────────────────────────────────────────┐
│                  Red Dog Application                │
│   ┌─────────────────────────────────────────────┐   │
│   │  OrderService (.NET)                        │   │
│   │  - Application Logs (Serilog)              │   │
│   │  - Business Metrics (custom counters)      │   │
│   │  ↓ HTTP/gRPC                               │   │
│   └──────────────────┬──────────────────────────┘   │
│                      │                              │
│   ┌──────────────────▼──────────────────────────┐   │
│   │         Dapr Sidecar (daprd)               │   │
│   │  - Service Invocation Traces (OTLP)       │   │
│   │  - Pub/Sub Context Propagation            │   │
│   │  - Dapr Runtime Metrics (Prometheus)      │   │
│   │  - Dapr Logs (stdout/JSON)                │   │
│   └──────────────────┬──────────────────────────┘   │
│                      │                              │
└──────────────────────┼──────────────────────────────┘
                       │
        ┌──────────────┼──────────────┐
        │              │              │
        ▼              ▼              ▼
    LOGS          TRACES          METRICS
   (Serilog        (Dapr)         (Dapr +
   + Custom)      (OTLP)          Custom)
        │              │              │
        ▼              ▼              ▼
    LOKI/ELK      JAEGER/TEMPO   PROMETHEUS
```

### What Dapr Handles

| Signal | Dapr Responsibility | Example |
|--------|-------------------|---------|
| **Traces** | Inter-service calls | OrderService calls MakeLineService (auto-traced) |
| **Metrics** | Sidecar/control plane | Requests/sec through Dapr, sidecar CPU/memory |
| **Service Context** | Propagation headers | W3C trace context, Dapr message routing |
| **Dapr's Own Logs** | Sidecar operation logs | Health checks, connection failures |

### What Applications Handle

| Signal | App Responsibility | Example |
|--------|------------------|---------|
| **Application Logs** | Business events | "Order validated", "Payment processed" |
| **Business Metrics** | Domain measurements | Orders/minute, revenue, customer lifetime value |
| **Application Traces** | Custom instrumentation | "Processing order step 3 of 5" |

### Dapr's Log Architecture (Does NOT Forward App Logs)

**Dapr sidecar:**
- Writes its own operational logs to stdout (configurable verbosity)
- Can output JSON format for parsing
- Can be collected via Fluentd, Grafana Alloy, or OpenTelemetry Collector
- BUT: Does NOT intercept or forward application logs

**Why not?** Because:
1. Dapr sidecar runs in separate container from application
2. Dapr has no visibility into application code execution
3. Each language needs proper structured logging (C# uses Serilog, Go uses Zap, etc.)
4. Mixing Dapr logs with app logs would lose context

### 2025 Best Practice Architecture for Polyglot Dapr Apps

```yaml
# Kubernetes Pod for OrderService
pod:
  containers:
  - name: app
    image: orderservice-dotnet
    # Application logs via Serilog → stdout (JSON)
    # + OpenTelemetry SDK sends app traces to OTLP collector

  - name: daprd
    image: daprd
    # Dapr sidecar logs → stdout (JSON)
    # + Dapr automatically sends service traces to OTLP collector
    # + Dapr exposes metrics on :9090 for Prometheus scrape

# On the host:
# Fluentd/Grafana Alloy watches both containers' stdout
# Merges logs → OpenTelemetry Collector (OTLP) → Loki
# OTLP traces → Jaeger/Tempo
# Prometheus scrapes :9090 → Prometheus TSDB
```

### Why This Architecture?

1. **Separation of Concerns:** Dapr handles distributed system concerns, apps handle business logic
2. **Language Neutrality:** Each language's logging library is optimized for that language
3. **Standard Protocols:** Everything uses OTLP (logs, traces) or Prometheus (metrics) standards
4. **Flexibility:** Can change logging libraries independently from Dapr updates
5. **Context Propagation:** App logs get trace context injected automatically (via active span)

### Best Practices for Logging in Dapr Apps (2025)

**Rule 1: Configure Dapr sidecar logging for observability**
```bash
# Kubernetes annotation
dapr.io/log-as-json: "true"
dapr.io/log-level: "info"
```

**Rule 2: Instrument applications with language-specific libraries**
- .NET (OrderService) → Serilog + OpenTelemetry sink
- Go (MakeLineService, VirtualWorker) → Zerolog or slog
- Python (ReceiptGenerationService) → structlog
- Node.js (LoyaltyService) → Winston or Pino
- Vue.js (UI) → Standard console with application context

**Rule 3: Use OpenTelemetry Collector for unified gathering**
```yaml
# Single Collector processes:
# - App logs (OpenTelemetry format)
# - Dapr sidecar logs (formatted to OpenTelemetry)
# - All traces (already in OTLP from Dapr)
# - All metrics (scraped from Prometheus endpoints)
```

**Rule 4: Correlate logs with traces**
```
application.log: {"msg": "order created", "trace_id": "abc123", "span_id": "def456"}
                                                           ↑                      ↑
                                          Automatically injected by OpenTelemetry SDK
                                          when logging within active trace context
```

**Rule 5: Dapr + Fluentd or Grafana Alloy for log collection**
- Don't try to have Dapr forward app logs (architectural mismatch)
- Use standard container log collection (Fluentd/Alloy) instead
- Both can parse JSON-formatted logs and convert to OTLP

### Recommendation for Red Dog

1. **Configure Dapr sidecar:**
   - Enable JSON logging via annotation
   - Set appropriate log level (info for prod, debug for development)
   - Dapr will handle its own operational logging

2. **Instrument each service:**
   - OrderService: Serilog + OTLP sink
   - MakeLineService/VirtualWorker: Zerolog or slog
   - ReceiptGenerationService/VirtualCustomers: structlog
   - LoyaltyService: Winston
   - UI: Console with correlation IDs

3. **Deploy Grafana Alloy (OTEL Collector) in Kubernetes:**
   - Receives app logs via OTLP
   - Parses Dapr JSON logs from stdout
   - Scrapes Prometheus metrics
   - Receives distributed traces from Dapr sidecars
   - Exports everything to centralized backends

4. **Central backends:**
   - Prometheus: For metrics
   - Loki: For logs
   - Jaeger/Tempo: For traces
   - Grafana: For dashboards

This separates concerns cleanly: applications log their business logic, Dapr handles distributed system observability, and a neutral collector unifies everything.

---

## Summary Table: Recommended Tech Stack for Red Dog

| Service | Language | Logging Library | OTLP Export | Notes |
|---------|----------|-----------------|-------------|-------|
| **OrderService** | .NET | Serilog | Serilog.Sinks.OpenTelemetry | Production-ready, mature |
| **AccountingService** | .NET | Serilog | Serilog.Sinks.OpenTelemetry | Same as OrderService |
| **MakeLineService** | Go | Zerolog | slog-zerolog bridge | Performance-critical queue |
| **VirtualWorker** | Go | Zerolog or slog | Direct OTLP | Worker pool efficiency |
| **ReceiptGenerationService** | Python | structlog | OTEL handler | Official integration |
| **VirtualCustomers** | Python | structlog | OTEL handler | Consistent with services |
| **LoyaltyService** | Node.js | Winston | @opentelemetry/instrumentation-winston | 12M+ weekly downloads |
| **RedDog UI** | Vue 3 | Console + middleware | Via collector | Browser traces via WebTelemetry |
| **Dapr Sidecars** | - | (auto) | JSON→Collector→OTLP | Configure with annotations |

**Collectors & Backends:**
- **Collection:** Grafana Alloy (OTEL Collector distribution)
- **Logs:** Grafana Loki (OTLP receiver)
- **Metrics:** Prometheus (scrape from sidecars + apps)
- **Traces:** Grafana Tempo or Jaeger (OTLP receiver)
- **Visualization:** Grafana (unified dashboard)

---

## Key 2025 Insights

1. **OpenTelemetry is now mainstream** – 48.5% in production, considered mature by 81% of IT professionals
2. **Logs became stable in Oct 2024** – OTLP logs 1.0 specification released, no longer experimental
3. **Language libraries over centralized solution** – Serilog (C#), structlog (Python), slog (Go), Winston (Node) are all language-appropriate
4. **Dapr + OpenTelemetry = golden combo** – Dapr handles distributed system observability, OTEL SDKs handle application instrumentation
5. **Prometheus + Loki + Tempo stack growing** – 85% of OTEL users also use Prometheus, Loki adoption rising for cost-effective logs
6. **No more "logging vendor lock-in"** – OTLP standardization means switching backends is now trivial
7. **Go 1.21+ slog changing the game** – Standard library structured logging reduces external dependencies for new Go projects

---

## Research Methodology

**Sources Evaluated (2024-2025):**
- OpenTelemetry official documentation (opentelemetry.io)
- Microsoft Learn (.NET guidance)
- CNCF projects and reports
- Reputable observability blogs: Last9, SigNoz, Uptrace, Dash0, Grafana Labs
- Official package repositories: NuGet, npm, PyPI, GitHub
- Industry surveys: EMA, Grafana Labs, Elastic Observability Labs
- Language community documentation: Go, Python, Node.js

**Key URLs Referenced:**
- https://opentelemetry.io/
- https://prometheus.io/
- https://grafana.com/
- https://learn.microsoft.com/dotnet/core/diagnostics/observability-with-otel
- https://docs.dapr.io/concepts/observability-concept/

All statistics are from 2024-2025 sources. Recommendations prioritize maturity, community adoption, and suitability for polyglot architectures.

---

**Document Version:** 1.0
**Last Updated:** 2025-11-06
**Next Review:** After technology decisions finalized
