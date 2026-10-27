using Microsoft.EntityFrameworkCore;
using TabulaWMS.Models;

namespace TabulaWMS.Data;

public class TabulaContext : DbContext
{
    public TabulaContext(DbContextOptions<TabulaContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>()
            .HasIndex(i => i.SKU)
            .IsUnique();


        modelBuilder.Entity<Product>()
            .Property(s => s.Quantity)
            .HasDefaultValue(0);
    }
}