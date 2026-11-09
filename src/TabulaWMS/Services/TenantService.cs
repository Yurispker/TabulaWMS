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
        var httpContext = _httpContextAccessor.HttpContext;

        // Check if the incoming request includes custom tenant header
        if (httpContext != null && httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader))
        {
            return tenantHeader.ToString();
        }

        return null;
    }
}