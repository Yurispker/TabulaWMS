using TabulaWMS.Models;

namespace TabulaWMS.Services;

public interface IProductService
{
    Task<Product> UpsertProductAsync(ProductUpsertDto dto);
}