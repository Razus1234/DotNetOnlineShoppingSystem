using OnlineShoppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Application.Common.Interfaces;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetByOrderIdAsync(Guid orderId);
    Task<Payment?> GetByTransactionIdAsync(string transactionId);
}