using AutoMapper;
using Microsoft.Extensions.Logging;
using OnlineShoppingSystem.Application.Commands.Cart;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.Exceptions;

namespace OnlineShoppingSystem.Application.Services;

public class CartService : ICartService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CartService> _logger;

    public CartService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<CartService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CartDto> GetCartAsync(Guid userId)
    {
        _logger.LogInformation("Getting cart for user {UserId}", userId);

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        // Verify user exists
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new UserNotFoundException(userId);

        var cart = await _unitOfWork.Carts.GetByUserIdWithItemsAsync(userId);
        
        if (cart == null)
        {
            // Create a new cart if one doesn't exist
            cart = Cart.Create(userId);
            await _unitOfWork.Carts.AddAsync(cart);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Created new cart {CartId} for user {UserId}", cart.Id, userId);
        }

        var cartDto = _mapper.Map<CartDto>(cart);
        
        _logger.LogInformation("Retrieved cart {CartId} with {ItemCount} items for user {UserId}", 
            cart.Id, cart.Items.Count, userId);

        return cartDto;
    }

    public async Task<CartDto> AddToCartAsync(Guid userId, AddToCartCommand command)
    {
        _logger.LogInformation("Adding product {ProductId} (quantity: {Quantity}) to cart for user {UserId}", 
            command.ProductId, command.Quantity, userId);

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        // Verify user exists
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new UserNotFoundException(userId);

        // Get product and verify it exists
        var product = await _unitOfWork.Products.GetByIdAsync(command.ProductId);
        if (product == null)
            throw new ProductNotFoundException(command.ProductId);

        // Validate stock availability
        if (!product.IsInStock(command.Quantity))
        {
            _logger.LogWarning("Insufficient stock for product {ProductId}. Available: {Available}, Requested: {Requested}", 
                command.ProductId, product.Stock, command.Quantity);
            throw new ProductOutOfStockException(product.Name);
        }

        // Get or create cart
        var cart = await _unitOfWork.Carts.GetByUserIdWithItemsAsync(userId);
        if (cart == null)
        {
            cart = Cart.Create(userId);
            await _unitOfWork.Carts.AddAsync(cart);
        }

        // Check if adding to existing item would exceed stock
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == command.ProductId);
        if (existingItem != null)
        {
            var totalQuantity = existingItem.Quantity + command.Quantity;
            if (!product.IsInStock(totalQuantity))
            {
                _logger.LogWarning("Total quantity would exceed stock for product {ProductId}. Available: {Available}, Total requested: {Total}", 
                    command.ProductId, product.Stock, totalQuantity);
                throw new ProductOutOfStockException(product.Name);
            }
        }

        // Add item to cart
        cart.AddItem(product, command.Quantity);
        
        await _unitOfWork.SaveChangesAsync();

        var cartDto = _mapper.Map<CartDto>(cart);
        
        _logger.LogInformation("Successfully added product {ProductId} to cart {CartId} for user {UserId}", 
            command.ProductId, cart.Id, userId);

        return cartDto;
    }

    public async Task<CartDto> UpdateCartItemAsync(Guid userId, UpdateCartItemCommand command)
    {
        _logger.LogInformation("Updating cart item {ProductId} to quantity {Quantity} for user {UserId}", 
            command.ProductId, command.Quantity, userId);

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        // Verify user exists
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new UserNotFoundException(userId);

        // Get cart
        var cart = await _unitOfWork.Carts.GetByUserIdWithItemsAsync(userId);
        if (cart == null)
            throw new InvalidOperationException("Cart not found for user");

        // Verify cart item exists
        var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == command.ProductId);
        if (cartItem == null)
            throw new InvalidOperationException("Cart item not found");

        // Get product and verify stock
        var product = await _unitOfWork.Products.GetByIdAsync(command.ProductId);
        if (product == null)
            throw new ProductNotFoundException(command.ProductId);

        if (!product.IsInStock(command.Quantity))
        {
            _logger.LogWarning("Insufficient stock for product {ProductId}. Available: {Available}, Requested: {Requested}", 
                command.ProductId, product.Stock, command.Quantity);
            throw new ProductOutOfStockException(product.Name);
        }

        // Update cart item quantity
        cart.UpdateItemQuantity(command.ProductId, command.Quantity, product);
        
        await _unitOfWork.SaveChangesAsync();

        var cartDto = _mapper.Map<CartDto>(cart);
        
        _logger.LogInformation("Successfully updated cart item {ProductId} to quantity {Quantity} for user {UserId}", 
            command.ProductId, command.Quantity, userId);

        return cartDto;
    }

    public async Task RemoveFromCartAsync(Guid userId, Guid productId)
    {
        _logger.LogInformation("Removing product {ProductId} from cart for user {UserId}", productId, userId);

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        // Verify user exists
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new UserNotFoundException(userId);

        // Get cart
        var cart = await _unitOfWork.Carts.GetByUserIdWithItemsAsync(userId);
        if (cart == null)
            throw new InvalidOperationException("Cart not found for user");

        // Verify cart item exists
        var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (cartItem == null)
            throw new InvalidOperationException("Cart item not found");

        // Remove item from cart
        cart.RemoveItem(productId);
        
        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Successfully removed product {ProductId} from cart for user {UserId}", 
            productId, userId);
    }

    public async Task ClearCartAsync(Guid userId)
    {
        _logger.LogInformation("Clearing cart for user {UserId}", userId);

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        // Verify user exists
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new UserNotFoundException(userId);

        // Get cart
        var cart = await _unitOfWork.Carts.GetByUserIdWithItemsAsync(userId);
        if (cart == null)
            throw new InvalidOperationException("Cart not found for user");

        // Clear all items
        cart.Clear();
        
        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Successfully cleared cart for user {UserId}", userId);
    }

    public async Task HandleStockChangesAsync(Guid productId, int newStock)
    {
        _logger.LogInformation("Handling stock changes for product {ProductId}, new stock: {NewStock}", 
            productId, newStock);

        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        if (newStock < 0)
            throw new ArgumentException("Stock cannot be negative", nameof(newStock));

        // Get all carts that contain this product
        var allCarts = await _unitOfWork.Carts.GetAllAsync();
        var cartsWithProduct = allCarts.Where(c => c.Items.Any(i => i.ProductId == productId)).ToList();

        if (!cartsWithProduct.Any())
        {
            _logger.LogInformation("No carts contain product {ProductId}, no adjustments needed", productId);
            return;
        }

        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null)
        {
            _logger.LogWarning("Product {ProductId} not found when handling stock changes", productId);
            return;
        }

        var adjustedCarts = 0;
        var removedItems = 0;

        foreach (var cart in cartsWithProduct)
        {
            var cartItem = cart.Items.First(i => i.ProductId == productId);
            
            if (cartItem.Quantity > newStock)
            {
                if (newStock == 0)
                {
                    // Remove item completely if no stock available
                    cart.RemoveItem(productId);
                    removedItems++;
                    _logger.LogInformation("Removed product {ProductId} from cart {CartId} due to zero stock", 
                        productId, cart.Id);
                }
                else
                {
                    // Adjust quantity to available stock
                    cart.UpdateItemQuantity(productId, newStock, product);
                    adjustedCarts++;
                    _logger.LogInformation("Adjusted product {ProductId} quantity in cart {CartId} from {OldQuantity} to {NewQuantity}", 
                        productId, cart.Id, cartItem.Quantity, newStock);
                }
            }
        }

        if (adjustedCarts > 0 || removedItems > 0)
        {
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Stock change handling completed for product {ProductId}. Adjusted {AdjustedCarts} carts, removed from {RemovedItems} carts", 
                productId, adjustedCarts, removedItems);
        }
        else
        {
            _logger.LogInformation("No cart adjustments needed for product {ProductId} stock change", productId);
        }
    }
}