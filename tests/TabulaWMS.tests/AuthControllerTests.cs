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
using TabulaWMS.Constants;

namespace TabulaWMS.Tests;

public class AuthControllerTests
{
    private static (
        Mock<ITenantService> tenantService,
        Mock<UserManager<User>> userManagerMock,
        Mock<RoleManager<IdentityRole>> roleManagerMock,
        TokenService tokenService,
        User user,
        string accessToken,
        string refreshToken
    ) CreateSetupForRefreshTest(string tenantId = "tenant_refresh_test")
    {
        string? activeTenantId = tenantId;

        // Mock Tenant Service
        var tenantService = new Mock<ITenantService>();
        tenantService.Setup(service => service.GetTenantId()).Returns(() => activeTenantId);
        tenantService.Setup(service => service.SetTenantId(It.IsAny<string>()))
            .Callback<string>(value => activeTenantId = value);

        // Mock Configuration for TokenService
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Issuer"] = "test-issuer",
                ["JwtSettings:Audience"] = "test-audience"
            })
            .Build();

        // Mock Identity Managers for Role & Permission injection
        var userStoreMock = new Mock<IUserStore<User>>();
        var userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<User>(),
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<User>>.Instance);

        var expectedPermissions = new List<Claim>
        {
            new Claim("permission", Permissions.ManageRoles),
            new Claim("permission", Permissions.ViewProducts)
        };

        var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
        roleStoreMock.As<IRoleClaimStore<IdentityRole>>()
            .Setup(s => s.GetClaimsAsync(It.IsAny<IdentityRole>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPermissions);

        var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            roleStoreMock.Object,
            Array.Empty<IRoleValidator<IdentityRole>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            NullLogger<RoleManager<IdentityRole>>.Instance);

        var refreshToken = "refresh-token";
        var user = new User
        {
            Email = "admin@example.com",
            Name = "Tenant Admin",
            Role = "Admin",
            RefreshToken = refreshToken,
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
        };

        userManagerMock.Setup(um => um.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Admin" });

        var identityRole = new IdentityRole { Name = "Admin" };
        roleManagerMock.Setup(rm => rm.FindByNameAsync("Admin"))
            .ReturnsAsync(identityRole);

        roleManagerMock.Setup(rm => rm.GetClaimsAsync(identityRole))
            .ReturnsAsync(expectedPermissions);

        var tokenService = new TokenService(
            configuration,
            tenantService.Object,
            userManagerMock.Object,
            roleManagerMock.Object
        );

        // Simulate initial token generation
        var accessToken = Task.Run(() => tokenService.CreateTokenAsync(user)).GetAwaiter().GetResult();

        // Prepare for refresh execution setup
        activeTenantId = null; // Reset tenant to test restoration during refresh

        userManagerMock.Setup(manager => manager.FindByEmailAsync(user.Email))
            .Callback<string>(_ => Assert.Equal(tenantId, activeTenantId))
            .ReturnsAsync(user);
        userManagerMock.Setup(manager => manager.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        return (tenantService, userManagerMock, roleManagerMock, tokenService, user, accessToken, refreshToken);
    }

    [Fact]
    public async Task Refresh_RestoresTenantFromAccessTokenBeforeLoadingUser()
    {
        // Arrange
        const string tenantId = "tenant_refresh_test";
        var setup = CreateSetupForRefreshTest(tenantId);
        
        var controller = new AuthController(
            setup.userManagerMock.Object,
            null!,
            setup.tokenService,
            null!,
            null!,
            setup.tenantService.Object,
            null!,
            null!);

        // Act
        var result = await controller.Refresh(new TokenRequestDto
        {
            AccessToken = setup.accessToken,
            RefreshToken = setup.refreshToken
        });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var refreshedToken = (string)okResult.Value!.GetType()
            .GetProperty("token")!
            .GetValue(okResult.Value)!;
        
        var refreshedJwt = new JwtSecurityTokenHandler().ReadJwtToken(refreshedToken);
        Assert.Equal(tenantId, refreshedJwt.Claims.Single(claim => claim.Type == "tenant_id").Value);
    }

    [Fact]
    public async Task Refresh_EmbedsUserRoleIntoRefreshedToken()
    {
        // Arrange
        var setup = CreateSetupForRefreshTest();

        var controller = new AuthController(
            setup.userManagerMock.Object,
            null!,
            setup.tokenService,
            null!,
            null!,
            setup.tenantService.Object,
            null!,
            null!);

        // Act
        var result = await controller.Refresh(new TokenRequestDto
        {
            AccessToken = setup.accessToken,
            RefreshToken = setup.refreshToken
        });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var refreshedToken = (string)okResult.Value!.GetType()
            .GetProperty("token")!
            .GetValue(okResult.Value)!;
        
        var refreshedJwt = new JwtSecurityTokenHandler().ReadJwtToken(refreshedToken);
        Assert.Contains(refreshedJwt.Claims, c => c.Type == "role" && c.Value == "Admin");
    }

    [Fact]
    public async Task Refresh_EmbedsPermissionClaimsIntoRefreshedToken()
    {
        // Arrange
        var setup = CreateSetupForRefreshTest();

        var controller = new AuthController(
            setup.userManagerMock.Object,
            null!,
            setup.tokenService,
            null!,
            null!,
            setup.tenantService.Object,
            null!,
            null!);

        // Act
        var result = await controller.Refresh(new TokenRequestDto
        {
            AccessToken = setup.accessToken,
            RefreshToken = setup.refreshToken
        });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var refreshedToken = (string)okResult.Value!.GetType()
            .GetProperty("token")!
            .GetValue(okResult.Value)!;
        
        var refreshedJwt = new JwtSecurityTokenHandler().ReadJwtToken(refreshedToken);
        var refreshedPermissions = refreshedJwt.Claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .ToList();

        Assert.Contains(Permissions.ManageRoles, refreshedPermissions);
        Assert.Contains(Permissions.ViewProducts, refreshedPermissions);
    }
}