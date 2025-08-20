using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Infrastructure.Data.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .Include(u => u.Cart!)
                .ThenInclude(c => c.Items)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _dbSet.AnyAsync(u => u.Email == email);
    }

    public override async Task<User?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(u => u.Cart!)
                .ThenInclude(c => c.Items)
            .FirstOrDefaultAsync(u => u.Id == id);
    }
}