using System.ComponentModel.DataAnnotations;

namespace RedDog.LoyaltyService.Configuration;

/// <summary>
/// Configuration for Dapr components used by LoyaltyService.
/// </summary>
public sealed class DaprOptions
{
    public const string SectionName = "Dapr";

    [Required(ErrorMessage = "Dapr:OrderTopic is required")]
    public string OrderTopic { get; init; } = "orders";

    [Required(ErrorMessage = "Dapr:PubSubName is required")]
    public string PubSubName { get; init; } = "reddog.pubsub";

    [Required(ErrorMessage = "Dapr:StateStoreName is required")]
    public string StateStoreName { get; init; } = "reddog.state.loyalty";
}
