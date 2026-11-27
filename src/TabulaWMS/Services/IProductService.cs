using TabulaWMS.Models;

namespace TabulaWMS.Services;

public interface IProductService
{
    Task<Product> UpsertProductAsync(ProductDto dto);
    Task<(IEnumerable<Product> Products, int TotalCount)> GetPagedProductsAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null);
    Task<ProductDto?> GetProductBySkuAsync(string sku);
}