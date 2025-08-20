using AutoMapper;
using FluentAssertions;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Mappings;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.ValueObjects;
using OnlineShoppingSystem.Domain.Enums;

namespace OnlineShoppingSystem.Tests.Unit.Application.Mappings;

[TestClass]
public class OrderMappingProfileTests
{
    private IMapper _mapper = null!;

    [TestInitialize]
    public void Setup()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<OrderMappingProfile>();
            cfg.AddProfile<UserMappingProfile>(); // For Address mapping
            cfg.AddProfile<PaymentMappingProfile>(); // For Payment mapping
        });
        _mapper = configuration.CreateMapper();
    }

    [TestMethod]
    public void Should_Map_Order_To_OrderDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var shippingAddress = new Address("123 Main St", "New York", "10001", "USA");
        var orderItems = new List<OrderItem>
        {
            new OrderItem(Guid.NewGuid(), Guid.NewGuid(), "Product 1", new Money(10.00m), 2),
            new OrderItem(Guid.NewGuid(), Guid.NewGuid(), "Product 2", new Money(15.00m), 1)
        };
        
        var order = new Order(userId, shippingAddress, orderItems);

        // Act
        var orderDto = _mapper.Map<OrderDto>(order);

        // Assert
        orderDto.Should().NotBeNull();
        orderDto.Id.Should().Be(order.Id);
        orderDto.UserId.Should().Be(userId);
        orderDto.Status.Should().Be(OrderStatus.Pending);
        orderDto.Total.Should().Be(35.00m); // (10 * 2) + (15 * 1)
        orderDto.Currency.Should().Be("USD");
        orderDto.ShippingAddress.Should().NotBeNull();
        orderDto.ShippingAddress.Street.Should().Be("123 Main St");
        orderDto.Items.Should().HaveCount(2);
    }

    [TestMethod]
    public void Should_Map_OrderItem_To_OrderItemDto()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var orderItem = new OrderItem(orderId, productId, "Test Product", new Money(25.50m), 3);

        // Act
        var orderItemDto = _mapper.Map<OrderItemDto>(orderItem);

        // Assert
        orderItemDto.Should().NotBeNull();
        orderItemDto.Id.Should().Be(orderItem.Id);
        orderItemDto.ProductId.Should().Be(productId);
        orderItemDto.ProductName.Should().Be("Test Product");
        orderItemDto.Price.Should().Be(25.50m);
        orderItemDto.Currency.Should().Be("USD");
        orderItemDto.Quantity.Should().Be(3);
        orderItemDto.Subtotal.Should().Be(76.50m); // 25.50 * 3
    }

    [TestMethod]
    public void Should_Map_Order_With_Payment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var shippingAddress = new Address("456 Oak Ave", "Los Angeles", "90210", "USA");
        var orderItems = new List<OrderItem>
        {
            new OrderItem(Guid.NewGuid(), Guid.NewGuid(), "Product 1", new Money(50.00m), 1)
        };
        
        var order = new Order(userId, shippingAddress, orderItems);
        var payment = Payment.Create(order.Id, 50.00m, "txn_123", "Credit Card");
        payment.MarkAsCompleted();
        order.AttachPayment(payment);

        // Act
        var orderDto = _mapper.Map<OrderDto>(order);

        // Assert
        orderDto.Should().NotBeNull();
        orderDto.Payment.Should().NotBeNull();
        orderDto.Payment!.TransactionId.Should().Be("txn_123");
        orderDto.Payment.Status.Should().Be(PaymentStatus.Completed);
    }
}