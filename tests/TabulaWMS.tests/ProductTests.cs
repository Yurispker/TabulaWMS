using Microsoft.EntityFrameworkCore;
using TabulaWMS.Data;
using TabulaWMS.Models;
using TabulaWMS.Services;

namespace TabulaWMS.Tests;

public class ProductServiceTests
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
    public async Task UpsertProductAsync_NewItem_CreatesProductAndSetsInitialQuantity()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ProductService(context);

        var newDto = new ProductUpsertDto
        {
            SKU = "TEST-001",
            Name = "Test Widget",
            Description = "A test product",
            Price = 19.99m,
            Quantity = 5
        };

        // Act
        // The service returns the created Product directly
        var product = await service.UpsertProductAsync(newDto);

        // Assert
        Assert.NotNull(product);
        Assert.Equal("TEST-001", product.SKU);
        Assert.Equal("Test Widget", product.Name);
        Assert.Equal(5, product.Quantity);

        // Verify it was actually written to the database context
        var dbItem = await context.Products.FirstOrDefaultAsync(p => p.SKU == "TEST-001");
        Assert.NotNull(dbItem);
        Assert.Equal(5, dbItem.Quantity);
    }

    [Fact]
    public async Task UpsertProductAsync_ExistingSku_UpdatesMetadataAndProtectsQuantity()
    {
        // Arrange
        using var context = GetInMemoryDbContext();

        // Seed an existing product into our in-memory database with real physical warehouse stock (50)
        context.Products.Add(new Product
        {
            SKU = "DUPE123",
            Name = "Original Name",
            Description = "Original Desc",
            Price = 10.00m,
            Quantity = 50, // Physical stock count
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var service = new ProductService(context);

        var updateDto = new ProductUpsertDto
        {
            SKU = "DUPE123", // Same SKU!
            Name = "Updated Shopify Title",
            Description = "Updated Desc",
            Price = 29.99m,
            Quantity = 999 // This should be ignored by the guardrail logic in the service to protect physical stock
        };

        // Act
        var updatedProduct = await service.UpsertProductAsync(updateDto);

        // Assert
        Assert.NotNull(updatedProduct);
        Assert.Equal("Updated Shopify Title", updatedProduct.Name);
        Assert.Equal(29.99m, updatedProduct.Price);

        // Verify the database item updated its title/price, but kept its physical quantity safe at 50!
        var dbItem = await context.Products.FirstOrDefaultAsync(p => p.SKU == "DUPE123");
        Assert.NotNull(dbItem);
        Assert.Equal("Updated Shopify Title", dbItem.Name);
        Assert.Equal(50, dbItem.Quantity);
    }

    [Fact]
    public async Task UpsertProductAsync_NegativeQuantity_ThrowsArgumentException()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ProductService(context);

        var invalidDto = new ProductUpsertDto
        {
            SKU = "NEG-001",
            Name = "Bad Widget",
            Price = 10.00m,
            Quantity = -15 // Invalid negative stock!
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpsertProductAsync(invalidDto));
    }
}