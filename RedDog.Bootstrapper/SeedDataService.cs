using Dapr.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RedDog.AccountingModel;

namespace RedDog.Bootstrapper;

/// <summary>
/// Service responsible for seeding the database with initial data.
/// </summary>
internal sealed class SeedDataService
{
    private const string SecretStoreName = "reddog.secretstore";
    private static readonly string DaprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";

    private readonly DaprClient _daprClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(
        DaprClient daprClient,
        IHttpClientFactory httpClientFactory,
        ILogger<SeedDataService> logger)
    {
        _daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Runs the database migration process.
    /// </summary>
    public async Task RunAsync()
    {
        _logger.LogInformation("Beginning EF Core migrations...");

        var connectionString = await GetDbConnectionStringAsync();

        var optionsBuilder = new DbContextOptionsBuilder<AccountingContext>()
            .UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            });

        using var context = new AccountingContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        _logger.LogInformation("Migrations complete.");
    }

    private async Task EnsureDaprOrTerminateAsync()
    {
        const int maxRetries = 30;
        const int retryDelayMs = 1000;

        using var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(5);

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var response = await httpClient.GetAsync($"http://localhost:{DaprHttpPort}/v1.0/healthz");
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("Successfully connected to Dapr sidecar.");
                return;
            }
            catch (Exception e)
            {
                _logger.LogInformation("Waiting for Dapr sidecar... Attempt {Attempt}/{MaxRetries}", i + 1, maxRetries);
                if (i == maxRetries - 1)
                {
                    _logger.LogError(e, "Error communicating with Dapr sidecar. Exiting...");
                    Environment.Exit(1);
                }
                await Task.Delay(retryDelayMs);
            }
        }
    }

    private async Task ShutdownDaprAsync()
    {
        _logger.LogInformation("Attempting to shutdown Dapr sidecar...");

        using var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(10);
        bool isDaprShutdownSuccessful = false;

        while (!isDaprShutdownSuccessful)
        {
            try
            {
                var response = await httpClient.PostAsync($"http://localhost:{DaprHttpPort}/v1.0/shutdown", null);

                if (response.IsSuccessStatusCode)
                {
                    isDaprShutdownSuccessful = true;
                    _logger.LogInformation("Successfully shutdown Dapr sidecar.");
                }
                else
                {
                    _logger.LogWarning("Unable to shutdown Dapr sidecar. Retrying in 5 seconds...");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Dapr error message: {ErrorMessage}", errorContent);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception occurred while attempting to shutdown the Dapr sidecar.");
            }

            if (!isDaprShutdownSuccessful)
            {
                await Task.Delay(5000);
            }
        }
    }

    private async Task<string> GetDbConnectionStringAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("reddog-sql");

        if (connectionString != null)
        {
            return connectionString;
        }

        await EnsureDaprOrTerminateAsync();

        _logger.LogInformation("Attempting to retrieve connection string from Dapr secret store...");

        Dictionary<string, string>? connectionStringSecret = null;

        while (connectionStringSecret == null)
        {
            try
            {
                _logger.LogInformation("Attempting to retrieve database connection string from Dapr...");
                connectionStringSecret = await _daprClient.GetSecretAsync(SecretStoreName, "reddog-sql");
                _logger.LogInformation("Successfully retrieved database connection string.");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "An exception occurred while retrieving the secret from the Dapr sidecar. Retrying in 5 seconds...");
                await Task.Delay(5000);
            }
        }

        connectionString = connectionStringSecret["reddog-sql"];
        await ShutdownDaprAsync();

        return connectionString;
    }
}
