using Dapr.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RedDog.AccountingModel;

namespace RedDog.Bootstrapper;

/// <summary>
/// Database bootstrapper for RedDog Accounting database.
/// Applies EF Core migrations on startup.
/// </summary>
internal sealed class Program : IDesignTimeDbContextFactory<AccountingContext>
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register DaprClient for client-only apps (no incoming HTTP listeners needed)
                services.AddDaprClient();

                // Register HttpClient for direct sidecar health/shutdown calls
                services.AddHttpClient();

                // Encapsulate logic in a service
                services.AddTransient<SeedDataService>();
            })
            .Build();

        // Execute the logic
        await host.Services.GetRequiredService<SeedDataService>().RunAsync();
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
}
