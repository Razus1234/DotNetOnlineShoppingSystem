using FluentAssertions;
using OnlineShoppingSystem.Domain.ValueObjects;

namespace OnlineShoppingSystem.Tests.Unit.Domain.ValueObjects;

[TestClass]
public class MoneyTests
{
    [TestMethod]
    public void Constructor_WithValidAmount_ShouldCreateMoney()
    {
        // Arrange
        var amount = 100.50m;
        var currency = "USD";

        // Act
        var money = new Money(amount, currency);

        // Assert
        money.Amount.Should().Be(amount);
        money.Currency.Should().Be(currency);
    }

    [TestMethod]
    public void Constructor_WithDefaultCurrency_ShouldUseUSD()
    {
        // Arrange
        var amount = 100.50m;

        // Act
        var money = new Money(amount);

        // Assert
        money.Amount.Should().Be(amount);
        money.Currency.Should().Be("USD");
    }

    [TestMethod]
    public void Constructor_WithLowercaseCurrency_ShouldConvertToUppercase()
    {
        // Arrange
        var amount = 100.50m;
        var currency = "eur";

        // Act
        var money = new Money(amount, currency);

        // Assert
        money.Currency.Should().Be("EUR");
    }

    [TestMethod]
    public void Constructor_WithMoreThanTwoDecimals_ShouldRoundToTwoDecimals()
    {
        // Arrange
        var amount = 100.567m;

        // Act
        var money = new Money(amount);

        // Assert
        money.Amount.Should().Be(100.57m);
    }

    [TestMethod]
    public void Constructor_WithNegativeAmount_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => new Money(-10.50m);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Amount cannot be negative*");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_WithInvalidCurrency_ShouldThrowArgumentException(string currency)
    {
        // Act & Assert
        var act = () => new Money(100m, currency);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Currency cannot be null or empty*");
    }

    [TestMethod]
    public void Addition_WithSameCurrency_ShouldReturnSum()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(50m, "USD");

        // Act
        var result = money1 + money2;

        // Assert
        result.Amount.Should().Be(150m);
        result.Currency.Should().Be("USD");
    }

    [TestMethod]
    public void Addition_WithDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(50m, "EUR");

        // Act & Assert
        var act = () => money1 + money2;
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Cannot add money with different currencies: USD and EUR");
    }

    [TestMethod]
    public void Subtraction_WithSameCurrency_ShouldReturnDifference()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(30m, "USD");

        // Act
        var result = money1 - money2;

        // Assert
        result.Amount.Should().Be(70m);
        result.Currency.Should().Be("USD");
    }

    [TestMethod]
    public void Subtraction_WithDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(30m, "EUR");

        // Act & Assert
        var act = () => money1 - money2;
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Cannot subtract money with different currencies: USD and EUR");
    }

    [TestMethod]
    public void Multiplication_WithDecimal_ShouldReturnProduct()
    {
        // Arrange
        var money = new Money(100m, "USD");
        var multiplier = 2.5m;

        // Act
        var result1 = money * multiplier;
        var result2 = multiplier * money;

        // Assert
        result1.Amount.Should().Be(250m);
        result1.Currency.Should().Be("USD");
        result2.Amount.Should().Be(250m);
        result2.Currency.Should().Be("USD");
    }

    [TestMethod]
    public void GreaterThan_WithSameCurrency_ShouldCompareAmounts()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(50m, "USD");

        // Act & Assert
        (money1 > money2).Should().BeTrue();
        (money2 > money1).Should().BeFalse();
    }

    [TestMethod]
    public void GreaterThan_WithDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(50m, "EUR");

        // Act & Assert
        var act = () => money1 > money2;
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Cannot compare money with different currencies: USD and EUR");
    }

    [TestMethod]
    public void LessThan_WithSameCurrency_ShouldCompareAmounts()
    {
        // Arrange
        var money1 = new Money(50m, "USD");
        var money2 = new Money(100m, "USD");

        // Act & Assert
        (money1 < money2).Should().BeTrue();
        (money2 < money1).Should().BeFalse();
    }

    [TestMethod]
    public void GreaterThanOrEqual_WithSameCurrency_ShouldCompareAmounts()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(100m, "USD");
        var money3 = new Money(50m, "USD");

        // Act & Assert
        (money1 >= money2).Should().BeTrue();
        (money1 >= money3).Should().BeTrue();
        (money3 >= money1).Should().BeFalse();
    }

    [TestMethod]
    public void LessThanOrEqual_WithSameCurrency_ShouldCompareAmounts()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(100m, "USD");
        var money3 = new Money(150m, "USD");

        // Act & Assert
        (money1 <= money2).Should().BeTrue();
        (money1 <= money3).Should().BeTrue();
        (money3 <= money1).Should().BeFalse();
    }

    [TestMethod]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(100m, "USD");

        // Act & Assert
        money1.Should().Be(money2);
        (money1 == money2).Should().BeTrue();
        (money1 != money2).Should().BeFalse();
    }

    [TestMethod]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(100m, "EUR");

        // Act & Assert
        money1.Should().NotBe(money2);
        (money1 == money2).Should().BeFalse();
        (money1 != money2).Should().BeTrue();
    }

    [TestMethod]
    public void GetHashCode_WithSameValues_ShouldReturnSameHashCode()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(100m, "USD");

        // Act & Assert
        money1.GetHashCode().Should().Be(money2.GetHashCode());
    }

    [TestMethod]
    public void ToString_ShouldReturnFormattedMoney()
    {
        // Arrange
        var money = new Money(100.50m, "USD");

        // Act
        var result = money.ToString();

        // Assert
        result.Should().Contain("100.50");
        result.Should().Contain("USD");
    }
}