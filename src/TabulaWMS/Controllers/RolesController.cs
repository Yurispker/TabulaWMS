using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TabulaWMS.Models;
using TabulaWMS.Services;
using TabulaWMS.Constants;

namespace TabulaWMS.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize(Roles = Constants.Roles.Admin)] // We can lock this down to Admins later!
public class RolesController : ControllerBase
{
    private readonly RoleService _roleService;

    public RolesController(RoleService roleService)
    {
        _roleService = roleService;
    }

    // DTO records for handling incoming JSON request bodies
    public record CreateRoleRequest(string RoleName);
    public record PermissionRequest(string Permission);

    // Creates a new custom role.
    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var result = await _roleService.CreateRoleAsync(request.RoleName);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(new { message = result.Message });
    }


    // Adds a permission claim to a role
    [HttpPost("{roleName}/permissions")]
    public async Task<IActionResult> AddPermissionToRole(string roleName, [FromBody] PermissionRequest request)
    {
        var result = await _roleService.AddPermissionToRoleAsync(roleName, request.Permission);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(new { message = result.Message });
    }


    // Removes a permission claim from a role.
    [HttpDelete("{roleName}/permissions")]
    public async Task<IActionResult> RemovePermissionFromRole(string roleName, [FromBody] PermissionRequest request)
    {
        var result = await _roleService.RemovePermissionFromRoleAsync(roleName, request.Permission);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(new { message = result.Message });
    }


    // Gets all permissions currently assigned to a role.
    [HttpGet("{roleName}/permissions")]
    public async Task<IActionResult> GetRolePermissions(string roleName)
    {
        var permissions = await _roleService.GetRolePermissionsAsync(roleName);
        return Ok(permissions);
    }


    // Helper endpoint to view all valid system permissions.
    [HttpGet("available-permissions")]
    public IActionResult GetAvailablePermissions()
    {
        var permissions = Permissions.GetAllPermissions();
        return Ok(permissions);
    }
}