using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineShoppingSystem.Application.Commands.Cart;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.DTOs;
using System.Security.Claims;

namespace OnlineShoppingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ILogger<CartController> _logger;

    public CartController(ICartService cartService, ILogger<CartController> logger)
    {
        _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get current user's shopping cart
    /// </summary>
    /// <returns>Shopping cart with items</returns>
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        var userId = GetCurrentUserId();
        
        _logger.LogInformation("Getting cart for user: {UserId}", userId);

        var cart = await _cartService.GetCartAsync(userId);

        return Ok(cart);
    }

    /// <summary>
    /// Add item to shopping cart
    /// </summary>
    /// <param name="command">Add to cart details</param>
    /// <returns>Updated shopping cart</returns>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CartDto>> AddToCart([FromBody] AddToCartCommand command)
    {
        var userId = GetCurrentUserId();
        
        _logger.LogInformation("Adding item to cart for user {UserId}: ProductId={ProductId}, Quantity={Quantity}", 
            userId, command.ProductId, command.Quantity);

        var cart = await _cartService.AddToCartAsync(userId, command);

        _logger.LogInformation("Item added to cart successfully for user: {UserId}", userId);

        return Ok(cart);
    }

    /// <summary>
    /// Update cart item quantity
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="command">Update cart item details</param>
    /// <returns>Updated shopping cart</returns>
    [HttpPut("items/{productId:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> UpdateCartItem(Guid productId, [FromBody] UpdateCartItemCommand command)
    {
        var userId = GetCurrentUserId();
        
        // Ensure the product ID in the URL matches the command
        if (command.ProductId != productId)
        {
            return BadRequest("Product ID in URL does not match the request body");
        }

        _logger.LogInformation("Updating cart item for user {UserId}: ProductId={ProductId}, Quantity={Quantity}", 
            userId, command.ProductId, command.Quantity);

        var cart = await _cartService.UpdateCartItemAsync(userId, command);

        _logger.LogInformation("Cart item updated successfully for user: {UserId}", userId);

        return Ok(cart);
    }

    /// <summary>
    /// Remove item from shopping cart
    /// </summary>
    /// <param name="productId">Product ID to remove</param>
    /// <returns>No content</returns>
    [HttpDelete("items/{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFromCart(Guid productId)
    {
        var userId = GetCurrentUserId();
        
        _logger.LogInformation("Removing item from cart for user {UserId}: ProductId={ProductId}", 
            userId, productId);

        await _cartService.RemoveFromCartAsync(userId, productId);

        _logger.LogInformation("Item removed from cart successfully for user: {UserId}", userId);

        return NoContent();
    }

    /// <summary>
    /// Clear all items from shopping cart
    /// </summary>
    /// <returns>No content</returns>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ClearCart()
    {
        var userId = GetCurrentUserId();
        
        _logger.LogInformation("Clearing cart for user: {UserId}", userId);

        await _cartService.ClearCartAsync(userId);

        _logger.LogInformation("Cart cleared successfully for user: {UserId}", userId);

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }
        return userId;
    }
}