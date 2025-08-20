using OnlineShoppingSystem.Domain.Common;
using OnlineShoppingSystem.Domain.ValueObjects;
using OnlineShoppingSystem.Domain.Enums;

namespace OnlineShoppingSystem.Domain.Entities;

public class Order : BaseEntity
{
    private readonly List<OrderItem> _items = new();
    
    public Guid UserId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money Total { get; private set; }
    public Address ShippingAddress { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    public Payment? Payment { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    private Order() // For EF Core
    {
        Total = new Money(0);
        ShippingAddress = new Address("", "", "", "");
    }

    public Order(Guid userId, Address shippingAddress, IEnumerable<OrderItem> items)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        if (shippingAddress == null)
            throw new ArgumentNullException(nameof(shippingAddress));

        if (items == null || !items.Any())
            throw new ArgumentException("Order must contain at least one item", nameof(items));

        UserId = userId;
        Status = OrderStatus.Pending;
        ShippingAddress = shippingAddress;
        
        foreach (var item in items)
        {
            if (item == null)
                throw new ArgumentException("Order items cannot be null", nameof(items));
            
            var orderItem = new OrderItem(Id, item.ProductId, item.ProductName, item.Price, item.Quantity);
            _items.Add(orderItem);
        }

        CalculateTotal();
    }

    public static Order Create(Guid userId)
    {
        var defaultAddress = new Address("123 Main St", "Default City", "12345", "USA");
        var emptyItems = new List<OrderItem>();
        return new Order(userId, defaultAddress, new[] { new OrderItem(Guid.NewGuid(), Guid.NewGuid(), "Default Item", new Money(1.00m), 1) });
    }

    public void AddItem(Product product, int quantity, decimal price)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        var orderItem = new OrderItem(Id, product.Id, product.Name, new Money(price), quantity);
        _items.Add(orderItem);
        CalculateTotal();
        UpdateTimestamp();
    }

    public void UpdateStatus(OrderStatus newStatus)
    {
        ValidateStatusTransition(Status, newStatus);
        
        var previousStatus = Status;
        Status = newStatus;

        switch (newStatus)
        {
            case OrderStatus.Shipped:
                ShippedAt = DateTime.UtcNow;
                break;
            case OrderStatus.Delivered:
                DeliveredAt = DateTime.UtcNow;
                break;
            case OrderStatus.Cancelled:
                CancelledAt = DateTime.UtcNow;
                break;
        }

        UpdateTimestamp();
    }

    public void AttachPayment(Payment payment)
    {
        if (payment == null)
            throw new ArgumentNullException(nameof(payment));

        if (Payment != null)
            throw new InvalidOperationException("Order already has a payment attached");

        if (payment.OrderId != Id)
            throw new InvalidOperationException("Payment order ID does not match this order");

        Payment = payment;
        UpdateTimestamp();
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Order is already cancelled");

        if (Status == OrderStatus.Shipped || Status == OrderStatus.Delivered)
            throw new InvalidOperationException("Cannot cancel shipped or delivered orders");

        UpdateStatus(OrderStatus.Cancelled);
    }

    public bool CanBeCancelled()
    {
        return Status != OrderStatus.Cancelled && 
               Status != OrderStatus.Shipped && 
               Status != OrderStatus.Delivered;
    }

    public bool IsCompleted()
    {
        return Status == OrderStatus.Delivered;
    }

    public bool IsPaid()
    {
        return Payment?.Status == PaymentStatus.Completed;
    }

    public void MarkAsPaid()
    {
        if (Status == OrderStatus.Pending)
        {
            UpdateStatus(OrderStatus.Confirmed);
        }
        UpdateTimestamp();
    }

    private void CalculateTotal()
    {
        if (!_items.Any())
        {
            Total = new Money(0);
            return;
        }

        var firstCurrency = _items.First().Price.Currency;
        var totalAmount = _items.Sum(item => item.GetSubtotal().Amount);
        Total = new Money(totalAmount, firstCurrency);
    }

    private static void ValidateStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        var validTransitions = new Dictionary<OrderStatus, OrderStatus[]>
        {
            [OrderStatus.Pending] = new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
            [OrderStatus.Confirmed] = new[] { OrderStatus.Processing, OrderStatus.Cancelled },
            [OrderStatus.Processing] = new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
            [OrderStatus.Shipped] = new[] { OrderStatus.Delivered },
            [OrderStatus.Delivered] = new OrderStatus[0], // Terminal state
            [OrderStatus.Cancelled] = new OrderStatus[0]  // Terminal state
        };

        if (!validTransitions.ContainsKey(currentStatus))
            throw new InvalidOperationException($"Unknown order status: {currentStatus}");

        if (!validTransitions[currentStatus].Contains(newStatus))
            throw new InvalidOperationException($"Invalid status transition from {currentStatus} to {newStatus}");
    }
}