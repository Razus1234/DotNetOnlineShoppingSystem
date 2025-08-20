using AutoMapper;
using FluentAssertions;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Mappings;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.ValueObjects;

namespace OnlineShoppingSystem.Tests.Unit.Application.Mappings;

[TestClass]
public class UserMappingProfileTests
{
    private IMapper _mapper = null!;

    [TestInitialize]
    public void Setup()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<UserMappingProfile>();
        });
        _mapper = configuration.CreateMapper();
    }

    [TestMethod]
    public void Should_Map_User_To_UserDto()
    {
        // Arrange
        var user = User.Create("test@example.com", "$2a$11$N9qo8uLOickgx2ZMRZoMye.IjdQXjXqjqkqhyuYLxtLiatu4W.ZSO", "John Doe");
        user.AddAddress(new Address("123 Main St", "New York", "10001", "USA"));

        // Act
        var userDto = _mapper.Map<UserDto>(user);

        // Assert
        userDto.Should().NotBeNull();
        userDto.Id.Should().Be(user.Id);
        userDto.Email.Should().Be(user.Email);
        userDto.FullName.Should().Be(user.FullName);
        userDto.Addresses.Should().HaveCount(1);
        userDto.Addresses[0].Street.Should().Be("123 Main St");
        userDto.Addresses[0].City.Should().Be("New York");
        userDto.Addresses[0].PostalCode.Should().Be("10001");
        userDto.Addresses[0].Country.Should().Be("USA");
    }

    [TestMethod]
    public void Should_Map_Address_To_AddressDto()
    {
        // Arrange
        var address = new Address("456 Oak Ave", "Los Angeles", "90210", "USA");

        // Act
        var addressDto = _mapper.Map<AddressDto>(address);

        // Assert
        addressDto.Should().NotBeNull();
        addressDto.Street.Should().Be("456 Oak Ave");
        addressDto.City.Should().Be("Los Angeles");
        addressDto.PostalCode.Should().Be("90210");
        addressDto.Country.Should().Be("USA");
    }

    [TestMethod]
    public void Should_Map_AddressDto_To_Address()
    {
        // Arrange
        var addressDto = new AddressDto
        {
            Street = "789 Pine St",
            City = "Chicago",
            PostalCode = "60601",
            Country = "USA"
        };

        // Act
        var address = _mapper.Map<Address>(addressDto);

        // Assert
        address.Should().NotBeNull();
        address.Street.Should().Be("789 Pine St");
        address.City.Should().Be("Chicago");
        address.PostalCode.Should().Be("60601");
        address.Country.Should().Be("USA");
    }

    [TestMethod]
    public void Should_Map_User_With_Empty_Addresses()
    {
        // Arrange
        var user = User.Create("test@example.com", "$2a$11$N9qo8uLOickgx2ZMRZoMye.IjdQXjXqjqkqhyuYLxtLiatu4W.ZSO", "Jane Doe");

        // Act
        var userDto = _mapper.Map<UserDto>(user);

        // Assert
        userDto.Should().NotBeNull();
        userDto.Addresses.Should().BeEmpty();
    }
}