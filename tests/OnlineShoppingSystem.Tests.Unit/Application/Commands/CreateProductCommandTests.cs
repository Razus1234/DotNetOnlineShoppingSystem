using FluentAssertions;
using OnlineShoppingSystem.Application.Commands.Product;
using System.ComponentModel.DataAnnotations;

namespace OnlineShoppingSystem.Tests.Unit.Application.Commands;

[TestClass]
public class CreateProductCommandTests
{
    [TestMethod]
    public void Should_Pass_Validation_With_Valid_Data()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "Test Product",
            Description = "This is a test product with a detailed description",
            Price = 29.99m,
            Stock = 100,
            Category = "Electronics",
            ImageUrls = new List<string> { "https://example.com/image.jpg" }
        };

        // Act
        var validationResults = ValidateCommand(command);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    public void Should_Fail_Validation_With_Zero_Price()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "Test Product",
            Description = "This is a test product with a detailed description",
            Price = 0,
            Stock = 100,
            Category = "Electronics"
        };

        // Act
        var validationResults = ValidateCommand(command);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("Price"));
    }

    [TestMethod]
    public void Should_Fail_Validation_With_Negative_Stock()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "Test Product",
            Description = "This is a test product with a detailed description",
            Price = 29.99m,
            Stock = -1,
            Category = "Electronics"
        };

        // Act
        var validationResults = ValidateCommand(command);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("Stock"));
    }

    [TestMethod]
    public void Should_Fail_Validation_With_Short_Description()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "Test Product",
            Description = "Short",
            Price = 29.99m,
            Stock = 100,
            Category = "Electronics"
        };

        // Act
        var validationResults = ValidateCommand(command);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("Description"));
    }

    [TestMethod]
    public void Should_Pass_Validation_With_Empty_ImageUrls()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "Test Product",
            Description = "This is a test product with a detailed description",
            Price = 29.99m,
            Stock = 100,
            Category = "Electronics",
            ImageUrls = new List<string>()
        };

        // Act
        var validationResults = ValidateCommand(command);

        // Assert
        validationResults.Should().BeEmpty();
    }

    private static List<ValidationResult> ValidateCommand(CreateProductCommand command)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(command);
        Validator.TryValidateObject(command, validationContext, validationResults, true);
        return validationResults;
    }
}