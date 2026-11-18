using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TabulaWMS.DTOs;
using TabulaWMS.Models;
using TabulaWMS.Services;
using TabulaWMS.Constants;
using TabulaWMS.Data;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;

namespace TabulaWMS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly TokenService _tokenService;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AuthService _authService;
    private readonly ITenantService _tenantService;
    private readonly TabulaContext _context;
    private readonly AdminDbContext _adminDbContext;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        TokenService tokenService,
        RoleManager<IdentityRole> roleManager,
        AuthService authService,
        ITenantService tenantService,
        TabulaContext context,
        AdminDbContext adminDbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _roleManager = roleManager;
        _authService = authService;
        _tenantService = tenantService;
        _context = context;
        _adminDbContext = adminDbContext;
    }

    // --------------------------------------------------------------------------------------------------------------
    // Registration Endpoint
    // --------------------------------------------------------------------------------------------------------------

    [HttpPost("register")]
    [Authorize(Policy = Permissions.ManageAccounts)]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        try
        {
            var callerEmail = User.FindFirstValue(ClaimTypes.Email);
            var user = await _authService.RegisterUserAsync(model, callerEmail);
            return Ok(new { message = "User registered successfully!", roleAssigned = user.Role });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // --------------------------------------------------------------------------------------------------------------
    // Login Endpoint
    // --------------------------------------------------------------------------------------------------------------

    [HttpPost("login")]
    [EnableRateLimiting("login-limiter")] // Apply rate limiting to the login endpoint
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {

        var directoryEntry = await _adminDbContext.TenantUserDirectories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == model.Email.Trim().ToLowerInvariant());

        // If not in TenantUserDirectory, default to public schema
        var targetTenant = directoryEntry?.TenantId ?? "public";
        // Switch active tenant schema before UserManager runs
        _tenantService.SetTenantId(targetTenant);


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
        var jwtToken = await _tokenService.CreateTokenAsync(user);

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

        var tenantId = principal.FindFirstValue("tenant_id");
        var email = principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(tenantId)
            || string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized(new { message = "Invalid access token" });
        }

        _tenantService.SetTenantId(tenantId);
        var user = await _userManager.FindByEmailAsync(email!);

        if (user == null || user.RefreshToken != model.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }

        // Generate new tokens (Rotation)
        var newAccessToken = await _tokenService.CreateTokenAsync(user);
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