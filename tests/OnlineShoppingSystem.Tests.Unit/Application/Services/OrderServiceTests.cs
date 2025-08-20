using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using OnlineShoppingSystem.Application.Commands.Order;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.Common.Models;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Queries;
using OnlineShoppingSystem.Application.Services;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.Enums;
using OnlineShoppingSystem.Domain.Exceptions;
using OnlineShoppingSystem.Domain.ValueObjects;

namespace OnlineShoppingSystem.Tests.Unit.Application.Services;

[TestClass]
public class OrderServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<IMapper> _mockMapper;
    private Mock<ILogger<OrderService>> _mockLogger;
    private Mock<IUserRepository> _mockUserRepository;
    private Mock<IProductRepository> _mockProductRepository;
    private Mock<ICartRepository> _mockCartRepository;
    private Mock<IOrderRepository> _mockOrderRepository;
    private OrderService _orderService;

    [TestInitialize]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<OrderService>>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockProductRepository = new Mock<IProductRepository>();
        _mockCartRepository = new Mock<ICartRepository>();
        _mockOrderRepository = new Mock<IOrderRepository>();

        _mockUnitOfWork.Setup(x => x.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(x => x.Products).Returns(_mockProductRepository.Object);
        _mockUnitOfWork.Setup(x => x.Carts).Returns(_mockCartRepository.Object);
        _mockUnitOfWork.Setup(x => x.Orders).Returns(_mockOrderRepository.Object);

        _orderService = new OrderService(_mockUnitOfWork.Object, _mockMapper.Object, _mockLogger.Object);
    }

    [TestMethod]
    public async Task PlaceOrderAsync_ValidOrder_ReturnsOrderDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = new PlaceOrderCommand
        {
            ShippingAddress = new AddressDto
            {
                Street = "123 Main St",
                City = "Test City",
                PostalCode = "12345",
                Country = "USA"
            }
        };

        var user = User.Create("test@example.com", "$2a$11$K2CtDP7zSGOKgjXjxD8eYO.r7Cl1eKIoLlNGYNFGqjQQQjKQf0p.2", "Test User");
        var product = Product.Create("Test Product", "Test Description", 10.00m, 5, "Electronics");
        var cart = Cart.Create(userId);
        cart.AddItem(product, 2);
        
        // Get the actual product ID from the cart item
        productId = cart.Items.First().ProductId;

        var expectedOrder = new Order(userId, new Address("123 Main St", "Test City", "12345", "USA"), 
            new List<OrderItem> { new OrderItem(Guid.NewGuid(), productId, "Test Product", new Money(10.00m), 2) });
        var expectedOrderDto = new OrderDto { Id = expectedOrder.Id, UserId = userId, Status = OrderStatus.Pending };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockCartRepository.Setup(x => x.GetByUserIdWithItemsAsync(userId)).ReturnsAsync(cart);
        _mockProductRepository.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockOrderRepository.Setup(x => x.AddAsync(It.IsAny<Order>())).ReturnsAsync(expectedOrder);
        _mockMapper.Setup(x => x.Map<Address>(command.ShippingAddress)).Returns(new Address("123 Main St", "Test City", "12345", "USA"));
        _mockMapper.Setup(x => x.Map<OrderDto>(It.IsAny<Order>())).Returns(expectedOrderDto);

        // Act
        var result = await _orderService.PlaceOrderAsync(userId, command);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedOrderDto.Id, result.Id);
        Assert.AreEqual(userId, result.UserId);
        Assert.AreEqual(OrderStatus.Pending, result.Status);

        _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
        _mockCartRepository.Verify(x => x.UpdateAsync(It.IsAny<Cart>()), Times.Once);
        _mockProductRepository.Verify(x => x.UpdateAsync(It.IsAny<Product>()), Times.Once);
    }

    [TestMethod]
    public async Task PlaceOrderAsync_UserNotFound_ThrowsUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new PlaceOrderCommand
        {
            ShippingAddress = new AddressDto { Street = "123 Main St", City = "Test City", PostalCode = "12345", Country = "USA" }
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<UserNotFoundException>(() => 
            _orderService.PlaceOrderAsync(userId, command));
    }

    [TestMethod]
    public async Task PlaceOrderAsync_EmptyCart_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new PlaceOrderCommand
        {
            ShippingAddress = new AddressDto { Street = "123 Main St", City = "Test City", PostalCode = "12345", Country = "USA" }
        };

        var user = User.Create("test@example.com", "$2a$11$K2CtDP7zSGOKgjXjxD8eYO.r7Cl1eKIoLlNGYNFGqjQQQjKQf0p.2", "Test User");
        var emptyCart = Cart.Create(userId);

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockCartRepository.Setup(x => x.GetByUserIdWithItemsAsync(userId)).ReturnsAsync(emptyCart);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => 
            _orderService.PlaceOrderAsync(userId, command));
    }

    [TestMethod]
    public async Task PlaceOrderAsync_InsufficientStock_ThrowsProductOutOfStockException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = new PlaceOrderCommand
        {
            ShippingAddress = new AddressDto { Street = "123 Main St", City = "Test City", PostalCode = "12345", Country = "USA" }
        };

        var user = User.Create("test@example.com", "$2a$11$K2CtDP7zSGOKgjXjxD8eYO.r7Cl1eKIoLlNGYNFGqjQQQjKQf0p.2", "Test User");
        var product = Product.Create("Test Product", "Test Description", 10.00m, 1, "Electronics"); // Only 1 in stock
        var cart = Cart.Create(userId);
        cart.AddItem(product, 1);
        
        // Get the actual product ID from the cart item
        productId = cart.Items.First().ProductId;
        
        // Reduce stock to 0 to simulate concurrent purchase
        product.ReduceStock(1);

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockCartRepository.Setup(x => x.GetByUserIdWithItemsAsync(userId)).ReturnsAsync(cart);
        _mockProductRepository.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ProductOutOfStockException>(() => 
            _orderService.PlaceOrderAsync(userId, command));

        _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }

    [TestMethod]
    public async Task GetOrderHistoryAsync_ValidUser_ReturnsPagedOrders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new OrderQuery { PageNumber = 1, PageSize = 10 };
        var user = User.Create("test@example.com", "$2a$11$K2CtDP7zSGOKgjXjxD8eYO.r7Cl1eKIoLlNGYNFGqjQQQjKQf0p.2", "Test User");
        
        var orders = new List<Order>
        {
            new Order(userId, new Address("123 Main St", "Test City", "12345", "USA"), 
                new List<OrderItem> { new OrderItem(Guid.NewGuid(), Guid.NewGuid(), "Product 1", new Money(10.00m), 1) })
        };
        
        var pagedOrders = new PagedResult<Order>(orders, 1, 1, 10);
        var orderDtos = new List<OrderDto> { new OrderDto { Id = orders[0].Id, UserId = userId } };
        var expectedResult = new PagedResult<OrderDto>
        {
            Items = orderDtos,
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockOrderRepository.Setup(x => x.GetByUserIdAsync(userId, query)).ReturnsAsync(pagedOrders);
        _mockMapper.Setup(x => x.Map<List<OrderDto>>(orders)).Returns(orderDtos);

        // Act
        var result = await _orderService.GetOrderHistoryAsync(userId, query);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.TotalCount);
        Assert.AreEqual(1, result.Items.Count());
        Assert.AreEqual(orders[0].Id, result.Items.First().Id);
    }

    [TestMethod]
    public async Task GetOrderByIdAsync_ValidOrderId_ReturnsOrderDto()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var order = new Order(userId, new Address("123 Main St", "Test City", "12345", "USA"), 
            new List<OrderItem> { new OrderItem(Guid.NewGuid(), Guid.NewGuid(), "Product 1", new Money(10.00m), 1) });
        var orderDto = new OrderDto { Id = orderId, UserId = userId, Status = OrderStatus.Pending };

        _mockOrderRepository.Setup(x => x.GetByIdWithItemsAsync(orderId)).ReturnsAsync(order);
        _mockMapper.Setup(x => x.Map<OrderDto>(order)).Returns(orderDto);

        // Act
        var result = await _orderService.GetOrderByIdAsync(orderId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(orderId, result.Id);
        Assert.AreEqual(userId, result.UserId);
    }

    [TestMethod]
    public async Task GetOrderByIdAsync_OrderNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockOrderRepository.Setup(x => x.GetByIdWithItemsAsync(orderId)).ReturnsAsync((Order?)null);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => 
            _orderService.GetOrderByIdAsync(orderId));
    }

    [TestMethod]
    public async Task CancelOrderAsync_ValidOrder_CancelsOrderAndRestoresStock()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        var product = Product.Create("Test Product", "Test Description", 10.00m, 5, "Electronics");
        var order = new Order(userId, new Address("123 Main St", "Test City", "12345", "USA"), 
            new List<OrderItem> { new OrderItem(Guid.NewGuid(), productId, "Test Product", new Money(10.00m), 2) });
        
        var orderDto = new OrderDto { Id = orderId, Status = OrderStatus.Cancelled };

        _mockOrderRepository.Setup(x => x.GetByIdWithItemsAsync(orderId)).ReturnsAsync(order);
        _mockProductRepository.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockMapper.Setup(x => x.Map<OrderDto>(order)).Returns(orderDto);

        // Act
        var result = await _orderService.CancelOrderAsync(orderId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(OrderStatus.Cancelled, result.Status);

        _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
        _mockProductRepository.Verify(x => x.UpdateAsync(product), Times.Once);
        _mockOrderRepository.Verify(x => x.UpdateAsync(order), Times.Once);
    }

    [TestMethod]
    public async Task CancelOrderAsync_OrderNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockOrderRepository.Setup(x => x.GetByIdWithItemsAsync(orderId)).ReturnsAsync((Order?)null);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => 
            _orderService.CancelOrderAsync(orderId));
    }

    [TestMethod]
    public async Task CancelOrderAsync_OrderCannotBeCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var order = new Order(userId, new Address("123 Main St", "Test City", "12345", "USA"), 
            new List<OrderItem> { new OrderItem(Guid.NewGuid(), Guid.NewGuid(), "Test Product", new Money(10.00m), 1) });
        
        // Set order to shipped status (cannot be cancelled)
        order.UpdateStatus(OrderStatus.Confirmed);
        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Shipped);

        _mockOrderRepository.Setup(x => x.GetByIdWithItemsAsync(orderId)).ReturnsAsync(order);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => 
            _orderService.CancelOrderAsync(orderId));
    }

    [TestMethod]
    public async Task UpdateOrderStatusAsync_ValidTransition_UpdatesOrderStatus()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var newStatus = OrderStatus.Confirmed;
        
        var order = new Order(userId, new Address("123 Main St", "Test City", "12345", "USA"), 
            new List<OrderItem> { new OrderItem(Guid.NewGuid(), Guid.NewGuid(), "Test Product", new Money(10.00m), 1) });
        
        var orderDto = new OrderDto { Id = orderId, Status = newStatus };

        _mockOrderRepository.Setup(x => x.GetByIdWithItemsAsync(orderId)).ReturnsAsync(order);
        _mockMapper.Setup(x => x.Map<OrderDto>(order)).Returns(orderDto);

        // Act
        var result = await _orderService.UpdateOrderStatusAsync(orderId, newStatus);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(newStatus, result.Status);

        _mockOrderRepository.Verify(x => x.UpdateAsync(order), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [TestMethod]
    public async Task UpdateOrderStatusAsync_InvalidTransition_ThrowsInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var invalidStatus = OrderStatus.Delivered; // Cannot go directly from Pending to Delivered
        
        var order = new Order(userId, new Address("123 Main St", "Test City", "12345", "USA"), 
            new List<OrderItem> { new OrderItem(Guid.NewGuid(), Guid.NewGuid(), "Test Product", new Money(10.00m), 1) });

        _mockOrderRepository.Setup(x => x.GetByIdWithItemsAsync(orderId)).ReturnsAsync(order);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => 
            _orderService.UpdateOrderStatusAsync(orderId, invalidStatus));
    }

    [TestMethod]
    public void Constructor_NullUnitOfWork_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new OrderService(null!, _mockMapper.Object, _mockLogger.Object));
    }

    [TestMethod]
    public void Constructor_NullMapper_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new OrderService(_mockUnitOfWork.Object, null!, _mockLogger.Object));
    }

    [TestMethod]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new OrderService(_mockUnitOfWork.Object, _mockMapper.Object, null!));
    }

    [TestMethod]
    public async Task PlaceOrderAsync_EmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var command = new PlaceOrderCommand
        {
            ShippingAddress = new AddressDto { Street = "123 Main St", City = "Test City", PostalCode = "12345", Country = "USA" }
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => 
            _orderService.PlaceOrderAsync(Guid.Empty, command));
    }

    [TestMethod]
    public async Task PlaceOrderAsync_NullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => 
            _orderService.PlaceOrderAsync(userId, null!));
    }
}