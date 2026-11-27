using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TabulaWMS.Data;

namespace TabulaWMS.Services;

public sealed class TenantMigrationRunner
{
    private static readonly Regex ValidTenantRegex = new("^[a-z0-9_]+$", RegexOptions.Compiled);

    private readonly AdminDbContext _adminContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TenantMigrationRunner> _logger;

    public TenantMigrationRunner(
        AdminDbContext adminContext,
        IHttpContextAccessor httpContextAccessor,
        IServiceScopeFactory scopeFactory,
        ILogger<TenantMigrationRunner> logger)
    {
        _adminContext = adminContext;
        _httpContextAccessor = httpContextAccessor;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var tenantIds = await _adminContext.TenantUserDirectories
            .AsNoTracking()
            .Select(directory => directory.TenantId)
            .Distinct()
            .OrderBy(tenantId => tenantId)
            .ToListAsync(cancellationToken);

        if (tenantIds.Count == 0)
        {
            _logger.LogInformation("No tenant schemas were found to migrate.");
            return 0;
        }

        var existingSchemas = await _adminContext.Database
            .SqlQueryRaw<string>("SELECT nspname AS \"Value\" FROM pg_namespace")
            .ToListAsync(cancellationToken);
        var existingSchemaSet = existingSchemas.ToHashSet(StringComparer.Ordinal);
        var failedTenants = new List<string>();

        foreach (var tenantId in tenantIds)
        {
            if (tenantId.Length > 63 || !ValidTenantRegex.IsMatch(tenantId))
            {
                _logger.LogError("Skipping invalid tenant schema identifier {TenantId}.", tenantId);
                failedTenants.Add(tenantId);
                continue;
            }

            if (!existingSchemaSet.Contains(tenantId))
            {
                _logger.LogError("Tenant schema {TenantId} does not exist.", tenantId);
                failedTenants.Add(tenantId);
                continue;
            }

            try
            {
                var context = new DefaultHttpContext();
                context.User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[] { new Claim("tenant_id", tenantId) },
                        authenticationType: "TenantMigrationRunner"));
                _httpContextAccessor.HttpContext = context;

                using var scope = _scopeFactory.CreateScope();
                var tenantContext = scope.ServiceProvider.GetRequiredService<TabulaContext>();
                await tenantContext.Database.MigrateAsync(cancellationToken);

                _logger.LogInformation("Applied pending migrations for tenant schema {TenantId}.", tenantId);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate tenant schema {TenantId}.", tenantId);
                failedTenants.Add(tenantId);
            }
            finally
            {
                _httpContextAccessor.HttpContext = null;
            }
        }

        if (failedTenants.Count == 0)
        {
            _logger.LogInformation("Finished migrating all {TenantCount} tenant schemas.", tenantIds.Count);
            return 0;
        }

        _logger.LogError(
            "Tenant migration failed for {FailedTenantCount} of {TenantCount} schemas: {TenantIds}",
            failedTenants.Count,
            tenantIds.Count,
            string.Join(", ", failedTenants));
        return 1;
    }
}
