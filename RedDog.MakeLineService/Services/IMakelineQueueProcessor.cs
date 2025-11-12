using System.Collections.Generic;
using System.Linq;
using Dapr.Client;
using Microsoft.Extensions.Options;
using RedDog.MakeLineService.Configuration;
using RedDog.MakeLineService.Models;

namespace RedDog.MakeLineService.Services;

public interface IMakelineQueueProcessor
{
    Task AddOrderAsync(OrderSummary orderSummary, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderSummary>> GetOrdersAsync(string storeId, CancellationToken cancellationToken = default);

    Task<bool> CompleteOrderAsync(string storeId, Guid orderId, DateTime orderCompletedDate, CancellationToken cancellationToken = default);
}

internal sealed class MakelineQueueProcessor : IMakelineQueueProcessor
{
    private readonly DaprClient _daprClient;
    private readonly DaprOptions _options;
    private readonly StateOptions _stateOptions = new()
    {
        Concurrency = ConcurrencyMode.FirstWrite,
        Consistency = ConsistencyMode.Eventual
    };

    public MakelineQueueProcessor(DaprClient daprClient, IOptions<DaprOptions> options)
    {
        _daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    public async Task AddOrderAsync(OrderSummary orderSummary, CancellationToken cancellationToken = default)
    {
        bool isSuccess;

        do
        {
            var state = await _daprClient.GetStateEntryAsync<List<OrderSummary>>(
                _options.StateStoreName,
                orderSummary.StoreId,
                cancellationToken: cancellationToken);

            state.Value ??= [];
            state.Value.Add(orderSummary);

            isSuccess = await state.TrySaveAsync(_stateOptions, cancellationToken: cancellationToken);
        }
        while (!isSuccess);
    }

    public async Task<IReadOnlyList<OrderSummary>> GetOrdersAsync(string storeId, CancellationToken cancellationToken = default)
    {
        var state = await _daprClient.GetStateEntryAsync<List<OrderSummary>>(
            _options.StateStoreName,
            storeId,
            cancellationToken: cancellationToken);

        return state.Value ?? [];
    }

    public async Task<bool> CompleteOrderAsync(string storeId, Guid orderId, DateTime orderCompletedDate, CancellationToken cancellationToken = default)
    {
        var orders = await _daprClient.GetStateEntryAsync<List<OrderSummary>>(
            _options.StateStoreName,
            storeId,
            cancellationToken: cancellationToken);

        var order = orders.Value?.FirstOrDefault(o => o.OrderId == orderId);

        if (order is null)
        {
            return false;
        }

        order.OrderCompletedDate = orderCompletedDate;

        try
        {
            await _daprClient.PublishEventAsync(
                _options.PubSubName,
                _options.OrderCompletedTopic,
                order,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            throw new OrderPublishException("Failed to publish order completed event.", ex);
        }

        bool isSuccess;
        do
        {
            orders.Value!.RemoveAll(o => o.OrderId == orderId);
            isSuccess = await orders.TrySaveAsync(_stateOptions, cancellationToken: cancellationToken);

            if (!isSuccess)
            {
                orders = await _daprClient.GetStateEntryAsync<List<OrderSummary>>(
                    _options.StateStoreName,
                    storeId,
                    cancellationToken: cancellationToken);

                order = orders.Value?.FirstOrDefault(o => o.OrderId == orderId);
                if (order != null)
                {
                    order.OrderCompletedDate = orderCompletedDate;
                }
            }
        }
        while (!isSuccess);

        return true;
    }
}

public sealed class OrderPublishException : Exception
{
    public OrderPublishException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
