using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using RedDog.AccountingService.HealthChecks;
using Xunit;

namespace RedDog.AccountingService.Tests.HealthChecks;

/// <summary>
/// Unit tests for DaprSidecarHealthCheck.
/// Validates health check behavior for various Dapr sidecar states.
/// </summary>
public sealed class DaprSidecarHealthCheckTests : IDisposable
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<ILogger<DaprSidecarHealthCheck>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly string _originalDaprHttpPort;

    public DaprSidecarHealthCheckTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockLogger = new Mock<ILogger<DaprSidecarHealthCheck>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockHttpClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(_httpClient);

        // Save original environment variable
        _originalDaprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? string.Empty;
    }

    public void Dispose()
    {
        // Restore original environment variable
        if (string.IsNullOrEmpty(_originalDaprHttpPort))
        {
            Environment.SetEnvironmentVariable("DAPR_HTTP_PORT", null);
        }
        else
        {
            Environment.SetEnvironmentVariable("DAPR_HTTP_PORT", _originalDaprHttpPort);
        }

        _httpClient.Dispose();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDaprIsHealthy_ReturnsHealthy()
    {
        // Arrange
        Environment.SetEnvironmentVariable("DAPR_HTTP_PORT", "3500");

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString() == "http://localhost:3500/v1.0/healthz"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Healthy")
            });

        var healthCheck = new DaprSidecarHealthCheck(_mockHttpClientFactory.Object, _mockLogger.Object);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Dapr sidecar is healthy");

        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDaprReturnsError_ReturnsUnhealthy()
    {
        // Arrange
        Environment.SetEnvironmentVariable("DAPR_HTTP_PORT", "3500");

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent("Service Unavailable")
            });

        var healthCheck = new DaprSidecarHealthCheck(_mockHttpClientFactory.Object, _mockLogger.Object);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Dapr sidecar returned non-success status");
        result.Description.Should().Contain("ServiceUnavailable");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDaprIsUnreachable_ReturnsUnhealthy()
    {
        // Arrange
        Environment.SetEnvironmentVariable("DAPR_HTTP_PORT", "3500");

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var healthCheck = new DaprSidecarHealthCheck(_mockHttpClientFactory.Object, _mockLogger.Object);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Cannot reach Dapr sidecar");
        result.Description.Should().Contain("Connection refused");
        result.Exception.Should().BeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCancellationRequested_ReturnsUnhealthy()
    {
        // Arrange
        Environment.SetEnvironmentVariable("DAPR_HTTP_PORT", "3500");

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancel the token

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException("Operation was canceled"));

        var healthCheck = new DaprSidecarHealthCheck(_mockHttpClientFactory.Object, _mockLogger.Object);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context, cts.Token);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Dapr sidecar health check was cancelled");
        result.Description.Should().Contain("Operation was canceled");
        result.Exception.Should().BeAssignableTo<OperationCanceledException>();
    }

    [Theory]
    [InlineData("4500", "4500")]
    [InlineData(null, "3500")] // Falls back to default
    [InlineData("", "3500")]   // Falls back to default
    public async Task CheckHealthAsync_UsesDaprHttpPortEnvironmentVariable(string? envValue, string expectedPort)
    {
        // Arrange
        Environment.SetEnvironmentVariable("DAPR_HTTP_PORT", envValue);

        string? capturedUrl = null;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                capturedUrl = req.RequestUri?.ToString();
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        var healthCheck = new DaprSidecarHealthCheck(_mockHttpClientFactory.Object, _mockLogger.Object);
        var context = new HealthCheckContext();

        // Act
        await healthCheck.CheckHealthAsync(context);

        // Assert
        capturedUrl.Should().NotBeNull();
        capturedUrl.Should().Be($"http://localhost:{expectedPort}/v1.0/healthz");
    }
}
