using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TabulaWMS.Data;
using TabulaWMS.Models;
using TabulaWMS.Services;

public class RoleServiceTests
{
    private RoleService GetInMemoryRoleService(out TabulaContext context)
    {
        // Set up a fresh In-Memory database
        var options = new DbContextOptionsBuilder<TabulaContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        context = new TabulaContext(options);

        // Boilerplate setup required to instantiate a real RoleManager in memory
        var store = new RoleStore<IdentityRole>(context);
        var roleValidators = new List<IRoleValidator<IdentityRole>> { new RoleValidator<IdentityRole>() };
        var normalizer = new UpperInvariantLookupNormalizer();
        var tokenProvider = new Mock<IdentityErrorDescriber>();
        var logger = new Mock<ILogger<RoleManager<IdentityRole>>>();

        var roleManager = new RoleManager<IdentityRole>(
            store, 
            roleValidators, 
            normalizer, 
            tokenProvider.Object, 
            logger.Object);

        return new RoleService(roleManager);
    }

    [Fact]
    public async Task CreateRoleAsync_WithValidName_ShouldSucceed()
    {
        // Arrange
        var service = GetInMemoryRoleService(out var context);
        string roleName = "InventoryAuditor";

        // Act
        var result = await service.CreateRoleAsync(roleName);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal($"Role '{roleName}' successfully created!", result.Message);
        Assert.Empty(result.Errors);

        // Verify it actually exists in the in-memory database
        var roleExists = await context.Roles.AnyAsync(r => r.Name == roleName);
        Assert.True(roleExists);
    }

    [Fact]
    public async Task CreateRoleAsync_WithDuplicateName_ShouldFail()
    {
        // Arrange
        var service = GetInMemoryRoleService(out var context);
        string roleName = "WarehouseManager";

        // Create it once successfully
        await service.CreateRoleAsync(roleName);

        // Act - Try creating it again
        var result = await service.CreateRoleAsync(roleName);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);
        Assert.Contains($"Role '{roleName}' already exists.", result.Errors);
    }

    [Fact]
    public async Task AddPermissionToRoleAsync_WithValidPermission_ShouldSucceed()
    {
        // Arrange
        var service = GetInMemoryRoleService(out var context);
        string roleName = "Picker";
        string validPermission = Constants.Permissions.Products.Create; // e.g., "products:create"

        await service.CreateRoleAsync(roleName);

        // Act
        var result = await service.AddPermissionToRoleAsync(roleName, validPermission);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify claim was actually assigned
        var permissions = await service.GetRolePermissionsAsync(roleName);
        Assert.Contains(validPermission, permissions);
    }

    [Fact]
    public async Task AddPermissionToRoleAsync_WithInvalidPermission_ShouldFail()
    {
        // Arrange
        var service = GetInMemoryRoleService(out var context);
        string roleName = "Picker";
        string invalidPermission = "fake:permission:string";

        await service.CreateRoleAsync(roleName);

        // Act
        var result = await service.AddPermissionToRoleAsync(roleName, invalidPermission);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains($"Permission '{invalidPermission}' is not valid.", result.Errors);
    }
}