namespace OnlineShoppingSystem.Application.Common.Interfaces;

/// <summary>
/// Interface for caching service operations
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached value by key
    /// </summary>
    /// <typeparam name="T">Type of the cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <returns>Cached value or default if not found</returns>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>
    /// Sets a value in cache with expiration
    /// </summary>
    /// <typeparam name="T">Type of the value to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="expiration">Cache expiration time</param>
    Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class;

    /// <summary>
    /// Removes a value from cache
    /// </summary>
    /// <param name="key">Cache key to remove</param>
    Task RemoveAsync(string key);

    /// <summary>
    /// Removes all cache entries that match the pattern
    /// </summary>
    /// <param name="pattern">Pattern to match cache keys</param>
    Task RemoveByPatternAsync(string pattern);

    /// <summary>
    /// Checks if a key exists in cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>True if key exists, false otherwise</returns>
    Task<bool> ExistsAsync(string key);
}