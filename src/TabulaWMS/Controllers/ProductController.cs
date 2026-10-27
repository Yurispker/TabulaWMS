using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TabulaWMS.Data;
using TabulaWMS.Models;

namespace TabulaWMS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly TabulaContext _context;

    public ProductController(TabulaContext context)
    {
        _context = context;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetItem(int id)
    {
        var item = await _context.Products.FindAsync(id);

        if (item == null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    [HttpPut]
    public async Task<IActionResult> UpsertProduct([FromBody] ProductUpsertDto payload)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existingProduct = await _context.Products
            .FirstOrDefaultAsync(p => p.SKU == payload.SKU);

        if (existingProduct == null)
        {
            // CASE 1: Brand new product. Set initial quantity from payload.
            var newProduct = new Product
            {
                SKU = payload.SKU,
                Name = payload.Name,
                Description = payload.Description,
                Quantity = payload.Quantity ?? 0
            };

            _context.Products.Add(newProduct);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetItem), new { id = newProduct.Id }, newProduct);
        }
        else
        {
            // CASE 2: Product already exists. Update metadata ONLY.
            // Quantity not to be declared here!
            existingProduct.Name = payload.Name;
            existingProduct.Description = payload.Description;

            await _context.SaveChangesAsync();
            return NoContent(); // 204 Success
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProduct([FromBody] ProductUpsertDto payload)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existingProduct = await _context.Products
            .FirstOrDefaultAsync(p => p.SKU == payload.SKU);

        if (existingProduct == null)
        {
            return NotFound(new { message = $"Product with SKU '{payload.SKU}' was not found." });
        }

        existingProduct.Name = payload.Name;
        existingProduct.Description = payload.Description;

        await _context.SaveChangesAsync();
        return NoContent();
    }
}