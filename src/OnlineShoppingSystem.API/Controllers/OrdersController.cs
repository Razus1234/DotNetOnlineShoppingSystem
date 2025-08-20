using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineShoppingSystem.Application.Commands.Order;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.Common.Models;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Queries;
using OnlineShoppingSystem.Domain.Enums;
using System.Security.Claims;

namespace OnlineShoppingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get user's order history with pagination
    /// </summary>
    /// <param name="query">Order query parameters</param>
    /// <returns>Paginated order history</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<OrderDto>>> GetOrderHistory([FromQuery] OrderQuery query)
    {
        var userId = GetCurrentUserId();
        
        _logger.LogInformation("Getting order history for user {UserId} with query: {@Query}", userId, query);

        var orders = await _orderService.GetOrderHistoryAsync(userId, query);

        return Ok(orders);
    }

    /// <summary>
    /// Get specific order by ID
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <returns>Order details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid id)
    {
        var userId = GetCurrentUserId();
        
        _logger.LogInformation("Getting order {OrderId} for user: {UserId}", id, userId);

        var order = await _orderService.GetOrderByIdAsync(id);

        // Ensure user can only access their own orders (unless admin)
        if (order.UserId != userId && !User.IsInRole("Admin"))
        {
            _logger.LogWarning("User {UserId} attempted to access order {OrderId} belonging to user {OrderUserId}", 
                userId, id, order.UserId);
            return Forbid("You can only access your own orders");
        }

        return Ok(order);
    }

    /// <summary>
    /// Place a new order
    /// </summary>
    /// <param name="command">Order placement details</param>
    /// <returns>Created order</returns>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<OrderDto>> PlaceOrder([FromBody] PlaceOrderCommand command)
    {
        var userId = GetCurrentUserId();
        
        _logger.LogInformation("Placing order for user: {UserId}", userId);

        var order = await _orderService.PlaceOrderAsync(userId, command);

        _logger.LogInformation("Order placed successfully: {OrderId} for user: {UserId}", order.Id, userId);

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    /// <summary>
    /// Cancel an order (only if not yet shipped)
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <returns>Updated order</returns>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> CancelOrder(Guid id)
    {
        var userId = GetCurrentUserId();
        
        _logger.LogInformation("Cancelling order {OrderId} for user: {UserId}", id, userId);

        // First get the order to check ownership
        var existingOrder = await _orderService.GetOrderByIdAsync(id);
        
        // Ensure user can only cancel their own orders (unless admin)
        if (existingOrder.UserId != userId && !User.IsInRole("Admin"))
        {
            _logger.LogWarning("User {UserId} attempted to cancel order {OrderId} belonging to user {OrderUserId}", 
                userId, id, existingOrder.UserId);
            return Forbid("You can only cancel your own orders");
        }

        var order = await _orderService.CancelOrderAsync(id);

        _logger.LogInformation("Order cancelled successfully: {OrderId}", id);

        return Ok(order);
    }

    /// <summary>
    /// Update order status (Admin only)
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="request">Status update request</param>
    /// <returns>Updated order</returns>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        _logger.LogInformation("Updating order {OrderId} status to: {Status}", id, request.Status);

        var order = await _orderService.UpdateOrderStatusAsync(id, request.Status);

        _logger.LogInformation("Order status updated successfully: {OrderId}", id);

        return Ok(order);
    }

    /// <summary>
    /// Get all orders (Admin only)
    /// </summary>
    /// <param name="query">Order query parameters</param>
    /// <returns>Paginated list of all orders</returns>
    [HttpGet("all")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<OrderDto>>> GetAllOrders([FromQuery] AdminOrderQuery query)
    {
        _logger.LogInformation("Admin getting all orders with query: {@Query}", query);

        // Convert AdminOrderQuery to OrderQuery for the service
        var orderQuery = new OrderQuery
        {
            PageNumber = query.Page,
            PageSize = query.PageSize,
            Status = query.Status
        };

        // For admin, we need to get orders for all users, so we'll need to modify the service
        // For now, we'll use a placeholder approach
        var orders = await _orderService.GetOrderHistoryAsync(Guid.Empty, orderQuery);

        return Ok(orders);
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

    public class UpdateOrderStatusRequest
    {
        public OrderStatus Status { get; set; }
    }

    public class AdminOrderQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public OrderStatus? Status { get; set; }
        public Guid? UserId { get; set; }
    }
}