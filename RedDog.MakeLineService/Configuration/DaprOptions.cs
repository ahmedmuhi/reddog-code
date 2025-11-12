using System.ComponentModel.DataAnnotations;

namespace RedDog.MakeLineService.Configuration;

/// <summary>
/// Configuration options for Dapr components used by MakeLineService.
/// Validates required configuration at application startup (fail-fast pattern).
/// </summary>
public sealed class DaprOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "Dapr";

    /// <summary>
    /// Name of the pub/sub topic for incoming orders.
    /// MakeLineService subscribes to this topic to receive new orders from OrderService.
    /// </summary>
    [Required(ErrorMessage = "Dapr:OrderTopic is required")]
    public string OrderTopic { get; init; } = "orders";

    /// <summary>
    /// Name of the pub/sub topic for completed orders.
    /// MakeLineService publishes to this topic when VirtualWorker completes an order.
    /// </summary>
    [Required(ErrorMessage = "Dapr:OrderCompletedTopic is required")]
    public string OrderCompletedTopic { get; init; } = "ordercompleted";

    /// <summary>
    /// Name of the Dapr pub/sub component.
    /// Configured in .dapr/components/ (local) or via Helm (Kubernetes).
    /// </summary>
    [Required(ErrorMessage = "Dapr:PubSubName is required")]
    public string PubSubName { get; init; } = "reddog.pubsub";

    /// <summary>
    /// Name of the Dapr state store component for order queue management.
    /// Stores order queue per storeId with optimistic concurrency control.
    /// </summary>
    [Required(ErrorMessage = "Dapr:StateStoreName is required")]
    public string StateStoreName { get; init; } = "reddog.state.makeline";
}
