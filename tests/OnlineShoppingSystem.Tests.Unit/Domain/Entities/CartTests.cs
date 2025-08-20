using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.ValueObjects;

namespace OnlineShoppingSystem.Tests.Unit.Domain.Entities;

[TestClass]
public class CartTests
{
    private static readonly Guid ValidUserId = Guid.NewGuid();
    private static readonly Money ValidPrice = new(19.99m);

    private Product CreateTestProduct(string name = "Test Product", int stock = 10)
    {
        return new Product(name, "Test description for product", ValidPrice, stock, "Electronics");
    }

    [TestMethod]
    public void Constructor_ValidUserId_CreatesCart()
    {
        // Act
        var cart = new Cart(ValidUserId);

        // Assert
        Assert.AreEqual(ValidUserId, cart.UserId);
        Assert.AreEqual(0, cart.Items.Count);
        Assert.IsTrue(cart.IsEmpty());
    }

    [TestMethod]
    public void Constructor_EmptyUserId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new Cart(Guid.Empty));
    }

    [TestMethod]
    public void AddItem_ValidProductAndQuantity_AddsItem()
    {
        // Arrange
        var cart = new Cart(ValidUserId);
        var product = CreateTestProduct();
        var quantity = 2;

        // Act
        cart.AddItem(product, quantity);

        // Assert
        Assert.AreEqual(1, cart.Items.Count);
        var item = cart.Items.First();
        Assert.AreEqual(product.Id, item.ProductId);
        Assert.AreEqual(product.Name, item.ProductName);
        Assert.AreEqual(product.Price, item.Price);
        Assert.AreEqual(quantity, item.Quantity);
    }

    [TestMethod]
    public void AddItem_NullProduct_ThrowsArgumentNullException()
    {
        // Arrange
        var cart = new Cart(ValidUserId);

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => cart.AddItem(null, 1));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void AddItem_InvalidQuantity_ThrowsArgumentException(int quantity)
    {
        // Arrange
        var cart = new Cart(ValidUserId);
        var product = CreateTestProduct();

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => cart.AddItem(product, quantity));
    }

    [TestMethod]
    public void AddItem_QuantityExceedsStock_ThrowsInvalidOperationException()
    {
        // Arrange
        var cart = new Cart(ValidUserId);
        var product = CreateTestProduct(stock: 5);

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => cart.AddItem(product, 10));
    }

    [TestMethod]
    public void AddItem_ExistingProduct_UpdatesQuantity()
    {
        // Arrange
        var cart = new Cart(ValidUserId);
        var product = CreateTestProduct(stock: 10);
        cart.AddItem(product, 2);

        // Act
        cart.AddItem(product, 3);

        // Assert
        Assert.AreEqual(1, cart.Items.Count);
        Assert.AreEqual(5, cart.Items.First().Quantity);
    }

    [TestMethod]
    public void AddItem_ExistingProductExceedsStock_ThrowsInvalidOperationException()
    {
        // Arrange
        var cart = new Cart(ValidUserId);
        var product = CreateTestProduct(stock: 5);
        cart.AddItem(product, 3);

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => cart.AddItem(product, 4));
    }

    [TestMethod]
    public void UpdateItemQuantity_ValidQuantity_UpdatesQuantity()
    {
        // Arrange
        var cart = new Cart(ValidUserId);
        var product = CreateTestProduct(stock: 10);
        cart.AddItem(product, 2);
        var newQuantity = 5;

        // Act
        cart.UpdateItemQuantity(product.Id, newQuantity, product);

        // Assert
        Assert.AreEqual(newQuantity, cart.Items.First().Quantity);
    }

    [TestMethod]
    public void UpdateItemQuantity_NonExistingProduct_ThrowsInvalidOperationException()
    {
        // Arrange
        var cart = new Cart(ValidUserId);
        var product = CreateTestProduct();

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => 
            cart.UpdateItemQuantity(product.Id, 5, product));
    }

    [TestMethod]
    public void UpdateItemQuantity_QuantityExceedsStock_ThrowsInvalidOperationException()
    {
        // Arrange
        var cart = new Cart(ValidUserId);
        var product = CreateTestProduct(stock: 5);
        cart.AddItem(product, 2);

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => 
            cart.UpdateItemQuantity(product.Id, 10, product));
    }

    [TestMethod]
    public void RemoveItem_ExistingProduct_RemovesItem()
    {
        // Arrange
        var cart = new Cart(ValidUserId);
        var product = CreateTestProduct();
        cart.AddItem(product, 2);

        // Act
        cart.RemoveItem(product.Id);

        // Assert
        Assert.AreEqual(0, cart.Items.Count);
        Assert.IsTrue(cart.IsEmpty());
    }

    [TestMethod]
    public void RemoveItem_NonExistingProduct_ThrowsInvalidOperationException()
    {
        // Arrange
        var cart = new Cart(ValidUserId);
        var productId = Guid.NewGuid();

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => cart.RemoveItem(productId));
    }

    [TestMethod]
    public void Clear_CartWithItems_RemovesAllItems()
    {
        // Arrange
        var cart = new Cart(ValidUserId);
        var product1 = CreateTestProduct("Product 1");
        var product2 = CreateTestProduct("Product 2");
        cart.AddItem(product1, 2);
        cart.AddItem(product2, 1);

        // Act
        cart.Clear();

        // Assert
        Assert.AreEqual(0, cart.Items.Count);
        Assert.IsTrue(cart.IsEmpty());
    }

    [TestMethod]
    public void GetTotal_EmptyCart_ReturnsZero()
    {
        // Arrange
        var cart = new Cart(ValidUserId);

        // Act
        var total = cart.GetTotal();

        // Assert
        Assert.AreEqual(new Money(0), total);
    }

    [TestMethod]
    public void GetTotal_CartWithItems_ReturnsCorrectTotal()
    {
        // Arrange
        var cart = new Cart(ValidUserId);
        var product1 = CreateTestProduct("Product 1"); // $19.99
        var product2 = new Product("Product 2", "Description", new Money(29.99m), 10, "Electronics"); // $29.99
        cart.AddItem(product1, 2); // $39.98
        cart.AddItem(product2, 1); // $29.99

        // Act
        var total = cart.GetTotal();

        // Assert
        Assert.AreEqual(new Money(69.97m), total);
    }

    [TestMethod]
    public void GetTotalItemCount_EmptyCart_ReturnsZero()
    {
        // Arrange
        var cart = new Cart(ValidUserId);

        // Act
        var count = cart.GetTotalItemCount();

        // Assert
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public void GetTotalItemCount_CartWithItems_ReturnsCorrectCount()
    {
        // Arrange
        var cart = new Cart(ValidUserId);
        var product1 = CreateTestProduct("Product 1");
        var product2 = CreateTestProduct("Product 2");
        cart.AddItem(product1, 3);
        cart.AddItem(product2, 2);

        // Act
        var count = cart.GetTotalItemCount();

        // Assert
        Assert.AreEqual(5, count);
    }

    [TestMethod]
    public void IsEmpty_EmptyCart_ReturnsTrue()
    {
        // Arrange
        var cart = new Cart(ValidUserId);

        // Act & Assert
        Assert.IsTrue(cart.IsEmpty());
    }

    [TestMethod]
    public void IsEmpty_CartWithItems_ReturnsFalse()
    {
        // Arrange
        var cart = new Cart(ValidUserId);
        var product = CreateTestProduct();
        cart.AddItem(product, 1);

        // Act & Assert
        Assert.IsFalse(cart.IsEmpty());
    }
}