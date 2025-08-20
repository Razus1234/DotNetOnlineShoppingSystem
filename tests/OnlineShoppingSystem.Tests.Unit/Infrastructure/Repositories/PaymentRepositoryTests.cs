using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.Enums;
using OnlineShoppingSystem.Infrastructure.Data;
using OnlineShoppingSystem.Infrastructure.Data.Repositories;

namespace OnlineShoppingSystem.Tests.Unit.Infrastructure.Repositories;

[TestClass]
public class PaymentRepositoryTests
{
    private ApplicationDbContext _context = null!;
    private PaymentRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new PaymentRepository(_context);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
    }

    [TestMethod]
    public async Task GetByOrderIdAsync_ExistingPayment_ReturnsPayment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = Order.Create(userId);
        var payment = Payment.Create(order.Id, 100.00m, "txn_123456");
        
        await _context.Orders.AddAsync(order);
        await _repository.AddAsync(payment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByOrderIdAsync(order.Id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(order.Id, result.OrderId);
        Assert.AreEqual(100.00m, result.Amount.Amount);
        Assert.AreEqual("txn_123456", result.TransactionId);
    }

    [TestMethod]
    public async Task GetByOrderIdAsync_NonExistingPayment_ReturnsNull()
    {
        // Arrange
        var nonExistingOrderId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByOrderIdAsync(nonExistingOrderId);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetByTransactionIdAsync_ExistingPayment_ReturnsPayment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = Order.Create(userId);
        var payment = Payment.Create(order.Id, 100.00m, "txn_123456");
        
        await _context.Orders.AddAsync(order);
        await _repository.AddAsync(payment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTransactionIdAsync("txn_123456");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("txn_123456", result.TransactionId);
        Assert.AreEqual(100.00m, result.Amount.Amount);
    }

    [TestMethod]
    public async Task GetByTransactionIdAsync_NonExistingPayment_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByTransactionIdAsync("non_existing_txn");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetByIdAsync_PaymentWithOrder_ReturnsPaymentWithOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order = Order.Create(userId);
        var payment = Payment.Create(order.Id, 100.00m, "txn_123456");
        
        await _context.Orders.AddAsync(order);
        await _repository.AddAsync(payment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(payment.Id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(payment.Id, result.Id);
        Assert.AreEqual(order.Id, result.OrderId);
    }
}