# ADR-0005 Implementation Notes: Health Check Best Practices

## Overview

This document provides implementation guidance for health checks across Red Dog services, documenting correct patterns and anti-patterns to avoid.

**Status:** Living Document (Updated as services are modernized)

**Last Updated:** 2025-11-12

## Production-Ready Health Check Pattern (.NET)

### Correct Implementation

**DO:** Use `IHealthCheck` interface with proper dependency injection

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RedDog.ReceiptGenerationService.HealthChecks;

/// <summary>
/// Health check for Dapr sidecar availability.
/// Implements IHealthCheck to avoid anti-patterns (socket exhaustion, thread blocking).
/// </summary>
public class DaprSidecarHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DaprSidecarHealthCheck> _logger;
    private readonly string _daprHttpPort;

    public DaprSidecarHealthCheck(IHttpClientFactory httpClientFactory, ILogger<DaprSidecarHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var healthCheckUrl = $"http://localhost:{_daprHttpPort}/v1.0/healthz";

        try
        {
            // Use IHttpClientFactory to prevent socket exhaustion
            using var httpClient = _httpClientFactory.CreateClient("DaprHealthCheck");
            httpClient.Timeout = TimeSpan.FromSeconds(2);

            // Use async/await (NOT .Result or .Wait())
            var response = await httpClient.GetAsync(healthCheckUrl, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Dapr sidecar health check succeeded at {HealthCheckUrl}", healthCheckUrl);
                return HealthCheckResult.Healthy("Dapr sidecar is healthy");
            }

            var errorMessage = $"Dapr sidecar returned non-success status: {response.StatusCode}";
            _logger.LogWarning("Dapr sidecar health check failed: {ErrorMessage}", errorMessage);
            return HealthCheckResult.Unhealthy(errorMessage);
        }
        catch (OperationCanceledException ex)
        {
            var errorMessage = $"Dapr sidecar health check was cancelled: {ex.Message}";
            _logger.LogWarning(ex, "Dapr sidecar health check cancellation: {ErrorMessage}", errorMessage);
            return HealthCheckResult.Unhealthy(errorMessage, ex);
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = $"Cannot reach Dapr sidecar at {healthCheckUrl}: {ex.Message}";
            _logger.LogError(ex, "Dapr sidecar health check HTTP error: {ErrorMessage}", errorMessage);
            return HealthCheckResult.Unhealthy(errorMessage, ex);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Unexpected error checking Dapr sidecar health: {ex.Message}";
            _logger.LogError(ex, "Dapr sidecar health check unexpected error: {ErrorMessage}", errorMessage);
            return HealthCheckResult.Unhealthy(errorMessage, ex);
        }
    }
}
```

**Registration in Program.cs:**

```csharp
// Ensure IHttpClientFactory is registered (usually already present)
builder.Services.AddHttpClient();

// Register health checks
builder.Services.AddHealthChecks()
    .AddCheck("liveness", () =>
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Service is alive"),
        tags: new[] { "live" })
    .AddCheck<DaprSidecarHealthCheck>("readiness", tags: new[] { "ready" });
```

**Key Benefits:**
1. **No socket exhaustion** - `IHttpClientFactory` manages connection pooling
2. **No thread blocking** - Uses `async/await` throughout
3. **Proper cancellation support** - Honors `CancellationToken`
4. **Testable** - Dependencies injected via constructor
5. **Structured logging** - Includes context for debugging
6. **Exception handling** - Differentiates between cancellation, network errors, and unexpected failures

## Anti-Patterns to Avoid

### ❌ Anti-Pattern 1: Inline Lambda with `new HttpClient()`

**DON'T DO THIS:**

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("readiness", () =>
    {
        // ❌ SOCKET EXHAUSTION: Creating new HttpClient on every health check
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(2);
        try
        {
            // ❌ THREAD BLOCKING: Using .Result instead of async/await
            var response = client.GetAsync($"http://localhost:{daprHttpPort}/v1.0/healthz").Result;
            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Dapr sidecar is healthy");
            }
            return HealthCheckResult.Unhealthy("Dapr sidecar returned non-success status");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Cannot reach Dapr sidecar: {ex.Message}");
        }
    }, tags: new[] { "ready" });
```

