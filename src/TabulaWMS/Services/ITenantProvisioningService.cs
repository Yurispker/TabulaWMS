namespace TabulaWMS.Services;

public interface ITenantProvisioningService
{
    Task ProvisionTenantAsync(string tenantId);
}