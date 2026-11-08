using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TabulaWMS.Models;
using TabulaWMS.Constants;
using TabulaWMS.DTOs;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // Admins only
public class UsersController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsersController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // View all users (Admin only)
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userManager.Users
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                Email = u.Email!,
                Name = u.Name,
                Role = u.Role,
                CreatedBy = u.CreatedBy
            })
            .ToListAsync();

        return Ok(users);
    }

    // POST: api/users/{userId}/roles
    [HttpPost("{userId}/roles")]
    public async Task<IActionResult> AddRoleToUser(string userId, [FromBody] UpdateRoleDto model)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (!await _roleManager.RoleExistsAsync(model.Role))
        {
            return BadRequest(new { message = $"Role '{model.Role}' does not exist." });
        }

        if (await _userManager.IsInRoleAsync(user, model.Role))
        {
            return BadRequest(new { message = "User already has this role assigned." });
        }

        var result = await _userManager.AddToRoleAsync(user, model.Role);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        user.Role = model.Role;
        await _userManager.UpdateAsync(user);

        return Ok(new { message = $"Successfully assigned role '{model.Role}' to user {user.Email}." });
    }

    // DELETE endpoint to remove a role from a user
    [HttpDelete("{userId}/roles")]
    public async Task<IActionResult> RemoveRoleFromUser(string userId, [FromBody] UpdateRoleDto model)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (!await _userManager.IsInRoleAsync(user, model.Role))
        {
            return BadRequest(new { message = "User does not have this role." });
        }

        // Safety check: Prevent removing the Admin
        if (model.Role == Roles.Admin)
        {
            return BadRequest(new
            {
                message = "Critical error: Cannot remove the Administrator."
            });
        }

        var result = await _userManager.RemoveFromRoleAsync(user, model.Role);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        user.Role = Roles.None;
        await _userManager.UpdateAsync(user);

        return Ok(new { message = $"Successfully removed role '{model.Role}' from user {user.Email}." });
    }

    [HttpPost("unlock")]
    public async Task<IActionResult> UnlockUser([FromBody] UnlockUserDto model)
    {
        // 1. Find the target user by their human-readable email
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return NotFound(new { message = $"No user found with email '{model.Email}'." });
        }

        // 2. Check if the user is actually locked out
        bool isLockedOut = await _userManager.IsLockedOutAsync(user);
        if (!isLockedOut)
        {
            return BadRequest(new { message = $"User '{model.Email}' is not currently locked out." });
        }

        // 3. Clear the lockout end date
        var setLockoutResult = await _userManager.SetLockoutEndDateAsync(user, null);
        if (!setLockoutResult.Succeeded)
        {
            return BadRequest(setLockoutResult.Errors);
        }

        // 4. Reset the failed access attempt counter back to 0
        var resetResult = await _userManager.ResetAccessFailedCountAsync(user);
        if (!resetResult.Succeeded)
        {
            return BadRequest(resetResult.Errors);
        }

        return Ok(new { message = $"Account for '{user.Email}' has been successfully unlocked." });
    }
}