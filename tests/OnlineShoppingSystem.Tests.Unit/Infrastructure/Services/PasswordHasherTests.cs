using OnlineShoppingSystem.Infrastructure.Services;

namespace OnlineShoppingSystem.Tests.Unit.Infrastructure.Services;

[TestClass]
public class PasswordHasherTests
{
    private PasswordHasher _passwordHasher;

    [TestInitialize]
    public void Setup()
    {
        _passwordHasher = new PasswordHasher();
    }

    [TestMethod]
    public void HashPassword_ValidPassword_ReturnsHashedPassword()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Assert
        Assert.IsNotNull(hashedPassword);
        Assert.AreNotEqual(password, hashedPassword);
        Assert.IsTrue(hashedPassword.Length >= 60, "BCrypt hash should be at least 60 characters");
        Assert.IsTrue(hashedPassword.StartsWith("$2"), "BCrypt hash should start with $2");
    }

    [TestMethod]
    public void HashPassword_SamePasswordTwice_ReturnsDifferentHashes()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

        // Assert
        Assert.AreNotEqual(hash1, hash2, "BCrypt should generate different salts for same password");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void HashPassword_NullPassword_ThrowsArgumentException()
    {
        // Act
        _passwordHasher.HashPassword(null!);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void HashPassword_EmptyPassword_ThrowsArgumentException()
    {
        // Act
        _passwordHasher.HashPassword(string.Empty);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void HashPassword_WhitespacePassword_ThrowsArgumentException()
    {
        // Act
        _passwordHasher.HashPassword("   ");
    }

    [TestMethod]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "TestPassword123!";
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(password, hashedPassword);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void VerifyPassword_IncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword123!";
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(wrongPassword, hashedPassword);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifyPassword_NullPassword_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(null!, hashedPassword);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifyPassword_EmptyPassword_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(string.Empty, hashedPassword);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifyPassword_NullHashedPassword_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var result = _passwordHasher.VerifyPassword(password, null!);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifyPassword_EmptyHashedPassword_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var result = _passwordHasher.VerifyPassword(password, string.Empty);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifyPassword_InvalidHashedPassword_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var invalidHash = "invalid-hash";

        // Act
        var result = _passwordHasher.VerifyPassword(password, invalidHash);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifyPassword_CaseSensitive_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var wrongCasePassword = "testpassword123!";
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(wrongCasePassword, hashedPassword);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void HashAndVerify_ComplexPassword_WorksCorrectly()
    {
        // Arrange
        var complexPassword = "P@ssw0rd!2023#$%^&*()_+-=[]{}|;':\",./<>?`~";

        // Act
        var hashedPassword = _passwordHasher.HashPassword(complexPassword);
        var verificationResult = _passwordHasher.VerifyPassword(complexPassword, hashedPassword);

        // Assert
        Assert.IsTrue(verificationResult);
    }

    [TestMethod]
    public void HashAndVerify_UnicodePassword_WorksCorrectly()
    {
        // Arrange
        var unicodePassword = "Пароль123!@#";

        // Act
        var hashedPassword = _passwordHasher.HashPassword(unicodePassword);
        var verificationResult = _passwordHasher.VerifyPassword(unicodePassword, hashedPassword);

        // Assert
        Assert.IsTrue(verificationResult);
    }
}