using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RedDog.MakeLineService.HealthChecks;

/// <summary>
/// Health check for Dapr sidecar availability.
/// Implements IHealthCheck to avoid anti-patterns (socket exhaustion, thread blocking).
/// Follows ADR-0005 implementation pattern.
/// </summary>
public class DaprSidecarHealthCheck(
    IHttpClientFactory httpClientFactory,
    ILogger<DaprSidecarHealthCheck> logger) : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory
        ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly ILogger<DaprSidecarHealthCheck> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));
    private readonly string _daprHttpPort = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DAPR_HTTP_PORT"))
        ? "3500"
        : Environment.GetEnvironmentVariable("DAPR_HTTP_PORT")!;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var healthCheckUrl = $"http://localhost:{_daprHttpPort}/v1.0/healthz";

        try
        {
            using var httpClient = _httpClientFactory.CreateClient("DaprHealthCheck");
            httpClient.Timeout = TimeSpan.FromSeconds(2);

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
