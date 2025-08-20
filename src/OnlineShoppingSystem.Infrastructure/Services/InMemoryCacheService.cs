using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OnlineShoppingSystem.Application.Common.Interfaces;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OnlineShoppingSystem.Infrastructure.Services;

/// <summary>
/// In-memory implementation of the cache service
/// </summary>
public class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<InMemoryCacheService> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _keyExpirations;

    public InMemoryCacheService(IMemoryCache memoryCache, ILogger<InMemoryCacheService> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyExpirations = new ConcurrentDictionary<string, DateTime>();
    }

    public Task<T?> GetAsync<T>(string key) where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        try
        {
            if (_memoryCache.TryGetValue(key, out var cachedValue))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                
                if (cachedValue is string jsonValue)
                {
                    return Task.FromResult(JsonSerializer.Deserialize<T>(jsonValue));
                }
                
                return Task.FromResult(cachedValue as T);
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            return Task.FromResult<T?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving value from cache for key: {Key}", key);
            return Task.FromResult<T?>(null);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        if (expiration <= TimeSpan.Zero)
            throw new ArgumentException("Expiration must be positive", nameof(expiration));

        try
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration,
                Priority = CacheItemPriority.Normal
            };

            // Register callback to remove from tracking when expired
            options.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
            {
                _keyExpirations.TryRemove(evictedKey.ToString()!, out _);
                _logger.LogDebug("Cache entry evicted for key: {Key}, Reason: {Reason}", evictedKey, reason);
            });

            // Serialize complex objects to JSON for consistent storage
            var cacheValue = typeof(T) == typeof(string) ? (object)value : JsonSerializer.Serialize(value);
            
            _memoryCache.Set(key, cacheValue, options);
            _keyExpirations[key] = DateTime.UtcNow.Add(expiration);

            _logger.LogDebug("Cached value for key: {Key} with expiration: {Expiration}", key, expiration);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
            throw;
        }
    }

    public Task RemoveAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        try
        {
            _memoryCache.Remove(key);
            _keyExpirations.TryRemove(key, out _);
            
            _logger.LogDebug("Removed cache entry for key: {Key}", key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entry for key: {Key}", key);
            throw;
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));

        try
        {
            var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var keysToRemove = _keyExpirations.Keys
                .Where(key => regex.IsMatch(key))
                .ToList();

            foreach (var key in keysToRemove)
            {
                await RemoveAsync(key);
            }

            _logger.LogDebug("Removed {Count} cache entries matching pattern: {Pattern}", keysToRemove.Count, pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entries by pattern: {Pattern}", pattern);
            throw;
        }
    }

    public Task<bool> ExistsAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        try
        {
            var exists = _memoryCache.TryGetValue(key, out _);
            _logger.LogDebug("Cache key {Key} exists: {Exists}", key, exists);
            return Task.FromResult(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache key existence: {Key}", key);
            return Task.FromResult(false);
        }
    }
}