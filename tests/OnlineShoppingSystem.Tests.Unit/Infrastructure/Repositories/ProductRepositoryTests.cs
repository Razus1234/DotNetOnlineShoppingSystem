using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.Queries;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Infrastructure.Data;
using OnlineShoppingSystem.Infrastructure.Data.Repositories;

namespace OnlineShoppingSystem.Tests.Unit.Infrastructure.Repositories;

[TestClass]
public class ProductRepositoryTests
{
    private ApplicationDbContext _context = null!;
    private ProductRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new ProductRepository(_context);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    [TestMethod]
    public async Task GetPagedAsync_WithKeywordFilter_ReturnsMatchingProducts()
    {
        // Arrange
        var product1 = Product.Create("Laptop Computer", "High-performance laptop", 999.99m, 5, "Electronics");
        var product2 = Product.Create("Desktop Computer", "Gaming desktop", 1299.99m, 3, "Electronics");
        var product3 = Product.Create("Book", "Programming book", 29.99m, 10, "Books");
        
        await _repository.AddAsync(product1);
        await _repository.AddAsync(product2);
        await _repository.AddAsync(product3);
        await _context.SaveChangesAsync();

        var query = new ProductQuery { Keyword = "Computer", PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _repository.GetPagedAsync(query);

        // Assert
        Assert.AreEqual(2, result.TotalCount);
        Assert.AreEqual(2, result.Items.Count());
        Assert.IsTrue(result.Items.All(p => p.Name.Contains("Computer")));
    }

    [TestMethod]
    public async Task GetPagedAsync_WithCategoryFilter_ReturnsMatchingProducts()
    {
        // Arrange
        var product1 = Product.Create("Laptop", "High-performance laptop", 999.99m, 5, "Electronics");
        var product2 = Product.Create("Phone", "Smartphone", 699.99m, 8, "Electronics");
        var product3 = Product.Create("Book", "Programming book", 29.99m, 10, "Books");
        
        await _repository.AddAsync(product1);
        await _repository.AddAsync(product2);
        await _repository.AddAsync(product3);
        await _context.SaveChangesAsync();

        var query = new ProductQuery { Category = "Electronics", PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _repository.GetPagedAsync(query);

        // Assert
        Assert.AreEqual(2, result.TotalCount);
        Assert.AreEqual(2, result.Items.Count());
        Assert.IsTrue(result.Items.All(p => p.Category == "Electronics"));
    }

    [TestMethod]
    public async Task GetPagedAsync_WithPriceRangeFilter_ReturnsMatchingProducts()
    {
        // Arrange
        var product1 = Product.Create("Cheap Item", "Low cost item", 10.99m, 5, "Electronics");
        var product2 = Product.Create("Mid Item", "Medium cost item", 50.99m, 8, "Electronics");
        var product3 = Product.Create("Expensive Item", "High cost item", 999.99m, 10, "Electronics");
        
        await _repository.AddAsync(product1);
        await _repository.AddAsync(product2);
        await _repository.AddAsync(product3);
        await _context.SaveChangesAsync();

        var query = new ProductQuery { MinPrice = 20m, MaxPrice = 100m, PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _repository.GetPagedAsync(query);

        // Assert
        Assert.AreEqual(1, result.TotalCount);
        Assert.AreEqual(1, result.Items.Count());
        Assert.AreEqual("Mid Item", result.Items.First().Name);
    }

    [TestMethod]
    public async Task GetPagedAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 15; i++)
        {
            var product = Product.Create($"Product {i}", $"Description {i}", 10.99m, 5, "Electronics");
            await _repository.AddAsync(product);
        }
        await _context.SaveChangesAsync();

        var query = new ProductQuery { PageNumber = 2, PageSize = 5 };

        // Act
        var result = await _repository.GetPagedAsync(query);

        // Assert
        Assert.AreEqual(15, result.TotalCount);
        Assert.AreEqual(5, result.Items.Count());
        Assert.AreEqual(2, result.Page);
        Assert.AreEqual(3, result.TotalPages);
        Assert.IsTrue(result.HasNextPage);
        Assert.IsTrue(result.HasPreviousPage);
    }

    [TestMethod]
    public async Task SearchAsync_WithKeyword_ReturnsMatchingProducts()
    {
        // Arrange
        var product1 = Product.Create("Gaming Laptop", "High-performance gaming laptop", 999.99m, 5, "Electronics");
        var product2 = Product.Create("Office Laptop", "Business laptop for office work", 699.99m, 3, "Electronics");
        var product3 = Product.Create("Desktop Computer", "Gaming desktop", 1299.99m, 2, "Electronics");
        
        await _repository.AddAsync(product1);
        await _repository.AddAsync(product2);
        await _repository.AddAsync(product3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SearchAsync("Laptop");

        // Assert
        Assert.AreEqual(2, result.Count());
        Assert.IsTrue(result.All(p => p.Name.Contains("Laptop") || p.Description.Contains("laptop")));
    }

    [TestMethod]
    public async Task GetByCategoryAsync_ValidCategory_ReturnsMatchingProducts()
    {
        // Arrange
        var product1 = Product.Create("Laptop", "Gaming laptop", 999.99m, 5, "Electronics");
        var product2 = Product.Create("Phone", "Smartphone", 699.99m, 8, "Electronics");
        var product3 = Product.Create("Book", "Programming book", 29.99m, 10, "Books");
        
        await _repository.AddAsync(product1);
        await _repository.AddAsync(product2);
        await _repository.AddAsync(product3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByCategoryAsync("Electronics");

        // Assert
        Assert.AreEqual(2, result.Count());
        Assert.IsTrue(result.All(p => p.Category == "Electronics"));
    }
}