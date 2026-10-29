using Microsoft.AspNetCore.Mvc;
using TabulaWMS.Services;
using TabulaWMS.Models;

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

    [HttpPost]
    public async Task<IActionResult> IngestProduct([FromBody] ProductUpsertDto dto)
    {
        // Check if the incoming payload is valid according to our DTO's data annotations
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Hand the payload over to our business logic service
            var product = await _productService.UpsertProductAsync(dto);

            // Return a clean success response back to Shopify
            return Ok(new
            {
                message = "Product successfully ingested into TabulaWMS.",
                sku = product.SKU,
                name = product.Name
            });
        }
        catch (Exception ex)
        {
            // 4. Catch unexpected errors, log them securely, and return a 500
            _logger.LogError(ex, "Error processing product ingestion for SKU {SKU}", dto.SKU);
            return StatusCode(500, new { error = "An internal error occurred while syncing the product." });
        }
    }
}