using Microsoft.AspNetCore.Http;

namespace TabulaWMS.Services;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetTenantId()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        // Check if the incoming request includes custom tenant header
        if (user == null)
        {
            return null;
        }

        var tenantIdClaim = user.FindFirst("tenant_id")?.Value;

        return tenantIdClaim;
    }
}