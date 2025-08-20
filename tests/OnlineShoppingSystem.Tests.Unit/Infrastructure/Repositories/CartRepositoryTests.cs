using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Infrastructure.Data;
using OnlineShoppingSystem.Infrastructure.Data.Repositories;

namespace OnlineShoppingSystem.Tests.Unit.Infrastructure.Repositories;

[TestClass]
public class CartRepositoryTests
{
    private ApplicationDbContext _context = null!;
    private CartRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new CartRepository(_context);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    [TestMethod]
    public async Task GetByUserIdAsync_ExistingCart_ReturnsCart()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = Cart.Create(userId);
        await _repository.AddAsync(cart);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(userId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(userId, result.UserId);
    }

    [TestMethod]
    public async Task GetByUserIdAsync_NonExistingCart_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByUserIdAsync(userId);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetByUserIdWithItemsAsync_CartWithItems_ReturnsCartWithItems()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = Cart.Create(userId);
        var product = Product.Create("Test Product", "Test Description", 10.99m, 5, "Electronics");
        
        await _context.Products.AddAsync(product);
        await _repository.AddAsync(cart);
        await _context.SaveChangesAsync();

        cart.AddItem(product, 2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdWithItemsAsync(userId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(userId, result.UserId);
        Assert.AreEqual(1, result.Items.Count);
        Assert.AreEqual(product.Id, result.Items.First().ProductId);
        Assert.AreEqual(2, result.Items.First().Quantity);
    }

    [TestMethod]
    public async Task GetByIdAsync_CartWithItems_ReturnsCartWithItems()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = Cart.Create(userId);
        var product = Product.Create("Test Product", "Test Description", 10.99m, 5, "Electronics");
        
        await _context.Products.AddAsync(product);
        await _repository.AddAsync(cart);
        await _context.SaveChangesAsync();

        cart.AddItem(product, 3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(cart.Id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(cart.Id, result.Id);
        Assert.AreEqual(1, result.Items.Count);
        Assert.AreEqual("Test Product", result.Items.First().ProductName);
    }
}