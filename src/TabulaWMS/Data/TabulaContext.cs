using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TabulaWMS.Models;

namespace TabulaWMS.Data;

public class TabulaContext : IdentityDbContext<User, IdentityRole, string>
{
    public TabulaContext(DbContextOptions<TabulaContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Build db tables
        base.OnModelCreating(builder);

        // Product rules
        builder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.SKU).IsRequired();
            entity.HasIndex(p => p.SKU).IsUnique(); // Duplicate SKUs are blocked
        });

        // User rules
        builder.Entity<User>(entity =>
        {
            entity.Property(u => u.Name).IsRequired();
            entity.Property(u => u.Role).IsRequired();
            entity.Property(u => u.CreatedBy).IsRequired();
        });
    }
}