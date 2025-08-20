using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.ValueObjects;
using OnlineShoppingSystem.Domain.Enums;

namespace OnlineShoppingSystem.Tests.Unit.Domain.Entities;

[TestClass]
public class OrderTests
{
    private static readonly Guid ValidUserId = Guid.NewGuid();
    private static readonly Address ValidShippingAddress = new("123 Main St", "Anytown", "12345", "USA");
    private static readonly Money ValidPrice = new(19.99m);

    private List<OrderItem> CreateTestOrderItems()
    {
        return new List<OrderItem>
        {
            new OrderItem(Guid.NewGuid(), Guid.NewGuid(), "Product 1", ValidPrice, 2),
            new OrderItem(Guid.NewGuid(), Guid.NewGuid(), "Product 2", new Money(29.99m), 1)
        };
    }

    [TestMethod]
    public void Constructor_ValidParameters_CreatesOrder()
    {
        // Arrange
        var orderItems = CreateTestOrderItems();

        // Act
        var order = new Order(ValidUserId, ValidShippingAddress, orderItems);

        // Assert
        Assert.AreEqual(ValidUserId, order.UserId);
        Assert.AreEqual(OrderStatus.Pending, order.Status);
        Assert.AreEqual(ValidShippingAddress, order.ShippingAddress);
        Assert.AreEqual(2, order.Items.Count);
        Assert.AreEqual(new Money(69.97m), order.Total); // (19.99 * 2) + (29.99 * 1)
        Assert.IsNull(order.Payment);
        Assert.IsNull(order.ShippedAt);
        Assert.IsNull(order.DeliveredAt);
        Assert.IsNull(order.CancelledAt);
    }

