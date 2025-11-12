using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using RedDog.ReceiptGenerationService.HealthChecks;
using System.Net;

namespace RedDog.ReceiptGenerationService.Tests.HealthChecks;

public class DaprSidecarHealthCheckTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<DaprSidecarHealthCheck>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

    public DaprSidecarHealthCheckTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<DaprSidecarHealthCheck>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
    }

    [Fact]
    public async Task CheckHealthAsync_DaprHealthy_ReturnsHealthy()
    {
        // Arrange
        var expectedUrl = "http://localhost:3500/v1.0/healthz";
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == expectedUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("healthy")
            });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock
            .Setup(x => x.CreateClient("DaprHealthCheck"))
            .Returns(httpClient);

        var healthCheck = new DaprSidecarHealthCheck(_httpClientFactoryMock.Object, _loggerMock.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Dapr sidecar is healthy");

        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == expectedUrl),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task CheckHealthAsync_DaprReturnsError_ReturnsUnhealthy()
    {
        // Arrange
        var expectedUrl = "http://localhost:3500/v1.0/healthz";
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == expectedUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent("unhealthy")
            });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock
            .Setup(x => x.CreateClient("DaprHealthCheck"))
            .Returns(httpClient);

        var healthCheck = new DaprSidecarHealthCheck(_httpClientFactoryMock.Object, _loggerMock.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("ServiceUnavailable");

        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == expectedUrl),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task CheckHealthAsync_DaprUnreachable_ReturnsUnhealthy()
    {
        // Arrange
        var expectedException = new HttpRequestException("Connection refused");
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(expectedException);

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock
            .Setup(x => x.CreateClient("DaprHealthCheck"))
            .Returns(httpClient);

        var healthCheck = new DaprSidecarHealthCheck(_httpClientFactoryMock.Object, _loggerMock.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Cannot reach Dapr sidecar");
        result.Description.Should().Contain("Connection refused");
        result.Exception.Should().Be(expectedException);
    }

    [Fact]
    public async Task CheckHealthAsync_CancellationRequested_ReturnsUnhealthy()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var expectedException = new OperationCanceledException("Operation was cancelled", cts.Token);
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(expectedException);

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock
            .Setup(x => x.CreateClient("DaprHealthCheck"))
            .Returns(httpClient);

        var healthCheck = new DaprSidecarHealthCheck(_httpClientFactoryMock.Object, _loggerMock.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), cts.Token);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("was cancelled");
        result.Exception.Should().BeOfType<OperationCanceledException>();
    }

    [Fact]
    public async Task CheckHealthAsync_UsesDaprHttpPortFromEnvironment()
    {
        // Arrange
        var customPort = "3501";
        Environment.SetEnvironmentVariable("DAPR_HTTP_PORT", customPort);

        var expectedUrl = $"http://localhost:{customPort}/v1.0/healthz";
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == expectedUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("healthy")
            });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock
            .Setup(x => x.CreateClient("DaprHealthCheck"))
            .Returns(httpClient);

        var healthCheck = new DaprSidecarHealthCheck(_httpClientFactoryMock.Object, _loggerMock.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);

        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == expectedUrl),
            ItExpr.IsAny<CancellationToken>());

        // Cleanup
        Environment.SetEnvironmentVariable("DAPR_HTTP_PORT", null);
    }
}
