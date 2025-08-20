namespace OnlineShoppingSystem.Domain.Exceptions;

public class ProductNotFoundException : DomainException
{
    public ProductNotFoundException(Guid productId) 
        : base($"Product with ID {productId} was not found")
    {
    }

    public ProductNotFoundException(string message) 
        : base(message)
    {
    }
}