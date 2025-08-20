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
using OnlineShoppingSystem.Domain.Exceptions;
using OnlineShoppingSystem.Domain.ValueObjects;

namespace OnlineShoppingSystem.Tests.Unit.Application.Services;

[TestClass]
public class PaymentServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<IPaymentGateway> _mockPaymentGateway;
    private Mock<IPaymentRepository> _mockPaymentRepository;
    private Mock<IOrderRepository> _mockOrderRepository;
    private Mock<ILogger<PaymentService>> _mockLogger;
    private IMapper _mapper;
    private PaymentService _paymentService;

    [TestInitialize]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockPaymentGateway = new Mock<IPaymentGateway>();
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

        _paymentService = new PaymentService(
            _mockUnitOfWork.Object,
            _mockPaymentGateway.Object,
            _mapper,
            _mockLogger.Object);
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_ValidOrder_ReturnsSuccessfulResult()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new ProcessPaymentCommand
        {
            OrderId = orderId,
            PaymentMethod = "Credit Card",
            PaymentToken = "test_token_123"
        };

        var order = CreateTestOrder(orderId, userId, 100.00m);
        var gatewayResult = new PaymentGatewayResult
        {
            IsSuccessful = true,
            TransactionId = "txn_123",
            Status = "Completed",
            Amount = 100.00m,
            Currency = "USD"
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);
        _mockPaymentRepository.Setup(x => x.GetByOrderIdAsync(orderId))
            .ReturnsAsync((Payment?)null);
        _mockPaymentRepository.Setup(x => x.AddAsync(It.IsAny<Payment>()))
            .ReturnsAsync((Payment p) => p);
        _mockPaymentGateway.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentGatewayRequest>()))
            .ReturnsAsync(gatewayResult);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(command);

        // Assert
        Assert.IsTrue(result.IsSuccessful);
        Assert.AreEqual("txn_123", result.TransactionId);
        Assert.AreEqual(PaymentStatus.Completed, result.Status);
        Assert.AreEqual(100.00m, result.Amount);
        Assert.IsNotNull(result.Payment);

        _mockPaymentRepository.Verify(x => x.AddAsync(It.IsAny<Payment>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_OrderNotFound_ThrowsOrderNotFoundException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = new ProcessPaymentCommand
        {
            OrderId = orderId,
            PaymentMethod = "Credit Card"
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<OrderNotFoundException>(
            () => _paymentService.ProcessPaymentAsync(command));
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_OrderAlreadyPaid_ReturnsFailureResult()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new ProcessPaymentCommand
        {
            OrderId = orderId,
            PaymentMethod = "Credit Card"
        };

        var order = CreateTestOrder(orderId, userId, 100.00m);
        var existingPayment = Payment.Create(orderId, 100.00m, "txn_existing", "Credit Card");
        existingPayment.MarkAsCompleted();

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);
        _mockPaymentRepository.Setup(x => x.GetByOrderIdAsync(orderId))
            .ReturnsAsync(existingPayment);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(command);

        // Assert
        Assert.IsFalse(result.IsSuccessful);
        Assert.AreEqual(PaymentStatus.Failed, result.Status);
        Assert.AreEqual("Order already has a successful payment", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_DuplicatePaymentToken_ReturnsFailureResult()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = new ProcessPaymentCommand
        {
            OrderId = orderId,
            PaymentMethod = "Credit Card",
            PaymentToken = "duplicate_token"
        };

        // Simulate duplicate by calling IsPaymentDuplicateAsync first
        var existingPayment = Payment.Create(orderId, 100.00m, "txn_existing", "Credit Card");
        existingPayment.MarkAsCompleted();

        _mockPaymentRepository.Setup(x => x.GetByOrderIdAsync(orderId))
            .ReturnsAsync(existingPayment);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(command);

        // Assert
        Assert.IsFalse(result.IsSuccessful);
        Assert.AreEqual(PaymentStatus.Failed, result.Status);
        Assert.AreEqual("Duplicate payment attempt detected", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ProcessPaymentAsync_PaymentGatewayFailure_ReturnsFailureResult()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new ProcessPaymentCommand
        {
            OrderId = orderId,
            PaymentMethod = "Credit Card",
            PaymentToken = "test_token_123"
        };

        var order = CreateTestOrder(orderId, userId, 100.00m);
        var gatewayResult = new PaymentGatewayResult
        {
            IsSuccessful = false,
            Status = "Failed",
            Amount = 100.00m,
            Currency = "USD",
            ErrorMessage = "Insufficient funds"
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);
        _mockPaymentRepository.Setup(x => x.GetByOrderIdAsync(orderId))
            .ReturnsAsync((Payment?)null);
        _mockPaymentRepository.Setup(x => x.AddAsync(It.IsAny<Payment>()))
            .ReturnsAsync((Payment p) => p);
        _mockPaymentGateway.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentGatewayRequest>()))
            .ReturnsAsync(gatewayResult);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(command);

        // Assert
        Assert.IsFalse(result.IsSuccessful);
        Assert.AreEqual(PaymentStatus.Failed, result.Status);
        Assert.AreEqual("Insufficient funds", result.ErrorMessage);
        Assert.IsNotNull(result.Payment);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task GetPaymentByOrderIdAsync_ExistingPayment_ReturnsPaymentDto()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var payment = Payment.Create(orderId, 100.00m, "txn_123", "Credit Card");

        _mockPaymentRepository.Setup(x => x.GetByOrderIdAsync(orderId))
            .ReturnsAsync(payment);

        // Act
        var result = await _paymentService.GetPaymentByOrderIdAsync(orderId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(orderId, result.OrderId);
        Assert.AreEqual(100.00m, result.Amount);
        Assert.AreEqual("Credit Card", result.PaymentMethod);
    }

    [TestMethod]
    public async Task GetPaymentByOrderIdAsync_PaymentNotFound_ThrowsPaymentNotFoundException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _mockPaymentRepository.Setup(x => x.GetByOrderIdAsync(orderId))
            .ReturnsAsync((Payment?)null);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<PaymentNotFoundException>(
            () => _paymentService.GetPaymentByOrderIdAsync(orderId));
    }

    [TestMethod]
    public async Task GetPaymentByIdAsync_ExistingPayment_ReturnsPaymentDto()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var payment = Payment.Create(orderId, 100.00m, "txn_123", "Credit Card");

        _mockPaymentRepository.Setup(x => x.GetByIdAsync(paymentId))
            .ReturnsAsync(payment);

        // Act
        var result = await _paymentService.GetPaymentByIdAsync(paymentId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(orderId, result.OrderId);
        Assert.AreEqual(100.00m, result.Amount);
    }

    [TestMethod]
    public async Task GetPaymentByIdAsync_PaymentNotFound_ThrowsPaymentNotFoundException()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        _mockPaymentRepository.Setup(x => x.GetByIdAsync(paymentId))
            .ReturnsAsync((Payment?)null);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<PaymentNotFoundException>(
            () => _paymentService.GetPaymentByIdAsync(paymentId));
    }

    [TestMethod]
    public async Task RefundPaymentAsync_ValidPayment_ReturnsSuccessfulResult()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var payment = Payment.Create(orderId, 100.00m, "txn_123", "Credit Card");
        payment.MarkAsCompleted();

        var gatewayResult = new PaymentGatewayResult
        {
            IsSuccessful = true,
            TransactionId = "refund_123",
            Status = "Completed",
            Amount = 50.00m,
            Currency = "USD"
        };

        _mockPaymentRepository.Setup(x => x.GetByIdAsync(paymentId))
            .ReturnsAsync(payment);
        _mockPaymentGateway.Setup(x => x.RefundPaymentAsync("txn_123", 50.00m))
            .ReturnsAsync(gatewayResult);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _paymentService.RefundPaymentAsync(paymentId, 50.00m);

        // Assert
        Assert.IsTrue(result.IsSuccessful);
        Assert.AreEqual("refund_123", result.TransactionId);
        Assert.AreEqual(PaymentStatus.Completed, result.Status);
        Assert.AreEqual(50.00m, result.Amount);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task RefundPaymentAsync_PaymentNotFound_ThrowsPaymentNotFoundException()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        _mockPaymentRepository.Setup(x => x.GetByIdAsync(paymentId))
            .ReturnsAsync((Payment?)null);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<PaymentNotFoundException>(
            () => _paymentService.RefundPaymentAsync(paymentId, 50.00m));
    }

    [TestMethod]
    public async Task RefundPaymentAsync_PaymentCannotBeRefunded_ThrowsInvalidOperationException()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var payment = Payment.Create(orderId, 100.00m, "txn_123", "Credit Card");
        // Payment is still pending, cannot be refunded

        _mockPaymentRepository.Setup(x => x.GetByIdAsync(paymentId))
            .ReturnsAsync(payment);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => _paymentService.RefundPaymentAsync(paymentId, 50.00m));
    }

    [TestMethod]
    public async Task RefundPaymentAsync_RefundAmountExceedsPayment_ThrowsInvalidOperationException()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var payment = Payment.Create(orderId, 100.00m, "txn_123", "Credit Card");
        payment.MarkAsCompleted();

        _mockPaymentRepository.Setup(x => x.GetByIdAsync(paymentId))
            .ReturnsAsync(payment);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => _paymentService.RefundPaymentAsync(paymentId, 150.00m));
    }

    [TestMethod]
    public async Task IsPaymentDuplicateAsync_NullOrEmptyToken_ReturnsFalse()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // Act
        var result1 = await _paymentService.IsPaymentDuplicateAsync(orderId, null);
        var result2 = await _paymentService.IsPaymentDuplicateAsync(orderId, "");
        var result3 = await _paymentService.IsPaymentDuplicateAsync(orderId, "   ");

        // Assert
        Assert.IsFalse(result1);
        Assert.IsFalse(result2);
        Assert.IsFalse(result3);
    }

    [TestMethod]
    public async Task IsPaymentDuplicateAsync_ExistingSuccessfulPayment_ReturnsTrue()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var paymentToken = "test_token";
        var existingPayment = Payment.Create(orderId, 100.00m, "txn_123", "Credit Card");
        existingPayment.MarkAsCompleted();

        _mockPaymentRepository.Setup(x => x.GetByOrderIdAsync(orderId))
            .ReturnsAsync(existingPayment);

        // Act
        var result = await _paymentService.IsPaymentDuplicateAsync(orderId, paymentToken);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task IsPaymentDuplicateAsync_NoExistingPayment_ReturnsFalse()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var paymentToken = "test_token";

        _mockPaymentRepository.Setup(x => x.GetByOrderIdAsync(orderId))
            .ReturnsAsync((Payment?)null);

        // Act
        var result = await _paymentService.IsPaymentDuplicateAsync(orderId, paymentToken);

        // Assert
        Assert.IsFalse(result);
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