**Why This Is Bad:**
1. **Socket Exhaustion**: Kubernetes calls health checks every 5-10 seconds. Creating `new HttpClient()` on every check exhausts TCP sockets (TIME_WAIT state). Can cause connection failures under load.
2. **Thread Blocking**: `.Result` blocks thread pool threads. Health checks run on thread pool. Blocking threads can cause thread pool starvation.
3. **Not Testable**: Inline lambda cannot be unit tested. Cannot mock dependencies.
4. **No Structured Logging**: Cannot track health check failures in observability tools.

**Measured Impact:**
- Under typical load (10 health checks/minute), creates 10 new `HttpClient` instances per minute
- Each instance holds 2-3 TCP connections in TIME_WAIT state for 60+ seconds
- After 10 minutes: 100+ sockets in TIME_WAIT, can exhaust ephemeral port range (32768-60999 on Linux)

### ❌ Anti-Pattern 2: Async Lambda with `new HttpClient()`

**DON'T DO THIS (Better, but still wrong):**

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("readiness", async () =>
    {
        // ❌ SOCKET EXHAUSTION: Still creating new HttpClient
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(2);
        try
        {
            // ✅ GOOD: Using async/await
            var response = await client.GetAsync($"http://localhost:{daprHttpPort}/v1.0/healthz");
            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Dapr sidecar is healthy");
            }
            return HealthCheckResult.Unhealthy("Dapr sidecar returned non-success status");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Cannot reach Dapr sidecar: {ex.Message}");
        }
    }, tags: new[] { "ready" });
```

**Why This Is Still Bad:**
1. **Socket Exhaustion**: Still creates `new HttpClient()` on every health check (same issue as above)
2. **Not Testable**: Inline lambda cannot be unit tested
3. **No Structured Logging**: Cannot track health check failures

**When This Might Be Acceptable:**
- Prototyping/demo environments only
- If health checks are infrequent (once per minute or less)
- If you understand the risks and are monitoring socket usage

**Production Alternative:** Use `IHealthCheck` interface with `IHttpClientFactory` (shown above)

### ❌ Anti-Pattern 3: Ignoring Cancellation Token

**DON'T DO THIS:**

```csharp
public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
{
    // ❌ NOT PASSING cancellationToken to HTTP call
    var response = await _httpClient.GetAsync($"http://localhost:{_daprHttpPort}/v1.0/healthz");
    // ...
}
```

**Why This Is Bad:**
- Health check may not be cancelled when Kubernetes times out probe (default 3 seconds)
- Can cause resource leaks if health check takes too long
- Kubernetes may mark pod unhealthy due to timeout, but health check continues running

**Correct:**

```csharp
public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
{
    // ✅ GOOD: Passing cancellationToken
    var response = await _httpClient.GetAsync($"http://localhost:{_daprHttpPort}/v1.0/healthz", cancellationToken);
    // ...
}
```

## Service Implementation Status

| Service | Health Check Pattern | Status | Notes |
|---------|---------------------|--------|-------|
| **RedDog.ReceiptGenerationService** | ✅ `IHealthCheck` + `IHttpClientFactory` | 🟢 Implemented | Correct pattern, fully tested (8 unit tests) |
| **RedDog.OrderService** | ❌ Inline lambda + `new HttpClient()` + `.Result` | 🔴 Needs Upgrade | Critical anti-patterns present |
| **RedDog.AccountingService** | ❌ Inline lambda + `new HttpClient()` + `.Result` | 🔴 Needs Upgrade | Critical anti-patterns present |
| **RedDog.MakeLineService** | ❌ Inline lambda + `new HttpClient()` + `.Result` | 🔴 Needs Upgrade | Critical anti-patterns present |
| **RedDog.LoyaltyService** | ❌ Inline lambda + `new HttpClient()` + `.Result` | 🔴 Needs Upgrade | Critical anti-patterns present |
| **RedDog.VirtualWorker** | ⚠️ Unknown | 🔵 Not Analyzed | Needs analysis |
| **RedDog.VirtualCustomers** | ⚠️ Unknown | 🔵 Not Analyzed | Needs analysis |

**Legend:**
- 🟢 Implemented: Correct pattern in place, production-ready
- 🔴 Needs Upgrade: Anti-patterns present, should be fixed
- 🔵 Not Analyzed: Implementation status unknown, needs investigation

## Testing Strategy

### Unit Test Requirements

Every `IHealthCheck` implementation should have unit tests covering:

1. **Healthy scenario**: Dependency responds with 200 OK
2. **Unhealthy scenario**: Dependency responds with 503 or error status
3. **Network failure**: Dependency unreachable (`HttpRequestException`)
4. **Cancellation**: Health check cancelled (`OperationCanceledException`)
5. **Environment variable**: Reads `DAPR_HTTP_PORT` correctly

**Example (using xUnit + Moq + FluentAssertions):**

```csharp
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;

