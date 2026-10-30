using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TabulaWMS.Models;

namespace TabulaWMS.Services;

public class TokenService
{
    private readonly IConfiguration _config;
    private readonly SymmetricSecurityKey _key;

    public TokenService(IConfiguration config)
    {
        _config = config;

        // Grab the secret key
        var secretKey = _config["JwtSettings:Key"];

        // Secret key enryption
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
    }

    public string CreateToken(User user)
    {
        // Define user data (claims) to be embedded in the JWT token
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role)
        };

        // Create signing credentials using secret key
        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

        // Describe the token properties
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(30), // Token expires in 30 minutes
            Issuer = _config["JwtSettings:Issuer"],
            Audience = _config["JwtSettings:Audience"],
            SigningCredentials = creds
        };

        // Generate the token
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        // Return token as string
        return tokenHandler.WriteToken(token);
    }
}