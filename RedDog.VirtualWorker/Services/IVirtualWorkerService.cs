using System.Net.Http;
using Dapr.Client;
using Microsoft.Extensions.Options;
using RedDog.VirtualWorker.Configuration;
using RedDog.VirtualWorker.Models;

namespace RedDog.VirtualWorker.Services;

public interface IVirtualWorkerService
{
    Task RunOnceAsync(CancellationToken cancellationToken = default);
}

internal sealed class VirtualWorkerService : IVirtualWorkerService
{
    private readonly DaprClient _daprClient;
    private readonly DaprOptions _options;
    private readonly ILogger<VirtualWorkerService> _logger;
    private readonly Random _random = Random.Shared;
    private readonly object _lock = new();
    private bool _isProcessing;

    public VirtualWorkerService(DaprClient daprClient, IOptions<DaprOptions> options, ILogger<VirtualWorkerService> logger)
    {
        _daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        if (!TryStartProcessing())
        {
            _logger.LogDebug("Virtual worker already processing orders for store {StoreId}", _options.StoreId);
            return;
        }

        try
        {
            _logger.LogInformation("Virtual worker ({StoreId}) checking make line orders...", _options.StoreId);
            var orders = await GetOrdersAsync(cancellationToken);
            _logger.LogInformation("Virtual worker found {Count} orders waiting", orders.Count);

            foreach (var order in orders)
            {
                await ProcessOrderAsync(order, cancellationToken);
            }
        }
        finally
        {
            FinishProcessing();
        }
    }

    private bool TryStartProcessing()
    {
        lock (_lock)
        {
            if (_isProcessing)
            {
                return false;
            }

            _isProcessing = true;
            return true;
        }
    }

    private void FinishProcessing()
    {
        lock (_lock)
        {
            _isProcessing = false;
        }
    }

    private async Task<List<OrderSummary>> GetOrdersAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _daprClient.InvokeMethodAsync<List<OrderSummary>>(
                HttpMethod.Get,
                _options.MakeLineServiceAppId,
                $"orders/{_options.StoreId}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve orders from MakeLineService ({AppId})", _options.MakeLineServiceAppId);
            return new List<OrderSummary>();
        }
    }

    private async Task ProcessOrderAsync(OrderSummary order, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Virtual worker making order {OrderId} for {First} {Last}", order.OrderId, order.FirstName, order.LastName);

        foreach (var item in order.OrderItems)
        {
            _logger.LogInformation("Preparing {Quantity} x {ProductName}", item.Quantity, item.ProductName);
            await Task.Delay(TimeSpan.FromSeconds(_random.Next(_options.MinSecondsToCompleteItem, _options.MaxSecondsToCompleteItem + 1)), cancellationToken);
        }

        await CompleteOrderAsync(order, cancellationToken);
        _logger.LogInformation("Order {OrderId} completed", order.OrderId);
    }

    private async Task CompleteOrderAsync(OrderSummary order, CancellationToken cancellationToken)
    {
        try
        {
            await _daprClient.InvokeMethodAsync(
                HttpMethod.Delete,
                _options.MakeLineServiceAppId,
                $"orders/{order.StoreId}/{order.OrderId}",
                order,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete order {OrderId}", order.OrderId);
        }
    }
}
