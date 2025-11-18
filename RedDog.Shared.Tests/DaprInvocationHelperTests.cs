using Dapr.Client;
using FluentAssertions;
using Moq;
using RedDog.Shared;
using Xunit;

namespace RedDog.Shared.Tests;

public class DaprInvocationHelperTests
{
    private readonly Mock<DaprClient> _daprClientMock;
    private readonly DaprInvocationHelper _helper;

    public DaprInvocationHelperTests()
    {
        _daprClientMock = new Mock<DaprClient>();
        _helper = new DaprInvocationHelper(_daprClientMock.Object);
    }

    [Fact]
    public void Constructor_WithNullDaprClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new DaprInvocationHelper(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("daprClient");
    }

    [Fact]
    public async Task InvokeMethodAsync_PostWithResponse_CreatesRequestWithContentType()
    {
        // Arrange
        var appId = "test-app";
        var methodName = "test-method";
        var request = new TestRequest { Name = "test" };
        var expectedResponse = new TestResponse { Value = "success" };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"http://localhost/v1.0/invoke/{appId}/method/{methodName}");

        _daprClientMock
            .Setup(x => x.CreateInvokeMethodRequest(
                HttpMethod.Post,
                appId,
                methodName))
            .Returns(httpRequest);

        _daprClientMock
            .Setup(x => x.InvokeMethodAsync<TestResponse>(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _helper.InvokeMethodAsync<TestRequest, TestResponse>(appId, methodName, request);

        // Assert
        result.Should().Be(expectedResponse);
        httpRequest.Content.Should().NotBeNull();
        httpRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
        _daprClientMock.Verify(x => x.CreateInvokeMethodRequest(
            HttpMethod.Post,
            appId,
            methodName), Times.Once);
    }

    [Fact]
    public async Task InvokeMethodAsync_PostWithoutResponse_CreatesRequestWithContentType()
    {
        // Arrange
        var appId = "test-app";
        var methodName = "test-method";
        var request = new TestRequest { Name = "test" };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"http://localhost/v1.0/invoke/{appId}/method/{methodName}");

        _daprClientMock
            .Setup(x => x.CreateInvokeMethodRequest(
                HttpMethod.Post,
                appId,
                methodName))
            .Returns(httpRequest);

        _daprClientMock
            .Setup(x => x.InvokeMethodAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _helper.InvokeMethodAsync(appId, methodName, request);

        // Assert
        httpRequest.Content.Should().NotBeNull();
        httpRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
        _daprClientMock.Verify(x => x.InvokeMethodAsync(
            It.IsAny<HttpRequestMessage>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvokeMethodDeleteAsync_WithBody_CreatesRequestWithContentType()
    {
        // Arrange
        var appId = "test-app";
        var methodName = "test-method";
        var request = new TestRequest { Name = "test" };

        var httpRequest = new HttpRequestMessage(HttpMethod.Delete, $"http://localhost/v1.0/invoke/{appId}/method/{methodName}");

        _daprClientMock
            .Setup(x => x.CreateInvokeMethodRequest(
                HttpMethod.Delete,
                appId,
                methodName))
            .Returns(httpRequest);

        _daprClientMock
            .Setup(x => x.InvokeMethodAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _helper.InvokeMethodDeleteAsync(appId, methodName, request);

        // Assert
        httpRequest.Content.Should().NotBeNull();
        httpRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task InvokeMethodPutAsync_WithoutResponse_CreatesRequestWithContentType()
    {
        // Arrange
        var appId = "test-app";
        var methodName = "test-method";
        var request = new TestRequest { Name = "test" };

        var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"http://localhost/v1.0/invoke/{appId}/method/{methodName}");

        _daprClientMock
            .Setup(x => x.CreateInvokeMethodRequest(
                HttpMethod.Put,
                appId,
                methodName))
            .Returns(httpRequest);

        _daprClientMock
            .Setup(x => x.InvokeMethodAsync(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _helper.InvokeMethodPutAsync(appId, methodName, request);

        // Assert
        httpRequest.Content.Should().NotBeNull();
        httpRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task InvokeMethodPutAsync_WithResponse_CreatesRequestWithContentType()
    {
        // Arrange
        var appId = "test-app";
        var methodName = "test-method";
        var request = new TestRequest { Name = "test" };
        var expectedResponse = new TestResponse { Value = "success" };

        var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"http://localhost/v1.0/invoke/{appId}/method/{methodName}");

        _daprClientMock
            .Setup(x => x.CreateInvokeMethodRequest(
                HttpMethod.Put,
                appId,
                methodName))
            .Returns(httpRequest);

        _daprClientMock
            .Setup(x => x.InvokeMethodAsync<TestResponse>(
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _helper.InvokeMethodPutAsync<TestRequest, TestResponse>(appId, methodName, request);

        // Assert
        result.Should().Be(expectedResponse);
        httpRequest.Content.Should().NotBeNull();
        httpRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    private class TestRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    private class TestResponse
    {
        public string Value { get; set; } = string.Empty;
    }
}
