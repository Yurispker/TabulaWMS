using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TabulaWMS.Models; 

namespace TabulaWMS.Data;

public class AdminDbContext : IdentityDbContext<User>
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options)
    {
    }

    // Global table mapping emails to their respective tenant schemas
    public DbSet<TenantUserDirectory> TenantUserDirectories => Set<TenantUserDirectory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Explicitly force this context to always use the "public" schema
        builder.HasDefaultSchema("public");

        builder.Entity<TenantUserDirectory>(entity =>
        {
            entity.HasKey(e => e.Email);
            entity.ToTable("TenantUserDirectory", "public");
        });
    }
}