using Microsoft.AspNetCore.Http;
using System.Security.Claims;

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

    public void SetTenantId(string tenantId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return;
        }

        var identity = httpContext.User.Identity as ClaimsIdentity;
        if (identity == null)
        {
            if (httpContext.User == null || httpContext.User.Identity == null)
            {
                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            }

            identity = httpContext.User.Identity as ClaimsIdentity;
            if (identity == null)
            {
                return;
            }
        }

        var existingClaim = identity.FindFirst("tenant_id");
        if (existingClaim != null)
        {
            identity.RemoveClaim(existingClaim);
        }

        identity.AddClaim(new Claim("tenant_id", tenantId));
    }
}