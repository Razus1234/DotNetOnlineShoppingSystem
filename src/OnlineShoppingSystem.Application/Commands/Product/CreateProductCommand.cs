using System.ComponentModel.DataAnnotations;

namespace OnlineShoppingSystem.Application.Commands.Product;

public class CreateProductCommand
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(2000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero")]
    public decimal Price { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
    public int Stock { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Category { get; set; } = string.Empty;

    public List<string> ImageUrls { get; set; } = new();
}