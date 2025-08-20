using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.ValueObjects;
using OnlineShoppingSystem.Domain.Enums;

namespace OnlineShoppingSystem.Tests.Unit.Domain.Entities;

[TestClass]
public class PaymentTests
{
    private static readonly Guid ValidOrderId = Guid.NewGuid();
    private static readonly Money ValidAmount = new(99.99m);
    private const string ValidPaymentMethod = "Credit Card";
    private const string ValidTransactionId = "txn_123456789";

    [TestMethod]
    public void Constructor_ValidParameters_CreatesPayment()
    {
        // Act
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);

        // Assert
        Assert.AreEqual(ValidOrderId, payment.OrderId);
        Assert.AreEqual(ValidAmount, payment.Amount);
        Assert.AreEqual(PaymentStatus.Pending, payment.Status);
        Assert.AreEqual(ValidPaymentMethod, payment.PaymentMethod);
        Assert.AreEqual(string.Empty, payment.TransactionId);
        Assert.IsNull(payment.FailureReason);
        Assert.IsNull(payment.ProcessedAt);
        Assert.IsNull(payment.RefundedAt);
        Assert.IsNull(payment.RefundAmount);
    }

    [TestMethod]
    public void Constructor_EmptyOrderId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new Payment(Guid.Empty, ValidAmount, ValidPaymentMethod));
    }

    [TestMethod]
    public void Constructor_NullAmount_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new Payment(ValidOrderId, null, ValidPaymentMethod));
    }

    [TestMethod]
    public void Constructor_ZeroAmount_ThrowsArgumentException()
    {
        // Arrange
        var zeroAmount = new Money(0);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new Payment(ValidOrderId, zeroAmount, ValidPaymentMethod));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_InvalidPaymentMethod_ThrowsArgumentException(string paymentMethod)
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new Payment(ValidOrderId, ValidAmount, paymentMethod));
    }

    [TestMethod]
    public void Constructor_InvalidPaymentMethodValue_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new Payment(ValidOrderId, ValidAmount, "Invalid Method"));
    }

    [TestMethod]
    [DataRow("Credit Card")]
    [DataRow("Debit Card")]
    [DataRow("PayPal")]
    [DataRow("Stripe")]
    [DataRow("Bank Transfer")]
    public void Constructor_ValidPaymentMethods_CreatesPayment(string paymentMethod)
    {
        // Act
        var payment = new Payment(ValidOrderId, ValidAmount, paymentMethod);

        // Assert
        Assert.AreEqual(paymentMethod, payment.PaymentMethod);
    }

    [TestMethod]
    public void MarkAsProcessing_ValidTransactionId_UpdatesStatus()
    {
        // Arrange
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);
        var originalTimestamp = payment.UpdatedAt;

        // Act
        Thread.Sleep(1); // Ensure timestamp difference
        payment.MarkAsProcessing(ValidTransactionId);

        // Assert
        Assert.AreEqual(PaymentStatus.Processing, payment.Status);
        Assert.AreEqual(ValidTransactionId, payment.TransactionId);
        Assert.IsTrue(payment.UpdatedAt > originalTimestamp);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void MarkAsProcessing_InvalidTransactionId_ThrowsArgumentException(string transactionId)
    {
        // Arrange
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => payment.MarkAsProcessing(transactionId));
    }

    [TestMethod]
    public void MarkAsProcessing_TransactionIdTooLong_ThrowsArgumentException()
    {
        // Arrange
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);
        var longTransactionId = new string('A', 101);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => payment.MarkAsProcessing(longTransactionId));
    }

    [TestMethod]
    public void MarkAsProcessing_NotPendingStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);
        payment.MarkAsProcessing(ValidTransactionId);

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => 
            payment.MarkAsProcessing("another_txn"));
    }

    [TestMethod]
    public void MarkAsCompleted_ProcessingStatus_UpdatesStatus()
    {
        // Arrange
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);
        payment.MarkAsProcessing(ValidTransactionId);
        var originalTimestamp = payment.UpdatedAt;

        // Act
        Thread.Sleep(1); // Ensure timestamp difference
        payment.MarkAsCompleted();

        // Assert
        Assert.AreEqual(PaymentStatus.Completed, payment.Status);
        Assert.IsNotNull(payment.ProcessedAt);
        Assert.IsTrue(payment.ProcessedAt <= DateTime.UtcNow);
        Assert.IsNull(payment.FailureReason);
        Assert.IsTrue(payment.UpdatedAt > originalTimestamp);
    }

    [TestMethod]
    public void MarkAsCompleted_NotProcessingStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => payment.MarkAsCompleted());
    }

    [TestMethod]
    public void MarkAsCompleted_NoTransactionId_ThrowsInvalidOperationException()
    {
        // Arrange
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);
        // Manually set status to Processing without transaction ID (simulating invalid state)
        var statusField = typeof(Payment).GetField("<Status>k__BackingField", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        statusField?.SetValue(payment, PaymentStatus.Processing);

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => payment.MarkAsCompleted());
    }

    [TestMethod]
    public void MarkAsFailed_ValidFailureReason_UpdatesStatus()
    {
        // Arrange
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);
        var failureReason = "Insufficient funds";
        var originalTimestamp = payment.UpdatedAt;

        // Act
        Thread.Sleep(1); // Ensure timestamp difference
        payment.MarkAsFailed(failureReason);

        // Assert
        Assert.AreEqual(PaymentStatus.Failed, payment.Status);
        Assert.AreEqual(failureReason, payment.FailureReason);
        Assert.IsTrue(payment.UpdatedAt > originalTimestamp);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void MarkAsFailed_InvalidFailureReason_ThrowsArgumentException(string failureReason)
    {
        // Arrange
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => payment.MarkAsFailed(failureReason));
    }

    [TestMethod]
    public void MarkAsFailed_CompletedPayment_ThrowsInvalidOperationException()
    {
        // Arrange
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);
        payment.MarkAsProcessing(ValidTransactionId);
        payment.MarkAsCompleted();

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => 
            payment.MarkAsFailed("Some reason"));
    }

    [TestMethod]
    public void ProcessRefund_ValidRefundAmount_ProcessesRefund()
    {
        // Arrange
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);
        payment.MarkAsProcessing(ValidTransactionId);
        payment.MarkAsCompleted();
        var refundAmount = new Money(50.00m);

        // Act
        payment.ProcessRefund(refundAmount);

        // Assert
        Assert.AreEqual(PaymentStatus.Refunded, payment.Status);
        Assert.AreEqual(refundAmount, payment.RefundAmount);
        Assert.IsNotNull(payment.RefundedAt);
        Assert.IsTrue(payment.RefundedAt <= DateTime.UtcNow);
    }

    [TestMethod]
    public void ProcessRefund_NotCompletedPayment_ThrowsInvalidOperationException()
    {
        // Arrange
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);
        var refundAmount = new Money(50.00m);

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => payment.ProcessRefund(refundAmount));
    }

    [TestMethod]
    public void ProcessRefund_RefundAmountExceedsPayment_ThrowsInvalidOperationException()
    {
        // Arrange
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);
        payment.MarkAsProcessing(ValidTransactionId);
        payment.MarkAsCompleted();
        var refundAmount = new Money(150.00m); // More than the original payment

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => payment.ProcessRefund(refundAmount));
    }

    [TestMethod]
    public void ProcessRefund_DifferentCurrency_ThrowsInvalidOperationException()
    {
        // Arrange
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);
        payment.MarkAsProcessing(ValidTransactionId);
        payment.MarkAsCompleted();
        var refundAmount = new Money(50.00m, "EUR");

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => payment.ProcessRefund(refundAmount));
    }

    [TestMethod]
    public void ProcessRefund_MultipleRefunds_AccumulatesRefundAmount()
    {
        // Arrange
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);
        payment.MarkAsProcessing(ValidTransactionId);
        payment.MarkAsCompleted();
        var firstRefund = new Money(30.00m);
        var secondRefund = new Money(20.00m);

        // Act
        payment.ProcessRefund(firstRefund);
        payment.ProcessRefund(secondRefund);

        // Assert
        Assert.AreEqual(new Money(50.00m), payment.RefundAmount);
    }

    [TestMethod]
    public void IsSuccessful_CompletedPayment_ReturnsTrue()
    {
        // Arrange
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);
        payment.MarkAsProcessing(ValidTransactionId);
        payment.MarkAsCompleted();

        // Act & Assert
        Assert.IsTrue(payment.IsSuccessful());
    }

    [TestMethod]
    public void IsSuccessful_NonCompletedPayment_ReturnsFalse()
    {
        // Arrange
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);

        // Act & Assert
        Assert.IsFalse(payment.IsSuccessful());
    }

    [TestMethod]
    public void CanBeRefunded_CompletedPayment_ReturnsTrue()
    {
        // Arrange
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);
        payment.MarkAsProcessing(ValidTransactionId);
        payment.MarkAsCompleted();

        // Act & Assert
        Assert.IsTrue(payment.CanBeRefunded());
    }

    [TestMethod]
    public void CanBeRefunded_FullyRefundedPayment_ReturnsFalse()
    {
        // Arrange
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);
        payment.MarkAsProcessing(ValidTransactionId);
        payment.MarkAsCompleted();
        payment.ProcessRefund(ValidAmount); // Full refund

        // Act & Assert
        Assert.IsFalse(payment.CanBeRefunded());
    }

    [TestMethod]
    public void GetRemainingRefundableAmount_NoRefunds_ReturnsFullAmount()
    {
        // Arrange
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);
        payment.MarkAsProcessing(ValidTransactionId);
        payment.MarkAsCompleted();

        // Act
        var remaining = payment.GetRemainingRefundableAmount();

        // Assert
        Assert.AreEqual(ValidAmount, remaining);
    }

    [TestMethod]
    public void GetRemainingRefundableAmount_PartialRefund_ReturnsRemainingAmount()
    {
        // Arrange
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);
        payment.MarkAsProcessing(ValidTransactionId);
        payment.MarkAsCompleted();
        var refundAmount = new Money(30.00m);
        payment.ProcessRefund(refundAmount);

        // Act
        var remaining = payment.GetRemainingRefundableAmount();

        // Assert
        Assert.AreEqual(new Money(69.99m), remaining);
    }

    [TestMethod]
    public void GetRemainingRefundableAmount_NonRefundablePayment_ReturnsZero()
    {
        // Arrange
        var payment = new Payment(ValidOrderId, ValidAmount, ValidPaymentMethod);

        // Act
        var remaining = payment.GetRemainingRefundableAmount();

        // Assert
        Assert.AreEqual(new Money(0, ValidAmount.Currency), remaining);
    }
}