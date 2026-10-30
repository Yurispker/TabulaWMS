using Microsoft.EntityFrameworkCore;
using TabulaWMS.Data;
using TabulaWMS.Models;

namespace TabulaWMS.Services;

public class ProductService : IProductService
{
    private readonly TabulaContext _context;

    public ProductService(TabulaContext context)
    {
        _context = context;
    }

    public async Task<Product> UpsertProductAsync(ProductUpsertDto dto)
    {

        // Check if quantity is negative
        if (dto.Quantity.HasValue && dto.Quantity.Value < 0)
        {
            throw new ArgumentException("Quantity cannot be negative.", nameof(dto.Quantity));
        }
        // Check if the SKU already exists in our database
        var existingProduct = await _context.Products
            .FirstOrDefaultAsync(p => p.SKU == dto.SKU);

        if (existingProduct == null)
        {
            // BRAND NEW PRODUCT: Safe Create
            var newProduct = new Product
            {
                SKU = dto.SKU,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Quantity = dto.Quantity ?? 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Products.Add(newProduct);
            await _context.SaveChangesAsync();
            return newProduct;
        }
        else
        {
            // PRODUCT ALREADY EXISTS: Safe Metadata Sync (but protect physical stock quantity)
            existingProduct.Name = dto.Name;
            existingProduct.Description = dto.Description;
            existingProduct.Price = dto.Price;
            existingProduct.UpdatedAt = DateTime.UtcNow;


            await _context.SaveChangesAsync();
            return existingProduct;
        }
    }

    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        // Fetch all products from your SQLite database asynchronously
        return await _context.Products.ToListAsync();
    }
}