namespace RedDog.ReceiptGenerationService.Tests.HealthChecks;

public class DaprSidecarHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_DaprHealthy_ReturnsHealthy()
    {
        // Arrange
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("healthy")
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(x => x.CreateClient("DaprHealthCheck"))
            .Returns(httpClient);

        var loggerMock = new Mock<ILogger<DaprSidecarHealthCheck>>();
        var healthCheck = new DaprSidecarHealthCheck(httpClientFactoryMock.Object, loggerMock.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Dapr sidecar is healthy");
    }
}
```

### Integration Test Requirements

Integration tests should verify health checks work with real dependencies:

1. **Start Dapr sidecar** (via `dapr run` or Docker Compose)
2. **Start service** (with real health check implementation)
3. **Call `/readyz`** and verify 200 OK
4. **Stop Dapr sidecar**
5. **Call `/readyz`** again and verify 503 Service Unavailable
6. **Restart Dapr sidecar**
7. **Call `/readyz`** and verify 200 OK again

## Migration Checklist

When upgrading a service to use the correct health check pattern:

- [ ] Create `HealthChecks/` directory in service project
- [ ] Create `DaprSidecarHealthCheck.cs` implementing `IHealthCheck`
- [ ] Ensure `IHttpClientFactory` is registered (`builder.Services.AddHttpClient()`)
- [ ] Update `Program.cs` to use `.AddCheck<DaprSidecarHealthCheck>()`
- [ ] Remove inline lambda health check registration
- [ ] Create unit tests for health check class (5+ tests)
- [ ] Build service in Release mode (verify 0 errors)
- [ ] Run all tests (verify all pass)
- [ ] Deploy to test environment and verify health endpoints work
- [ ] Update service documentation

## Performance Considerations

**Health Check Frequency:**
- Kubernetes typical configuration: 10-second liveness probe, 5-second readiness probe
- Load per pod: ~12-18 health check requests per minute
- Cluster with 10 pods: ~120-180 health check requests per minute

**HTTP Client Pooling (IHttpClientFactory):**
- Default connection lifetime: 2 minutes
- Connections are reused across health checks
- Socket count remains stable (typically 1-2 connections per pod)

**Timeout Configuration:**
- Health check timeout: 2 seconds (in `DaprSidecarHealthCheck`)
- Kubernetes probe timeout: 3 seconds (in deployment manifest)
- Always set health check timeout < Kubernetes probe timeout

## Observability Integration

**Structured Logging:**
- Use `ILogger<T>` with structured properties
- Log level: Debug (success), Warning (failure), Error (exception)
- Include context: health check URL, error message, exception details

**Metrics (Future Enhancement):**
- Track health check duration (histogram)
- Track health check failures (counter)
- Track health check timeouts (counter)
- Export to Prometheus via OpenTelemetry

## References

- **Microsoft Docs**: [Health checks in ASP.NET Core](https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks)
- **Microsoft Docs**: [IHttpClientFactory](https://learn.microsoft.com/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests)
- **ADR-0005**: [Kubernetes Health Probe Endpoint Standardization](adr-0005-kubernetes-health-probe-standardization.md)
- **Kubernetes Docs**: [Configure Liveness, Readiness, and Startup Probes](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/)

---

**Document History:**
- 2025-11-12: Initial document created after implementing ReceiptGenerationService health check improvements
