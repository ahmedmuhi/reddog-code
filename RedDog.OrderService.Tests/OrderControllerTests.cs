using Dapr.Client;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RedDog.OrderService.Controllers;
using RedDog.OrderService.Models;
using Xunit;

namespace RedDog.OrderService.Tests;

public class OrderControllerTests
{
    private readonly Mock<ILogger<OrderController>> _loggerMock;
    private readonly Mock<DaprClient> _daprClientMock;
    private readonly OrderController _controller;

    public OrderControllerTests()
    {
        _loggerMock = new Mock<ILogger<OrderController>>();
        _daprClientMock = new Mock<DaprClient>();
        _controller = new OrderController(_loggerMock.Object, _daprClientMock.Object);
    }

    [Fact]
    public async Task NewOrder_ValidOrder_ReturnsOk()
    {
        // Arrange
        var order = new CustomerOrder
        {
            StoreId = "Redmond",
            FirstName = "John",
            LastName = "Doe",
            LoyaltyId = "12345",
            OrderItems =
            [
                new CustomerOrderItem { ProductId = 1, Quantity = 2 }
            ]
        };

        // Mock Dapr PublishEventAsync to succeed
        _daprClientMock
            .Setup(x => x.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<OrderSummary>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.NewOrder(order);

        // Assert
        result.Should().BeOfType<OkResult>();
        _daprClientMock.Verify(x => x.PublishEventAsync(
            "reddog.pubsub",
            "orders",
            It.IsAny<OrderSummary>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NewOrder_PublishFails_ReturnsProblem()
    {
        // Arrange
        var order = new CustomerOrder
        {
            StoreId = "Redmond",
            FirstName = "John",
            LastName = "Doe",
            LoyaltyId = "12345",
            OrderItems =
            [
                new CustomerOrderItem { ProductId = 1, Quantity = 2 }
            ]
        };

        // Mock Dapr PublishEventAsync to throw exception
        _daprClientMock
            .Setup(x => x.PublishEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<OrderSummary>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Dapr publish failed"));

        // Act
        var result = await _controller.NewOrder(order);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }
}
