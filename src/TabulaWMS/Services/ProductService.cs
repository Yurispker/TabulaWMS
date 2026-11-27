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

    public async Task<Product> UpsertProductAsync(ProductDto dto)
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

    public async Task<(IEnumerable<Product> Products, int TotalCount)> GetPagedProductsAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null)
    {
        var query = _context.Products.AsNoTracking();


        // Filter by SKU or Name
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(product =>
                product.SKU.Contains(term) ||
                product.Name.Contains(term) ||
                (product.Description != null && product.Description.Contains(term)));
        }

        var totalCount = await query.CountAsync();

        var products = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (products, totalCount);
    }
    public async Task<ProductDto?> GetProductBySkuAsync(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return null;
        }

        var sanitizedSku = sku.Trim();

        return await _context.Products
            .AsNoTracking()
            .Where(p => p.SKU == sanitizedSku)
            .Select(p => new ProductDto 
            { 
                SKU = p.SKU,
                Name = p.Name,
                Price = p.Price,
                Quantity = p.Quantity 
            })
            .FirstOrDefaultAsync();
    }
}