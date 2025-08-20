using OnlineShoppingSystem.Application.Commands.Cart;
using OnlineShoppingSystem.Application.DTOs;

namespace OnlineShoppingSystem.Application.Common.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(Guid userId);
    Task<CartDto> AddToCartAsync(Guid userId, AddToCartCommand command);
    Task<CartDto> UpdateCartItemAsync(Guid userId, UpdateCartItemCommand command);
    Task RemoveFromCartAsync(Guid userId, Guid productId);
    Task ClearCartAsync(Guid userId);
    Task HandleStockChangesAsync(Guid productId, int newStock);
}