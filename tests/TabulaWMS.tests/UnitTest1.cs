using Microsoft.EntityFrameworkCore;
using Xunit;
using TabulaWMS.Controllers;
using TabulaWMS.Data;
using TabulaWMS.Models;
using Microsoft.AspNetCore.Mvc;

namespace TabulaWMS.tests;

public class InventoryControllerTests
{
    private TabulaContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TabulaContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

        var context = new TabulaContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task AddInventoryItem_ReturnsCreatedAtActionResult()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var controller = new InventoryController(context);
        var newItem = new InventoryItem
        {
            Name = "Test Item",
            Quantity = 3,
            SKU = "Test001",
            Location = "Warehouse Sector A"
        };

        // Act
        var result = await controller.CreateItem(newItem);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
        var returnValue = Assert.IsType<InventoryItem>(createdAtActionResult.Value);
        Assert.Equal(newItem.Name, returnValue.Name);
        Assert.Equal(newItem.Quantity, returnValue.Quantity);
        Assert.Equal(newItem.SKU, returnValue.SKU);
        Assert.Equal(newItem.Location, returnValue.Location);

        var dbItem = await context.InventoryItems.FirstOrDefaultAsync(i => i.SKU == "Test001");
        Assert.NotNull(dbItem);
        Assert.Equal(3, dbItem.Quantity);
    }

    [Fact]
    public async Task CreateItem_NegativeQuantity_ReturnsBadRequest()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var controller = new InventoryController(context);

        var invalidItem = new InventoryItem
        {
            Name = "Negative Widget",
            SKU = "NEG001",
            Quantity = -10,
            Location = "Warehouse B"
        };

        // Manually simulate model validation since we are unit testing the controller method directly
        controller.ModelState.AddModelError("Quantity", "Quantity cannot be negative.");

        // Act
        var result = await controller.CreateItem(invalidItem);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task CreateItem_DuplicateSku_ReturnsConflict()
    {
        // Arrange
        using var context = GetInMemoryDbContext();

        // Seed an existing item into the in-memory database
        context.InventoryItems.Add(new InventoryItem { Name = "Duplicate Test Item", SKU = "DUPE123", Quantity = 5 });
        await context.SaveChangesAsync();

        var controller = new InventoryController(context);

        var duplicateItem = new InventoryItem
        {
            Name = "Duplicate Item",
            SKU = "DUPE123", // Same SKU!
            Quantity = 10,
            Location = "Warehouse A"
        };

        // Act
        var result = await controller.CreateItem(duplicateItem);

        // Assert
        // Expects a 409 Conflict response
        Assert.IsType<ConflictObjectResult>(result);
    }
}