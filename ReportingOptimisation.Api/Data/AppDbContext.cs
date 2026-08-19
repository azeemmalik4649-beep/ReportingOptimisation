using Microsoft.EntityFrameworkCore;
using ReportingOptimisation.Api.Models;

namespace ReportingOptimisation.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // decimal precision explicitly set — SQL Server default precision can silently
        // truncate money values, so we're always explicit with financial columns.
        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.UnitPriceAtPurchase)
            .HasPrecision(18, 2);

        // Helpful indexes we'll actually exercise later in the optimisation steps
        modelBuilder.Entity<Order>()
            .HasIndex(o => o.CustomerId);

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.OrderDate);

        modelBuilder.Entity<OrderItem>()
            .HasIndex(oi => oi.ProductId);
    }
}
