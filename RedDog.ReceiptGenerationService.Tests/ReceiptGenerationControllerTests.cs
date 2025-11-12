using Dapr.Client;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RedDog.ReceiptGenerationService.Controllers;
using RedDog.ReceiptGenerationService.Models;

namespace RedDog.ReceiptGenerationService.Tests;

public class ReceiptGenerationControllerTests
{
    private readonly Mock<ILogger<ReceiptGenerationConsumerController>> _loggerMock;
    private readonly Mock<DaprClient> _daprClientMock;
    private readonly ReceiptGenerationConsumerController _controller;

    public ReceiptGenerationControllerTests()
    {
        _loggerMock = new Mock<ILogger<ReceiptGenerationConsumerController>>();
        _daprClientMock = new Mock<DaprClient>();
        _controller = new ReceiptGenerationConsumerController(_loggerMock.Object);
    }

    [Fact]
    public async Task GenerateReceipt_ValidOrder_ReturnsOkAndInvokesBinding()
    {
        // Arrange
        var orderSummary = new OrderSummary
        {
            OrderId = Guid.NewGuid(),
            StoreId = "Redmond",
            FirstName = "John",
            LastName = "Doe",
            LoyaltyId = "12345",
            OrderTotal = 25.50m,
            OrderItems = new List<OrderItemSummary>
            {
                new() { ProductId = 1, ProductName = "Latte", Quantity = 2, UnitPrice = 5.00m, UnitCost = 2.50m }
            }
        };

        // Mock Dapr InvokeBindingAsync to succeed
        _daprClientMock
            .Setup(x => x.InvokeBindingAsync<OrderSummary>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<OrderSummary>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.GenerateReceipt(orderSummary, _daprClientMock.Object);

        // Assert
        result.Should().BeOfType<OkResult>();
        _daprClientMock.Verify(x => x.InvokeBindingAsync<OrderSummary>(
            "reddog.binding.receipt",
            "create",
            orderSummary,
            It.Is<IReadOnlyDictionary<string, string>>(m => m.ContainsKey("blobName") && m["blobName"] == $"{orderSummary.OrderId}.json"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateReceipt_BindingFails_ReturnsProblemAndLogsError()
    {
        // Arrange
        var orderSummary = new OrderSummary
        {
            OrderId = Guid.NewGuid(),
            StoreId = "Redmond",
            FirstName = "Jane",
            LastName = "Smith",
            LoyaltyId = "67890",
            OrderTotal = 15.75m,
            OrderItems = new List<OrderItemSummary>
            {
                new() { ProductId = 2, ProductName = "Cappuccino", Quantity = 1, UnitPrice = 4.50m, UnitCost = 2.00m }
            }
        };

        var exception = new InvalidOperationException("Dapr binding failed");

        // Mock Dapr InvokeBindingAsync to throw exception
        _daprClientMock
            .Setup(x => x.InvokeBindingAsync<OrderSummary>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<OrderSummary>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GenerateReceipt(orderSummary, _daprClientMock.Object);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);

        // Verify error was logged with structured properties
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to write receipt to storage")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateReceipt_LogsStructuredInformationOnSuccess()
    {
        // Arrange
        var orderSummary = new OrderSummary
        {
            OrderId = Guid.NewGuid(),
            StoreId = "Seattle",
            FirstName = "Alice",
            LastName = "Johnson",
            LoyaltyId = "11111",
            OrderTotal = 30.00m,
            OrderItems = new List<OrderItemSummary>
            {
                new() { ProductId = 3, ProductName = "Americano", Quantity = 3, UnitPrice = 3.50m, UnitCost = 1.50m }
            }
        };

        _daprClientMock
            .Setup(x => x.InvokeBindingAsync<OrderSummary>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<OrderSummary>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _controller.GenerateReceipt(orderSummary, _daprClientMock.Object);

        // Assert - Verify structured logging was called with order details
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Received order for receipt generation")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Receipt successfully written to storage")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
