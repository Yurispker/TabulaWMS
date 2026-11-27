using Microsoft.AspNetCore.Mvc;
using TabulaWMS.Services;
using TabulaWMS.Models;
using Microsoft.AspNetCore.Authorization;
using TabulaWMS.Constants;

namespace TabulaWMS.Controllers;


[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;


    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    // GET /api/products
    [HttpGet]
    [Authorize(Policy = Permissions.ViewProducts)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null)
    {
        try
        {
            // Safety validation for pagination limits
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Prevent massive memory dumps

            // Fetch paginated products and total count from service
            var (products, totalCount) = await _productService.GetPagedProductsAsync(
                pageNumber,
                pageSize,
                searchTerm);

            return Ok(new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                TotalCount = totalCount,
                Items = products
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated products list.");
            return StatusCode(500, new { error = "An internal error occurred while retrieving products." });
        }
    }

    // POST /api/products
    [HttpPost]
    [Authorize(Policy = Permissions.CreateProducts)]
    public async Task<IActionResult> IngestProduct([FromBody] ProductDto dto)
    {
        // Check if the incoming payload is valid
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Hand the payload over to the service layer for processing
            var product = await _productService.UpsertProductAsync(dto);

            // Return a clean success response back to the client
            return Ok(new
            {
                message = "Product successfully ingested into TabulaWMS.",
                sku = product.SKU,
                name = product.Name
            });
        }
        catch (Exception ex)
        {
            // Catch unexpected errors, log them securely, and return a 500 response to the client
            _logger.LogError(ex, "Error processing product ingestion for SKU {SKU}", dto.SKU);
            return StatusCode(500, new { error = "An internal error occurred while syncing the product." });
        }
    }

    [HttpGet("{sku}")]
    [Authorize(Policy = Permissions.ViewProducts)]
    public async Task<IActionResult> GetProductBySku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return BadRequest(new { Error = "SKU parameter is required." });
        }

        try
        {
            var product = await _productService.GetProductBySkuAsync(sku);

            if (product == null)
            {
                return NotFound(new { Error = $"Product with SKU '{sku.Trim()}' was not found." });
            }

            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product by SKU: {Sku}", sku);
            return StatusCode(500, new { Error = "An internal error occurred while retrieving the product." });
        }
    }


    // PUT api/products/sku
    [HttpPut("{sku}")]
    [Authorize(Policy = Permissions.UpdateProducts)]
    public async Task<IActionResult> UpdateProduct(string sku, [FromBody] UpdateProductDto dto)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return BadRequest(new { Error = "SKU parameter is required." });
        }

        if ((dto.SKU == null) &&
            (dto.Name == null) &&
            !dto.Quantity.HasValue &&
            !dto.DescriptionSpecified &&
            !dto.LocationSpecified &&
            !dto.Price.HasValue)
        {
            return BadRequest(new { Error = "At least one product field must be provided." });
        }

        if (dto.Name != null && string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { Error = "Product name cannot be empty or whitespace." });
        }

        if (dto.SKU != null && string.IsNullOrWhiteSpace(dto.SKU))
        {
            return BadRequest(new { Error = "Product SKU cannot be empty or whitespace." });
        }

        try
        {
            var product = await _productService.UpdateProductAsync(sku, dto);
            if (product == null)
            {
                return NotFound(new { Error = $"Product with SKU '{sku.Trim()}' was not found." });
            }

            return Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product with SKU {Sku}", sku);
            return StatusCode(500, new { Error = "An internal error occurred while updating the product." });
        }
    }

    // DELETE api/products/sku
    [HttpDelete("{sku}")]
    [Authorize(Policy = Permissions.DeleteProducts)]
    public async Task<IActionResult> DeleteProduct(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return BadRequest(new { Error = "SKU parameter is required." });
        }

        try
        {
            var result = await _productService.DeleteProductAsync(sku);

            return result switch
            {
                ProductDeletionResult.Deleted => NoContent(),
                ProductDeletionResult.NotFound =>
                    NotFound(new { Error = $"Product with SKU '{sku.Trim()}' was not found." }),
                ProductDeletionResult.AlreadyDeleted =>
                    StatusCode(StatusCodes.Status410Gone, new
                    {
                        Error = $"Product with SKU '{sku.Trim()}' has already been deleted."
                    }),
                _ => StatusCode(StatusCodes.Status500InternalServerError,
                    new { Error = "An unexpected product deletion result was returned." })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product with SKU {Sku}", sku);
            return StatusCode(500, new { Error = "An internal error occurred while deleting the product." });
        }
    }
}