    [TestMethod]
    public void Constructor_EmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var orderItems = CreateTestOrderItems();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new Order(Guid.Empty, ValidShippingAddress, orderItems));
    }

    [TestMethod]
    public void Constructor_NullShippingAddress_ThrowsArgumentNullException()
    {
        // Arrange
        var orderItems = CreateTestOrderItems();

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new Order(ValidUserId, null, orderItems));
    }

    [TestMethod]
    public void Constructor_NullOrderItems_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new Order(ValidUserId, ValidShippingAddress, null));
    }

    [TestMethod]
    public void Constructor_EmptyOrderItems_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new Order(ValidUserId, ValidShippingAddress, new List<OrderItem>()));
    }

    [TestMethod]
    public void Constructor_OrderItemsContainNull_ThrowsArgumentException()
    {
        // Arrange
        var orderItems = new List<OrderItem> { null };

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new Order(ValidUserId, ValidShippingAddress, orderItems));
    }

    [TestMethod]
    public void UpdateStatus_ValidTransition_UpdatesStatus()
    {
        // Arrange
        var order = new Order(ValidUserId, ValidShippingAddress, CreateTestOrderItems());
        var originalTimestamp = order.UpdatedAt;

        // Act
        Thread.Sleep(1); // Ensure timestamp difference
        order.UpdateStatus(OrderStatus.Confirmed);

        // Assert
        Assert.AreEqual(OrderStatus.Confirmed, order.Status);
        Assert.IsTrue(order.UpdatedAt > originalTimestamp);
    }

    [TestMethod]
    public void UpdateStatus_ToShipped_SetsShippedAt()
    {
        // Arrange
        var order = new Order(ValidUserId, ValidShippingAddress, CreateTestOrderItems());
        order.UpdateStatus(OrderStatus.Confirmed);
        order.UpdateStatus(OrderStatus.Processing);

        // Act
        order.UpdateStatus(OrderStatus.Shipped);

        // Assert
        Assert.AreEqual(OrderStatus.Shipped, order.Status);
        Assert.IsNotNull(order.ShippedAt);
        Assert.IsTrue(order.ShippedAt <= DateTime.UtcNow);
    }

    [TestMethod]
    public void UpdateStatus_ToDelivered_SetsDeliveredAt()
    {
        // Arrange
        var order = new Order(ValidUserId, ValidShippingAddress, CreateTestOrderItems());
        order.UpdateStatus(OrderStatus.Confirmed);
        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Shipped);

        // Act
        order.UpdateStatus(OrderStatus.Delivered);

        // Assert
        Assert.AreEqual(OrderStatus.Delivered, order.Status);
        Assert.IsNotNull(order.DeliveredAt);
        Assert.IsTrue(order.DeliveredAt <= DateTime.UtcNow);
    }

    [TestMethod]
    public void UpdateStatus_ToCancelled_SetsCancelledAt()
    {
        // Arrange
        var order = new Order(ValidUserId, ValidShippingAddress, CreateTestOrderItems());

        // Act
        order.UpdateStatus(OrderStatus.Cancelled);

        // Assert
        Assert.AreEqual(OrderStatus.Cancelled, order.Status);
        Assert.IsNotNull(order.CancelledAt);
        Assert.IsTrue(order.CancelledAt <= DateTime.UtcNow);
    }

    [TestMethod]
    public void UpdateStatus_InvalidTransition_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = new Order(ValidUserId, ValidShippingAddress, CreateTestOrderItems());

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => 
            order.UpdateStatus(OrderStatus.Shipped)); // Can't go directly from Pending to Shipped
    }

    [TestMethod]
    public void AttachPayment_ValidPayment_AttachesPayment()
    {
        // Arrange
        var order = new Order(ValidUserId, ValidShippingAddress, CreateTestOrderItems());
        var payment = new Payment(order.Id, order.Total, "Credit Card");

        // Act
        order.AttachPayment(payment);

        // Assert
        Assert.AreEqual(payment, order.Payment);
    }

    [TestMethod]
    public void AttachPayment_NullPayment_ThrowsArgumentNullException()
    {
        // Arrange
        var order = new Order(ValidUserId, ValidShippingAddress, CreateTestOrderItems());

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => order.AttachPayment(null));
    }

    [TestMethod]
    public void AttachPayment_PaymentAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = new Order(ValidUserId, ValidShippingAddress, CreateTestOrderItems());
        var payment1 = new Payment(order.Id, order.Total, "Credit Card");
        var payment2 = new Payment(order.Id, order.Total, "PayPal");
        order.AttachPayment(payment1);

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => order.AttachPayment(payment2));
    }

    [TestMethod]
    public void AttachPayment_PaymentOrderIdMismatch_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = new Order(ValidUserId, ValidShippingAddress, CreateTestOrderItems());
        var payment = new Payment(Guid.NewGuid(), order.Total, "Credit Card");

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => order.AttachPayment(payment));
    }

    [TestMethod]
    public void Cancel_PendingOrder_CancelsOrder()
    {
        // Arrange
        var order = new Order(ValidUserId, ValidShippingAddress, CreateTestOrderItems());

        // Act
        order.Cancel();

        // Assert
        Assert.AreEqual(OrderStatus.Cancelled, order.Status);
        Assert.IsNotNull(order.CancelledAt);
    }

    [TestMethod]
    public void Cancel_AlreadyCancelledOrder_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = new Order(ValidUserId, ValidShippingAddress, CreateTestOrderItems());
        order.Cancel();

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => order.Cancel());
    }

    [TestMethod]
    public void Cancel_ShippedOrder_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = new Order(ValidUserId, ValidShippingAddress, CreateTestOrderItems());
        order.UpdateStatus(OrderStatus.Confirmed);
        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Shipped);

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => order.Cancel());
    }

    [TestMethod]
    [DataRow(OrderStatus.Pending, true)]
    [DataRow(OrderStatus.Confirmed, true)]
    [DataRow(OrderStatus.Processing, true)]
    [DataRow(OrderStatus.Shipped, false)]
    [DataRow(OrderStatus.Delivered, false)]
    [DataRow(OrderStatus.Cancelled, false)]
    public void CanBeCancelled_VariousStatuses_ReturnsCorrectResult(OrderStatus status, bool expected)
    {
        // Arrange
        var order = new Order(ValidUserId, ValidShippingAddress, CreateTestOrderItems());
        if (status != OrderStatus.Pending)
        {
            // Navigate to the desired status
            switch (status)
            {
                case OrderStatus.Confirmed:
                    order.UpdateStatus(OrderStatus.Confirmed);
                    break;
                case OrderStatus.Processing:
                    order.UpdateStatus(OrderStatus.Confirmed);
                    order.UpdateStatus(OrderStatus.Processing);
                    break;
                case OrderStatus.Shipped:
                    order.UpdateStatus(OrderStatus.Confirmed);
                    order.UpdateStatus(OrderStatus.Processing);
                    order.UpdateStatus(OrderStatus.Shipped);
                    break;
                case OrderStatus.Delivered:
                    order.UpdateStatus(OrderStatus.Confirmed);
                    order.UpdateStatus(OrderStatus.Processing);
                    order.UpdateStatus(OrderStatus.Shipped);
                    order.UpdateStatus(OrderStatus.Delivered);
                    break;
                case OrderStatus.Cancelled:
                    order.UpdateStatus(OrderStatus.Cancelled);
                    break;
            }
        }

        // Act
        var result = order.CanBeCancelled();

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void IsCompleted_DeliveredOrder_ReturnsTrue()
    {
        // Arrange
        var order = new Order(ValidUserId, ValidShippingAddress, CreateTestOrderItems());
        order.UpdateStatus(OrderStatus.Confirmed);
        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Shipped);
        order.UpdateStatus(OrderStatus.Delivered);

        // Act & Assert
        Assert.IsTrue(order.IsCompleted());
    }

    [TestMethod]
    public void IsCompleted_NonDeliveredOrder_ReturnsFalse()
    {
        // Arrange
        var order = new Order(ValidUserId, ValidShippingAddress, CreateTestOrderItems());

        // Act & Assert
        Assert.IsFalse(order.IsCompleted());
    }

    [TestMethod]
    public void IsPaid_OrderWithCompletedPayment_ReturnsTrue()
    {
        // Arrange
        var order = new Order(ValidUserId, ValidShippingAddress, CreateTestOrderItems());
        var payment = new Payment(order.Id, order.Total, "Credit Card");
        payment.MarkAsProcessing("txn_123");
        payment.MarkAsCompleted();
        order.AttachPayment(payment);

        // Act & Assert
        Assert.IsTrue(order.IsPaid());
    }

    [TestMethod]
    public void IsPaid_OrderWithoutPayment_ReturnsFalse()
    {
        // Arrange
        var order = new Order(ValidUserId, ValidShippingAddress, CreateTestOrderItems());

        // Act & Assert
        Assert.IsFalse(order.IsPaid());
    }

    [TestMethod]
    public void IsPaid_OrderWithFailedPayment_ReturnsFalse()
    {
        // Arrange
        var order = new Order(ValidUserId, ValidShippingAddress, CreateTestOrderItems());
        var payment = new Payment(order.Id, order.Total, "Credit Card");
        payment.MarkAsFailed("Insufficient funds");
        order.AttachPayment(payment);

        // Act & Assert
        Assert.IsFalse(order.IsPaid());
    }
}