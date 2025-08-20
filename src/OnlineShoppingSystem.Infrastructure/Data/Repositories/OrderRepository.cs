using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.Common.Models;
using OnlineShoppingSystem.Application.Queries;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.Enums;

namespace OnlineShoppingSystem.Infrastructure.Data.Repositories;

public class OrderRepository : BaseRepository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<Order>> GetByUserIdAsync(Guid userId, OrderQuery query)
    {
        var queryable = _dbSet
            .Include(o => o.Items)
            .Where(o => o.UserId == userId);

        // Apply filters
        if (query.Status.HasValue)
        {
            queryable = queryable.Where(o => o.Status == query.Status.Value);
        }

        if (query.FromDate.HasValue)
        {
            queryable = queryable.Where(o => o.CreatedAt >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            queryable = queryable.Where(o => o.CreatedAt <= query.ToDate.Value);
        }

        var totalCount = await queryable.CountAsync();

        var items = await queryable
            .OrderByDescending(o => o.CreatedAt)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<Order>(items, totalCount, query.PageNumber, query.PageSize);
    }

    public async Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status)
    {
        return await _dbSet
            .Include(o => o.Items)
            .Where(o => o.Status == status)
            .ToListAsync();
    }

    public async Task<Order?> GetByIdWithItemsAsync(Guid orderId)
    {
        return await _dbSet
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public override async Task<Order?> GetByIdAsync(Guid id)
    {
        return await GetByIdWithItemsAsync(id);
    }

    public async Task<PagedResult<Order>> GetAllOrdersAsync(AdminOrderQuery query)
    {
        var queryable = _dbSet
            .Include(o => o.Items)
            .AsQueryable();

        // Apply filters
        if (query.Status.HasValue)
        {
            queryable = queryable.Where(o => o.Status == query.Status.Value);
        }

        if (query.UserId.HasValue)
        {
            queryable = queryable.Where(o => o.UserId == query.UserId.Value);
        }

        if (query.StartDate.HasValue)
        {
            queryable = queryable.Where(o => o.CreatedAt >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            queryable = queryable.Where(o => o.CreatedAt <= query.EndDate.Value);
        }

        if (query.MinTotal.HasValue)
        {
            queryable = queryable.Where(o => o.Total.Amount >= query.MinTotal.Value);
        }

        if (query.MaxTotal.HasValue)
        {
            queryable = queryable.Where(o => o.Total.Amount <= query.MaxTotal.Value);
        }

        // Apply base query filters from OrderQuery
        if (query.FromDate.HasValue)
        {
            queryable = queryable.Where(o => o.CreatedAt >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            queryable = queryable.Where(o => o.CreatedAt <= query.ToDate.Value);
        }

        var totalCount = await queryable.CountAsync();

        var items = await queryable
            .OrderByDescending(o => o.CreatedAt)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<Order>(items, totalCount, query.PageNumber, query.PageSize);
    }
}