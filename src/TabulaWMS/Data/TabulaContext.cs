using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using TabulaWMS.Models;
using TabulaWMS.Services;
using Npgsql;

namespace TabulaWMS.Data;

public class TabulaContext : IdentityDbContext<User, IdentityRole, string>
{
    private readonly ITenantService _tenantService;
    private readonly IConfiguration _configuration;

    public TabulaContext(
        DbContextOptions<TabulaContext> options,
        ITenantService tenantService,
        IConfiguration configuration) : base(options)
    {
        _tenantService = tenantService;
        _configuration = configuration;
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<TenantUserDirectory> TenantUserDirectories { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {

        base.OnConfiguring(optionsBuilder);

        // Dynamically shift the PostgreSQL search path based on the resolved tenant header
        var baseConnString = _configuration.GetConnectionString("DefaultConnection");
        var tenantId = _tenantService.GetTenantId();

        var builder = new NpgsqlConnectionStringBuilder(baseConnString);
        builder.SearchPath = string.IsNullOrEmpty(tenantId) ? "public" : $"{tenantId}, public";

        optionsBuilder.UseNpgsql(builder.ConnectionString);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Build db tables (including ASP.NET Core Identity tables)
        base.OnModelCreating(builder);

        // Product rules
        builder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.SKU).IsRequired();
            entity.Property(p => p.IsDeleted).HasDefaultValue(false);
            entity.HasIndex(p => p.SKU).IsUnique(); // Duplicate SKUs are blocked
        });

        // User rules
        builder.Entity<User>(entity =>
        {
            entity.Property(u => u.Name).IsRequired();
            entity.Property(u => u.Role).IsRequired();
            entity.Property(u => u.CreatedBy).IsRequired();
        });

        //Email lookup table for tenant-user mapping
        builder.Entity<TenantUserDirectory>(entity => 
        {
            entity.ToTable("TenantUserDirectory", "public"); // Ensure this table is always in the public schema
            entity.HasKey(t => t.Email);
        });
    }
}