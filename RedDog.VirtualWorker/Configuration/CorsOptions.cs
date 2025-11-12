using System.ComponentModel.DataAnnotations;

namespace RedDog.VirtualWorker.Configuration;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    [Required]
    [MinLength(1)]
    public string[] AllowedOrigins { get; init; } = new[] { "http://localhost:8080" };
}
