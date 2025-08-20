using FluentAssertions;
using OnlineShoppingSystem.Application.Commands.User;
using System.ComponentModel.DataAnnotations;

namespace OnlineShoppingSystem.Tests.Unit.Application.Commands;

[TestClass]
public class RegisterUserCommandTests
{
    [TestMethod]
    public void Should_Pass_Validation_With_Valid_Data()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "test@example.com",
            Password = "SecurePassword123",
            FullName = "John Doe"
        };

        // Act
        var validationResults = ValidateCommand(command);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    public void Should_Fail_Validation_With_Invalid_Email()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "invalid-email",
            Password = "SecurePassword123",
            FullName = "John Doe"
        };

        // Act
        var validationResults = ValidateCommand(command);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("Email"));
    }

    [TestMethod]
    public void Should_Fail_Validation_With_Short_Password()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "test@example.com",
            Password = "short",
            FullName = "John Doe"
        };

        // Act
        var validationResults = ValidateCommand(command);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("Password"));
    }

    [TestMethod]
    public void Should_Fail_Validation_With_Short_FullName()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "test@example.com",
            Password = "SecurePassword123",
            FullName = "A"
        };

        // Act
        var validationResults = ValidateCommand(command);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("FullName"));
    }

    [TestMethod]
    public void Should_Fail_Validation_With_Empty_Required_Fields()
    {
        // Arrange
        var command = new RegisterUserCommand();

        // Act
        var validationResults = ValidateCommand(command);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(r => r.MemberNames.Contains("Email"));
        validationResults.Should().Contain(r => r.MemberNames.Contains("Password"));
        validationResults.Should().Contain(r => r.MemberNames.Contains("FullName"));
    }

    private static List<ValidationResult> ValidateCommand(RegisterUserCommand command)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(command);
        Validator.TryValidateObject(command, validationContext, validationResults, true);
        return validationResults;
    }
}