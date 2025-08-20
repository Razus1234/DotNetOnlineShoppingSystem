using OnlineShoppingSystem.Application.DTOs;

namespace OnlineShoppingSystem.Application.Interfaces;

public interface IPaymentGateway
{
    Task<PaymentGatewayResult> ProcessPaymentAsync(PaymentGatewayRequest request);
    Task<PaymentGatewayResult> RefundPaymentAsync(string transactionId, decimal amount);
    Task<PaymentGatewayStatus> GetPaymentStatusAsync(string transactionId);
}

public class PaymentGatewayRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentToken { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "Credit Card";
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class PaymentGatewayResult
{
    public bool IsSuccessful { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class PaymentGatewayStatus
{
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime? ProcessedAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}