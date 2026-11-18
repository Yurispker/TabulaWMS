using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TabulaWMS.Data;
using TabulaWMS.Models.DTOs;
using TabulaWMS.Models;
using TabulaWMS.DTOs;
using TabulaWMS.Constants;


namespace TabulaWMS.Services;

public class TenantProvisioningService : ITenantProvisioningService
{
    private readonly IConfiguration _configuration;
    private readonly ITenantService _tenantService;
    private readonly TabulaContext _context;
    private readonly ILogger<TenantProvisioningService> _logger;
    private readonly UserManager<User> _userManager;
    private readonly AuthService _authService;
    private readonly AdminDbContext _adminContext;
    private readonly RoleManager<IdentityRole> _roleManager;
    private static readonly Regex ValidTenantRegex = new("^[a-z0-9_]+$", RegexOptions.Compiled);

    public TenantProvisioningService(
        IConfiguration configuration,
        ITenantService tenantService,
        TabulaContext context,
        ILogger<TenantProvisioningService> logger,
        UserManager<User> userManager,
        AuthService authService,
        AdminDbContext adminContext,
        RoleManager<IdentityRole> roleManager)
    {
        _configuration = configuration;
        _tenantService = tenantService;
        _context = context;
        _logger = logger;  
        _userManager = userManager;
        _authService = authService;
        _adminContext = adminContext;
        _roleManager = roleManager;
    }

    public async Task ProvisionTenantAsync(CreateTenantDto model)
    {
        
        var tenantId = model.TenantId;

        // Sanitize and validate tenantId to prevent SQL injection.
        if (string.IsNullOrWhiteSpace(tenantId) || !ValidTenantRegex.IsMatch(tenantId))
        {
            throw new ArgumentException("Invalid tenant identifier. Use only lowercase letters, numbers, and underscores.", nameof(tenantId));
        }

        _logger.LogInformation("Starting provisioning process for tenant schema: {TenantId}", tenantId);

        var baseConnString = _configuration.GetConnectionString("DefaultConnection");

        // Open a direct connection to PostgreSQL and create the schema
        using (var connection = new NpgsqlConnection(baseConnString))
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            
            // Create the schema if it doesn't already exist
            command.CommandText = $"CREATE SCHEMA IF NOT EXISTS \"{tenantId}\";";
            await command.ExecuteNonQueryAsync();
            
            _logger.LogInformation("Successfully created PostgreSQL schema: {TenantId}", tenantId);
        }

        // Set the temporary override in ITenantService so TabulaContext 
        // knows which schema to target for the migration command.
        _tenantService.SetTenantId(tenantId);

        try
        {
            // Apply EF Core migrations to materialize all tables inside the new schema
            await _context.Database.MigrateAsync();
            _logger.LogInformation("Successfully applied EF Core migrations for tenant schema: {TenantId}", tenantId);

            await _context.Database.OpenConnectionAsync();


            // Set the search path to the new schema for the current connection
            #pragma warning disable EF1002 // Input sanitized in line 44
            await _context.Database.ExecuteSqlRawAsync(
                $"SET search_path TO \"{tenantId}\", public;");
            #pragma warning restore EF1002

            await _authService.RegisterUserAsync(new RegisterDto
            {
                Email = model.AdminEmail,
                Password = model.AdminPassword,
                Name = model.AdminName,
                Role = Roles.Admin
            });

            var adminRole = await _roleManager.FindByNameAsync(Roles.Admin);
            if (adminRole != null)
            {
                foreach (var permission in Permissions.GetAllPermissions())
                {
                    // Check if the claim already exists to prevent duplicate key exceptions
                    var existingClaims = await _roleManager.GetClaimsAsync(adminRole);
                    if (!existingClaims.Any(c => c.Type == "permission" && c.Value == permission))
                    {
                        await _roleManager.AddClaimAsync(adminRole, new Claim("permission", permission));
                    }
                }
                _logger.LogInformation("Successfully assigned all system permissions to the Admin role for tenant: {TenantId}", tenantId);
            }

            var normalizedEmail = model.AdminEmail.Trim().ToLowerInvariant();

            var exists = await _context.TenantUserDirectories
                .AnyAsync(x => x.Email == normalizedEmail);

            if (!exists)
            {
                _context.TenantUserDirectories.Add(new TenantUserDirectory
                {
                    Email = normalizedEmail,
                    TenantId = tenantId
                });

                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply migrations for tenant schema: {TenantId}", tenantId);
            
            // If migrations fail halfway, drop the empty/corrupted schema
            try
            {
                using var cleanupConnection = new NpgsqlConnection(baseConnString);
                await cleanupConnection.OpenAsync();
                using var cleanupCommand = cleanupConnection.CreateCommand();
                
                // Drop schema and any subsequent objects (tables, sequences, etc.) that may have been partially created
                cleanupCommand.CommandText = $"DROP SCHEMA IF EXISTS \"{tenantId}\" CASCADE;";
                await cleanupCommand.ExecuteNonQueryAsync();
                
                _logger.LogInformation("Successfully cleaned up orphaned schema: {TenantId}", tenantId);

                
            }
            catch (Exception cleanupEx)
            {
                _logger.LogCritical(cleanupEx, "CRITICAL: Failed to clean up orphaned schema {TenantId}", tenantId);
            }

            throw; // Rethrow the original migration exception
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }

    public async Task DeleteTenantAsync(string tenantId)
    {
        var sanitizedTenantId = tenantId?.Trim().ToLowerInvariant();

        // Validate format to prevent SQL injection
        if (string.IsNullOrWhiteSpace(sanitizedTenantId) || !ValidTenantRegex.IsMatch(sanitizedTenantId))
        {
            throw new InvalidOperationException("Invalid tenant identifier format.");
        }

        // Protect reserved system schemas
        if (sanitizedTenantId == "public" || sanitizedTenantId == "dbo" || sanitizedTenantId == "postgres")
        {
            throw new InvalidOperationException("Cannot delete reserved system schemas.");
        }

        // Find directory entries in the public schema
        var tenantEntries = await _adminContext.TenantUserDirectories
            .Where(x => x.TenantId == sanitizedTenantId)
            .ToListAsync();

        if (!tenantEntries.Any())
        {
            throw new KeyNotFoundException($"Tenant '{sanitizedTenantId}' not found.");
        }

        // Perform deletion inside a transaction
        using var transaction = await _adminContext.Database.BeginTransactionAsync();
        try
        {
            // Remove the user directory mappings from public schema
            _adminContext.TenantUserDirectories.RemoveRange(tenantEntries);
            await _adminContext.SaveChangesAsync();

            // Drop the actual PostgreSQL schema and all tables/relations inside it
            var dropSchemaSql = $"DROP SCHEMA IF EXISTS \"{sanitizedTenantId}\" CASCADE;";
            await _adminContext.Database.ExecuteSqlRawAsync(dropSchemaSql);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
