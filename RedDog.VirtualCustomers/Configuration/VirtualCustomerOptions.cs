using System.ComponentModel.DataAnnotations;

namespace RedDog.VirtualCustomers.Configuration;

public sealed class VirtualCustomerOptions
{
    public const string SectionName = "VirtualCustomers";

    [Required]
    public string StoreId { get; init; } = "Redmond";

    [Range(1, 50)]
    public int MaxItemQuantity { get; init; } = 1;

    [Range(1, 20)]
    public int MaxUniqueItemsPerOrder { get; init; } = 5;

    [Range(0, 60)]
    public int MinSecondsToPlaceOrder { get; init; } = 1;

    [Range(0, 60)]
    public int MaxSecondsToPlaceOrder { get; init; } = 3;

    [Range(0, 60)]
    public int MinSecondsBetweenOrders { get; init; } = 1;

    [Range(0, 60)]
    public int MaxSecondsBetweenOrders { get; init; } = 3;

    /// <summary>
    /// -1 indicates infinite orders.
    /// </summary>
    public int NumOrders { get; init; } = -1;

    /// <summary>
    /// When true, Dapr calls are skipped (used for local smoke tests).
    /// </summary>
    public bool DisableDaprCalls { get; init; }
}
