using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TabulaWMS.DTOs;
using TabulaWMS.Models;
using TabulaWMS.Services;
using TabulaWMS.Constants;

namespace TabulaWMS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly TokenService _tokenService;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        TokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    // Registration Endpoint
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {

        // Validate the role against our predefined roles
        if (!Roles.All.Contains(model.Role))
        {
            return BadRequest($"Invalid role. Allowed roles are: {string.Join(", ", Roles.All)}");
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

        return Ok(new { message = "User registered successfully!" });
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