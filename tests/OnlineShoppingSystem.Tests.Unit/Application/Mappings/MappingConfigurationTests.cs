using AutoMapper;
using FluentAssertions;
using OnlineShoppingSystem.Application.Mappings;

namespace OnlineShoppingSystem.Tests.Unit.Application.Mappings;

[TestClass]
public class MappingConfigurationTests
{
    [TestMethod]
    public void Should_Have_Valid_AutoMapper_Configuration()
    {
        // Arrange
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<UserMappingProfile>();
            cfg.AddProfile<ProductMappingProfile>();
            cfg.AddProfile<CartMappingProfile>();
            cfg.AddProfile<OrderMappingProfile>();
            cfg.AddProfile<PaymentMappingProfile>();
        });

        // Act & Assert
        configuration.AssertConfigurationIsValid();
    }

    [TestMethod]
    public void Should_Create_Mapper_Successfully()
    {
        // Arrange
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<UserMappingProfile>();
            cfg.AddProfile<ProductMappingProfile>();
            cfg.AddProfile<CartMappingProfile>();
            cfg.AddProfile<OrderMappingProfile>();
            cfg.AddProfile<PaymentMappingProfile>();
        });

        // Act
        var mapper = configuration.CreateMapper();

        // Assert
        mapper.Should().NotBeNull();
    }
}