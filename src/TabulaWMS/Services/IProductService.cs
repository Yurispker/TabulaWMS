using TabulaWMS.Models;

namespace TabulaWMS.Services;

public interface IProductService
{
    Task<Product> UpsertProductAsync(ProductUpsertDto dto);
    Task<(IEnumerable<Product> Products, int TotalCount)> GetPagedProductsAsync(int pageNumber, int pageSize);
}