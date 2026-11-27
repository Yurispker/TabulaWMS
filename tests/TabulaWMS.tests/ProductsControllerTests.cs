using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TabulaWMS.Controllers;
using TabulaWMS.Services;

namespace TabulaWMS.Tests;

public class ProductsControllerTests
{
    [Fact]
    public async Task DeleteProduct_ReturnsNoContent_WhenProductIsDeleted()
    {
        var productService = new Mock<IProductService>();
        productService
            .Setup(service => service.DeleteProductAsync("SKU-1"))
            .ReturnsAsync(ProductDeletionResult.Deleted);
        var controller = new ProductsController(
            productService.Object,
            NullLogger<ProductsController>.Instance);

        var result = await controller.DeleteProduct("SKU-1");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteProduct_ReturnsNotFound_WhenProductDoesNotExist()
    {
        var productService = new Mock<IProductService>();
        productService
            .Setup(service => service.DeleteProductAsync("missing"))
            .ReturnsAsync(ProductDeletionResult.NotFound);
        var controller = new ProductsController(
            productService.Object,
            NullLogger<ProductsController>.Instance);

        var result = await controller.DeleteProduct("missing");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteProduct_ReturnsGone_WhenProductIsAlreadyDeleted()
    {
        var productService = new Mock<IProductService>();
        productService
            .Setup(service => service.DeleteProductAsync("SKU-1"))
            .ReturnsAsync(ProductDeletionResult.AlreadyDeleted);
        var controller = new ProductsController(
            productService.Object,
            NullLogger<ProductsController>.Instance);

        var result = await controller.DeleteProduct("SKU-1");

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status410Gone, objectResult.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task DeleteProduct_ReturnsBadRequest_WhenSkuIsBlank(string sku)
    {
        var productService = new Mock<IProductService>();
        var controller = new ProductsController(
            productService.Object,
            NullLogger<ProductsController>.Instance);

        var result = await controller.DeleteProduct(sku);

        Assert.IsType<BadRequestObjectResult>(result);
        productService.Verify(
            service => service.DeleteProductAsync(It.IsAny<string>()),
            Times.Never);
    }
}
