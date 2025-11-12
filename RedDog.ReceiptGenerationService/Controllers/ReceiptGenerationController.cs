using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using RedDog.ReceiptGenerationService.Models;

namespace RedDog.ReceiptGenerationService.Controllers;

[ApiController]
public class ReceiptGenerationConsumerController : ControllerBase
{
    // TODO ADR-0004: Migrate hardcoded constants to Dapr Configuration API
    // These constants work for demo purposes but should be externalized for production
    private const string OrderTopic = "orders";
    private const string PubSubName = "reddog.pubsub";
    private const string ReceiptBindingName = "reddog.binding.receipt";

    private readonly ILogger<ReceiptGenerationConsumerController> _logger;

    public ReceiptGenerationConsumerController(ILogger<ReceiptGenerationConsumerController> logger)
    {
        _logger = logger;
    }

    [Topic(PubSubName, OrderTopic)]
    [HttpPost("orders")]
    public async Task<IActionResult> GenerateReceipt(OrderSummary orderSummary, [FromServices] DaprClient daprClient)
    {
        // ADR-0011: Structured logging with contextual properties (OrderId, StoreId, CustomerName)
        _logger.LogInformation(
            "Received order for receipt generation: OrderId={OrderId}, StoreId={StoreId}, CustomerName={CustomerName}, OrderTotal={OrderTotal}",
            orderSummary.OrderId,
            orderSummary.StoreId,
            $"{orderSummary.FirstName} {orderSummary.LastName}",
            orderSummary.OrderTotal);

        try
        {
            // ADR-0012: Dapr binding for object storage (localstorage for local dev, cloud blob storage for production)
            Dictionary<string, string> metadata = new()
            {
                { "blobName", $"{orderSummary.OrderId}.json" }
            };

            await daprClient.InvokeBindingAsync(ReceiptBindingName, "create", orderSummary, metadata);

            _logger.LogInformation(
                "Receipt successfully written to storage: OrderId={OrderId}, BlobName={BlobName}",
                orderSummary.OrderId,
                $"{orderSummary.OrderId}.json");
        }
        catch (Exception ex)
        {
            // ADR-0011: Log full exception details with structured properties
            _logger.LogError(
                ex,
                "Failed to write receipt to storage: OrderId={OrderId}, StoreId={StoreId}, ErrorMessage={ErrorMessage}",
                orderSummary.OrderId,
                orderSummary.StoreId,
                ex.Message);

            return Problem(ex.Message, null, 500);
        }

        return Ok();
    }
}
