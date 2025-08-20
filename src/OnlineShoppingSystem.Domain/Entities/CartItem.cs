using OnlineShoppingSystem.Domain.Common;
using OnlineShoppingSystem.Domain.ValueObjects;

namespace OnlineShoppingSystem.Domain.Entities;

public class CartItem : BaseEntity
{
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public Money Price { get; private set; }
    public int Quantity { get; private set; }

    private CartItem() // For EF Core
    {
        ProductName = string.Empty;
        Price = new Money(0);
    }

    public CartItem(Guid cartId, Guid productId, string productName, Money price, int quantity)
    {
        if (cartId == Guid.Empty)
            throw new ArgumentException("Cart ID cannot be empty", nameof(cartId));

        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        ValidateProductName(productName);
        ValidatePrice(price);
        ValidateQuantity(quantity);

        CartId = cartId;
        ProductId = productId;
        ProductName = productName.Trim();
        Price = price;
        Quantity = quantity;
    }

    public void UpdateQuantity(int newQuantity)
    {
        ValidateQuantity(newQuantity);
        Quantity = newQuantity;
        UpdateTimestamp();
    }

    public void UpdatePrice(Money newPrice)
    {
        ValidatePrice(newPrice);
        
        if (newPrice.Currency != Price.Currency)
            throw new InvalidOperationException($"Cannot change currency from {Price.Currency} to {newPrice.Currency}");

        Price = newPrice;
        UpdateTimestamp();
    }

    public Money GetSubtotal()
    {
        return Price * Quantity;
    }

    private static void ValidateProductName(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name cannot be null or empty", nameof(productName));

        if (productName.Length > 200)
            throw new ArgumentException("Product name cannot exceed 200 characters", nameof(productName));
    }

    private static void ValidatePrice(Money price)
    {
        if (price == null)
            throw new ArgumentNullException(nameof(price));

        if (price.Amount <= 0)
            throw new ArgumentException("Price must be greater than zero", nameof(price));
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        if (quantity > 100)
            throw new ArgumentException("Quantity cannot exceed 100 items", nameof(quantity));
    }
}