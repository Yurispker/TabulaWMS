using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using TabulaWMS.Models;

namespace TabulaWMS.Services;

public class RoleService
{
    private readonly RoleManager<IdentityRole> _roleManager;

    public RoleService(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }

    // Creates a new base role (e.g., "Supervisor").
    public async Task<(bool Succeeded, string Message, IEnumerable<string> Errors)> CreateRoleAsync(string roleName)
    {
        if (await _roleManager.RoleExistsAsync(roleName))
        {
            return (false, string.Empty, new[] { $"Role {roleName} already exists."});
        }

        var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
        if (!result.Succeeded)
        {
            return (false, string.Empty, result.Errors.Select(e => e.Description));
        }

        return (true, $"Role {roleName} successfully created!", Enumerable.Empty<string>());
    }

    // Grants a permission to an existing role.
    public async Task<(bool Succeeded, string Message, IEnumerable<string> Errors)> AddPermissionToRoleAsync(string roleName, string permission)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            return (false, string.Empty, new[] { $"Role {roleName} does not exist."});
        }

        // Validate that the permission is a legitimate one from our Constants list
        if (!Constants.Permissions.GetAllPermissions().Contains(permission))
        {
            return (false, string.Empty, new[] { $"Permission {permission} is not valid." });
        }

        // Check if the role already has this permission to prevent duplicate claims
        var existingClaims = await _roleManager.GetClaimsAsync(role);
        if (existingClaims.Any(c => c.Type == "permission" && c.Value == permission))
        {
            return (false, string.Empty, new[] { $"Role already has permission {permission}."});
        }

        var result = await _roleManager.AddClaimAsync(role, new Claim("permission", permission));
        if (!result.Succeeded)
        {
            return (false, string.Empty, result.Errors.Select(e => e.Description));
        }

        return (true, $"Permission {permission} successfully added to role {roleName}.", Enumerable.Empty<string>());
    }


    // Revokes a permission from a role.
    public async Task<(bool Succeeded, string Message, IEnumerable<string> Errors)> RemovePermissionFromRoleAsync(string roleName, string permission)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            return (false, string.Empty, new[] { $"Role {roleName} does not exist." });
        }

        var existingClaims = await _roleManager.GetClaimsAsync(role);
        var claimToRemove = existingClaims.FirstOrDefault(c => c.Type == "permission" && c.Value == permission);

        if (claimToRemove == null)
        {
            return (false, string.Empty, new[] { $"Role does not have permission {permission}." });
        }

        var result = await _roleManager.RemoveClaimAsync(role, claimToRemove);
        if (!result.Succeeded)
        {
            return (false, string.Empty, result.Errors.Select(e => e.Description));
        }

        return (true, $"Permission {permission} successfully removed from role {roleName}.", Enumerable.Empty<string>());
    }

    // Returns a list of all permissions currently assigned to a role.
    public async Task<IEnumerable<string>> GetRolePermissionsAsync(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null) return Enumerable.Empty<string>();

        var claims = await _roleManager.GetClaimsAsync(role);
        // Filter only claims of type "permission"
        return claims.Where(c => c.Type == "permission").Select(c => c.Value);
    }
}