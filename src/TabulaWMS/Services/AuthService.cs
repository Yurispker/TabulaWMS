using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TabulaWMS.DTOs;
using TabulaWMS.Models;
using TabulaWMS.Constants;

namespace TabulaWMS.Services;


public class AuthService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AuthService(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<User> RegisterUserAsync(RegisterDto model, string? callerEmail = null)
    {
        if (string.IsNullOrWhiteSpace(model.Role))
        {
            model.Role = Roles.None;
        }

        var isFirstUser = !await _userManager.Users.AnyAsync();

        if (isFirstUser)
        {
            model.Role = Roles.Admin;
            model.CreatedBy = "System";

            if (!await _roleManager.RoleExistsAsync(Roles.Admin))
            {
                await _roleManager.CreateAsync(new IdentityRole(Roles.Admin));
                await _roleManager.CreateAsync(new IdentityRole(Roles.None));
            }
        }
        else
        {
            if (string.IsNullOrEmpty(callerEmail))
            {
                throw new UnauthorizedAccessException("Authentication required to register new users.");
            }

            model.CreatedBy = callerEmail;
        }

        if (!isFirstUser && model.Role == Roles.Admin)
        {
            throw new InvalidOperationException("The Admin role can only be assigned to the first registered user.");
        }

        var isValidRole = model.Role == Roles.Admin
            || model.Role == Roles.None
            || await _roleManager.RoleExistsAsync(model.Role);

        if (!isValidRole)
        {
            throw new InvalidOperationException($"Invalid role: {model.Role}");
        }

        var user = new User
        {
            UserName = model.Email,
            Email = model.Email,
            Name = model.Name,
            Role = model.Role,
            CreatedBy = model.CreatedBy
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"User creation failed: {errors}");
        }

        await _userManager.AddToRoleAsync(user, model.Role);
        return user;
    }
}