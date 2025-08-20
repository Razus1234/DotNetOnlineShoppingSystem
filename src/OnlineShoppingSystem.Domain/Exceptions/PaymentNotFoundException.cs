namespace OnlineShoppingSystem.Domain.Exceptions;

public class PaymentNotFoundException : DomainException
{
    public PaymentNotFoundException(Guid paymentId) 
        : base($"Payment with ID {paymentId} was not found")
    {
    }

    public PaymentNotFoundException(string message) 
        : base(message)
    {
    }
}