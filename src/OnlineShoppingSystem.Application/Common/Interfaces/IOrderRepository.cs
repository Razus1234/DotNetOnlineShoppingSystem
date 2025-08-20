using OnlineShoppingSystem.Application.Common.Models;
using OnlineShoppingSystem.Application.Queries;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.Enums;

namespace OnlineShoppingSystem.Application.Common.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<PagedResult<Order>> GetByUserIdAsync(Guid userId, OrderQuery query);
    Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status);
    Task<Order?> GetByIdWithItemsAsync(Guid orderId);
    Task<PagedResult<Order>> GetAllOrdersAsync(AdminOrderQuery query);
}