using OnlineShoppingSystem.Application.Common.Models;
using OnlineShoppingSystem.Application.Queries;
using OnlineShoppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Application.Common.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<PagedResult<Product>> GetPagedAsync(ProductQuery query);
    Task<IEnumerable<Product>> SearchAsync(string keyword);
    Task<IEnumerable<Product>> GetByCategoryAsync(string category);
}