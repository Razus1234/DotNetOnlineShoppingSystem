using Microsoft.Extensions.Logging;
using OnlineShoppingSystem.Application.Commands.Product;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Queries;

namespace OnlineShoppingSystem.Application.Services;

/// <summary>
/// Decorator for ProductService that adds caching functionality
/// </summary>
public class CachedProductService : IProductService
{
    private readonly IProductService _productService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedProductService> _logger;

    // Cache configuration
    private static readonly TimeSpan ProductCacheExpiration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ProductListCacheExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SearchCacheExpiration = TimeSpan.FromMinutes(10);

    // Cache key patterns
    private const string ProductCacheKeyPrefix = "product:";
    private const string ProductListCacheKeyPrefix = "products:";
    private const string SearchCacheKeyPrefix = "search:";
    private const string CategoryCacheKeyPrefix = "category:";

    public CachedProductService(
        IProductService productService, 
        ICacheService cacheService, 
        ILogger<CachedProductService> logger)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResultDto<ProductDto>> GetProductsAsync(ProductQuery query)
    {
        var cacheKey = GenerateProductListCacheKey(query);
        
        var cachedResult = await _cacheService.GetAsync<PagedResultDto<ProductDto>>(cacheKey);
        if (cachedResult != null)
        {
            _logger.LogDebug("Retrieved products from cache with key: {CacheKey}", cacheKey);
            return cachedResult;
        }

        var result = await _productService.GetProductsAsync(query);
        
        await _cacheService.SetAsync(cacheKey, result, ProductListCacheExpiration);
        _logger.LogDebug("Cached products with key: {CacheKey}", cacheKey);

        return result;
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid productId)
    {
        var cacheKey = $"{ProductCacheKeyPrefix}{productId}";
        
        var cachedProduct = await _cacheService.GetAsync<ProductDto>(cacheKey);
        if (cachedProduct != null)
        {
            _logger.LogDebug("Retrieved product from cache: {ProductId}", productId);
            return cachedProduct;
        }

        var product = await _productService.GetProductByIdAsync(productId);
        if (product != null)
        {
            await _cacheService.SetAsync(cacheKey, product, ProductCacheExpiration);
            _logger.LogDebug("Cached product: {ProductId}", productId);
        }

        return product;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductCommand command)
    {
        var result = await _productService.CreateProductAsync(command);
        
        // Invalidate related caches
        await InvalidateProductCaches();
        _logger.LogDebug("Invalidated product caches after creating product: {ProductId}", result.Id);

        return result;
    }

    public async Task<ProductDto> UpdateProductAsync(Guid productId, UpdateProductCommand command)
    {
        var result = await _productService.UpdateProductAsync(productId, command);
        
        // Invalidate specific product cache and related caches
        await InvalidateProductCache(productId);
        await InvalidateProductCaches();
        _logger.LogDebug("Invalidated caches after updating product: {ProductId}", productId);

        return result;
    }

    public async Task DeleteProductAsync(Guid productId)
    {
        await _productService.DeleteProductAsync(productId);
        
        // Invalidate specific product cache and related caches
        await InvalidateProductCache(productId);
        await InvalidateProductCaches();
        _logger.LogDebug("Invalidated caches after deleting product: {ProductId}", productId);
    }

    public async Task<ProductDto> UpdateStockAsync(Guid productId, int newStock)
    {
        var result = await _productService.UpdateStockAsync(productId, newStock);
        
        // Invalidate specific product cache (stock changes affect product data)
        await InvalidateProductCache(productId);
        _logger.LogDebug("Invalidated product cache after stock update: {ProductId}", productId);

        return result;
    }

    public async Task<bool> IsProductInStockAsync(Guid productId, int requestedQuantity = 1)
    {
        // For stock checks, we don't cache as stock can change frequently
        // and we need real-time accuracy for inventory decisions
        return await _productService.IsProductInStockAsync(productId, requestedQuantity);
    }

    public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string keyword)
    {
        var cacheKey = $"{SearchCacheKeyPrefix}{keyword?.ToLowerInvariant()}";
        
        var cachedResults = await _cacheService.GetAsync<IEnumerable<ProductDto>>(cacheKey);
        if (cachedResults != null)
        {
            _logger.LogDebug("Retrieved search results from cache for keyword: {Keyword}", keyword);
            return cachedResults;
        }

        var results = await _productService.SearchProductsAsync(keyword);
        
        if (results.Any())
        {
            await _cacheService.SetAsync(cacheKey, results, SearchCacheExpiration);
            _logger.LogDebug("Cached search results for keyword: {Keyword}", keyword);
        }

        return results;
    }

    public async Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(string category)
    {
        var cacheKey = $"{CategoryCacheKeyPrefix}{category?.ToLowerInvariant()}";
        
        var cachedResults = await _cacheService.GetAsync<IEnumerable<ProductDto>>(cacheKey);
        if (cachedResults != null)
        {
            _logger.LogDebug("Retrieved category products from cache: {Category}", category);
            return cachedResults;
        }

        var results = await _productService.GetProductsByCategoryAsync(category);
        
        if (results.Any())
        {
            await _cacheService.SetAsync(cacheKey, results, ProductListCacheExpiration);
            _logger.LogDebug("Cached category products: {Category}", category);
        }

        return results;
    }

    private async Task InvalidateProductCache(Guid productId)
    {
        var cacheKey = $"{ProductCacheKeyPrefix}{productId}";
        await _cacheService.RemoveAsync(cacheKey);
    }

    private async Task InvalidateProductCaches()
    {
        // Invalidate all product list caches, search caches, and category caches
        await _cacheService.RemoveByPatternAsync($"^{ProductListCacheKeyPrefix}.*");
        await _cacheService.RemoveByPatternAsync($"^{SearchCacheKeyPrefix}.*");
        await _cacheService.RemoveByPatternAsync($"^{CategoryCacheKeyPrefix}.*");
    }

    private static string GenerateProductListCacheKey(ProductQuery query)
    {
        var keyParts = new List<string>
        {
            ProductListCacheKeyPrefix,
            $"page:{query.PageNumber}",
            $"size:{query.PageSize}"
        };

        if (!string.IsNullOrWhiteSpace(query.Category))
            keyParts.Add($"cat:{query.Category.ToLowerInvariant()}");

        if (!string.IsNullOrWhiteSpace(query.Keyword))
            keyParts.Add($"kw:{query.Keyword.ToLowerInvariant()}");

        if (query.MinPrice.HasValue)
            keyParts.Add($"minp:{query.MinPrice.Value}");

        if (query.MaxPrice.HasValue)
            keyParts.Add($"maxp:{query.MaxPrice.Value}");

        return string.Join(":", keyParts);
    }
}