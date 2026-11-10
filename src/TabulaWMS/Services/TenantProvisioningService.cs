using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TabulaWMS.Data;

namespace TabulaWMS.Services;

public class TenantProvisioningService : ITenantProvisioningService
{
    private readonly IConfiguration _configuration;
    private readonly ITenantService _tenantService;
    private readonly TabulaContext _context;
    private readonly ILogger<TenantProvisioningService> _logger;

    public TenantProvisioningService(
        IConfiguration configuration,
        ITenantService tenantService,
        TabulaContext context,
        ILogger<TenantProvisioningService> logger)
    {
        _configuration = configuration;
        _tenantService = tenantService;
        _context = context;
        _logger = logger;
    }

    public async Task ProvisionTenantAsync(string tenantId)
    {
        // Sanitize and validate tenantId to prevent SQL injection.
        if (string.IsNullOrWhiteSpace(tenantId) || !Regex.IsMatch(tenantId, "^[a-z0-9_]+$"))
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply migrations for tenant schema: {TenantId}", tenantId);
            
            // RESILIENCY / CLEANUP: If migrations fail halfway, drop the empty/corrupted schema
            try
            {
                using var cleanupConnection = new NpgsqlConnection(baseConnString);
                await cleanupConnection.OpenAsync();
                using var cleanupCommand = cleanupConnection.CreateCommand();
                
                // CASCADE ensures any partial tables dropped safely
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
    }
}
