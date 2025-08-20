using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Infrastructure.Data.Repositories;

public class CartRepository : BaseRepository<Cart>, ICartRepository
{
    public CartRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Cart?> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<Cart?> GetByUserIdWithItemsAsync(Guid userId)
    {
        return await _dbSet
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public override async Task<Cart?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id);
    }
}