using FluentAssertions;
using OnlineShoppingSystem.Application.Commands.Cart;
using System.ComponentModel.DataAnnotations;

namespace OnlineShoppingSystem.Tests.Unit.Application.Commands;

[TestClass]
public class AddToCartCommandTests
{
    [TestMethod]
    public void Should_Pass_Validation_With_Valid_Data()
    {
        // Arrange
        var command = new AddToCartCommand
        {
            ProductId = Guid.NewGuid(),
            Quantity = 5
        };

        // Act
        var validationResults = ValidateCommand(command);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    public void Should_Fail_Validation_With_Zero_Quantity()
    {
        // Arrange
        var command = new AddToCartCommand
        {
            ProductId = Guid.NewGuid(),
            Quantity = 0
        };

        // Act
        var validationResults = ValidateCommand(command);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("Quantity"));
    }

    [TestMethod]
    public void Should_Fail_Validation_With_Excessive_Quantity()
    {
        // Arrange
        var command = new AddToCartCommand
        {
            ProductId = Guid.NewGuid(),
            Quantity = 101
        };

        // Act
        var validationResults = ValidateCommand(command);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("Quantity"));
    }

    [TestMethod]
    public void Should_Pass_Validation_With_Valid_ProductId()
    {
        // Arrange
        var command = new AddToCartCommand
        {
            ProductId = Guid.NewGuid(),
            Quantity = 5
        };

        // Act
        var validationResults = ValidateCommand(command);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    public void Should_Pass_Validation_With_Maximum_Allowed_Quantity()
    {
        // Arrange
        var command = new AddToCartCommand
        {
            ProductId = Guid.NewGuid(),
            Quantity = 100
        };

        // Act
        var validationResults = ValidateCommand(command);

        // Assert
        validationResults.Should().BeEmpty();
    }

    private static List<ValidationResult> ValidateCommand(AddToCartCommand command)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(command);
        Validator.TryValidateObject(command, validationContext, validationResults, true);
        return validationResults;
    }
}