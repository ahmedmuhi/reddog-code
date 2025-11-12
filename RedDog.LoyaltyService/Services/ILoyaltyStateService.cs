using Dapr.Client;
using Microsoft.Extensions.Options;
using RedDog.LoyaltyService.Configuration;
using RedDog.LoyaltyService.Models;

namespace RedDog.LoyaltyService.Services;

public interface ILoyaltyStateService
{
    Task<LoyaltySummary> UpdateAsync(OrderSummary orderSummary, CancellationToken cancellationToken = default);
}

internal sealed class LoyaltyStateService : ILoyaltyStateService
{
    private readonly DaprClient _daprClient;
    private readonly DaprOptions _options;
    private readonly StateOptions _stateOptions = new()
    {
        Concurrency = ConcurrencyMode.FirstWrite,
        Consistency = ConsistencyMode.Eventual
    };

    public LoyaltyStateService(DaprClient daprClient, IOptions<DaprOptions> options)
    {
        _daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    public async Task<LoyaltySummary> UpdateAsync(OrderSummary orderSummary, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(orderSummary);

        var loyaltyPointsEarned = (int)Math.Round(orderSummary.OrderTotal * 10, 0, MidpointRounding.AwayFromZero);

        while (true)
        {
            var (currentSummary, etag) = await _daprClient.GetStateAndETagAsync<LoyaltySummary>(
                _options.StateStoreName,
                orderSummary.LoyaltyId,
                cancellationToken: cancellationToken);

            currentSummary ??= new LoyaltySummary
            {
                FirstName = orderSummary.FirstName,
                LastName = orderSummary.LastName,
                LoyaltyId = orderSummary.LoyaltyId,
                PointTotal = 0
            };

            currentSummary.PointsEarned = loyaltyPointsEarned;
            currentSummary.PointTotal += loyaltyPointsEarned;

            var saved = await _daprClient.TrySaveStateAsync(
                _options.StateStoreName,
                orderSummary.LoyaltyId,
                currentSummary,
                etag,
                _stateOptions,
                cancellationToken: cancellationToken);

            if (saved)
            {
                return currentSummary;
            }
        }
    }
}
