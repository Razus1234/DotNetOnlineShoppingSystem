using FluentAssertions;
using OnlineShoppingSystem.Domain.Exceptions;

namespace OnlineShoppingSystem.Tests.Unit.Domain.Exceptions;

[TestClass]
public class DomainExceptionTests
{
    [TestMethod]
    public void UserNotFoundException_WithUserId_ShouldCreateExceptionWithCorrectMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var exception = new UserNotFoundException(userId);

        // Assert
        exception.Should().BeOfType<UserNotFoundException>();
        exception.Should().BeAssignableTo<DomainException>();
        exception.Message.Should().Be($"User with ID {userId} was not found");
    }

    [TestMethod]
    public void UserNotFoundException_WithEmail_ShouldCreateExceptionWithCorrectMessage()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        var exception = new UserNotFoundException(email);

        // Assert
        exception.Should().BeOfType<UserNotFoundException>();
        exception.Should().BeAssignableTo<DomainException>();
        exception.Message.Should().Be($"User with email '{email}' was not found");
    }

    [TestMethod]
    public void ProductOutOfStockException_WithProductName_ShouldCreateExceptionWithCorrectMessage()
    {
        // Arrange
        var productName = "iPhone 15";

        // Act
        var exception = new ProductOutOfStockException(productName);

        // Assert
        exception.Should().BeOfType<ProductOutOfStockException>();
        exception.Should().BeAssignableTo<DomainException>();
        exception.Message.Should().Be($"Product '{productName}' is out of stock");
    }

    [TestMethod]
    public void ProductOutOfStockException_WithProductIdAndName_ShouldCreateExceptionWithCorrectMessage()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var productName = "iPhone 15";

        // Act
        var exception = new ProductOutOfStockException(productId, productName);

        // Assert
        exception.Should().BeOfType<ProductOutOfStockException>();
        exception.Should().BeAssignableTo<DomainException>();
        exception.Message.Should().Be($"Product '{productName}' (ID: {productId}) is out of stock");
    }

    [TestMethod]
    public void ProductOutOfStockException_WithQuantityDetails_ShouldCreateExceptionWithCorrectMessage()
    {
        // Arrange
        var productName = "iPhone 15";
        var requestedQuantity = 5;
        var availableStock = 2;

        // Act
        var exception = new ProductOutOfStockException(productName, requestedQuantity, availableStock);

        // Assert
        exception.Should().BeOfType<ProductOutOfStockException>();
        exception.Should().BeAssignableTo<DomainException>();
        exception.Message.Should().Be($"Product '{productName}' has insufficient stock. Requested: {requestedQuantity}, Available: {availableStock}");
    }

    [TestMethod]
    public void PaymentFailedException_WithReason_ShouldCreateExceptionWithCorrectMessage()
    {
        // Arrange
        var reason = "Insufficient funds";

        // Act
        var exception = new PaymentFailedException(reason);

        // Assert
        exception.Should().BeOfType<PaymentFailedException>();
        exception.Should().BeAssignableTo<DomainException>();
        exception.Message.Should().Be($"Payment failed: {reason}");
    }

    [TestMethod]
    public void PaymentFailedException_WithReasonAndInnerException_ShouldCreateExceptionWithCorrectMessage()
    {
        // Arrange
        var reason = "Network error";
        var innerException = new InvalidOperationException("Connection timeout");

        // Act
        var exception = new PaymentFailedException(reason, innerException);

        // Assert
        exception.Should().BeOfType<PaymentFailedException>();
        exception.Should().BeAssignableTo<DomainException>();
        exception.Message.Should().Be($"Payment failed: {reason}");
        exception.InnerException.Should().Be(innerException);
    }

    [TestMethod]
    public void PaymentFailedException_WithOrderIdAndReason_ShouldCreateExceptionWithCorrectMessage()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var reason = "Card declined";

        // Act
        var exception = new PaymentFailedException(orderId, reason);

        // Assert
        exception.Should().BeOfType<PaymentFailedException>();
        exception.Should().BeAssignableTo<DomainException>();
        exception.Message.Should().Be($"Payment failed for order {orderId}: {reason}");
    }

    [TestMethod]
    public void DomainException_ShouldBeException()
    {
        // Arrange
        var exception = new UserNotFoundException(Guid.NewGuid());

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }
}