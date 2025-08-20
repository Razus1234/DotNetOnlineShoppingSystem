using System.ComponentModel.DataAnnotations;

namespace OnlineShoppingSystem.Application.Commands.Payment;

public class ProcessPaymentCommand
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = "Credit Card";

    public string? PaymentToken { get; set; }
    
    public Dictionary<string, string> PaymentDetails { get; set; } = new();
}