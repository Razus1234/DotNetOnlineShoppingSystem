using OnlineShoppingSystem.Application.Commands.Product;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Queries;

namespace OnlineShoppingSystem.Application.Common.Interfaces;

public interface IProductService
{
    Task<PagedResultDto<ProductDto>> GetProductsAsync(ProductQuery query);
    Task<ProductDto?> GetProductByIdAsync(Guid productId);
    Task<ProductDto> CreateProductAsync(CreateProductCommand command);
    Task<ProductDto> UpdateProductAsync(Guid productId, UpdateProductCommand command);
    Task DeleteProductAsync(Guid productId);
    Task<ProductDto> UpdateStockAsync(Guid productId, int newStock);
    Task<bool> IsProductInStockAsync(Guid productId, int requestedQuantity = 1);
    Task<IEnumerable<ProductDto>> SearchProductsAsync(string keyword);
    Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(string category);
}