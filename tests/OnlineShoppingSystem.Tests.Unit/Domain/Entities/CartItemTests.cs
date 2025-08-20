using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.ValueObjects;

namespace OnlineShoppingSystem.Tests.Unit.Domain.Entities;

[TestClass]
public class CartItemTests
{
    private static readonly Guid ValidCartId = Guid.NewGuid();
    private static readonly Guid ValidProductId = Guid.NewGuid();
    private const string ValidProductName = "Test Product";
    private static readonly Money ValidPrice = new(19.99m);
    private const int ValidQuantity = 2;

    [TestMethod]
    public void Constructor_ValidParameters_CreatesCartItem()
    {
        // Act
        var cartItem = new CartItem(ValidCartId, ValidProductId, ValidProductName, ValidPrice, ValidQuantity);

        // Assert
        Assert.AreEqual(ValidCartId, cartItem.CartId);
        Assert.AreEqual(ValidProductId, cartItem.ProductId);
        Assert.AreEqual(ValidProductName, cartItem.ProductName);
        Assert.AreEqual(ValidPrice, cartItem.Price);
        Assert.AreEqual(ValidQuantity, cartItem.Quantity);
    }

    [TestMethod]
    public void Constructor_EmptyCartId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new CartItem(Guid.Empty, ValidProductId, ValidProductName, ValidPrice, ValidQuantity));
    }

    [TestMethod]
    public void Constructor_EmptyProductId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new CartItem(ValidCartId, Guid.Empty, ValidProductName, ValidPrice, ValidQuantity));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_InvalidProductName_ThrowsArgumentException(string productName)
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new CartItem(ValidCartId, ValidProductId, productName, ValidPrice, ValidQuantity));
    }

    [TestMethod]
    public void Constructor_ProductNameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longName = new string('A', 201);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new CartItem(ValidCartId, ValidProductId, longName, ValidPrice, ValidQuantity));
    }

    [TestMethod]
    public void Constructor_NullPrice_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new CartItem(ValidCartId, ValidProductId, ValidProductName, null, ValidQuantity));
    }

    [TestMethod]
    public void Constructor_ZeroPrice_ThrowsArgumentException()
    {
        // Arrange
        var zeroPrice = new Money(0);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new CartItem(ValidCartId, ValidProductId, ValidProductName, zeroPrice, ValidQuantity));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Constructor_InvalidQuantity_ThrowsArgumentException(int quantity)
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new CartItem(ValidCartId, ValidProductId, ValidProductName, ValidPrice, quantity));
    }

    [TestMethod]
    public void Constructor_QuantityTooHigh_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new CartItem(ValidCartId, ValidProductId, ValidProductName, ValidPrice, 101));
    }

    [TestMethod]
    public void UpdateQuantity_ValidQuantity_UpdatesQuantity()
    {
        // Arrange
        var cartItem = new CartItem(ValidCartId, ValidProductId, ValidProductName, ValidPrice, ValidQuantity);
        var newQuantity = 5;
        var originalTimestamp = cartItem.UpdatedAt;

        // Act
        Thread.Sleep(1); // Ensure timestamp difference
        cartItem.UpdateQuantity(newQuantity);

        // Assert
        Assert.AreEqual(newQuantity, cartItem.Quantity);
        Assert.IsTrue(cartItem.UpdatedAt > originalTimestamp);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(101)]
    public void UpdateQuantity_InvalidQuantity_ThrowsArgumentException(int quantity)
    {
        // Arrange
        var cartItem = new CartItem(ValidCartId, ValidProductId, ValidProductName, ValidPrice, ValidQuantity);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => cartItem.UpdateQuantity(quantity));
    }

    [TestMethod]
    public void UpdatePrice_ValidPrice_UpdatesPrice()
    {
        // Arrange
        var cartItem = new CartItem(ValidCartId, ValidProductId, ValidProductName, ValidPrice, ValidQuantity);
        var newPrice = new Money(29.99m);
        var originalTimestamp = cartItem.UpdatedAt;

        // Act
        Thread.Sleep(1); // Ensure timestamp difference
        cartItem.UpdatePrice(newPrice);

        // Assert
        Assert.AreEqual(newPrice, cartItem.Price);
        Assert.IsTrue(cartItem.UpdatedAt > originalTimestamp);
    }

    [TestMethod]
    public void UpdatePrice_NullPrice_ThrowsArgumentNullException()
    {
        // Arrange
        var cartItem = new CartItem(ValidCartId, ValidProductId, ValidProductName, ValidPrice, ValidQuantity);

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => cartItem.UpdatePrice(null));
    }

    [TestMethod]
    public void UpdatePrice_DifferentCurrency_ThrowsInvalidOperationException()
    {
        // Arrange
        var cartItem = new CartItem(ValidCartId, ValidProductId, ValidProductName, ValidPrice, ValidQuantity);
        var differentCurrencyPrice = new Money(29.99m, "EUR");

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => cartItem.UpdatePrice(differentCurrencyPrice));
    }

    [TestMethod]
    public void GetSubtotal_ValidCartItem_ReturnsCorrectSubtotal()
    {
        // Arrange
        var price = new Money(19.99m);
        var quantity = 3;
        var cartItem = new CartItem(ValidCartId, ValidProductId, ValidProductName, price, quantity);
        var expectedSubtotal = new Money(59.97m);

        // Act
        var subtotal = cartItem.GetSubtotal();

        // Assert
        Assert.AreEqual(expectedSubtotal, subtotal);
    }
}