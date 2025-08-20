using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OnlineShoppingSystem.Application.Commands.Cart;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Mappings;
using OnlineShoppingSystem.Application.Services;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.Exceptions;
using OnlineShoppingSystem.Domain.ValueObjects;

namespace OnlineShoppingSystem.Tests.Unit.Application.Services;

[TestClass]
public class CartServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<ICartRepository> _mockCartRepository;
    private Mock<IUserRepository> _mockUserRepository;
    private Mock<IProductRepository> _mockProductRepository;
    private Mock<ILogger<CartService>> _mockLogger;
    private IMapper _mapper;
    private CartService _cartService;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _cartId = Guid.NewGuid();

    [TestInitialize]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCartRepository = new Mock<ICartRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockProductRepository = new Mock<IProductRepository>();
        _mockLogger = new Mock<ILogger<CartService>>();

        // Setup AutoMapper
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<CartMappingProfile>();
        });
        _mapper = config.CreateMapper();

        // Setup UnitOfWork
        _mockUnitOfWork.Setup(x => x.Carts).Returns(_mockCartRepository.Object);
        _mockUnitOfWork.Setup(x => x.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(x => x.Products).Returns(_mockProductRepository.Object);

        _cartService = new CartService(_mockUnitOfWork.Object, _mapper, _mockLogger.Object);
    }

    [TestMethod]
    public async Task GetCartAsync_UserExists_ReturnsExistingCart()
    {
        // Arrange
        var user = CreateTestUser();
        var cart = CreateTestCart();
        
        _mockUserRepository.Setup(x => x.GetByIdAsync(_userId))
            .ReturnsAsync(user);
        _mockCartRepository.Setup(x => x.GetByUserIdWithItemsAsync(_userId))
            .ReturnsAsync(cart);

        // Act
        var result = await _cartService.GetCartAsync(_userId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(cart.Id, result.Id);
        Assert.AreEqual(_userId, result.UserId);
        _mockCartRepository.Verify(x => x.AddAsync(It.IsAny<Cart>()), Times.Never);
    }

    [TestMethod]
    public async Task GetCartAsync_UserExistsButNoCart_CreatesNewCart()
    {
        // Arrange
        var user = CreateTestUser();
        
        _mockUserRepository.Setup(x => x.GetByIdAsync(_userId))
            .ReturnsAsync(user);
        _mockCartRepository.Setup(x => x.GetByUserIdWithItemsAsync(_userId))
            .ReturnsAsync((Cart?)null);
        _mockCartRepository.Setup(x => x.AddAsync(It.IsAny<Cart>()))
            .ReturnsAsync((Cart cart) => cart);

        // Act
        var result = await _cartService.GetCartAsync(_userId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(_userId, result.UserId);
        _mockCartRepository.Verify(x => x.AddAsync(It.IsAny<Cart>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [TestMethod]
    public async Task GetCartAsync_UserNotFound_ThrowsUserNotFoundException()
    {
        // Arrange
        _mockUserRepository.Setup(x => x.GetByIdAsync(_userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<UserNotFoundException>(
            () => _cartService.GetCartAsync(_userId));
    }

    [TestMethod]
    public async Task GetCartAsync_EmptyUserId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _cartService.GetCartAsync(Guid.Empty));
    }

    [TestMethod]
    public async Task AddToCartAsync_ValidRequest_AddsItemToCart()
    {
        // Arrange
        var user = CreateTestUser();
        var product = CreateTestProduct(10); // 10 in stock
        var cart = CreateTestCart();
        var command = new AddToCartCommand { ProductId = _productId, Quantity = 2 };

        _mockUserRepository.Setup(x => x.GetByIdAsync(_userId))
            .ReturnsAsync(user);
        _mockProductRepository.Setup(x => x.GetByIdAsync(_productId))
            .ReturnsAsync(product);
        _mockCartRepository.Setup(x => x.GetByUserIdWithItemsAsync(_userId))
            .ReturnsAsync(cart);

        // Act
        var result = await _cartService.AddToCartAsync(_userId, command);

        // Assert
        Assert.IsNotNull(result);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [TestMethod]
    public async Task AddToCartAsync_ProductNotFound_ThrowsProductNotFoundException()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new AddToCartCommand { ProductId = _productId, Quantity = 2 };

        _mockUserRepository.Setup(x => x.GetByIdAsync(_userId))
            .ReturnsAsync(user);
        _mockProductRepository.Setup(x => x.GetByIdAsync(_productId))
            .ReturnsAsync((Product?)null);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ProductNotFoundException>(
            () => _cartService.AddToCartAsync(_userId, command));
    }

    [TestMethod]
    public async Task AddToCartAsync_InsufficientStock_ThrowsProductOutOfStockException()
    {
        // Arrange
        var user = CreateTestUser();
        var product = CreateTestProduct(1); // Only 1 in stock
        var command = new AddToCartCommand { ProductId = _productId, Quantity = 5 }; // Requesting 5

        _mockUserRepository.Setup(x => x.GetByIdAsync(_userId))
            .ReturnsAsync(user);
        _mockProductRepository.Setup(x => x.GetByIdAsync(_productId))
            .ReturnsAsync(product);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ProductOutOfStockException>(
            () => _cartService.AddToCartAsync(_userId, command));
    }

    [TestMethod]
    public async Task AddToCartAsync_NoExistingCart_CreatesNewCart()
    {
        // Arrange
        var user = CreateTestUser();
        var product = CreateTestProduct(10);
        var command = new AddToCartCommand { ProductId = _productId, Quantity = 2 };

        _mockUserRepository.Setup(x => x.GetByIdAsync(_userId))
            .ReturnsAsync(user);
        _mockProductRepository.Setup(x => x.GetByIdAsync(_productId))
            .ReturnsAsync(product);
        _mockCartRepository.Setup(x => x.GetByUserIdWithItemsAsync(_userId))
            .ReturnsAsync((Cart?)null);
        _mockCartRepository.Setup(x => x.AddAsync(It.IsAny<Cart>()))
            .ReturnsAsync((Cart cart) => cart);

        // Act
        var result = await _cartService.AddToCartAsync(_userId, command);

        // Assert
        Assert.IsNotNull(result);
        _mockCartRepository.Verify(x => x.AddAsync(It.IsAny<Cart>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [TestMethod]
    public async Task UpdateCartItemAsync_ValidRequest_UpdatesQuantity()
    {
        // Arrange
        var user = CreateTestUser();
        var product = CreateTestProduct(10);
        var cart = CreateTestCartWithItem();
        var command = new UpdateCartItemCommand { ProductId = _productId, Quantity = 5 };

        _mockUserRepository.Setup(x => x.GetByIdAsync(_userId))
            .ReturnsAsync(user);
        _mockProductRepository.Setup(x => x.GetByIdAsync(_productId))
            .ReturnsAsync(product);
        _mockCartRepository.Setup(x => x.GetByUserIdWithItemsAsync(_userId))
            .ReturnsAsync(cart);

        // Act
        var result = await _cartService.UpdateCartItemAsync(_userId, command);

        // Assert
        Assert.IsNotNull(result);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [TestMethod]
    public async Task UpdateCartItemAsync_CartNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new UpdateCartItemCommand { ProductId = _productId, Quantity = 5 };

        _mockUserRepository.Setup(x => x.GetByIdAsync(_userId))
            .ReturnsAsync(user);
        _mockCartRepository.Setup(x => x.GetByUserIdWithItemsAsync(_userId))
            .ReturnsAsync((Cart?)null);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => _cartService.UpdateCartItemAsync(_userId, command));
    }

    [TestMethod]
    public async Task UpdateCartItemAsync_CartItemNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = CreateTestUser();
        var cart = CreateTestCart(); // Empty cart
        var command = new UpdateCartItemCommand { ProductId = _productId, Quantity = 5 };

        _mockUserRepository.Setup(x => x.GetByIdAsync(_userId))
            .ReturnsAsync(user);
        _mockCartRepository.Setup(x => x.GetByUserIdWithItemsAsync(_userId))
            .ReturnsAsync(cart);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => _cartService.UpdateCartItemAsync(_userId, command));
    }

    [TestMethod]
    public async Task RemoveFromCartAsync_ValidRequest_RemovesItem()
    {
        // Arrange
        var user = CreateTestUser();
        var cart = CreateTestCartWithItem();

        _mockUserRepository.Setup(x => x.GetByIdAsync(_userId))
            .ReturnsAsync(user);
        _mockCartRepository.Setup(x => x.GetByUserIdWithItemsAsync(_userId))
            .ReturnsAsync(cart);

        // Act
        await _cartService.RemoveFromCartAsync(_userId, _productId);

        // Assert
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [TestMethod]
    public async Task RemoveFromCartAsync_EmptyProductId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _cartService.RemoveFromCartAsync(_userId, Guid.Empty));
    }

    [TestMethod]
    public async Task ClearCartAsync_ValidRequest_ClearsCart()
    {
        // Arrange
        var user = CreateTestUser();
        var cart = CreateTestCartWithItem();

        _mockUserRepository.Setup(x => x.GetByIdAsync(_userId))
            .ReturnsAsync(user);
        _mockCartRepository.Setup(x => x.GetByUserIdWithItemsAsync(_userId))
            .ReturnsAsync(cart);

        // Act
        await _cartService.ClearCartAsync(_userId);

        // Assert
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [TestMethod]
    public async Task HandleStockChangesAsync_StockReduced_AdjustsCartQuantities()
    {
        // Arrange
        var product = CreateTestProduct(5); // New stock is 5
        var cart = CreateTestCartWithItem(10); // Cart has 10 items
        var carts = new List<Cart> { cart };

        _mockProductRepository.Setup(x => x.GetByIdAsync(_productId))
            .ReturnsAsync(product);
        _mockCartRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(carts);

        // Act
        await _cartService.HandleStockChangesAsync(_productId, 5);

        // Assert
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [TestMethod]
    public async Task HandleStockChangesAsync_StockZero_RemovesItemFromCart()
    {
        // Arrange
        var product = CreateTestProduct(0); // No stock
        var cart = CreateTestCartWithItem(5);
        var carts = new List<Cart> { cart };

        _mockProductRepository.Setup(x => x.GetByIdAsync(_productId))
            .ReturnsAsync(product);
        _mockCartRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(carts);

        // Act
        await _cartService.HandleStockChangesAsync(_productId, 0);

        // Assert
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [TestMethod]
    public async Task HandleStockChangesAsync_EmptyProductId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _cartService.HandleStockChangesAsync(Guid.Empty, 10));
    }

    [TestMethod]
    public async Task HandleStockChangesAsync_NegativeStock_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _cartService.HandleStockChangesAsync(_productId, -1));
    }

    [TestMethod]
    public async Task AddToCartAsync_ExistingItemWouldExceedStock_ThrowsProductOutOfStockException()
    {
        // Arrange
        var user = CreateTestUser();
        var product = CreateTestProduct(5); // Only 5 in stock
        var cart = CreateTestCartWithItem(3); // Already has 3 items
        var command = new AddToCartCommand { ProductId = _productId, Quantity = 3 }; // Trying to add 3 more (total would be 6)

        _mockUserRepository.Setup(x => x.GetByIdAsync(_userId))
            .ReturnsAsync(user);
        _mockProductRepository.Setup(x => x.GetByIdAsync(_productId))
            .ReturnsAsync(product);
        _mockCartRepository.Setup(x => x.GetByUserIdWithItemsAsync(_userId))
            .ReturnsAsync(cart);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ProductOutOfStockException>(
            () => _cartService.AddToCartAsync(_userId, command));
    }

    [TestMethod]
    public async Task GetCartAsync_EmptyCart_ReturnsCartWithZeroTotal()
    {
        // Arrange
        var user = CreateTestUser();
        var cart = CreateTestCart(); // Empty cart
        
        _mockUserRepository.Setup(x => x.GetByIdAsync(_userId))
            .ReturnsAsync(user);
        _mockCartRepository.Setup(x => x.GetByUserIdWithItemsAsync(_userId))
            .ReturnsAsync(cart);

        // Act
        var result = await _cartService.GetCartAsync(_userId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Total);
        Assert.AreEqual(0, result.ItemCount);
        Assert.AreEqual(0, result.Items.Count);
    }

    [TestMethod]
    public async Task HandleStockChangesAsync_NoCartsWithProduct_NoChangesNeeded()
    {
        // Arrange
        var product = CreateTestProduct(5);
        var emptyCartList = new List<Cart>(); // No carts

        _mockProductRepository.Setup(x => x.GetByIdAsync(_productId))
            .ReturnsAsync(product);
        _mockCartRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(emptyCartList);

        // Act
        await _cartService.HandleStockChangesAsync(_productId, 5);

        // Assert
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Never);
    }

    [TestMethod]
    public async Task HandleStockChangesAsync_ProductNotFound_NoChangesNeeded()
    {
        // Arrange
        _mockProductRepository.Setup(x => x.GetByIdAsync(_productId))
            .ReturnsAsync((Product?)null);
        _mockCartRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Cart>());

        // Act
        await _cartService.HandleStockChangesAsync(_productId, 5);

        // Assert
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Never);
    }

    private User CreateTestUser()
    {
        // Use a valid BCrypt hash format for testing (this is a hash of "password123")
        var validBCryptHash = "$2a$11$K2CtDP7zSGOKgjXjxD8eAOqP9QzJvd1/JFCdEYNZZjgUYzZzZzZzZ";
        return User.Create("test@example.com", validBCryptHash, "Test User");
    }

    private Product CreateTestProduct(int stock)
    {
        var product = Product.Create("Test Product", "Test Description", 10.00m, stock, "Electronics");
        // Use reflection to set the Id for testing
        typeof(Product).GetProperty("Id")?.SetValue(product, _productId);
        return product;
    }

    private Cart CreateTestCart()
    {
        var cart = Cart.Create(_userId);
        // Use reflection to set the Id for testing
        typeof(Cart).GetProperty("Id")?.SetValue(cart, _cartId);
        return cart;
    }

    private Cart CreateTestCartWithItem(int quantity = 3)
    {
        var cart = CreateTestCart();
        var product = CreateTestProduct(20); // Plenty of stock for testing
        cart.AddItem(product, quantity);
        return cart;
    }
}