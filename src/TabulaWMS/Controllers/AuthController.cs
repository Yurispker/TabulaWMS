using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TabulaWMS.DTOs;
using TabulaWMS.Models;
using TabulaWMS.Services;
using TabulaWMS.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

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

    // --------------------------------------------------------------------------------------------------------------
    // Registration Endpoint
    // --------------------------------------------------------------------------------------------------------------

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {

        // Set the role to None if none has been indicated
        if (string.IsNullOrWhiteSpace(model.Role))
        {
            model.Role = Roles.None;
        }

        // Check if this is the first user being registered
        var isFirstUser = !await _userManager.Users.AnyAsync();

        if (isFirstUser)
        {
            // Force the first user to be an Admin
            model.Role = Roles.Admin;
            model.CreatedBy = "System";

            // Ensure the the default role records exist in the database
            if (!await _roleManager.RoleExistsAsync(Roles.Admin))
            {
                await _roleManager.CreateAsync(new IdentityRole(Roles.Admin));
                await _roleManager.CreateAsync(new IdentityRole(Roles.None));
            }
        }

        // Check if user IS logged in and inject the current user's email as the CreatedBy field
        else
        {
            var currentUserEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(currentUserEmail))
            {
                return Unauthorized(new { message = "Authentication required to register new users." });
            }

            model.CreatedBy = currentUserEmail;
        }

        if (!isFirstUser && model.Role == Roles.Admin)
        {
            // Prevent any subsequent users from being assigned the Admin role
            return BadRequest("The Admin role can only be assigned to the first registered user.");
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
            UserName = model.Email, // Username is the e-mail address;
            Email = model.Email,
            Name = model.Name,
            Role = model.Role,
            CreatedBy = model.CreatedBy // Record who created this account;
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

    // --------------------------------------------------------------------------------------------------------------
    // Login Endpoint
    // --------------------------------------------------------------------------------------------------------------

    [HttpPost("login")]
    [EnableRateLimiting("login-limiter")] // Apply rate limiting to the login endpoint
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

        // If the user is locked out, return an error message
        if (result.IsLockedOut)
        {
            return Unauthorized("This account has been locked out due to multiple failed login attempts. Please try again in 15 minutes.");
        }

        // Wrong credentials, but not locked out yet
        else if (!result.Succeeded)
        {
            return Unauthorized("Invalid email or password.");
        }

        // Generate JWT token
        var jwtToken = _tokenService.CreateToken(user);

        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        // Save refresh token to database
        await _userManager.UpdateAsync(user);

        // Return success
        return Ok(new
        {
            message = "Login successful!",
            token = jwtToken,
            refreshToken = refreshToken,
            email = user.Email,
            name = user.Name,
            role = user.Role
        });
    }

    // --------------------------------------------------------------------------------------------------------------
    // Refresh Endpoint
    // --------------------------------------------------------------------------------------------------------------

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] TokenRequestDto model)
    {
        if (model == null || string.IsNullOrEmpty(model.AccessToken) || string.IsNullOrEmpty(model.RefreshToken))
        {
            return BadRequest("Invalid client request");
        }

        // Extract principal from expired access token (ignoring lifetime check)
        var principal = _tokenService.GetPrincipalFromExpiredToken(model.AccessToken);
        if (principal == null)
        {
            return Unauthorized(new { message = "Invalid access token" });
        }


        var email = principal.FindFirstValue(ClaimTypes.Email);
        var user = await _userManager.FindByEmailAsync(email!);

        if (user == null || user.RefreshToken != model.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }

        // Generate new tokens (Rotation)
        var newAccessToken = _tokenService.CreateToken(user);
        var newRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        return Ok(new
        {
            token = newAccessToken,
            refreshToken = newRefreshToken
        });
    }
}