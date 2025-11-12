using System.ComponentModel.DataAnnotations;

namespace RedDog.VirtualWorker.Configuration;

public sealed class DaprOptions
{
    public const string SectionName = "Dapr";

    [Required]
    public string MakeLineServiceAppId { get; init; } = "makelineservice";

    [Required]
    public string StoreId { get; init; } = "Redmond";

    [Range(1, 60)]
    public int MinSecondsToCompleteItem { get; init; } = 1;

    [Range(1, 60)]
    public int MaxSecondsToCompleteItem { get; init; } = 5;
}
