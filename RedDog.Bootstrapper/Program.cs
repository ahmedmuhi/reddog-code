using Dapr.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RedDog.AccountingModel;

namespace RedDog.Bootstrapper;

/// <summary>
/// Database bootstrapper for RedDog Accounting database.
/// Applies EF Core migrations on startup.
/// </summary>
internal sealed class Program : IDesignTimeDbContextFactory<AccountingContext>
{
    private const string SecretStoreName = "reddog.secretstore";
    private static readonly string DaprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";

    static async Task Main(string[] args)
    {
        Console.WriteLine("Beginning EF Core migrations...");

        var program = new Program();
        var connectionString = await program.GetDbConnectionStringAsync();

        using var context = program.CreateDbContextWithConnectionString(connectionString);
        await context.Database.MigrateAsync();

        Console.WriteLine("Migrations complete.");
    }

    /// <summary>
    /// Creates DbContext for EF Core design-time tools (dotnet ef).
    /// Required by IDesignTimeDbContextFactory - must be synchronous.
    /// </summary>
    public AccountingContext CreateDbContext(string[]? args)
    {
        // For design-time tools (dotnet ef), read from environment variable
        var connectionString = Environment.GetEnvironmentVariable("reddog-sql")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__RedDog")
            ?? throw new InvalidOperationException(
                "Database connection string 'reddog-sql' not found in environment variables. " +
                "Set the environment variable before running EF Core tools.");

        return CreateDbContextWithConnectionString(connectionString);
    }

    private AccountingContext CreateDbContextWithConnectionString(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AccountingContext>()
            .UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            });

        return new AccountingContext(optionsBuilder.Options);
    }

    private async Task EnsureDaprOrTerminateAsync()
    {
        const int maxRetries = 30;
        const int retryDelayMs = 1000;

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var response = await httpClient.GetAsync($"http://localhost:{DaprHttpPort}/v1.0/healthz");
                response.EnsureSuccessStatusCode();
                Console.WriteLine("Successfully connected to Dapr sidecar.");
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Waiting for Dapr sidecar... Attempt {i + 1}/{maxRetries}");
                if (i == maxRetries - 1)
                {
                    Console.WriteLine($"Error communicating with Dapr sidecar. Exiting... {e.InnerException?.Message ?? e.Message}");
                    Environment.Exit(1);
                }
                await Task.Delay(retryDelayMs);
            }
        }
    }

    private async Task ShutdownDaprAsync()
    {
        Console.WriteLine("Attempting to shutdown Dapr sidecar...");

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        bool isDaprShutdownSuccessful = false;

        while (!isDaprShutdownSuccessful)
        {
            try
            {
                var response = await httpClient.PostAsync($"http://localhost:{DaprHttpPort}/v1.0/shutdown", null);

                if (response.IsSuccessStatusCode)
                {
                    isDaprShutdownSuccessful = true;
                    Console.WriteLine("Successfully shutdown Dapr sidecar.");
                }
                else
                {
                    Console.WriteLine("Unable to shutdown Dapr sidecar. Retrying in 5 seconds...");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Dapr error message: {errorContent}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An exception occurred while attempting to shutdown the Dapr sidecar.");
                Console.WriteLine(e.StackTrace);
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

        Console.WriteLine("Attempting to retrieve connection string from Dapr secret store...");

        using var daprClient = new DaprClientBuilder().Build();
        Dictionary<string, string>? connectionStringSecret = null;

        while (connectionStringSecret == null)
        {
            try
            {
                Console.WriteLine("Attempting to retrieve database connection string from Dapr...");
                connectionStringSecret = await daprClient.GetSecretAsync(SecretStoreName, "reddog-sql");
                Console.WriteLine("Successfully retrieved database connection string.");
            }
            catch (Exception e)
            {
                Console.WriteLine("An exception occurred while retrieving the secret from the Dapr sidecar. Retrying in 5 seconds...");
                Console.WriteLine(e.InnerException?.Message ?? e.Message);
                Console.WriteLine(e.StackTrace);
                await Task.Delay(5000);
            }
        }

        connectionString = connectionStringSecret["reddog-sql"];
        await ShutdownDaprAsync();

        return connectionString;
    }
}
