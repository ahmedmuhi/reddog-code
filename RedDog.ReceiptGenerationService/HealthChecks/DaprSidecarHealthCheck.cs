using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RedDog.ReceiptGenerationService.HealthChecks;

/// <summary>
/// Health check for Dapr sidecar availability.
/// Implements IHealthCheck to avoid anti-patterns (socket exhaustion from new HttpClient, thread blocking from .Result).
/// </summary>
public class DaprSidecarHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DaprSidecarHealthCheck> _logger;
    private readonly string _daprHttpPort;

    /// <summary>
    /// Initializes a new instance of the <see cref="DaprSidecarHealthCheck"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HttpClient instances (prevents socket exhaustion).</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public DaprSidecarHealthCheck(IHttpClientFactory httpClientFactory, ILogger<DaprSidecarHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";
    }

    /// <summary>
    /// Checks the health of the Dapr sidecar by calling its /v1.0/healthz endpoint.
    /// </summary>
    /// <param name="context">Health check context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the health check result.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
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
