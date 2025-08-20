using Microsoft.VisualStudio.TestTools.UnitTesting;
using OnlineShoppingSystem.Domain.Entities;
using System.Net;
using System.Text.Json;

namespace OnlineShoppingSystem.Tests.Integration.Controllers;

[TestClass]
public class ProductsControllerTests : BaseControllerTest
{
    [TestInitialize]
    public async Task Setup()
    {
        // Seed some test products
        var products = new[]
        {
            Product.Create("Test Product 1", "Description 1", 10.99m, 100, "Electronics"),
            Product.Create("Test Product 2", "Description 2", 20.99m, 50, "Books"),
            Product.Create("Search Product", "Searchable description", 15.99m, 25, "Electronics")
        };

        DbContext.Products.AddRange(products);
        await DbContext.SaveChangesAsync();
    }

    [TestMethod]
    public async Task GetProducts_ReturnsProductList()
    {
        // Act
        var response = await Client.GetAsync("/api/products");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var result = await DeserializeResponseAsync<PagedResultResponse<ProductResponse>>(response);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Items.Count > 0);
    }

    [TestMethod]
    public async Task GetProducts_WithPagination_ReturnsPagedResults()
    {
        // Act
        var response = await Client.GetAsync("/api/products?page=1&pageSize=2");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var result = await DeserializeResponseAsync<PagedResultResponse<ProductResponse>>(response);
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Items.Count);
        Assert.AreEqual(1, result.Page);
        Assert.AreEqual(2, result.PageSize);
    }

    [TestMethod]
    public async Task GetProduct_ExistingId_ReturnsProduct()
    {
        // Arrange
        var product = DbContext.Products.First();

        // Act
        var response = await Client.GetAsync($"/api/products/{product.Id}");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var result = await DeserializeResponseAsync<ProductResponse>(response);
        Assert.IsNotNull(result);
        Assert.AreEqual(product.Id, result.Id);
        Assert.AreEqual(product.Name, result.Name);
    }

    [TestMethod]
    public async Task GetProduct_NonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await Client.GetAsync($"/api/products/{Guid.NewGuid()}");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task SearchProducts_WithKeyword_ReturnsMatchingProducts()
    {
        // Act
        var response = await Client.GetAsync("/api/products/search?keyword=Search");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var products = await DeserializeResponseAsync<List<ProductResponse>>(response);
        Assert.IsNotNull(products);
        Assert.IsTrue(products.Any(p => p.Name.Contains("Search")));
    }

    [TestMethod]
    public async Task SearchProducts_EmptyKeyword_ReturnsBadRequest()
    {
        // Act
        var response = await Client.GetAsync("/api/products/search?keyword=");

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task GetProductsByCategory_ValidCategory_ReturnsProducts()
    {
        // Act
        var response = await Client.GetAsync("/api/products/category/Electronics");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var products = await DeserializeResponseAsync<List<ProductResponse>>(response);
        Assert.IsNotNull(products);
        Assert.IsTrue(products.All(p => p.Category == "Electronics"));
    }

    [TestMethod]
    public async Task CheckStock_ValidProduct_ReturnsStockStatus()
    {
        // Arrange
        var product = DbContext.Products.First();

        // Act
        var response = await Client.GetAsync($"/api/products/{product.Id}/stock?quantity=5");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var result = await DeserializeResponseAsync<StockCheckResponse>(response);
        Assert.IsNotNull(result);
        Assert.AreEqual(product.Id, result.ProductId);
        Assert.AreEqual(5, result.RequestedQuantity);
        Assert.IsTrue(result.IsInStock); // Should be true since test products have stock
    }

    [TestMethod]
    public async Task CheckStock_InvalidQuantity_ReturnsBadRequest()
    {
        // Arrange
        var product = DbContext.Products.First();

        // Act
        var response = await Client.GetAsync($"/api/products/{product.Id}/stock?quantity=0");

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Note: Admin-only endpoints (Create, Update, Delete, UpdateStock) would require
    // admin role setup which is not implemented in this basic test setup.
    // These would be tested with proper admin user authentication.

    private class ProductResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Category { get; set; } = string.Empty;
        public List<string> ImageUrls { get; set; } = new();
    }

    private class PagedResultResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    private class StockCheckResponse
    {
        public Guid ProductId { get; set; }
        public int RequestedQuantity { get; set; }
        public bool IsInStock { get; set; }
    }
}