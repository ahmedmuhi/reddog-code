using System.ComponentModel.DataAnnotations;

namespace RedDog.MakeLineService.Configuration;

/// <summary>
/// Configuration options for CORS (Cross-Origin Resource Sharing) policy.
/// Per ADR-0006, infrastructure configuration is managed via environment variables.
/// TODO-ADR0004: Consider migrating to Dapr Configuration API in future for centralized config management.
/// </summary>
public sealed class CorsOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "Cors";

    /// <summary>
    /// Array of allowed origins for CORS requests.
    /// Typically includes UI application URLs (e.g., "http://localhost:8080" for local dev).
    /// Can be set via environment variable: CORS__ALLOWED_ORIGINS (double underscore for array delimiter).
    /// </summary>
    [Required(ErrorMessage = "Cors:AllowedOrigins is required")]
    [MinLength(1, ErrorMessage = "At least one allowed origin must be specified")]
    public string[] AllowedOrigins { get; init; } = ["http://localhost:8080"];
}
