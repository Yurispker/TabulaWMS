using Microsoft.EntityFrameworkCore;
using Xunit;
using TabulaWMS.Controllers;
using TabulaWMS.Data;
using TabulaWMS.Models;
using Microsoft.AspNetCore.Mvc;

namespace TabulaWMS.tests;

public class ProductsControllerTests
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
    public async Task UpsertProduct_NewItem_ReturnsCreatedAtActionResult()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var controller = new ProductController(context);

        var newDto = new ProductUpsertDto
        {
            Name = "Test Item",
            Quantity = 3,
            SKU = "Test001",
            Description = "A test product"
        };

        // Act
        var result = await controller.UpsertProduct(newDto);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
        var returnValue = Assert.IsType<Product>(createdAtActionResult.Value);
        Assert.Equal(newDto.Name, returnValue.Name);
        Assert.Equal(newDto.Quantity, returnValue.Quantity);
        Assert.Equal(newDto.SKU, returnValue.SKU);

        var dbItem = await context.Products.FirstOrDefaultAsync(i => i.SKU == "Test001");
        Assert.NotNull(dbItem);
        Assert.Equal(3, dbItem.Quantity);
    }

    [Fact]
    public async Task UpsertProduct_NegativeQuantity_ReturnsBadRequest()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var controller = new ProductController(context);

        var invalidDto = new ProductUpsertDto
        {
            Name = "Negative Widget",
            SKU = "NEG001",
            Quantity = -10
        };

        // Manually simulate model validation since DTO DataAnnotations handle this
        controller.ModelState.AddModelError("InitialQuantity", "Initial quantity cannot be negative.");

        // Act
        var result = await controller.UpsertProduct(invalidDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task UpsertProduct_ExistingSku_UpdatesAndReturnsNoContent()
    {
        // Arrange
        using var context = GetInMemoryDbContext();

        // Seed an existing item into the in-memory database
        context.Products.Add(new Product { Name = "Original Name", SKU = "DUPE123", Quantity = 5 });
        await context.SaveChangesAsync();

        var controller = new ProductController(context);

        var updateDto = new ProductUpsertDto
        {
            Name = "Updated Name",
            SKU = "DUPE123", // Same SKU!
            Quantity = 99 // Should be ignored by upsert logic to protect stock
        };

        // Act
        var result = await controller.UpsertProduct(updateDto);

        // Assert
        // PUT Upsert updates metadata and returns 204 NoContent on success
        Assert.IsType<NoContentResult>(result);

        // Verify name changed, but live quantity remained safe (5, not 99)
        var dbItem = await context.Products.FirstOrDefaultAsync(i => i.SKU == "DUPE123");
        Assert.NotNull(dbItem);
        Assert.Equal("Updated Name", dbItem.Name);
        Assert.Equal(5, dbItem.Quantity); // Proves quantity was protected from sync overwrite!
    }
}