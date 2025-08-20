using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Infrastructure.Data;
using OnlineShoppingSystem.Infrastructure.Data.Repositories;

namespace OnlineShoppingSystem.Tests.Unit.Infrastructure.Repositories;

[TestClass]
public class UserRepositoryTests
{
    private ApplicationDbContext _context = null!;
    private UserRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new UserRepository(_context);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    [TestMethod]
    public async Task GetByEmailAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var user = User.Create("test@example.com", "$2a$11$K2iBIg2badO1KdAKf.tzQOuVRxQNi8u9gE.8TQqyWsRfwCCkJroS2", "Test User");
        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync("test@example.com");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test@example.com", result.Email);
        Assert.AreEqual("Test User", result.FullName);
    }

    [TestMethod]
    public async Task GetByEmailAsync_NonExistingUser_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByEmailAsync("nonexisting@example.com");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task EmailExistsAsync_ExistingEmail_ReturnsTrue()
    {
        // Arrange
        var user = User.Create("test@example.com", "$2a$11$K2iBIg2badO1KdAKf.tzQOuVRxQNi8u9gE.8TQqyWsRfwCCkJroS2", "Test User");
        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.EmailExistsAsync("test@example.com");

        // Assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task EmailExistsAsync_NonExistingEmail_ReturnsFalse()
    {
        // Act
        var exists = await _repository.EmailExistsAsync("nonexisting@example.com");

        // Assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task GetByIdAsync_UserWithCart_ReturnsUserWithCart()
    {
        // Arrange
        var user = User.Create("test@example.com", "$2a$11$K2iBIg2badO1KdAKf.tzQOuVRxQNi8u9gE.8TQqyWsRfwCCkJroS2", "Test User");
        var cart = Cart.Create(user.Id);
        user.AssignCart(cart);
        
        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Cart);
        Assert.AreEqual(user.Id, result.Cart.UserId);
    }
}