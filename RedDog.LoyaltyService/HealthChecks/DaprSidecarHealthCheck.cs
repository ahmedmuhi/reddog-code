using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RedDog.LoyaltyService.HealthChecks;

/// <summary>
/// Verifies the colocated Dapr sidecar is responding on the HTTP health endpoint.
/// </summary>
public class DaprSidecarHealthCheck(
    IHttpClientFactory httpClientFactory,
    ILogger<DaprSidecarHealthCheck> logger) : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly ILogger<DaprSidecarHealthCheck> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private static string DaprHttpPort => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DAPR_HTTP_PORT"))
        ? "3500"
        : Environment.GetEnvironmentVariable("DAPR_HTTP_PORT")!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var url = $"http://localhost:{DaprHttpPort}/v1.0/healthz";

        try
        {
            using var client = _httpClientFactory.CreateClient("DaprHealth");
            client.Timeout = TimeSpan.FromSeconds(2);

            var response = await client.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Dapr sidecar health check succeeded ({Url})", url);
                return HealthCheckResult.Healthy("Dapr sidecar healthy");
            }

            var errorMessage = $"Dapr health endpoint returned {response.StatusCode}";
            _logger.LogWarning("Dapr sidecar health check failed: {Message}", errorMessage);
            return HealthCheckResult.Unhealthy(errorMessage);
        }
        catch (Exception ex) when (ex is OperationCanceledException or HttpRequestException)
        {
            var message = $"Unable to reach Dapr sidecar: {ex.Message}";
            _logger.LogWarning(ex, "Dapr sidecar health check error: {Message}", message);
            return HealthCheckResult.Unhealthy(message, ex);
        }
    }
}
