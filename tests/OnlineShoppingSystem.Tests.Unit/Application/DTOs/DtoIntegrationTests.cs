using AutoMapper;
using FluentAssertions;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Mappings;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.ValueObjects;
using OnlineShoppingSystem.Domain.Enums;

namespace OnlineShoppingSystem.Tests.Unit.Application.DTOs;

[TestClass]
public class DtoIntegrationTests
{
    private IMapper _mapper = null!;

    [TestInitialize]
    public void Setup()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<UserMappingProfile>();
            cfg.AddProfile<ProductMappingProfile>();
            cfg.AddProfile<CartMappingProfile>();
            cfg.AddProfile<OrderMappingProfile>();
            cfg.AddProfile<PaymentMappingProfile>();
        });
        _mapper = configuration.CreateMapper();
    }

    [TestMethod]
    public void Should_Map_Complete_Order_With_All_Related_Entities()
    {
        // Arrange
        var user = User.Create("customer@example.com", "$2a$11$N9qo8uLOickgx2ZMRZoMye.IjdQXjXqjqkqhyuYLxtLiatu4W.ZSO", "John Customer");
        var product1 = Product.Create("Laptop", "High-performance laptop", 999.99m, 10, "Electronics");
        var product2 = Product.Create("Mouse", "Wireless mouse", 29.99m, 50, "Electronics");
        
        var cart = Cart.Create(user.Id);
        cart.AddItem(product1, 1);
        cart.AddItem(product2, 2);

        var shippingAddress = new Address("123 Main St", "New York", "10001", "USA");
        var orderItems = cart.Items.Select(item => 
            new OrderItem(Guid.NewGuid(), item.ProductId, item.ProductName, item.Price, item.Quantity)).ToList();
        
        var order = new Order(user.Id, shippingAddress, orderItems);
        var payment = Payment.Create(order.Id, order.Total.Amount, "txn_12345", "Credit Card");
        payment.MarkAsCompleted();
        order.AttachPayment(payment);

        // Act
        var userDto = _mapper.Map<UserDto>(user);
        var cartDto = _mapper.Map<CartDto>(cart);
        var orderDto = _mapper.Map<OrderDto>(order);
        var paymentDto = _mapper.Map<PaymentDto>(payment);

        // Assert
        userDto.Should().NotBeNull();
        userDto.Email.Should().Be("customer@example.com");
        userDto.FullName.Should().Be("John Customer");

        cartDto.Should().NotBeNull();
        cartDto.Items.Should().HaveCount(2);
        cartDto.Total.Should().Be(1059.97m); // 999.99 + (29.99 * 2)

        orderDto.Should().NotBeNull();
        orderDto.Status.Should().Be(OrderStatus.Pending);
        orderDto.Items.Should().HaveCount(2);
        orderDto.Total.Should().Be(1059.97m);
        orderDto.Payment.Should().NotBeNull();
        orderDto.Payment!.Status.Should().Be(PaymentStatus.Completed);

        paymentDto.Should().NotBeNull();
        paymentDto.TransactionId.Should().Be("txn_12345");
        paymentDto.Amount.Should().Be(1059.97m);
    }

    [TestMethod]
    public void Should_Handle_Empty_Collections_In_DTOs()
    {
        // Arrange
        var user = User.Create("empty@example.com", "$2a$11$N9qo8uLOickgx2ZMRZoMye.IjdQXjXqjqkqhyuYLxtLiatu4W.ZSO", "Empty User");
        var cart = Cart.Create(user.Id);
        var product = Product.Create("Simple Product", "A product without images", 10.00m, 5, "Simple");

        // Act
        var userDto = _mapper.Map<UserDto>(user);
        var cartDto = _mapper.Map<CartDto>(cart);
        var productDto = _mapper.Map<ProductDto>(product);

        // Assert
        userDto.Addresses.Should().BeEmpty();
        cartDto.Items.Should().BeEmpty();
        cartDto.Total.Should().Be(0);
        cartDto.ItemCount.Should().Be(0);
        productDto.ImageUrls.Should().BeEmpty();
    }

    [TestMethod]
    public void Should_Preserve_Decimal_Precision_In_Money_Mappings()
    {
        // Arrange
        var product = Product.Create("Precision Product", "Product with precise pricing", 123.456m, 10, "Test");
        var cart = Cart.Create(Guid.NewGuid());
        cart.AddItem(product, 3);

        // Act
        var productDto = _mapper.Map<ProductDto>(product);
        var cartDto = _mapper.Map<CartDto>(cart);

        // Assert
        productDto.Price.Should().Be(123.46m); // Money rounds to 2 decimal places
        cartDto.Total.Should().Be(370.38m); // 123.46 * 3
    }
}