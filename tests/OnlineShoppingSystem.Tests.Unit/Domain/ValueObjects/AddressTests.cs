using FluentAssertions;
using OnlineShoppingSystem.Domain.ValueObjects;

namespace OnlineShoppingSystem.Tests.Unit.Domain.ValueObjects;

[TestClass]
public class AddressTests
{
    [TestMethod]
    public void Constructor_WithValidParameters_ShouldCreateAddress()
    {
        // Arrange
        var street = "123 Main St";
        var city = "New York";
        var postalCode = "10001";
        var country = "USA";

        // Act
        var address = new Address(street, city, postalCode, country);

        // Assert
        address.Street.Should().Be(street);
        address.City.Should().Be(city);
        address.PostalCode.Should().Be(postalCode);
        address.Country.Should().Be(country);
    }

    [TestMethod]
    public void Constructor_WithWhitespaceParameters_ShouldTrimValues()
    {
        // Arrange
        var street = "  123 Main St  ";
        var city = "  New York  ";
        var postalCode = "  10001  ";
        var country = "  USA  ";

        // Act
        var address = new Address(street, city, postalCode, country);

        // Assert
        address.Street.Should().Be("123 Main St");
        address.City.Should().Be("New York");
        address.PostalCode.Should().Be("10001");
        address.Country.Should().Be("USA");
    }

    [TestMethod]
    [DataRow(null, "City", "12345", "USA")]
    [DataRow("", "City", "12345", "USA")]
    [DataRow("   ", "City", "12345", "USA")]
    [DataRow("Street", null, "12345", "USA")]
    [DataRow("Street", "", "12345", "USA")]
    [DataRow("Street", "   ", "12345", "USA")]
    [DataRow("Street", "City", null, "USA")]
    [DataRow("Street", "City", "", "USA")]
    [DataRow("Street", "City", "   ", "USA")]
    [DataRow("Street", "City", "12345", null)]
    [DataRow("Street", "City", "12345", "")]
    [DataRow("Street", "City", "12345", "   ")]
    public void Constructor_WithInvalidParameters_ShouldThrowArgumentException(
        string street, string city, string postalCode, string country)
    {
        // Act & Assert
        var act = () => new Address(street, city, postalCode, country);
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "10001", "USA");
        var address2 = new Address("123 Main St", "New York", "10001", "USA");

        // Act & Assert
        address1.Should().Be(address2);
        (address1 == address2).Should().BeTrue();
        (address1 != address2).Should().BeFalse();
    }

    [TestMethod]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "10001", "USA");
        var address2 = new Address("456 Oak Ave", "New York", "10001", "USA");

        // Act & Assert
        address1.Should().NotBe(address2);
        (address1 == address2).Should().BeFalse();
        (address1 != address2).Should().BeTrue();
    }

    [TestMethod]
    public void GetHashCode_WithSameValues_ShouldReturnSameHashCode()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "10001", "USA");
        var address2 = new Address("123 Main St", "New York", "10001", "USA");

        // Act & Assert
        address1.GetHashCode().Should().Be(address2.GetHashCode());
    }

    [TestMethod]
    public void ToString_ShouldReturnFormattedAddress()
    {
        // Arrange
        var address = new Address("123 Main St", "New York", "10001", "USA");

        // Act
        var result = address.ToString();

        // Assert
        result.Should().Be("123 Main St, New York, 10001, USA");
    }
}