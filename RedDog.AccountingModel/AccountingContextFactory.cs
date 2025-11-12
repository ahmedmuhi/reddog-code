using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RedDog.AccountingModel;

/// <summary>
/// Design-time factory for AccountingContext.
/// Used by dotnet ef commands (migrations, dbcontext optimize, etc.)
/// This is NOT used at runtime - only for EF Core tooling.
/// </summary>
public class AccountingContextFactory : IDesignTimeDbContextFactory<AccountingContext>
{
    public AccountingContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AccountingContext>();

        // Use connection string from environment variable or safe localhost default
        // This is ONLY used by dotnet ef commands, not at runtime
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__RedDog")
            ?? "Server=localhost,1433;Database=reddog;User Id=sa;Password=DummyPassword123!;TrustServerCertificate=true;";

        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });

        return new AccountingContext(optionsBuilder.Options);
    }
}
