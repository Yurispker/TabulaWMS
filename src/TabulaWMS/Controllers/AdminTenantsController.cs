using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TabulaWMS.Models.DTOs;
using TabulaWMS.Services;

namespace TabulaWMS.Controllers;

[Authorize(Roles = "SuperAdmin")]
[ApiController]
[Route("api/admin/tenants")]
public class AdminTenantsController : ControllerBase
{
    private readonly ITenantProvisioningService _provisioningService;
    private readonly ILogger<AdminTenantsController> _logger;

    public AdminTenantsController(
        ITenantProvisioningService provisioningService,
        ILogger<AdminTenantsController> logger)
    {
        _provisioningService = provisioningService;
        _logger = logger;
    }

    [HttpPost("provision")]
    public async Task<IActionResult> ProvisionTenant([FromBody] CreateTenantDto model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.TenantId))
        {
            return BadRequest(new { Error = "Tenant identifier is required." });
        }

        try
        {
            _logger.LogInformation("Admin requested provisioning for tenant: {TenantId}", model.TenantId);
            
            // Calls the TenantProvisioningService to create the schema and apply migrations
            await _provisioningService.ProvisionTenantAsync(model.TenantId);

            return Ok(new { 
                Message = $"Tenant schema '{model.TenantId}' was successfully created and migrated.",
                TenantId = model.TenantId 
            });
        }
        catch (ArgumentException ex)
        {
            // Handles validation errors (e.g., invalid characters failing the regex check)
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to provision tenant schema: {TenantId}", model.TenantId);
            return StatusCode(500, new { Error = "An error occurred while provisioning the tenant. The operation was rolled back." });
        }
    }
}