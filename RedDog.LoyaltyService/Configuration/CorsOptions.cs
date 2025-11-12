using System.ComponentModel.DataAnnotations;

namespace RedDog.LoyaltyService.Configuration;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    [Required(ErrorMessage = "Cors:AllowedOrigins is required")]
    [MinLength(1, ErrorMessage = "At least one allowed origin must be provided")]
    public string[] AllowedOrigins { get; init; } = new[] { "http://localhost:8080" };
}
