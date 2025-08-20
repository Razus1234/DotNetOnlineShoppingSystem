using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using OnlineShoppingSystem.Application.Commands.Payment;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Interfaces;
using OnlineShoppingSystem.Application.Mappings;
using OnlineShoppingSystem.Application.Services;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.Enums;
using OnlineShoppingSystem.Domain.ValueObjects;
using OnlineShoppingSystem.Tests.Unit.Infrastructure.Services;

namespace OnlineShoppingSystem.Tests.Unit.Application.Services;

[TestClass]
public class PaymentServiceWithMockGatewayTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<IPaymentRepository> _mockPaymentRepository;
    private Mock<IOrderRepository> _mockOrderRepository;
    private Mock<ILogger<PaymentService>> _mockLogger;
    private IMapper _mapper;
    private PaymentService _paymentService;

    [TestInitialize]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockPaymentRepository = new Mock<IPaymentRepository>();
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockLogger = new Mock<ILogger<PaymentService>>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PaymentMappingProfile>();
        });
        _mapper = config.CreateMapper();

        _mockUnitOfWork.Setup(x => x.Payments).Returns(_mockPaymentRepository.Object);
        _mockUnitOfWork.Setup(x => x.Orders).Returns(_mockOrderRepository.Object);
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_WithSuccessfulMockGateway_CompletesPaymentFlow()
    {
        // Arrange
        var mockGateway = new MockStripePaymentGateway(shouldSucceed: true);
        _paymentService = new PaymentService(_mockUnitOfWork.Object, mockGateway, _mapper, _mockLogger.Object);

        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new ProcessPaymentCommand
        {
            OrderId = orderId,
            PaymentMethod = "Credit Card",
            PaymentToken = "test_token_123",
            PaymentDetails = new Dictionary<string, string>
            {
                ["CardLast4"] = "4242",
                ["CardBrand"] = "Visa"
            }
        };

        var order = CreateTestOrder(orderId, userId, 150.00m);

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);
        _mockPaymentRepository.Setup(x => x.GetByOrderIdAsync(orderId))
            .ReturnsAsync((Payment?)null);
        _mockPaymentRepository.Setup(x => x.AddAsync(It.IsAny<Payment>()))
            .ReturnsAsync((Payment p) => p);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(command);

        // Assert
        Assert.IsTrue(result.IsSuccessful);
        Assert.AreEqual(PaymentStatus.Completed, result.Status);
        Assert.AreEqual(150.00m, result.Amount);
        Assert.AreEqual("USD", result.Currency);
        Assert.IsNotNull(result.Payment);
        Assert.IsTrue(result.TransactionId.StartsWith("txn_"));

        // Verify that payment was added and saved
        _mockPaymentRepository.Verify(x => x.AddAsync(It.Is<Payment>(p => 
            p.OrderId == orderId && 
            p.Amount.Amount == 150.00m && 
            p.PaymentMethod == "Credit Card")), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_WithFailingMockGateway_HandlesFailureCorrectly()
    {
        // Arrange
        var mockGateway = new MockStripePaymentGateway(shouldSucceed: false, errorMessage: "Card declined");
        _paymentService = new PaymentService(_mockUnitOfWork.Object, mockGateway, _mapper, _mockLogger.Object);

        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new ProcessPaymentCommand
        {
            OrderId = orderId,
            PaymentMethod = "Credit Card",
            PaymentToken = "test_token_declined"
        };

        var order = CreateTestOrder(orderId, userId, 100.00m);

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);
        _mockPaymentRepository.Setup(x => x.GetByOrderIdAsync(orderId))
            .ReturnsAsync((Payment?)null);
        _mockPaymentRepository.Setup(x => x.AddAsync(It.IsAny<Payment>()))
            .ReturnsAsync((Payment p) => p);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(command);

        // Assert
        Assert.IsFalse(result.IsSuccessful);
        Assert.AreEqual(PaymentStatus.Failed, result.Status);
        Assert.AreEqual("Card declined", result.ErrorMessage);
        Assert.IsNotNull(result.Payment);

        // Verify that payment was still added and saved (for audit trail)
        _mockPaymentRepository.Verify(x => x.AddAsync(It.Is<Payment>(p => 
            p.OrderId == orderId && 
            p.Status == PaymentStatus.Failed)), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task RefundPaymentAsync_WithSuccessfulMockGateway_ProcessesRefundCorrectly()
    {
        // Arrange
        var mockGateway = new MockStripePaymentGateway(shouldSucceed: true);
        _paymentService = new PaymentService(_mockUnitOfWork.Object, mockGateway, _mapper, _mockLogger.Object);

        var paymentId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var payment = Payment.Create(orderId, 200.00m, "txn_original", "Credit Card");
        payment.MarkAsCompleted();

        _mockPaymentRepository.Setup(x => x.GetByIdAsync(paymentId))
            .ReturnsAsync(payment);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _paymentService.RefundPaymentAsync(paymentId, 75.00m);

        // Assert
        Assert.IsTrue(result.IsSuccessful);
        Assert.AreEqual(PaymentStatus.Completed, result.Status);
        Assert.AreEqual(75.00m, result.Amount);
        Assert.IsTrue(result.TransactionId.StartsWith("refund_"));

        // Verify that the payment was updated
        Assert.AreEqual(PaymentStatus.Refunded, payment.Status);
        Assert.AreEqual(75.00m, payment.RefundAmount?.Amount);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task RefundPaymentAsync_WithFailingMockGateway_HandlesRefundFailure()
    {
        // Arrange
        var mockGateway = new MockStripePaymentGateway(shouldSucceed: false, errorMessage: "Refund not allowed");
        _paymentService = new PaymentService(_mockUnitOfWork.Object, mockGateway, _mapper, _mockLogger.Object);

        var paymentId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var payment = Payment.Create(orderId, 100.00m, "txn_original", "Credit Card");
        payment.MarkAsCompleted();

        _mockPaymentRepository.Setup(x => x.GetByIdAsync(paymentId))
            .ReturnsAsync(payment);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _paymentService.RefundPaymentAsync(paymentId, 50.00m);

        // Assert
        Assert.IsFalse(result.IsSuccessful);
        Assert.AreEqual(PaymentStatus.Failed, result.Status);
        Assert.AreEqual("Refund not allowed", result.ErrorMessage);

        // Verify that the payment status wasn't changed since refund failed
        Assert.AreEqual(PaymentStatus.Completed, payment.Status);
        Assert.IsNull(payment.RefundAmount);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_WithPaymentDetails_IncludesMetadataInGatewayRequest()
    {
        // Arrange
        var capturedRequest = new PaymentGatewayRequest();
        var mockGateway = new Mock<IPaymentGateway>();
        mockGateway.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentGatewayRequest>()))
            .Callback<PaymentGatewayRequest>(req => capturedRequest = req)
            .ReturnsAsync(new PaymentGatewayResult
            {
                IsSuccessful = true,
                TransactionId = "txn_test",
                Status = "Completed",
                Amount = 100.00m,
                Currency = "USD"
            });

        _paymentService = new PaymentService(_mockUnitOfWork.Object, mockGateway.Object, _mapper, _mockLogger.Object);

        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new ProcessPaymentCommand
        {
            OrderId = orderId,
            PaymentMethod = "Credit Card",
            PaymentToken = "test_token",
            PaymentDetails = new Dictionary<string, string>
            {
                ["CardLast4"] = "1234",
                ["CardBrand"] = "MasterCard",
                ["CustomerEmail"] = "test@example.com"
            }
        };

        var order = CreateTestOrder(orderId, userId, 100.00m);

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);
        _mockPaymentRepository.Setup(x => x.GetByOrderIdAsync(orderId))
            .ReturnsAsync((Payment?)null);
        _mockPaymentRepository.Setup(x => x.AddAsync(It.IsAny<Payment>()))
            .ReturnsAsync((Payment p) => p);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _paymentService.ProcessPaymentAsync(command);

        // Assert
        Assert.AreEqual(100.00m, capturedRequest.Amount);
        Assert.AreEqual("USD", capturedRequest.Currency);
        Assert.AreEqual("test_token", capturedRequest.PaymentToken);
        Assert.AreEqual("Credit Card", capturedRequest.PaymentMethod);
        
        // Verify metadata includes order and payment details
        Assert.IsTrue(capturedRequest.Metadata.ContainsKey("OrderId"));
        Assert.AreEqual(orderId.ToString(), capturedRequest.Metadata["OrderId"]);
        Assert.IsTrue(capturedRequest.Metadata.ContainsKey("CardLast4"));
        Assert.AreEqual("1234", capturedRequest.Metadata["CardLast4"]);
        Assert.IsTrue(capturedRequest.Metadata.ContainsKey("CardBrand"));
        Assert.AreEqual("MasterCard", capturedRequest.Metadata["CardBrand"]);
        Assert.IsTrue(capturedRequest.Metadata.ContainsKey("CustomerEmail"));
        Assert.AreEqual("test@example.com", capturedRequest.Metadata["CustomerEmail"]);
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_FullRefundAmount_RefundsEntirePayment()
    {
        // Arrange
        var mockGateway = new MockStripePaymentGateway(shouldSucceed: true);
        _paymentService = new PaymentService(_mockUnitOfWork.Object, mockGateway, _mapper, _mockLogger.Object);

        var paymentId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var payment = Payment.Create(orderId, 100.00m, "txn_original", "Credit Card");
        payment.MarkAsCompleted();

        _mockPaymentRepository.Setup(x => x.GetByIdAsync(paymentId))
            .ReturnsAsync(payment);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act - Refund without specifying amount (should refund full amount)
        var result = await _paymentService.RefundPaymentAsync(paymentId);

        // Assert
        Assert.IsTrue(result.IsSuccessful);
        Assert.AreEqual(100.00m, result.Amount); // Full refund amount
        Assert.AreEqual(PaymentStatus.Refunded, payment.Status);
        Assert.AreEqual(100.00m, payment.RefundAmount?.Amount);
    }

    private static Order CreateTestOrder(Guid orderId, Guid userId, decimal total)
    {
        var address = new Address("123 Test St", "Test City", "12345", "USA");
        var orderItem = new OrderItem(orderId, Guid.NewGuid(), "Test Product", new Money(total), 1);
        var order = new Order(userId, address, new[] { orderItem });
        
        // Use reflection to set the Id since it's typically set by EF Core
        var idProperty = typeof(Order).BaseType?.GetProperty("Id");
        idProperty?.SetValue(order, orderId);
        
        return order;
    }
}