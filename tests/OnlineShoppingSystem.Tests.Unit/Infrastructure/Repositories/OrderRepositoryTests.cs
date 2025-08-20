using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.Queries;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.Enums;
using OnlineShoppingSystem.Infrastructure.Data;
using OnlineShoppingSystem.Infrastructure.Data.Repositories;

namespace OnlineShoppingSystem.Tests.Unit.Infrastructure.Repositories;

[TestClass]
public class OrderRepositoryTests
{
    private ApplicationDbContext _context = null!;
    private OrderRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new OrderRepository(_context);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    [TestMethod]
    public async Task GetByUserIdAsync_UserWithOrders_ReturnsPagedOrders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order1 = Order.Create(userId);
        var order2 = Order.Create(userId);
        var order3 = Order.Create(Guid.NewGuid()); // Different user
        
        await _repository.AddAsync(order1);
        await _repository.AddAsync(order2);
        await _repository.AddAsync(order3);
        await _context.SaveChangesAsync();

        var query = new OrderQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _repository.GetByUserIdAsync(userId, query);

        // Assert
        Assert.AreEqual(2, result.TotalCount);
        Assert.AreEqual(2, result.Items.Count());
        Assert.IsTrue(result.Items.All(o => o.UserId == userId));
    }

    [TestMethod]
    public async Task GetByUserIdAsync_WithStatusFilter_ReturnsFilteredOrders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order1 = Order.Create(userId);
        var order2 = Order.Create(userId);
        // Follow proper status transition: Pending -> Confirmed -> Processing -> Shipped
        order2.UpdateStatus(OrderStatus.Confirmed);
        order2.UpdateStatus(OrderStatus.Processing);
        order2.UpdateStatus(OrderStatus.Shipped);
        
        await _repository.AddAsync(order1);
        await _repository.AddAsync(order2);
        await _context.SaveChangesAsync();

        var query = new OrderQuery { Status = OrderStatus.Pending, PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _repository.GetByUserIdAsync(userId, query);

        // Assert
        Assert.AreEqual(1, result.TotalCount);
        Assert.AreEqual(1, result.Items.Count());
        Assert.AreEqual(OrderStatus.Pending, result.Items.First().Status);
    }

    [TestMethod]
    public async Task GetByUserIdAsync_WithDateFilter_ReturnsFilteredOrders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order1 = Order.Create(userId);
        var order2 = Order.Create(userId);
        
        await _repository.AddAsync(order1);
        await _repository.AddAsync(order2);
        await _context.SaveChangesAsync();

        var fromDate = DateTime.UtcNow.AddDays(-1);
        var toDate = DateTime.UtcNow.AddDays(1);
        var query = new OrderQuery { FromDate = fromDate, ToDate = toDate, PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _repository.GetByUserIdAsync(userId, query);

        // Assert
        Assert.AreEqual(2, result.TotalCount);
        Assert.AreEqual(2, result.Items.Count());
    }

    [TestMethod]
    public async Task GetByStatusAsync_ValidStatus_ReturnsMatchingOrders()
    {
        // Arrange
        var order1 = Order.Create(Guid.NewGuid());
        var order2 = Order.Create(Guid.NewGuid());
        var order3 = Order.Create(Guid.NewGuid());
        
        // Follow proper status transitions
        order2.UpdateStatus(OrderStatus.Confirmed);
        order2.UpdateStatus(OrderStatus.Processing);
        order2.UpdateStatus(OrderStatus.Shipped);
        
        order3.UpdateStatus(OrderStatus.Confirmed);
        order3.UpdateStatus(OrderStatus.Processing);
        order3.UpdateStatus(OrderStatus.Shipped);
        order3.UpdateStatus(OrderStatus.Delivered);
        
        await _repository.AddAsync(order1);
        await _repository.AddAsync(order2);
        await _repository.AddAsync(order3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStatusAsync(OrderStatus.Shipped);

        // Assert
        Assert.AreEqual(1, result.Count());
        Assert.AreEqual(OrderStatus.Shipped, result.First().Status);
    }

    [TestMethod]
    public async Task GetByIdWithItemsAsync_OrderWithItems_ReturnsOrderWithItems()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = Order.Create(userId);
        var product = Product.Create("Test Product", "Test Description", 10.99m, 5, "Electronics");
        
        await _context.Products.AddAsync(product);
        await _repository.AddAsync(order);
        await _context.SaveChangesAsync();

        order.AddItem(product, 2, 10.99m);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdWithItemsAsync(order.Id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(order.Id, result.Id);
        Assert.AreEqual(2, result.Items.Count); // Default item + added item
        Assert.IsTrue(result.Items.Any(i => i.ProductName == "Test Product"));
    }
}