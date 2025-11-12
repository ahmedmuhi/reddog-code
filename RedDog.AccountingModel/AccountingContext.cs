using Microsoft.EntityFrameworkCore;

namespace RedDog.AccountingModel;

public class AccountingContext(DbContextOptions<AccountingContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<StoreLocation> Stores => Set<StoreLocation>();
}