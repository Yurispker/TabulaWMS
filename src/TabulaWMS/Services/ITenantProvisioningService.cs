namespace TabulaWMS.Services;
using TabulaWMS.Models.DTOs;

public interface ITenantProvisioningService
{
    Task ProvisionTenantAsync(CreateTenantDto model);

    Task DeleteTenantAsync(string tenantId);
}