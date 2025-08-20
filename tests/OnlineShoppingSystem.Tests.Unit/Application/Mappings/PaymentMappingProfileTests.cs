using AutoMapper;
using FluentAssertions;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Mappings;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.Enums;
using OnlineShoppingSystem.Domain.ValueObjects;

namespace OnlineShoppingSystem.Tests.Unit.Application.Mappings;

[TestClass]
public class PaymentMappingProfileTests
{
    private IMapper _mapper = null!;

    [TestInitialize]
    public void Setup()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PaymentMappingProfile>();
        });
        _mapper = configuration.CreateMapper();
    }

    [TestMethod]
    public void Should_Map_Payment_To_PaymentDto()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var payment = Payment.Create(orderId, 99.99m, "txn_12345", "Credit Card");
        payment.MarkAsCompleted();

        // Act
        var paymentDto = _mapper.Map<PaymentDto>(payment);

        // Assert
        paymentDto.Should().NotBeNull();
        paymentDto.Id.Should().Be(payment.Id);
        paymentDto.OrderId.Should().Be(orderId);
        paymentDto.Amount.Should().Be(99.99m);
        paymentDto.Currency.Should().Be("USD");
        paymentDto.Status.Should().Be(PaymentStatus.Completed);
        paymentDto.TransactionId.Should().Be("txn_12345");
        paymentDto.PaymentMethod.Should().Be("Credit Card");
        paymentDto.ProcessedAt.Should().NotBeNull();
    }

    [TestMethod]
    public void Should_Map_Failed_Payment()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var payment = Payment.Create(orderId, 50.00m, "txn_failed", "Credit Card");
        payment.MarkAsFailed("Insufficient funds");

        // Act
        var paymentDto = _mapper.Map<PaymentDto>(payment);

        // Assert
        paymentDto.Should().NotBeNull();
        paymentDto.Status.Should().Be(PaymentStatus.Failed);
        paymentDto.FailureReason.Should().Be("Insufficient funds");
        paymentDto.ProcessedAt.Should().BeNull();
    }

    [TestMethod]
    public void Should_Map_Payment_With_Refund()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var payment = Payment.Create(orderId, 100.00m, "txn_refund", "Credit Card");
        payment.MarkAsCompleted();
        payment.ProcessRefund(new Money(25.00m));

        // Act
        var paymentDto = _mapper.Map<PaymentDto>(payment);

        // Assert
        paymentDto.Should().NotBeNull();
        paymentDto.Status.Should().Be(PaymentStatus.Refunded);
        paymentDto.RefundAmount.Should().Be(25.00m);
        paymentDto.RefundedAt.Should().NotBeNull();
    }

    [TestMethod]
    public void Should_Map_Payment_Without_Refund()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var payment = Payment.Create(orderId, 75.00m, "txn_no_refund", "PayPal");
        payment.MarkAsCompleted();

        // Act
        var paymentDto = _mapper.Map<PaymentDto>(payment);

        // Assert
        paymentDto.Should().NotBeNull();
        paymentDto.RefundAmount.Should().BeNull();
        paymentDto.RefundedAt.Should().BeNull();
    }
}