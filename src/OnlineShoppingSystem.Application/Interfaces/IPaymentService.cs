using OnlineShoppingSystem.Application.Commands.Payment;
using OnlineShoppingSystem.Application.DTOs;

namespace OnlineShoppingSystem.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentCommand command);
    Task<PaymentDto> GetPaymentByOrderIdAsync(Guid orderId);
    Task<PaymentDto> GetPaymentByIdAsync(Guid paymentId);
    Task<PaymentResultDto> RefundPaymentAsync(Guid paymentId, decimal? refundAmount = null);
    Task<bool> IsPaymentDuplicateAsync(Guid orderId, string paymentToken);
}