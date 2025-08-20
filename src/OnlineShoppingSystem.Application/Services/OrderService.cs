using AutoMapper;
using Microsoft.Extensions.Logging;
using OnlineShoppingSystem.Application.Commands.Order;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.Common.Models;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Queries;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.Enums;
using OnlineShoppingSystem.Domain.Exceptions;

namespace OnlineShoppingSystem.Application.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<OrderService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OrderDto> PlaceOrderAsync(Guid userId, PlaceOrderCommand command)
    {
        _logger.LogInformation("Placing order for user {UserId}", userId);

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        if (command == null)
            throw new ArgumentNullException(nameof(command));

        // Verify user exists
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new UserNotFoundException(userId);

        // Get user's cart with items
        var cart = await _unitOfWork.Carts.GetByUserIdWithItemsAsync(userId);
        if (cart == null || cart.IsEmpty())
            throw new InvalidOperationException("Cannot place order with empty cart");

        // Begin transaction for order placement
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            // Validate stock availability and collect order items
            var orderItems = new List<OrderItem>();
            var productsToUpdate = new List<Product>();

            foreach (var cartItem in cart.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(cartItem.ProductId);
                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found when placing order", cartItem.ProductId);
                    throw new ProductNotFoundException(cartItem.ProductId);
                }

                // Check stock availability
                if (!product.IsInStock(cartItem.Quantity))
                {
                    _logger.LogWarning("Insufficient stock for product {ProductId}. Available: {Available}, Requested: {Requested}", 
                        cartItem.ProductId, product.Stock, cartItem.Quantity);
                    throw new ProductOutOfStockException(product.Name);
                }

                // Create order item
                var orderItem = new OrderItem(Guid.NewGuid(), cartItem.ProductId, cartItem.ProductName, cartItem.Price, cartItem.Quantity);
                orderItems.Add(orderItem);

                // Prepare product for stock reduction
                product.ReduceStock(cartItem.Quantity);
                productsToUpdate.Add(product);
            }

            // Create order with shipping address
            var shippingAddress = _mapper.Map<Domain.ValueObjects.Address>(command.ShippingAddress);
            var order = new Order(userId, shippingAddress, orderItems);

            // Add order to repository
            await _unitOfWork.Orders.AddAsync(order);

            // Update product stock levels
            foreach (var product in productsToUpdate)
            {
                await _unitOfWork.Products.UpdateAsync(product);
            }

            // Clear the cart after successful order placement
            cart.Clear();
            await _unitOfWork.Carts.UpdateAsync(cart);

            // Save all changes
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            var orderDto = _mapper.Map<OrderDto>(order);
            
            _logger.LogInformation("Successfully placed order {OrderId} for user {UserId} with {ItemCount} items", 
                order.Id, userId, order.Items.Count);

            return orderDto;
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError("Failed to place order for user {UserId}, transaction rolled back", userId);
            throw;
        }
    }

    public async Task<PagedResult<OrderDto>> GetOrderHistoryAsync(Guid userId, OrderQuery query)
    {
        _logger.LogInformation("Getting order history for user {UserId}", userId);

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        if (query == null)
            throw new ArgumentNullException(nameof(query));

        // Verify user exists
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new UserNotFoundException(userId);

        var pagedOrders = await _unitOfWork.Orders.GetByUserIdAsync(userId, query);
        var orderDtos = _mapper.Map<List<OrderDto>>(pagedOrders.Items);

        var result = new PagedResult<OrderDto>
        {
            Items = orderDtos,
            TotalCount = pagedOrders.TotalCount,
            Page = pagedOrders.Page,
            PageSize = pagedOrders.PageSize
        };

        _logger.LogInformation("Retrieved {Count} orders for user {UserId} (page {Page} of {TotalPages})", 
            orderDtos.Count, userId, query.PageNumber, result.TotalPages);

        return result;
    }

    public async Task<OrderDto> GetOrderByIdAsync(Guid orderId)
    {
        _logger.LogInformation("Getting order {OrderId}", orderId);

        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID cannot be empty", nameof(orderId));

        var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(orderId);
        if (order == null)
            throw new InvalidOperationException($"Order with ID {orderId} not found");

        var orderDto = _mapper.Map<OrderDto>(order);
        
        _logger.LogInformation("Retrieved order {OrderId} with status {Status}", orderId, order.Status);

        return orderDto;
    }

    public async Task<OrderDto> CancelOrderAsync(Guid orderId)
    {
        _logger.LogInformation("Cancelling order {OrderId}", orderId);

        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID cannot be empty", nameof(orderId));

        var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(orderId);
        if (order == null)
            throw new InvalidOperationException($"Order with ID {orderId} not found");

        if (!order.CanBeCancelled())
        {
            _logger.LogWarning("Cannot cancel order {OrderId} with status {Status}", orderId, order.Status);
            throw new InvalidOperationException($"Cannot cancel order with status {order.Status}");
        }

        // Begin transaction for order cancellation
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            // Restore stock for all order items
            foreach (var orderItem in order.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(orderItem.ProductId);
                if (product != null)
                {
                    product.IncreaseStock(orderItem.Quantity);
                    await _unitOfWork.Products.UpdateAsync(product);
                    
                    _logger.LogInformation("Restored {Quantity} units of stock for product {ProductId}", 
                        orderItem.Quantity, orderItem.ProductId);
                }
                else
                {
                    _logger.LogWarning("Product {ProductId} not found when restoring stock for cancelled order {OrderId}", 
                        orderItem.ProductId, orderId);
                }
            }

            // Cancel the order
            order.Cancel();
            await _unitOfWork.Orders.UpdateAsync(order);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            var orderDto = _mapper.Map<OrderDto>(order);
            
            _logger.LogInformation("Successfully cancelled order {OrderId}", orderId);

            return orderDto;
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError("Failed to cancel order {OrderId}, transaction rolled back", orderId);
            throw;
        }
    }

    public async Task<OrderDto> UpdateOrderStatusAsync(Guid orderId, OrderStatus status)
    {
        _logger.LogInformation("Updating order {OrderId} status to {Status}", orderId, status);

        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID cannot be empty", nameof(orderId));

        var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(orderId);
        if (order == null)
            throw new InvalidOperationException($"Order with ID {orderId} not found");

        var previousStatus = order.Status;

        try
        {
            order.UpdateStatus(status);
            await _unitOfWork.Orders.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            var orderDto = _mapper.Map<OrderDto>(order);
            
            _logger.LogInformation("Successfully updated order {OrderId} status from {PreviousStatus} to {NewStatus}", 
                orderId, previousStatus, status);

            return orderDto;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid status transition for order {OrderId}: {Message}", orderId, ex.Message);
            throw;
        }
    }

    public async Task<PagedResult<OrderDto>> GetAllOrdersAsync(AdminOrderQuery query)
    {
        _logger.LogInformation("Admin getting all orders with query: {@Query}", query);

        if (query == null)
            throw new ArgumentNullException(nameof(query));

        var pagedOrders = await _unitOfWork.Orders.GetAllOrdersAsync(query);
        var orderDtos = _mapper.Map<List<OrderDto>>(pagedOrders.Items);

        var result = new PagedResult<OrderDto>
        {
            Items = orderDtos,
            TotalCount = pagedOrders.TotalCount,
            Page = pagedOrders.Page,
            PageSize = pagedOrders.PageSize
        };

        _logger.LogInformation("Retrieved {Count} orders for admin (page {Page} of {TotalPages})", 
            orderDtos.Count, query.PageNumber, result.TotalPages);

        return result;
    }
}