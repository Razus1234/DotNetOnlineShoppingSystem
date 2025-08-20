using Microsoft.VisualStudio.TestTools.UnitTesting;
using OnlineShoppingSystem.Domain.Entities;
using System.Net;

namespace OnlineShoppingSystem.Tests.Integration.Controllers;

[TestClass]
public class PaymentControllerTests : BaseControllerTest
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
    public async Task ProcessPayment_WithValidOrder_ReturnsPaymentResult()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        // First create an order
        var orderId = await CreateTestOrderAsync();

        var paymentRequest = new
        {
            OrderId = orderId,
            PaymentMethodId = "pm_test_card",
            PaymentToken = "test_token_123"
        };

        // Act
        var response = await Client.PostAsync("/api/payment/process", CreateJsonContent(paymentRequest));

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var result = await DeserializeResponseAsync<PaymentResultResponse>(response);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.TransactionId);
    }

    [TestMethod]
    public async Task ProcessPayment_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var paymentRequest = new
        {
            OrderId = Guid.NewGuid(),
            PaymentMethodId = "pm_test_card",
            PaymentToken = "test_token_123"
        };

        // Act
        var response = await Client.PostAsync("/api/payment/process", CreateJsonContent(paymentRequest));

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetPaymentByOrderId_ExistingPayment_ReturnsPayment()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        // First create an order and process payment
        var orderId = await CreateTestOrderAsync();
        
        var paymentRequest = new
        {
            OrderId = orderId,
            PaymentMethodId = "pm_test_card",
            PaymentToken = "test_token_123"
        };

        await Client.PostAsync("/api/payment/process", CreateJsonContent(paymentRequest));

        // Act
        var response = await Client.GetAsync($"/api/payment/order/{orderId}");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var payment = await DeserializeResponseAsync<PaymentResponse>(response);
        Assert.IsNotNull(payment);
        Assert.AreEqual(orderId, payment.OrderId);
    }

    [TestMethod]
    public async Task GetPaymentByOrderId_NonExistentPayment_ReturnsNotFound()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        // Act
        var response = await Client.GetAsync($"/api/payment/order/{Guid.NewGuid()}");

        // Assert
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task GetPayment_ExistingPayment_ReturnsPayment()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        // First create an order and process payment
        var orderId = await CreateTestOrderAsync();
        
        var paymentRequest = new
        {
            OrderId = orderId,
            PaymentMethodId = "pm_test_card",
            PaymentToken = "test_token_123"
        };

        var paymentResponse = await Client.PostAsync("/api/payment/process", CreateJsonContent(paymentRequest));
        var paymentResult = await DeserializeResponseAsync<PaymentResultResponse>(paymentResponse);

        // Get the payment by order first to get the payment ID
        var orderPaymentResponse = await Client.GetAsync($"/api/payment/order/{orderId}");
        var orderPayment = await DeserializeResponseAsync<PaymentResponse>(orderPaymentResponse);

        // Act
        var response = await Client.GetAsync($"/api/payment/{orderPayment!.Id}");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var payment = await DeserializeResponseAsync<PaymentResponse>(response);
        Assert.IsNotNull(payment);
        Assert.AreEqual(orderPayment.Id, payment.Id);
    }

    [TestMethod]
    public async Task CheckDuplicatePayment_WithValidParameters_ReturnsCheckResult()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        var orderId = Guid.NewGuid();
        var paymentToken = "test_token_123";

        // Act
        var response = await Client.GetAsync($"/api/payment/duplicate-check?orderId={orderId}&paymentToken={paymentToken}");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var result = await DeserializeResponseAsync<DuplicateCheckResponse>(response);
        Assert.IsNotNull(result);
        Assert.AreEqual(orderId, result.OrderId);
        Assert.AreEqual(paymentToken, result.PaymentToken);
        Assert.IsFalse(result.IsDuplicate); // Should be false for new payment
    }

    [TestMethod]
    public async Task CheckDuplicatePayment_MissingOrderId_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        // Act
        var response = await Client.GetAsync("/api/payment/duplicate-check?paymentToken=test_token");

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task CheckDuplicatePayment_MissingPaymentToken_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetJwtTokenAsync();
        SetAuthorizationHeader(token);

        // Act
        var response = await Client.GetAsync($"/api/payment/duplicate-check?orderId={Guid.NewGuid()}");

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task CheckDuplicatePayment_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/payment/duplicate-check?orderId={Guid.NewGuid()}&paymentToken=test");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<Guid> CreateTestOrderAsync()
    {
        // Add item to cart
        var addToCartRequest = new
        {
            ProductId = _testProduct.Id,
            Quantity = 1
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

        var orderResponse = await Client.PostAsync("/api/orders", CreateJsonContent(placeOrderRequest));
        var order = await DeserializeResponseAsync<OrderResponse>(orderResponse);
        
        return order!.Id;
    }

    private class PaymentResultResponse
    {
        public bool IsSuccess { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    private class PaymentResponse
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    private class OrderResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class DuplicateCheckResponse
    {
        public Guid OrderId { get; set; }
        public string PaymentToken { get; set; } = string.Empty;
        public bool IsDuplicate { get; set; }
    }
}