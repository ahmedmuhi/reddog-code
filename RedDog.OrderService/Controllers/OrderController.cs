using System.Net;
using Dapr.Client;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using RedDog.OrderService.Models;

namespace RedDog.OrderService.Controllers;

[ApiController]
[EnableCors]
[Route("[controller]")]
public class OrderController : ControllerBase
{
    private readonly ILogger<OrderController> _logger;
    private readonly DaprClient _daprClient;

    // TODO: Migrate to Dapr Configuration API (ADR-0004)
    // For now, hardcoded constants remain
    private const string OrderTopic = "orders";
    private const string PubSubName = "reddog.pubsub";

    public OrderController(ILogger<OrderController> logger, DaprClient daprClient)
    {
        _logger = logger;
        _daprClient = daprClient;
    }

    [HttpPost]
    public async Task<IActionResult> NewOrder(CustomerOrder order)
    {
        // Structured logging with contextual properties (ADR-0011)
        _logger.LogInformation(
            "Customer Order received: StoreId={StoreId}, CustomerName={CustomerName}, OrderItemCount={OrderItemCount}",
            order.StoreId,
            $"{order.FirstName} {order.LastName}",
            order.OrderItems.Count);

        var orderSummary = await CreateOrderSummaryAsync(order);

        _logger.LogInformation(
            "Created Order Summary: OrderId={OrderId}, OrderTotal={OrderTotal}",
            orderSummary.OrderId,
            orderSummary.OrderTotal);

        try
        {
            await _daprClient.PublishEventAsync(PubSubName, OrderTopic, orderSummary);

            _logger.LogInformation(
                "Published Order Summary: OrderId={OrderId}",
                orderSummary.OrderId);
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Error publishing Order Summary: OrderId={OrderId}, Message={ErrorMessage}",
                orderSummary.OrderId,
                e.InnerException?.Message ?? e.Message);

            return Problem(e.Message, null, (int)HttpStatusCode.InternalServerError);
        }

        return Ok();
    }

    private static async Task<OrderSummary> CreateOrderSummaryAsync(CustomerOrder order)
    {
        // Retrieve all the items
        var products = await Product.GetAllAsync();

        // Iterate through the list of ordered items to calculate
        // the total and compile a list of item summaries.
        var orderTotal = 0.0m;
        var itemSummaries = new List<OrderItemSummary>();

        foreach (var orderItem in order.OrderItems)
        {
            var product = products.FirstOrDefault(x => x.ProductId == orderItem.ProductId);
            if (product == null) continue;

            orderTotal += product.UnitPrice * orderItem.Quantity;
            itemSummaries.Add(new OrderItemSummary
            {
                ProductId = orderItem.ProductId,
                ProductName = product.ProductName,
                Quantity = orderItem.Quantity,
                UnitCost = product.UnitCost,
                UnitPrice = product.UnitPrice,
                ImageUrl = product.ImageUrl
            });
        }

        // Initialize and return the order summary
        var summary = new OrderSummary
        {
            OrderId = Guid.NewGuid(),
            StoreId = order.StoreId,
            FirstName = order.FirstName,
            LastName = order.LastName,
            LoyaltyId = order.LoyaltyId,
            OrderDate = DateTime.UtcNow,
            OrderItems = itemSummaries,
            OrderTotal = orderTotal
        };

        return summary;
    }
}
