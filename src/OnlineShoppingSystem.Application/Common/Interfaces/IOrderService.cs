using OnlineShoppingSystem.Application.Commands.Order;
using OnlineShoppingSystem.Application.Common.Models;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Queries;
using OnlineShoppingSystem.Domain.Enums;

namespace OnlineShoppingSystem.Application.Common.Interfaces;

public interface IOrderService
{
    Task<OrderDto> PlaceOrderAsync(Guid userId, PlaceOrderCommand command);
    Task<PagedResult<OrderDto>> GetOrderHistoryAsync(Guid userId, OrderQuery query);
    Task<OrderDto> GetOrderByIdAsync(Guid orderId);
    Task<OrderDto> CancelOrderAsync(Guid orderId);
    Task<OrderDto> UpdateOrderStatusAsync(Guid orderId, OrderStatus status);
    Task<PagedResult<OrderDto>> GetAllOrdersAsync(AdminOrderQuery query);
}