using System.Security.Claims;
using System.Text.RegularExpressions;
using TabulaWMS.Services;

namespace TabulaWMS.Middleware;

public class TenantMiddleware
{
    private static readonly Regex ValidTenantRegex = new("^[a-z0-9_]+$", RegexOptions.Compiled);
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantId = context.User.FindFirstValue("tenant_id");

            if (string.IsNullOrWhiteSpace(tenantId) || !ValidTenantRegex.IsMatch(tenantId))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            tenantService.SetTenantId(tenantId);
        }

        await _next(context);
    }
}