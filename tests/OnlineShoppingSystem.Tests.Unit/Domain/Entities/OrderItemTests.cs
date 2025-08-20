using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.ValueObjects;

namespace OnlineShoppingSystem.Tests.Unit.Domain.Entities;

[TestClass]
public class OrderItemTests
{
    private static readonly Guid ValidOrderId = Guid.NewGuid();
    private static readonly Guid ValidProductId = Guid.NewGuid();
    private const string ValidProductName = "Test Product";
    private static readonly Money ValidPrice = new(19.99m);
    private const int ValidQuantity = 2;

    [TestMethod]
    public void Constructor_ValidParameters_CreatesOrderItem()
    {
        // Act
        var orderItem = new OrderItem(ValidOrderId, ValidProductId, ValidProductName, ValidPrice, ValidQuantity);

        // Assert
        Assert.AreEqual(ValidOrderId, orderItem.OrderId);
        Assert.AreEqual(ValidProductId, orderItem.ProductId);
        Assert.AreEqual(ValidProductName, orderItem.ProductName);
        Assert.AreEqual(ValidPrice, orderItem.Price);
        Assert.AreEqual(ValidQuantity, orderItem.Quantity);
    }

    [TestMethod]
    public void Constructor_EmptyOrderId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new OrderItem(Guid.Empty, ValidProductId, ValidProductName, ValidPrice, ValidQuantity));
    }

    [TestMethod]
    public void Constructor_EmptyProductId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new OrderItem(ValidOrderId, Guid.Empty, ValidProductName, ValidPrice, ValidQuantity));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_InvalidProductName_ThrowsArgumentException(string productName)
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new OrderItem(ValidOrderId, ValidProductId, productName, ValidPrice, ValidQuantity));
    }

    [TestMethod]
    public void Constructor_ProductNameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longName = new string('A', 201);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new OrderItem(ValidOrderId, ValidProductId, longName, ValidPrice, ValidQuantity));
    }

    [TestMethod]
    public void Constructor_NullPrice_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new OrderItem(ValidOrderId, ValidProductId, ValidProductName, null, ValidQuantity));
    }

    [TestMethod]
    public void Constructor_ZeroPrice_ThrowsArgumentException()
    {
        // Arrange
        var zeroPrice = new Money(0);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new OrderItem(ValidOrderId, ValidProductId, ValidProductName, zeroPrice, ValidQuantity));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Constructor_InvalidQuantity_ThrowsArgumentException(int quantity)
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new OrderItem(ValidOrderId, ValidProductId, ValidProductName, ValidPrice, quantity));
    }

    [TestMethod]
    public void Constructor_QuantityTooHigh_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new OrderItem(ValidOrderId, ValidProductId, ValidProductName, ValidPrice, 1001));
    }

    [TestMethod]
    public void GetSubtotal_ValidOrderItem_ReturnsCorrectSubtotal()
    {
        // Arrange
        var price = new Money(25.50m);
        var quantity = 3;
        var orderItem = new OrderItem(ValidOrderId, ValidProductId, ValidProductName, price, quantity);
        var expectedSubtotal = new Money(76.50m);

        // Act
        var subtotal = orderItem.GetSubtotal();

        // Assert
        Assert.AreEqual(expectedSubtotal, subtotal);
    }

    [TestMethod]
    public void GetSubtotal_SingleQuantity_ReturnsPriceAsSubtotal()
    {
        // Arrange
        var price = new Money(19.99m);
        var orderItem = new OrderItem(ValidOrderId, ValidProductId, ValidProductName, price, 1);

        // Act
        var subtotal = orderItem.GetSubtotal();

        // Assert
        Assert.AreEqual(price, subtotal);
    }
}