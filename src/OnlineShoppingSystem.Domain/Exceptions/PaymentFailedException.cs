namespace OnlineShoppingSystem.Domain.Exceptions;

public class PaymentFailedException : DomainException
{
    public PaymentFailedException(string reason) 
        : base($"Payment failed: {reason}")
    {
    }

    public PaymentFailedException(string reason, Exception innerException) 
        : base($"Payment failed: {reason}", innerException)
    {
    }

    public PaymentFailedException(Guid orderId, string reason) 
        : base($"Payment failed for order {orderId}: {reason}")
    {
    }
}