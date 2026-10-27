using Microsoft.EntityFrameworkCore;
using TabulaWMS.Models;

namespace TabulaWMS.Data;

public class TabulaContext : DbContext
{
    public TabulaContext(DbContextOptions<TabulaContext> options) : base(options)
    {
    }

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<InventoryItem>()
            .HasIndex(i => i.SKU)
            .IsUnique();
    }
}