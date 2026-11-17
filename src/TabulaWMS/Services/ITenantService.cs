namespace TabulaWMS.Services;

public interface ITenantService
{
    string? GetTenantId();
    void SetTenantId(string tenantId);
}