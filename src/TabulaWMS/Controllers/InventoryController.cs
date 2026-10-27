using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TabulaWMS.Data;
using TabulaWMS.Models;

namespace TabulaWMS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly TabulaContext _context;

    public InventoryController(TabulaContext context)
    {
        _context = context;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetItem(int id)
    {
        var item = await _context.InventoryItems.FindAsync(id);

        if (item == null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> CreateItem([FromBody] InventoryItem item)
    {

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check for duplicate SKU
        var existingItem = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.SKU == item.SKU);

        if (existingItem != null)
        {
            ModelState.AddModelError("SKU", "An item with this SKU already exists.");
            return Conflict(ModelState);
        }

        _context.InventoryItems.Add(item);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetItem), new { id = item.Id }, item);
    }
}