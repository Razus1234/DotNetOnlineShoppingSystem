using OnlineShoppingSystem.Domain.Common;
using OnlineShoppingSystem.Domain.ValueObjects;

namespace OnlineShoppingSystem.Domain.Entities;

public class Cart : BaseEntity
{
    private readonly List<CartItem> _items = new();
    
    public Guid UserId { get; private set; }
    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();

    private Cart() // For EF Core
    {
    }

    public Cart(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        UserId = userId;
    }

    public static Cart Create(Guid userId)
    {
        return new Cart(userId);
    }

    public void AddItem(Product product, int quantity)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        ValidateQuantity(quantity);

        if (!product.IsInStock(quantity))
            throw new InvalidOperationException($"Insufficient stock for product '{product.Name}'. Available: {product.Stock}, Requested: {quantity}");

        var existingItem = _items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existingItem != null)
        {
            var newQuantity = existingItem.Quantity + quantity;
            if (!product.IsInStock(newQuantity))
                throw new InvalidOperationException($"Insufficient stock for product '{product.Name}'. Available: {product.Stock}, Total requested: {newQuantity}");
            
            existingItem.UpdateQuantity(newQuantity);
        }
        else
        {
            var cartItem = new CartItem(Id, product.Id, product.Name, product.Price, quantity);
            _items.Add(cartItem);
        }

        UpdateTimestamp();
    }

    public void UpdateItemQuantity(Guid productId, int quantity, Product product)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        if (product == null)
            throw new ArgumentNullException(nameof(product));

        ValidateQuantity(quantity);

        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
            throw new InvalidOperationException("Cart item not found");

        if (!product.IsInStock(quantity))
            throw new InvalidOperationException($"Insufficient stock for product '{product.Name}'. Available: {product.Stock}, Requested: {quantity}");

        item.UpdateQuantity(quantity);
        UpdateTimestamp();
    }

    public void RemoveItem(Guid productId)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
            throw new InvalidOperationException("Cart item not found");

        _items.Remove(item);
        UpdateTimestamp();
    }

    public void Clear()
    {
        _items.Clear();
        UpdateTimestamp();
    }

    public Money GetTotal()
    {
        if (!_items.Any())
            return new Money(0);

        var firstCurrency = _items.First().Price.Currency;
        var total = _items.Sum(item => item.GetSubtotal().Amount);
        
        return new Money(total, firstCurrency);
    }

    public int GetTotalItemCount()
    {
        return _items.Sum(item => item.Quantity);
    }

    public bool IsEmpty()
    {
        return !_items.Any();
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        if (quantity > 100)
            throw new ArgumentException("Quantity cannot exceed 100 items per product", nameof(quantity));
    }
}