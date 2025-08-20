namespace OnlineShoppingSystem.Domain.Exceptions;

public class OrderNotFoundException : DomainException
{
    public OrderNotFoundException(Guid orderId) 
        : base($"Order with ID {orderId} was not found")
    {
    }

    public OrderNotFoundException(string message) 
        : base(message)
    {
    }
}