using OnlineShoppingSystem.Domain.Common;
using OnlineShoppingSystem.Domain.ValueObjects;

namespace OnlineShoppingSystem.Domain.Entities;

public class Product : BaseEntity
{
    private readonly List<string> _imageUrls = new();
    
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Money Price { get; private set; }
    public int Stock { get; private set; }
    public string Category { get; private set; }
    public IReadOnlyList<string> ImageUrls => _imageUrls.AsReadOnly();

    private Product() // For EF Core
    {
        Name = string.Empty;
        Description = string.Empty;
        Price = new Money(0);
        Category = string.Empty;
    }

    public Product(string name, string description, Money price, int stock, string category)
    {
        ValidateName(name);
        ValidateDescription(description);
        ValidatePrice(price);
        ValidateStock(stock);
        ValidateCategory(category);

        Name = name.Trim();
        Description = description.Trim();
        Price = price;
        Stock = stock;
        Category = category.Trim();
    }

    public static Product Create(string name, string description, decimal price, int stock, string category)
    {
        return new Product(name, description, new Money(price), stock, category);
    }

    public void UpdateDetails(string name, string description, Money price, string category)
    {
        ValidateName(name);
        ValidateDescription(description);
        ValidatePrice(price);
        ValidateCategory(category);

        Name = name.Trim();
        Description = description.Trim();
        Price = price;
        Category = category.Trim();
        UpdateTimestamp();
    }

    public void UpdateStock(int newStock)
    {
        ValidateStock(newStock);
        Stock = newStock;
        UpdateTimestamp();
    }

    public void ReduceStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        if (Stock < quantity)
            throw new InvalidOperationException($"Insufficient stock. Available: {Stock}, Requested: {quantity}");

        Stock -= quantity;
        UpdateTimestamp();
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        Stock += quantity;
        UpdateTimestamp();
    }

    public void AddImageUrl(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException("Image URL cannot be null or empty", nameof(imageUrl));

        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out _))
            throw new ArgumentException("Invalid image URL format", nameof(imageUrl));

        if (_imageUrls.Contains(imageUrl))
            throw new InvalidOperationException("Image URL already exists for this product");

        _imageUrls.Add(imageUrl);
        UpdateTimestamp();
    }

    public void RemoveImageUrl(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException("Image URL cannot be null or empty", nameof(imageUrl));

        if (!_imageUrls.Remove(imageUrl))
            throw new InvalidOperationException("Image URL not found for this product");

        UpdateTimestamp();
    }

    public bool IsInStock(int requestedQuantity = 1)
    {
        if (requestedQuantity <= 0)
            return false;
            
        return Stock >= requestedQuantity;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be null or empty", nameof(name));

        if (name.Trim().Length < 2)
            throw new ArgumentException("Product name must be at least 2 characters long", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Product name cannot exceed 200 characters", nameof(name));
    }

    private static void ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Product description cannot be null or empty", nameof(description));

        if (description.Trim().Length < 10)
            throw new ArgumentException("Product description must be at least 10 characters long", nameof(description));

        if (description.Length > 2000)
            throw new ArgumentException("Product description cannot exceed 2000 characters", nameof(description));
    }

    private static void ValidatePrice(Money price)
    {
        if (price == null)
            throw new ArgumentNullException(nameof(price));

        if (price.Amount <= 0)
            throw new ArgumentException("Product price must be greater than zero", nameof(price));
    }

    private static void ValidateStock(int stock)
    {
        if (stock < 0)
            throw new ArgumentException("Stock cannot be negative", nameof(stock));
    }

    private static void ValidateCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Product category cannot be null or empty", nameof(category));

        if (category.Trim().Length < 2)
            throw new ArgumentException("Product category must be at least 2 characters long", nameof(category));

        if (category.Length > 50)
            throw new ArgumentException("Product category cannot exceed 50 characters", nameof(category));
    }
}