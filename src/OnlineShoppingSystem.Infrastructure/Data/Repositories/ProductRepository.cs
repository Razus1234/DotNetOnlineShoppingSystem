using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.Common.Models;
using OnlineShoppingSystem.Application.Queries;
using OnlineShoppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Infrastructure.Data.Repositories;

public class ProductRepository : BaseRepository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<Product>> GetPagedAsync(ProductQuery query)
    {
        var queryable = _dbSet.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(query.Keyword))
        {
            queryable = queryable.Where(p => 
                p.Name.Contains(query.Keyword) || 
                p.Description.Contains(query.Keyword));
        }

        if (!string.IsNullOrEmpty(query.Category))
        {
            queryable = queryable.Where(p => p.Category == query.Category);
        }

        if (query.MinPrice.HasValue)
        {
            queryable = queryable.Where(p => p.Price.Amount >= query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            queryable = queryable.Where(p => p.Price.Amount <= query.MaxPrice.Value);
        }

        if (query.MinStock.HasValue)
        {
            queryable = queryable.Where(p => p.Stock >= query.MinStock.Value);
        }

        if (query.MaxStock.HasValue)
        {
            queryable = queryable.Where(p => p.Stock <= query.MaxStock.Value);
        }

        var totalCount = await queryable.CountAsync();

        var items = await queryable
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<Product>(items, totalCount, query.PageNumber, query.PageSize);
    }

    public async Task<IEnumerable<Product>> SearchAsync(string keyword)
    {
        return await _dbSet
            .Where(p => p.Name.Contains(keyword) || p.Description.Contains(keyword))
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(string category)
    {
        return await _dbSet
            .Where(p => p.Category == category)
            .ToListAsync();
    }
}