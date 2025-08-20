using Microsoft.VisualStudio.TestTools.UnitTesting;
using OnlineShoppingSystem.Domain.Entities;
using System.Net;

namespace OnlineShoppingSystem.Tests.Integration.Controllers;

[TestClass]
public class CartControllerTests : BaseControllerTest
{
    private Product _testProduct = null!;

    [TestInitialize]
    public async Task Setup()
    {
        // Seed a test product
        _testProduct = Product.Create("Test Product", "Test Description", 19.99m, 100, "Test Category");
        DbContext.Products.Add(_testProduct);
        await DbContext.SaveChangesAsync();
    }

    [TestMethod]
    public async Task GetCart_WithValidToken_ReturnsCart()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        // Act
        var response = await Client.GetAsync("/api/cart");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var cart = await DeserializeResponseAsync<CartResponse>(response);
        Assert.IsNotNull(cart);
        Assert.IsNotNull(cart.Items);
    }

    [TestMethod]
    public async Task GetCart_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/cart");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task AddToCart_ValidProduct_ReturnsUpdatedCart()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        var request = new
        {
            ProductId = _testProduct.Id,
            Quantity = 2
        };

        // Act
        var response = await Client.PostAsync("/api/cart/items", CreateJsonContent(request));

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var cart = await DeserializeResponseAsync<CartResponse>(response);
        Assert.IsNotNull(cart);
        Assert.IsTrue(cart.Items.Any(i => i.ProductId == _testProduct.Id));
        Assert.AreEqual(2, cart.Items.First(i => i.ProductId == _testProduct.Id).Quantity);
    }

    [TestMethod]
    public async Task AddToCart_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            ProductId = _testProduct.Id,
            Quantity = 2
        };

        // Act
        var response = await Client.PostAsync("/api/cart/items", CreateJsonContent(request));

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task AddToCart_InvalidQuantity_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        var request = new
        {
            ProductId = _testProduct.Id,
            Quantity = 0 // Invalid quantity
        };

        // Act
        var response = await Client.PostAsync("/api/cart/items", CreateJsonContent(request));

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateCartItem_ValidUpdate_ReturnsUpdatedCart()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        // First add item to cart
        var addRequest = new
        {
            ProductId = _testProduct.Id,
            Quantity = 2
        };
        await Client.PostAsync("/api/cart/items", CreateJsonContent(addRequest));

        // Now update the quantity
        var updateRequest = new
        {
            ProductId = _testProduct.Id,
            Quantity = 5
        };

        // Act
        var response = await Client.PutAsync($"/api/cart/items/{_testProduct.Id}", CreateJsonContent(updateRequest));

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var cart = await DeserializeResponseAsync<CartResponse>(response);
        Assert.IsNotNull(cart);
        Assert.AreEqual(5, cart.Items.First(i => i.ProductId == _testProduct.Id).Quantity);
    }

    [TestMethod]
    public async Task UpdateCartItem_MismatchedProductId_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        var updateRequest = new
        {
            ProductId = Guid.NewGuid(), // Different from URL
            Quantity = 5
        };

        // Act
        var response = await Client.PutAsync($"/api/cart/items/{_testProduct.Id}", CreateJsonContent(updateRequest));

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task RemoveFromCart_ExistingItem_ReturnsNoContent()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        // First add item to cart
        var addRequest = new
        {
            ProductId = _testProduct.Id,
            Quantity = 2
        };
        await Client.PostAsync("/api/cart/items", CreateJsonContent(addRequest));

        // Act
        var response = await Client.DeleteAsync($"/api/cart/items/{_testProduct.Id}");

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    [TestMethod]
    public async Task ClearCart_WithItems_ReturnsNoContent()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        // First add item to cart
        var addRequest = new
        {
            ProductId = _testProduct.Id,
            Quantity = 2
        };
        await Client.PostAsync("/api/cart/items", CreateJsonContent(addRequest));

        // Act
        var response = await Client.DeleteAsync("/api/cart");

        // Assert
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }

    [TestMethod]
    public async Task ClearCart_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.DeleteAsync("/api/cart");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private class CartResponse
    {
        public Guid Id { get; set; }
        public List<CartItemResponse> Items { get; set; } = new();
        public decimal Total { get; set; }
        public int ItemCount { get; set; }
    }

    private class CartItemResponse
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
    }
}