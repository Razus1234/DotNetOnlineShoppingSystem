using System.ComponentModel.DataAnnotations;
using OnlineShoppingSystem.Application.DTOs;

namespace OnlineShoppingSystem.Application.Commands.Order;

public class PlaceOrderCommand
{
    [Required]
    public AddressDto ShippingAddress { get; set; } = new();
}