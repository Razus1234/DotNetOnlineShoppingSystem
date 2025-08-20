using OnlineShoppingSystem.Domain.Common;
using OnlineShoppingSystem.Domain.ValueObjects;
using OnlineShoppingSystem.Domain.Enums;

namespace OnlineShoppingSystem.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Money Amount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string TransactionId { get; private set; }
    public string PaymentMethod { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }
    public Money? RefundAmount { get; private set; }

    private Payment() // For EF Core
    {
        Amount = new Money(0);
        TransactionId = string.Empty;
        PaymentMethod = string.Empty;
    }

    public Payment(Guid orderId, Money amount, string paymentMethod)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID cannot be empty", nameof(orderId));

        ValidateAmount(amount);
        ValidatePaymentMethod(paymentMethod);

        OrderId = orderId;
        Amount = amount;
        Status = PaymentStatus.Pending;
        PaymentMethod = paymentMethod.Trim();
        TransactionId = string.Empty;
    }

    public static Payment Create(Guid orderId, decimal amount, string transactionId, string paymentMethod = "Credit Card")
    {
        var payment = new Payment(orderId, new Money(amount), paymentMethod);
        if (!string.IsNullOrEmpty(transactionId))
        {
            payment.MarkAsProcessing(transactionId);
        }
        return payment;
    }

    public void MarkAsProcessing(string transactionId)
    {
        ValidateTransactionId(transactionId);

        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot mark payment as processing. Current status: {Status}");

        Status = PaymentStatus.Processing;
        TransactionId = transactionId.Trim();
        UpdateTimestamp();
    }

    public void MarkAsCompleted()
    {
        if (Status != PaymentStatus.Processing)
            throw new InvalidOperationException($"Cannot mark payment as completed. Current status: {Status}");

        if (string.IsNullOrWhiteSpace(TransactionId))
            throw new InvalidOperationException("Cannot complete payment without transaction ID");

        Status = PaymentStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
        FailureReason = null;
        UpdateTimestamp();
    }

    public void MarkAsFailed(string failureReason)
    {
        if (string.IsNullOrWhiteSpace(failureReason))
            throw new ArgumentException("Failure reason cannot be null or empty", nameof(failureReason));

        if (Status == PaymentStatus.Completed)
            throw new InvalidOperationException("Cannot mark completed payment as failed");

        Status = PaymentStatus.Failed;
        FailureReason = failureReason.Trim();
        UpdateTimestamp();
    }

    public void ProcessRefund(Money refundAmount)
    {
        ValidateAmount(refundAmount);

        if (Status != PaymentStatus.Completed && Status != PaymentStatus.Refunded)
            throw new InvalidOperationException("Can only refund completed payments");

        if (refundAmount.Currency != Amount.Currency)
            throw new InvalidOperationException($"Refund currency {refundAmount.Currency} does not match payment currency {Amount.Currency}");

        if (refundAmount > Amount)
            throw new InvalidOperationException("Refund amount cannot exceed payment amount");

        var totalRefunded = RefundAmount?.Amount ?? 0;
        if (totalRefunded + refundAmount.Amount > Amount.Amount)
            throw new InvalidOperationException("Total refund amount cannot exceed payment amount");

        Status = PaymentStatus.Refunded;
        RefundAmount = RefundAmount == null ? refundAmount : new Money(totalRefunded + refundAmount.Amount, Amount.Currency);
        if (RefundedAt == null)
            RefundedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public bool IsSuccessful()
    {
        return Status == PaymentStatus.Completed;
    }

    public bool CanBeRefunded()
    {
        return (Status == PaymentStatus.Completed || Status == PaymentStatus.Refunded) && 
               (RefundAmount == null || RefundAmount.Amount < Amount.Amount);
    }

    public Money GetRemainingRefundableAmount()
    {
        if (Status != PaymentStatus.Completed && Status != PaymentStatus.Refunded)
            return new Money(0, Amount.Currency);

        var refundedAmount = RefundAmount?.Amount ?? 0;
        return new Money(Amount.Amount - refundedAmount, Amount.Currency);
    }

    private static void ValidateAmount(Money amount)
    {
        if (amount == null)
            throw new ArgumentNullException(nameof(amount));

        if (amount.Amount <= 0)
            throw new ArgumentException("Payment amount must be greater than zero", nameof(amount));
    }

    private static void ValidatePaymentMethod(string paymentMethod)
    {
        if (string.IsNullOrWhiteSpace(paymentMethod))
            throw new ArgumentException("Payment method cannot be null or empty", nameof(paymentMethod));

        var validMethods = new[] { "Credit Card", "Debit Card", "PayPal", "Stripe", "Bank Transfer" };
        if (!validMethods.Contains(paymentMethod.Trim(), StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Invalid payment method: {paymentMethod}", nameof(paymentMethod));
    }

    private static void ValidateTransactionId(string transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            throw new ArgumentException("Transaction ID cannot be null or empty", nameof(transactionId));

        if (transactionId.Length > 100)
            throw new ArgumentException("Transaction ID cannot exceed 100 characters", nameof(transactionId));
    }
}