using System.ComponentModel.DataAnnotations;

namespace OnlineShoppingSystem.Application.Commands.Cart;

public class UpdateCartItemCommand
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
    public int Quantity { get; set; }
}