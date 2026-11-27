using TabulaWMS.Models;

namespace TabulaWMS.Services;

public enum ProductDeletionResult
{
    Deleted,
    NotFound,
    AlreadyDeleted
}

public interface IProductService
{
    Task<Product> UpsertProductAsync(ProductDto dto);
    Task<(IEnumerable<Product> Products, int TotalCount)> GetPagedProductsAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null);
    Task<ProductDto?> GetProductBySkuAsync(string sku);
    Task<Product?> UpdateProductAsync(string sku, UpdateProductDto dto);
    Task<ProductDeletionResult> DeleteProductAsync(string sku);
}