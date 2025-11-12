using System.Net;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RedDog.MakeLineService.Configuration;
using RedDog.MakeLineService.Models;
using RedDog.MakeLineService.Services;

namespace RedDog.MakeLineService.Controllers;

/// <summary>
/// MakeLineController manages the order queue for coffee orders.
/// Subscribes to incoming orders via Dapr pub/sub and provides REST API for order queue management.
/// Per ADR-0004: Configuration values will migrate to Dapr Configuration API in future iteration.
/// </summary>
[ApiController]
[Route("[controller]")]
public partial class MakelineController(
    IMakelineQueueProcessor makelineQueueProcessor,
    IOptions<DaprOptions> daprOptions,
    ILogger<MakelineController> logger) : ControllerBase
{
    private readonly DaprOptions _daprOptions = daprOptions?.Value ?? throw new ArgumentNullException(nameof(daprOptions));
    private readonly ILogger<MakelineController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMakelineQueueProcessor _makelineQueueProcessor = makelineQueueProcessor ?? throw new ArgumentNullException(nameof(makelineQueueProcessor));

    /// <summary>
    /// Receives incoming orders from Dapr pub/sub and adds them to the store's order queue.
    /// Implements optimistic concurrency control with retry loop for high-volume scenarios.
    /// </summary>
    /// <param name="orderSummary">Order details including store ID, customer info, and order items.</param>
    /// <returns>HTTP 200 OK if order successfully queued, HTTP 500 if error occurs.</returns>
    [HttpPost("/orders")]
    public async Task<IActionResult> AddOrderToMakeLine(OrderSummary orderSummary, CancellationToken cancellationToken)
    {
        if (orderSummary == null)
        {
            return BadRequest("OrderSummary cannot be null");
        }

        LogReceivedOrder(orderSummary.OrderId, orderSummary.StoreId, orderSummary.OrderTotal);

        try
        {
            await _makelineQueueProcessor.AddOrderAsync(orderSummary, cancellationToken);
            LogOrderAddedSuccessfully(orderSummary.OrderId);
        }
        catch (Exception e)
        {
            LogErrorSavingOrder(e, orderSummary.OrderId);
            return Problem(e.Message, null, (int)HttpStatusCode.InternalServerError);
        }

        return Ok();
    }

    /// <summary>
    /// Retrieves all orders in the queue for a specific store.
    /// Orders are returned sorted by order date (oldest first).
    /// </summary>
    /// <param name="storeId">The unique identifier for the store location.</param>
    /// <returns>Array of orders in the queue, sorted by order date.</returns>
    [HttpGet("/orders/{storeId}")]
    public async Task<IActionResult> GetOrders(string storeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(storeId))
        {
            return BadRequest("StoreId cannot be null or empty");
        }

        var orders = await _makelineQueueProcessor.GetOrdersAsync(storeId, cancellationToken);
        return Ok(orders.OrderBy(o => o.OrderDate));
    }

    /// <summary>
    /// Marks an order as completed and removes it from the queue.
    /// Publishes an order completed event to the pub/sub system.
    /// Implements optimistic concurrency control with retry loop.
    /// </summary>
    /// <param name="storeId">The unique identifier for the store location.</param>
    /// <param name="orderId">The unique identifier for the order to complete.</param>
    /// <returns>HTTP 200 OK if order completed successfully, HTTP 500 if error occurs.</returns>
    [HttpDelete("/orders/{storeId}/{orderId}")]
    public async Task<IActionResult> CompleteOrder(string storeId, Guid orderId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(storeId))
        {
            return BadRequest("StoreId cannot be null or empty");
        }

        LogCompletingOrder(orderId);

        DateTime orderCompletedDate = DateTime.UtcNow;

        try
        {
            var completed = await _makelineQueueProcessor.CompleteOrderAsync(storeId, orderId, orderCompletedDate, cancellationToken);
            if (completed)
            {
                LogPublishedOrderCompletedMessage(orderId);
            }
        }
        catch (OrderPublishException e)
        {
            LogErrorPublishingOrderCompletedMessage(e, orderId);
            return Problem(e.Message, null, (int)HttpStatusCode.InternalServerError);
        }
        catch (Exception e)
        {
            LogErrorSavingOrderSummaries(e);
            return Problem(e.Message, null, (int)HttpStatusCode.InternalServerError);
        }

        LogCompletedOrder(orderId);

        return Ok();
    }

    // Source generator logging (C# Pro performance pattern)
    // See: https://learn.microsoft.com/dotnet/core/extensions/logger-message-generator

    [LoggerMessage(Level = LogLevel.Information, Message = "Received order {OrderId} for store {StoreId} with total ${OrderTotal:F2}")]
    partial void LogReceivedOrder(Guid orderId, string storeId, decimal orderTotal);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully added order {OrderId} to Make Line")]
    partial void LogOrderAddedSuccessfully(Guid orderId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error saving order {OrderId} to state store")]
    partial void LogErrorSavingOrder(Exception exception, Guid orderId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Completing order {OrderId}")]
    partial void LogCompletingOrder(Guid orderId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Published order completed message for order {OrderId}")]
    partial void LogPublishedOrderCompletedMessage(Guid orderId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error publishing order completed message for order {OrderId}")]
    partial void LogErrorPublishingOrderCompletedMessage(Exception exception, Guid orderId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error saving order summaries to state store")]
    partial void LogErrorSavingOrderSummaries(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Completed order {OrderId}")]
    partial void LogCompletedOrder(Guid orderId);
}
