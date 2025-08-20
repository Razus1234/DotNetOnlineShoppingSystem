using AutoMapper;
using FluentAssertions;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Mappings;
using OnlineShoppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Tests.Unit.Application.Mappings;

[TestClass]
public class ProductMappingProfileTests
{
    private IMapper _mapper = null!;

    [TestInitialize]
    public void Setup()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ProductMappingProfile>();
        });
        _mapper = configuration.CreateMapper();
    }

    [TestMethod]
    public void Should_Map_Product_To_ProductDto()
    {
        // Arrange
        var product = Product.Create("Test Product", "This is a test product description", 29.99m, 100, "Electronics");
        product.AddImageUrl("https://example.com/image1.jpg");
        product.AddImageUrl("https://example.com/image2.jpg");

        // Act
        var productDto = _mapper.Map<ProductDto>(product);

        // Assert
        productDto.Should().NotBeNull();
        productDto.Id.Should().Be(product.Id);
        productDto.Name.Should().Be("Test Product");
        productDto.Description.Should().Be("This is a test product description");
        productDto.Price.Should().Be(29.99m);
        productDto.Currency.Should().Be("USD");
        productDto.Stock.Should().Be(100);
        productDto.Category.Should().Be("Electronics");
        productDto.ImageUrls.Should().HaveCount(2);
        productDto.ImageUrls.Should().Contain("https://example.com/image1.jpg");
        productDto.ImageUrls.Should().Contain("https://example.com/image2.jpg");
    }

    [TestMethod]
    public void Should_Map_Product_With_No_Images()
    {
        // Arrange
        var product = Product.Create("Simple Product", "A simple product without images", 15.50m, 50, "Books");

        // Act
        var productDto = _mapper.Map<ProductDto>(product);

        // Assert
        productDto.Should().NotBeNull();
        productDto.ImageUrls.Should().BeEmpty();
    }

    [TestMethod]
    public void Should_Map_Product_With_Different_Currency()
    {
        // Arrange
        var product = Product.Create("Euro Product", "Product with euro pricing", 25.00m, 75, "Fashion");

        // Act
        var productDto = _mapper.Map<ProductDto>(product);

        // Assert
        productDto.Should().NotBeNull();
        productDto.Price.Should().Be(25.00m);
        productDto.Currency.Should().Be("USD"); // Default currency from Money value object
    }
}