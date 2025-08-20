using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.ValueObjects;

namespace OnlineShoppingSystem.Tests.Unit.Domain.Entities;

[TestClass]
public class ProductTests
{
    private const string ValidName = "Test Product";
    private const string ValidDescription = "This is a test product description";
    private const string ValidCategory = "Electronics";
    private static readonly Money ValidPrice = new(99.99m);
    private const int ValidStock = 10;

    [TestMethod]
    public void Constructor_ValidParameters_CreatesProduct()
    {
        // Act
        var product = new Product(ValidName, ValidDescription, ValidPrice, ValidStock, ValidCategory);

        // Assert
        Assert.AreEqual(ValidName, product.Name);
        Assert.AreEqual(ValidDescription, product.Description);
        Assert.AreEqual(ValidPrice, product.Price);
        Assert.AreEqual(ValidStock, product.Stock);
        Assert.AreEqual(ValidCategory, product.Category);
        Assert.AreEqual(0, product.ImageUrls.Count);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("A")] // Too short
    public void Constructor_InvalidName_ThrowsArgumentException(string name)
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new Product(name, ValidDescription, ValidPrice, ValidStock, ValidCategory));
    }

    [TestMethod]
    public void Constructor_NameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longName = new string('A', 201);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new Product(longName, ValidDescription, ValidPrice, ValidStock, ValidCategory));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("Short")] // Too short (less than 10 characters)
    public void Constructor_InvalidDescription_ThrowsArgumentException(string description)
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new Product(ValidName, description, ValidPrice, ValidStock, ValidCategory));
    }

    [TestMethod]
    public void Constructor_DescriptionTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longDescription = new string('A', 2001);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new Product(ValidName, longDescription, ValidPrice, ValidStock, ValidCategory));
    }

    [TestMethod]
    public void Constructor_NullPrice_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new Product(ValidName, ValidDescription, null, ValidStock, ValidCategory));
    }

    [TestMethod]
    public void Constructor_ZeroPrice_ThrowsArgumentException()
    {
        // Arrange
        var zeroPrice = new Money(0);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new Product(ValidName, ValidDescription, zeroPrice, ValidStock, ValidCategory));
    }

    [TestMethod]
    public void Constructor_NegativeStock_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new Product(ValidName, ValidDescription, ValidPrice, -1, ValidCategory));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("A")] // Too short
    public void Constructor_InvalidCategory_ThrowsArgumentException(string category)
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new Product(ValidName, ValidDescription, ValidPrice, ValidStock, category));
    }

    [TestMethod]
    public void Constructor_CategoryTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longCategory = new string('A', 51);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new Product(ValidName, ValidDescription, ValidPrice, ValidStock, longCategory));
    }

    [TestMethod]
    public void UpdateDetails_ValidParameters_UpdatesProduct()
    {
        // Arrange
        var product = new Product(ValidName, ValidDescription, ValidPrice, ValidStock, ValidCategory);
        var newName = "Updated Product";
        var newDescription = "Updated description for the product";
        var newPrice = new Money(149.99m);
        var newCategory = "Books";
        var originalTimestamp = product.UpdatedAt;

        // Act
        Thread.Sleep(1); // Ensure timestamp difference
        product.UpdateDetails(newName, newDescription, newPrice, newCategory);

        // Assert
        Assert.AreEqual(newName, product.Name);
        Assert.AreEqual(newDescription, product.Description);
        Assert.AreEqual(newPrice, product.Price);
        Assert.AreEqual(newCategory, product.Category);
        Assert.IsTrue(product.UpdatedAt > originalTimestamp);
    }

    [TestMethod]
    public void UpdateStock_ValidStock_UpdatesStock()
    {
        // Arrange
        var product = new Product(ValidName, ValidDescription, ValidPrice, ValidStock, ValidCategory);
        var newStock = 25;
        var originalTimestamp = product.UpdatedAt;

        // Act
        Thread.Sleep(1); // Ensure timestamp difference
        product.UpdateStock(newStock);

        // Assert
        Assert.AreEqual(newStock, product.Stock);
        Assert.IsTrue(product.UpdatedAt > originalTimestamp);
    }

    [TestMethod]
    public void ReduceStock_ValidQuantity_ReducesStock()
    {
        // Arrange
        var product = new Product(ValidName, ValidDescription, ValidPrice, ValidStock, ValidCategory);
        var reduceBy = 3;

        // Act
        product.ReduceStock(reduceBy);

        // Assert
        Assert.AreEqual(ValidStock - reduceBy, product.Stock);
    }

    [TestMethod]
    public void ReduceStock_InsufficientStock_ThrowsInvalidOperationException()
    {
        // Arrange
        var product = new Product(ValidName, ValidDescription, ValidPrice, 5, ValidCategory);

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => product.ReduceStock(10));
    }

    [TestMethod]
    public void ReduceStock_ZeroOrNegativeQuantity_ThrowsArgumentException()
    {
        // Arrange
        var product = new Product(ValidName, ValidDescription, ValidPrice, ValidStock, ValidCategory);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => product.ReduceStock(0));
        Assert.ThrowsException<ArgumentException>(() => product.ReduceStock(-1));
    }

    [TestMethod]
    public void IncreaseStock_ValidQuantity_IncreasesStock()
    {
        // Arrange
        var product = new Product(ValidName, ValidDescription, ValidPrice, ValidStock, ValidCategory);
        var increaseBy = 5;

        // Act
        product.IncreaseStock(increaseBy);

        // Assert
        Assert.AreEqual(ValidStock + increaseBy, product.Stock);
    }

    [TestMethod]
    public void IncreaseStock_ZeroOrNegativeQuantity_ThrowsArgumentException()
    {
        // Arrange
        var product = new Product(ValidName, ValidDescription, ValidPrice, ValidStock, ValidCategory);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => product.IncreaseStock(0));
        Assert.ThrowsException<ArgumentException>(() => product.IncreaseStock(-1));
    }

    [TestMethod]
    public void AddImageUrl_ValidUrl_AddsImageUrl()
    {
        // Arrange
        var product = new Product(ValidName, ValidDescription, ValidPrice, ValidStock, ValidCategory);
        var imageUrl = "https://example.com/image.jpg";

        // Act
        product.AddImageUrl(imageUrl);

        // Assert
        Assert.AreEqual(1, product.ImageUrls.Count);
        Assert.IsTrue(product.ImageUrls.Contains(imageUrl));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void AddImageUrl_InvalidUrl_ThrowsArgumentException(string imageUrl)
    {
        // Arrange
        var product = new Product(ValidName, ValidDescription, ValidPrice, ValidStock, ValidCategory);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => product.AddImageUrl(imageUrl));
    }

    [TestMethod]
    public void AddImageUrl_InvalidUrlFormat_ThrowsArgumentException()
    {
        // Arrange
        var product = new Product(ValidName, ValidDescription, ValidPrice, ValidStock, ValidCategory);
        var invalidUrl = "not-a-valid-url";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => product.AddImageUrl(invalidUrl));
    }

    [TestMethod]
    public void AddImageUrl_DuplicateUrl_ThrowsInvalidOperationException()
    {
        // Arrange
        var product = new Product(ValidName, ValidDescription, ValidPrice, ValidStock, ValidCategory);
        var imageUrl = "https://example.com/image.jpg";
        product.AddImageUrl(imageUrl);

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => product.AddImageUrl(imageUrl));
    }

    [TestMethod]
    public void RemoveImageUrl_ExistingUrl_RemovesImageUrl()
    {
        // Arrange
        var product = new Product(ValidName, ValidDescription, ValidPrice, ValidStock, ValidCategory);
        var imageUrl = "https://example.com/image.jpg";
        product.AddImageUrl(imageUrl);

        // Act
        product.RemoveImageUrl(imageUrl);

        // Assert
        Assert.AreEqual(0, product.ImageUrls.Count);
        Assert.IsFalse(product.ImageUrls.Contains(imageUrl));
    }

    [TestMethod]
    public void RemoveImageUrl_NonExistingUrl_ThrowsInvalidOperationException()
    {
        // Arrange
        var product = new Product(ValidName, ValidDescription, ValidPrice, ValidStock, ValidCategory);
        var imageUrl = "https://example.com/image.jpg";

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => product.RemoveImageUrl(imageUrl));
    }

    [TestMethod]
    [DataRow(1, true)]
    [DataRow(10, true)]
    [DataRow(11, false)]
    [DataRow(0, false)]
    public void IsInStock_VariousQuantities_ReturnsCorrectResult(int requestedQuantity, bool expected)
    {
        // Arrange
        var product = new Product(ValidName, ValidDescription, ValidPrice, 10, ValidCategory);

        // Act
        var result = product.IsInStock(requestedQuantity);

        // Assert
        Assert.AreEqual(expected, result);
    }
}