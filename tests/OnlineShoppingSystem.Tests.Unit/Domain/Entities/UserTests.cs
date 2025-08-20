using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.ValueObjects;

namespace OnlineShoppingSystem.Tests.Unit.Domain.Entities;

[TestClass]
public class UserTests
{
    private const string ValidEmail = "test@example.com";
    private const string ValidPasswordHash = "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj/VJBzwESWy"; // BCrypt hash
    private const string ValidFullName = "John Doe";

    [TestMethod]
    public void Constructor_ValidParameters_CreatesUser()
    {
        // Act
        var user = new User(ValidEmail, ValidPasswordHash, ValidFullName);

        // Assert
        Assert.AreEqual(ValidEmail.ToLowerInvariant(), user.Email);
        Assert.AreEqual(ValidPasswordHash, user.PasswordHash);
        Assert.AreEqual(ValidFullName, user.FullName);
        Assert.AreEqual(0, user.Addresses.Count);
        Assert.IsNull(user.Cart);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_InvalidEmail_ThrowsArgumentException(string email)
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new User(email, ValidPasswordHash, ValidFullName));
    }

    [TestMethod]
    [DataRow("invalid-email")]
    [DataRow("@example.com")]
    [DataRow("test@")]
    [DataRow("test.example.com")]
    public void Constructor_InvalidEmailFormat_ThrowsArgumentException(string email)
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new User(email, ValidPasswordHash, ValidFullName));
    }

    [TestMethod]
    public void Constructor_EmailTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longEmail = new string('a', 250) + "@example.com"; // 263 characters

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new User(longEmail, ValidPasswordHash, ValidFullName));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("short")] // Less than 60 characters
    public void Constructor_InvalidPasswordHash_ThrowsArgumentException(string passwordHash)
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new User(ValidEmail, passwordHash, ValidFullName));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("A")] // Too short
    public void Constructor_InvalidFullName_ThrowsArgumentException(string fullName)
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new User(ValidEmail, ValidPasswordHash, fullName));
    }

    [TestMethod]
    public void Constructor_FullNameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longName = new string('A', 101);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => 
            new User(ValidEmail, ValidPasswordHash, longName));
    }

    [TestMethod]
    public void UpdateProfile_ValidFullName_UpdatesProfile()
    {
        // Arrange
        var user = new User(ValidEmail, ValidPasswordHash, ValidFullName);
        var newFullName = "Jane Smith";
        var originalTimestamp = user.UpdatedAt;

        // Act
        Thread.Sleep(1); // Ensure timestamp difference
        user.UpdateProfile(newFullName);

        // Assert
        Assert.AreEqual(newFullName, user.FullName);
        Assert.IsTrue(user.UpdatedAt > originalTimestamp);
    }

    [TestMethod]
    public void ChangePassword_ValidPasswordHash_UpdatesPassword()
    {
        // Arrange
        var user = new User(ValidEmail, ValidPasswordHash, ValidFullName);
        var newPasswordHash = "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj/VJBzwESWy"; // 60 chars
        var originalTimestamp = user.UpdatedAt;

        // Act
        Thread.Sleep(1); // Ensure timestamp difference
        user.ChangePassword(newPasswordHash);

        // Assert
        Assert.AreEqual(newPasswordHash, user.PasswordHash);
        Assert.IsTrue(user.UpdatedAt > originalTimestamp);
    }

    [TestMethod]
    public void AddAddress_ValidAddress_AddsAddress()
    {
        // Arrange
        var user = new User(ValidEmail, ValidPasswordHash, ValidFullName);
        var address = new Address("123 Main St", "Anytown", "12345", "USA");

        // Act
        user.AddAddress(address);

        // Assert
        Assert.AreEqual(1, user.Addresses.Count);
        Assert.IsTrue(user.Addresses.Contains(address));
    }

    [TestMethod]
    public void AddAddress_NullAddress_ThrowsArgumentNullException()
    {
        // Arrange
        var user = new User(ValidEmail, ValidPasswordHash, ValidFullName);

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => user.AddAddress(null));
    }

    [TestMethod]
    public void AddAddress_DuplicateAddress_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = new User(ValidEmail, ValidPasswordHash, ValidFullName);
        var address = new Address("123 Main St", "Anytown", "12345", "USA");
        user.AddAddress(address);

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => user.AddAddress(address));
    }

    [TestMethod]
    public void RemoveAddress_ExistingAddress_RemovesAddress()
    {
        // Arrange
        var user = new User(ValidEmail, ValidPasswordHash, ValidFullName);
        var address = new Address("123 Main St", "Anytown", "12345", "USA");
        user.AddAddress(address);

        // Act
        user.RemoveAddress(address);

        // Assert
        Assert.AreEqual(0, user.Addresses.Count);
        Assert.IsFalse(user.Addresses.Contains(address));
    }

    [TestMethod]
    public void RemoveAddress_NonExistingAddress_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = new User(ValidEmail, ValidPasswordHash, ValidFullName);
        var address = new Address("123 Main St", "Anytown", "12345", "USA");

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => user.RemoveAddress(address));
    }

    [TestMethod]
    public void CreateCart_NoExistingCart_CreatesCart()
    {
        // Arrange
        var user = new User(ValidEmail, ValidPasswordHash, ValidFullName);

        // Act
        user.CreateCart();

        // Assert
        Assert.IsNotNull(user.Cart);
        Assert.AreEqual(user.Id, user.Cart.UserId);
    }

    [TestMethod]
    public void CreateCart_ExistingCart_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = new User(ValidEmail, ValidPasswordHash, ValidFullName);
        user.CreateCart();

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => user.CreateCart());
    }
}