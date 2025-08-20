namespace OnlineShoppingSystem.Domain.Exceptions;

public class ProductOutOfStockException : DomainException
{
    public ProductOutOfStockException(string productName) 
        : base($"Product '{productName}' is out of stock")
    {
    }

    public ProductOutOfStockException(Guid productId, string productName) 
        : base($"Product '{productName}' (ID: {productId}) is out of stock")
    {
    }

    public ProductOutOfStockException(string productName, int requestedQuantity, int availableStock) 
        : base($"Product '{productName}' has insufficient stock. Requested: {requestedQuantity}, Available: {availableStock}")
    {
    }
}