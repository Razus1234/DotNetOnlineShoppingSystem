using OnlineShoppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Application.Common.Interfaces;

public interface ICartRepository : IRepository<Cart>
{
    Task<Cart?> GetByUserIdAsync(Guid userId);
    Task<Cart?> GetByUserIdWithItemsAsync(Guid userId);
}