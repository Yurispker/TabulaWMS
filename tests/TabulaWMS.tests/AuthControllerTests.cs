using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TabulaWMS.Controllers;
using TabulaWMS.DTOs;
using TabulaWMS.Models;
using TabulaWMS.Services;

namespace TabulaWMS.Tests;

public class AuthControllerTests
{
    [Fact]
    public async Task Refresh_RestoresTenantFromAccessTokenBeforeLoadingUser()
    {
        const string tenantId = "tenant_refresh_test";
        const string refreshToken = "refresh-token";
        string? activeTenantId = tenantId;

        var tenantService = new Mock<ITenantService>();
        tenantService.Setup(service => service.GetTenantId()).Returns(() => activeTenantId);
        tenantService.Setup(service => service.SetTenantId(It.IsAny<string>()))
            .Callback<string>(value => activeTenantId = value);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Key"] = new string('k', 64),
                ["JwtSettings:Issuer"] = "test-issuer",
                ["JwtSettings:Audience"] = "test-audience"
            })
            .Build();
        var tokenService = new TokenService(configuration, tenantService.Object);
        var user = new User
        {
            Email = "admin@example.com",
            Name = "Tenant Admin",
            Role = "Admin",
            RefreshToken = refreshToken,
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
        };
        var accessToken = tokenService.CreateToken(user);
        var principal = tokenService.GetPrincipalFromExpiredToken(accessToken);
        Assert.True(
            principal is not null,
            $"Refresh token validation rejected JWT algorithm '{new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Header.Alg}'.");
        Assert.Equal(tenantId, principal.FindFirstValue("tenant_id"));
        Assert.Equal(user.Email, principal.FindFirstValue(ClaimTypes.Email));
        activeTenantId = null;

        var userStore = new Mock<IUserStore<User>>();
        var userManager = new Mock<UserManager<User>>(
            userStore.Object,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<User>(),
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<User>>.Instance);
        userManager.Setup(manager => manager.FindByEmailAsync(user.Email))
            .Callback<string>(_ => Assert.Equal(tenantId, activeTenantId))
            .ReturnsAsync(user);
        userManager.Setup(manager => manager.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var controller = new AuthController(
            userManager.Object,
            null!,
            tokenService,
            null!,
            null!,
            tenantService.Object,
            null!,
            null!);

        var result = await controller.Refresh(new TokenRequestDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        });

        var okResult = Assert.IsType<OkObjectResult>(result);
        var refreshedToken = (string)okResult.Value!.GetType()
            .GetProperty("token")!
            .GetValue(okResult.Value)!;
        var refreshedPrincipal = new JwtSecurityTokenHandler().ReadJwtToken(refreshedToken);

        Assert.Equal(tenantId, refreshedPrincipal.Claims.Single(claim => claim.Type == "tenant_id").Value);
    }
}
