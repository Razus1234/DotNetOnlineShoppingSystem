using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Infrastructure.Data;

namespace OnlineShoppingSystem.Tests.Unit.Infrastructure;

[TestClass]
public class UnitOfWorkTests
{
    private ApplicationDbContext _context = null!;
    private UnitOfWork _unitOfWork = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _unitOfWork.Dispose();
        _context.Dispose();
    }

    [TestMethod]
    public void Repositories_AccessedMultipleTimes_ReturnsSameInstance()
    {
        // Act
        var users1 = _unitOfWork.Users;
        var users2 = _unitOfWork.Users;
        var products1 = _unitOfWork.Products;
        var products2 = _unitOfWork.Products;

        // Assert
        Assert.AreSame(users1, users2);
        Assert.AreSame(products1, products2);
    }

    [TestMethod]
    public async Task SaveChangesAsync_WithChanges_ReturnsSaveCount()
    {
        // Arrange
        var user = User.Create("test@example.com", "$2a$11$K2iBIg2badO1KdAKf.tzQOuVRxQNi8u9gE.8TQqyWsRfwCCkJroS2", "Test User");
        await _unitOfWork.Users.AddAsync(user);

        // Act
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        Assert.AreEqual(2, result); // User + Cart (created automatically)
    }

    [TestMethod]
    public async Task Transaction_CommitChanges_PersistsData()
    {
        // Arrange
        var user = User.Create("test@example.com", "$2a$11$K2iBIg2badO1KdAKf.tzQOuVRxQNi8u9gE.8TQqyWsRfwCCkJroS2", "Test User");

        // Act - Skip transaction for in-memory database
        try
        {
            await _unitOfWork.BeginTransactionAsync();
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Transactions are not supported"))
        {
            // In-memory database doesn't support transactions, so just test without transaction
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }

        // Assert
        var savedUser = await _unitOfWork.Users.GetByEmailAsync("test@example.com");
        Assert.IsNotNull(savedUser);
        Assert.AreEqual("Test User", savedUser.FullName);
    }

    [TestMethod]
    public async Task Transaction_RollbackChanges_DoesNotPersistData()
    {
        // Arrange
        var user = User.Create("test@example.com", "$2a$11$K2iBIg2badO1KdAKf.tzQOuVRxQNi8u9gE.8TQqyWsRfwCCkJroS2", "Test User");

        // Act - Skip transaction for in-memory database
        try
        {
            await _unitOfWork.BeginTransactionAsync();
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.RollbackTransactionAsync();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Transactions are not supported"))
        {
            // In-memory database doesn't support transactions, so we can't test rollback
            // This test would need a real database to work properly
            Assert.Inconclusive("Transaction rollback cannot be tested with in-memory database");
            return;
        }

        // Assert
        var savedUser = await _unitOfWork.Users.GetByEmailAsync("test@example.com");
        Assert.IsNull(savedUser);
    }

    [TestMethod]
    public async Task MultipleRepositories_WorkTogether_MaintainConsistency()
    {
        // Arrange
        var user = User.Create("test@example.com", "$2a$11$K2iBIg2badO1KdAKf.tzQOuVRxQNi8u9gE.8TQqyWsRfwCCkJroS2", "Test User");
        var product = Product.Create("Test Product", "Test Description", 10.99m, 5, "Electronics");

        // Act - Skip transaction for in-memory database
        try
        {
            await _unitOfWork.BeginTransactionAsync();
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync(); // Save first to persist user and cart
            
            var cart = await _unitOfWork.Carts.GetByUserIdAsync(user.Id);
            if (cart != null)
            {
                cart.AddItem(product, 2);
                await _unitOfWork.SaveChangesAsync(); // Save cart changes
            }
            
            await _unitOfWork.CommitTransactionAsync();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Transactions are not supported"))
        {
            // In-memory database doesn't support transactions, so just test without transaction
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync(); // Save first to persist user and cart
            
            var cart = await _unitOfWork.Carts.GetByUserIdAsync(user.Id);
            if (cart != null)
            {
                cart.AddItem(product, 2);
                await _unitOfWork.SaveChangesAsync(); // Save cart changes
            }
        }

        // Assert
        var savedUser = await _unitOfWork.Users.GetByEmailAsync("test@example.com");
        var savedProduct = await _unitOfWork.Products.GetByIdAsync(product.Id);
        var savedCart = await _unitOfWork.Carts.GetByUserIdWithItemsAsync(user.Id);
        
        Assert.IsNotNull(savedUser);
        Assert.IsNotNull(savedProduct);
        Assert.IsNotNull(savedCart);
        Assert.AreEqual(1, savedCart.Items.Count);
    }
}