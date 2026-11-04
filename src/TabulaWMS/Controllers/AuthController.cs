using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TabulaWMS.DTOs;
using TabulaWMS.Models;
using TabulaWMS.Services;
using TabulaWMS.Constants;
using Microsoft.EntityFrameworkCore;

namespace TabulaWMS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly TokenService _tokenService;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        TokenService tokenService,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _roleManager = roleManager;
    }

    // Registration Endpoint
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {

        // Check if this is the first user being registered
        var isFirstUser = !await _userManager.Users.AnyAsync();

        if (isFirstUser)
        {
            // Force the first user to be an Admin
            model.Role = Roles.Admin;

            // Ensure the "Admin" role record exists in the database
            if (!await _roleManager.RoleExistsAsync(Roles.Admin))
            {
                await _roleManager.CreateAsync(new IdentityRole(Roles.Admin));
            }
        }

        else if (string.IsNullOrWhiteSpace(model.Role))
        {
            // Default role for any users who didn't specify one
            model.Role = "None";
        }

        var isValidRole = model.Role == Roles.Admin || model.Role == Roles.None || await _roleManager.RoleExistsAsync(model.Role);

        if (!isValidRole)
        {
            // Fetch all database
            var dbRoles = await _roleManager.Roles
                .Select(r => r.Name)
                .ToListAsync();

            // Ensure Admin is included in the allowed list
            var allowedRoles = dbRoles.Contains(Roles.Admin)
                ? dbRoles
                : dbRoles.Append(Roles.Admin);

            return BadRequest($"Invalid role. Allowed roles are: {string.Join(", ", allowedRoles)}");
        }

        // Map incoming web DTO data to database User model
        var user = new User
        {
            UserName = model.Email, // ASP.NET Core Identity uses UserName as the unique login handle
            Email = model.Email,
            Name = model.Name,
            Role = model.Role,
            CreatedBy = "System" // For now
        };

        // Create user AND hash password
        var result = await _userManager.CreateAsync(user, model.Password);

        // If creation failed, return a 400 status code
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        await _userManager.AddToRoleAsync(user, model.Role);

        return Ok(new { message = "User registered successfully!", roleAssigned = model.Role });
    }

    // Login Endpoint
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        // Find user by email
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return Unauthorized("Invalid email or password.");
        }

        // Check if password matches
        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return Unauthorized("Invalid email or password.");
        }

        // Generate JWT token
        var jwtToken = _tokenService.CreateToken(user);

        // Return success
        return Ok(new
        {
            message = "Login successful!",
            token = jwtToken,
            email = user.Email,
            name = user.Name,
            role = user.Role
        });
    }
}