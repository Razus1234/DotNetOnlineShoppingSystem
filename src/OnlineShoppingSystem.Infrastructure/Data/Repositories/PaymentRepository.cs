using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Infrastructure.Data.Repositories;

public class PaymentRepository : BaseRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Payment?> GetByOrderIdAsync(Guid orderId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.OrderId == orderId);
    }

    public async Task<Payment?> GetByTransactionIdAsync(string transactionId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId);
    }

    public override async Task<Payment?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.Id == id);
    }
}