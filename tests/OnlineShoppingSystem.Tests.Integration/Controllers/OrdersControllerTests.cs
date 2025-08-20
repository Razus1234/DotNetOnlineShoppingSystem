using Microsoft.VisualStudio.TestTools.UnitTesting;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.Enums;
using System.Net;

namespace OnlineShoppingSystem.Tests.Integration.Controllers;

[TestClass]
public class OrdersControllerTests : BaseControllerTest
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
    public async Task GetOrderHistory_WithValidToken_ReturnsOrders()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        // Act
        var response = await Client.GetAsync("/api/orders");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var result = await DeserializeResponseAsync<PagedResultResponse<OrderResponse>>(response);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Items);
    }

    [TestMethod]
    public async Task GetOrderHistory_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/orders");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetOrderHistory_WithPagination_ReturnsPagedResults()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        // Act
        var response = await Client.GetAsync("/api/orders?page=1&pageSize=5");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var result = await DeserializeResponseAsync<PagedResultResponse<OrderResponse>>(response);
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Page);
        Assert.AreEqual(5, result.PageSize);
    }

    [TestMethod]
    public async Task PlaceOrder_WithValidCart_ReturnsCreatedOrder()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        // First add item to cart
        var addToCartRequest = new
        {
            ProductId = _testProduct.Id,
            Quantity = 2
        };
        await Client.PostAsync("/api/cart/items", CreateJsonContent(addToCartRequest));

        // Place order
        var placeOrderRequest = new
        {
            ShippingAddress = new
            {
                Street = "123 Test St",
                City = "Test City",
                PostalCode = "12345",
                Country = "Test Country"
            }
        };

        // Act
        var response = await Client.PostAsync("/api/orders", CreateJsonContent(placeOrderRequest));

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        
        var order = await DeserializeResponseAsync<OrderResponse>(response);
        Assert.IsNotNull(order);
        Assert.AreEqual(OrderStatus.Pending, order.Status);
        Assert.IsTrue(order.Items.Any());
        Assert.IsTrue(order.Total > 0);
    }

    [TestMethod]
    public async Task PlaceOrder_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var placeOrderRequest = new
        {
            ShippingAddress = new
            {
                Street = "123 Test St",
                City = "Test City",
                PostalCode = "12345",
                Country = "Test Country"
            }
        };

        // Act
        var response = await Client.PostAsync("/api/orders", CreateJsonContent(placeOrderRequest));

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetOrder_ExistingOrder_ReturnsOrder()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        // First create an order
        var addToCartRequest = new
        {
            ProductId = _testProduct.Id,
            Quantity = 1
        };
        await Client.PostAsync("/api/cart/items", CreateJsonContent(addToCartRequest));

        var placeOrderRequest = new
        {
            ShippingAddress = new
            {
                Street = "123 Test St",
                City = "Test City",
                PostalCode = "12345",
                Country = "Test Country"
            }
        };

        var orderResponse = await Client.PostAsync("/api/orders", CreateJsonContent(placeOrderRequest));
        var createdOrder = await DeserializeResponseAsync<OrderResponse>(orderResponse);

        // Act
        var response = await Client.GetAsync($"/api/orders/{createdOrder!.Id}");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var order = await DeserializeResponseAsync<OrderResponse>(response);
        Assert.IsNotNull(order);
        Assert.AreEqual(createdOrder.Id, order.Id);
    }

    [TestMethod]
    public async Task GetOrder_NonExistentOrder_ReturnsNotFound()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        // Act
        var response = await Client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task CancelOrder_PendingOrder_ReturnsUpdatedOrder()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        // First create an order
        var addToCartRequest = new
        {
            ProductId = _testProduct.Id,
            Quantity = 1
        };
        await Client.PostAsync("/api/cart/items", CreateJsonContent(addToCartRequest));

        var placeOrderRequest = new
        {
            ShippingAddress = new
            {
                Street = "123 Test St",
                City = "Test City",
                PostalCode = "12345",
                Country = "Test Country"
            }
        };

        var orderResponse = await Client.PostAsync("/api/orders", CreateJsonContent(placeOrderRequest));
        var createdOrder = await DeserializeResponseAsync<OrderResponse>(orderResponse);

        // Act
        var response = await Client.PostAsync($"/api/orders/{createdOrder!.Id}/cancel", CreateJsonContent(new { }));

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var cancelledOrder = await DeserializeResponseAsync<OrderResponse>(response);
        Assert.IsNotNull(cancelledOrder);
        Assert.AreEqual(OrderStatus.Cancelled, cancelledOrder.Status);
    }

    [TestMethod]
    public async Task CancelOrder_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsync($"/api/orders/{Guid.NewGuid()}/cancel", CreateJsonContent(new { }));

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private class OrderResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public OrderStatus Status { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrderItemResponse> Items { get; set; } = new();
        public PaymentResponse? Payment { get; set; }
    }

    private class OrderItemResponse
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
    }

    private class PaymentResponse
    {
        public Guid Id { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
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
}