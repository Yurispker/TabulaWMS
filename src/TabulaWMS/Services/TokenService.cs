using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using TabulaWMS.Models;

namespace TabulaWMS.Services;

public class TokenService
{
    private readonly IConfiguration _config;
    private readonly SymmetricSecurityKey _key;
    private readonly ITenantService _tenantService;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public TokenService(
        IConfiguration config,
        ITenantService tenantService, 
        UserManager<User> userManager, 
        RoleManager<IdentityRole> roleManager)
    {
        _config = config;
        _tenantService = tenantService;
        _userManager = userManager;
        _roleManager = roleManager;

        // Grab the secret key
        var secretKey = _config["JwtSettings:Key"];

        // Secret key enryption
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
    }

    public async Task<string> CreateTokenAsync(User user)
    {

        var tenantId = _tenantService.GetTenantId();

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            tenantId = "public"; // Default to public schema if no tenant is set
        }

        // Define user data (claims) to be embedded in the JWT token
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Name, user.Name ?? string.Empty),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("tenant_id", tenantId)
        };

        // Fetch the user's role from Identity and embed its permission claims into the token
        var role = await _roleManager.FindByNameAsync(user.Role);
        if (role != null)
        {
            var roleClaims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in roleClaims)
            {
                if (claim.Type == "permission")
                {
                    claims.Add(new Claim("permission", claim.Value));
                }
            }
        }

        // Describe the token properties
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(10), // Token expires in 20 minutes
            Issuer = _config["JwtSettings:Issuer"],
            Audience = _config["JwtSettings:Audience"],
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature)
        };

        // Generate the token
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        // Return token as string
        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = _config["JwtSettings:Audience"],
            ValidateIssuer = true,
            ValidIssuer = _config["JwtSettings:Issuer"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key, // Reuses existing private _key field
            ValidateLifetime = false, // Allows validation of expired tokens
            ClockSkew = TimeSpan.Zero
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha512, StringComparison.InvariantCultureIgnoreCase))
        {
            return null;
        }

        return principal;
    }
}