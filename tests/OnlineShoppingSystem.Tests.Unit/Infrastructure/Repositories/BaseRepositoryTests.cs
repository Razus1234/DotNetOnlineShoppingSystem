using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Infrastructure.Data;
using OnlineShoppingSystem.Infrastructure.Data.Repositories;

namespace OnlineShoppingSystem.Tests.Unit.Infrastructure.Repositories;

[TestClass]
public class BaseRepositoryTests
{
    private ApplicationDbContext _context = null!;
    private BaseRepository<Product> _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new BaseRepository<Product>(_context);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    [TestMethod]
    public async Task AddAsync_ValidEntity_ReturnsEntity()
    {
        // Arrange
        var product = Product.Create("Test Product", "Test Description", 10.99m, 5, "Electronics");

        // Act
        var result = await _repository.AddAsync(product);
        await _context.SaveChangesAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(product.Id, result.Id);
        Assert.AreEqual("Test Product", result.Name);
    }

    [TestMethod]
    public async Task GetByIdAsync_ExistingEntity_ReturnsEntity()
    {
        // Arrange
        var product = Product.Create("Test Product", "Test Description", 10.99m, 5, "Electronics");
        await _repository.AddAsync(product);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(product.Id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(product.Id, result.Id);
        Assert.AreEqual("Test Product", result.Name);
    }

    [TestMethod]
    public async Task GetByIdAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistingId);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetAllAsync_MultipleEntities_ReturnsAllEntities()
    {
        // Arrange
        var product1 = Product.Create("Product 1", "Description 1", 10.99m, 5, "Electronics");
        var product2 = Product.Create("Product 2", "Description 2", 20.99m, 3, "Books");
        
        await _repository.AddAsync(product1);
        await _repository.AddAsync(product2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.AreEqual(2, result.Count());
    }

    [TestMethod]
    public async Task UpdateAsync_ExistingEntity_UpdatesEntity()
    {
        // Arrange
        var product = Product.Create("Original Name", "Original Description", 10.99m, 5, "Electronics");
        await _repository.AddAsync(product);
        await _context.SaveChangesAsync();

        // Act
        product.UpdateDetails("Updated Name", "Updated Description", new OnlineShoppingSystem.Domain.ValueObjects.Money(15.99m), "Books");
        await _repository.UpdateAsync(product);
        await _context.SaveChangesAsync();

        // Assert
        var updatedProduct = await _repository.GetByIdAsync(product.Id);
        Assert.IsNotNull(updatedProduct);
        Assert.AreEqual("Updated Name", updatedProduct.Name);
        Assert.AreEqual("Updated Description", updatedProduct.Description);
        Assert.AreEqual(15.99m, updatedProduct.Price.Amount);
    }

    [TestMethod]
    public async Task DeleteAsync_ExistingEntity_RemovesEntity()
    {
        // Arrange
        var product = Product.Create("Test Product", "Test Description", 10.99m, 5, "Electronics");
        await _repository.AddAsync(product);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(product.Id);
        await _context.SaveChangesAsync();

        // Assert
        var deletedProduct = await _repository.GetByIdAsync(product.Id);
        Assert.IsNull(deletedProduct);
    }

    [TestMethod]
    public async Task ExistsAsync_ExistingEntity_ReturnsTrue()
    {
        // Arrange
        var product = Product.Create("Test Product", "Test Description", 10.99m, 5, "Electronics");
        await _repository.AddAsync(product);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.ExistsAsync(product.Id);

        // Assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsAsync_NonExistingEntity_ReturnsFalse()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var exists = await _repository.ExistsAsync(nonExistingId);

        // Assert
        Assert.IsFalse(exists);
    }
}