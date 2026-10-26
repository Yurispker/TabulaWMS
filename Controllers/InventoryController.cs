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

    [HttpPost]
    public async Task<IActionResult> CreateItem([FromBody] InventoryItem item)
    {
        _context.InventoryItems.Add(item);
        await _context.SaveChangesAsync();
        return Ok(item);
    }
}