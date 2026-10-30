using Microsoft.AspNetCore.Mvc;
using TabulaWMS.Services;
using TabulaWMS.Models;
using Microsoft.AspNetCore.Authorization;

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
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        try
        {
            // Fetch products from the service layer
            var products = await _productService.GetAllProductsAsync();

            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products list.");
            return StatusCode(500, new { error = "An internal error occurred while retrieving products." });
        }
    }

    // POST /api/products
    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    public async Task<IActionResult> IngestProduct([FromBody] ProductUpsertDto dto)
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
            // 4. Catch unexpected errors, log them securely, and return a 500 response to the client
            _logger.LogError(ex, "Error processing product ingestion for SKU {SKU}", dto.SKU);
            return StatusCode(500, new { error = "An internal error occurred while syncing the product." });
        }
    }
}