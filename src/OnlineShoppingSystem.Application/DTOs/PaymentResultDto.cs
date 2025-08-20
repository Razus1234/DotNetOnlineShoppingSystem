using OnlineShoppingSystem.Domain.Enums;

namespace OnlineShoppingSystem.Application.DTOs;

public class PaymentResultDto
{
    public bool IsSuccessful { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? ErrorMessage { get; set; }
    public PaymentDto? Payment { get; set; }
}