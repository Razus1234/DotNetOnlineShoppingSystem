using AutoMapper;
using FluentAssertions;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Mappings;
using OnlineShoppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Tests.Unit.Application.Mappings;

[TestClass]
public class CartMappingProfileTests
{
    private IMapper _mapper = null!;

    [TestInitialize]
    public void Setup()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<CartMappingProfile>();
        });
        _mapper = configuration.CreateMapper();
    }

    [TestMethod]
    public void Should_Map_Cart_To_CartDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = Cart.Create(userId);
        var product1 = Product.Create("Product 1", "Description 1", 10.00m, 100, "Category1");
        var product2 = Product.Create("Product 2", "Description 2", 20.00m, 50, "Category2");
        
        cart.AddItem(product1, 2);
        cart.AddItem(product2, 1);

        // Act
        var cartDto = _mapper.Map<CartDto>(cart);

        // Assert
        cartDto.Should().NotBeNull();
        cartDto.Id.Should().Be(cart.Id);
        cartDto.UserId.Should().Be(userId);
        cartDto.Items.Should().HaveCount(2);
        cartDto.Total.Should().Be(40.00m); // (10 * 2) + (20 * 1)
        cartDto.Currency.Should().Be("USD");
        cartDto.ItemCount.Should().Be(3); // 2 + 1
    }

    [TestMethod]
    public void Should_Map_CartItem_To_CartItemDto()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = Product.Create("Test Product", "Test Description", 15.99m, 10, "Test Category");
        var cart = Cart.Create(Guid.NewGuid());
        cart.AddItem(product, 3);
        var cartItem = cart.Items.First();

        // Act
        var cartItemDto = _mapper.Map<CartItemDto>(cartItem);

        // Assert
        cartItemDto.Should().NotBeNull();
        cartItemDto.Id.Should().Be(cartItem.Id);
        cartItemDto.ProductId.Should().Be(cartItem.ProductId);
        cartItemDto.ProductName.Should().Be("Test Product");
        cartItemDto.Price.Should().Be(15.99m);
        cartItemDto.Currency.Should().Be("USD");
        cartItemDto.Quantity.Should().Be(3);
        cartItemDto.Subtotal.Should().Be(47.97m); // 15.99 * 3
    }

    [TestMethod]
    public void Should_Map_Empty_Cart()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cart = Cart.Create(userId);

        // Act
        var cartDto = _mapper.Map<CartDto>(cart);

        // Assert
        cartDto.Should().NotBeNull();
        cartDto.Items.Should().BeEmpty();
        cartDto.Total.Should().Be(0);
        cartDto.ItemCount.Should().Be(0);
    }
}