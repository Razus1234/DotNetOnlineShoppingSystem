using Microsoft.Extensions.Logging;
using OnlineShoppingSystem.Application.Commands.User;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.DTOs;

namespace OnlineShoppingSystem.Application.Services;

/// <summary>
/// Decorator for UserService that adds caching functionality for frequently accessed user data
/// </summary>
public class CachedUserService : IUserService
{
    private readonly IUserService _userService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedUserService> _logger;

    // Cache configuration
    private static readonly TimeSpan UserProfileCacheExpiration = TimeSpan.FromMinutes(30);
    
    // Cache key patterns
    private const string UserProfileCacheKeyPrefix = "user:profile:";

    public CachedUserService(
        IUserService userService,
        ICacheService cacheService,
        ILogger<CachedUserService> logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UserDto> RegisterAsync(RegisterUserCommand command)
    {
        // Registration doesn't need caching as it's a one-time operation
        var result = await _userService.RegisterAsync(command);
        
        // Cache the newly created user profile
        var cacheKey = $"{UserProfileCacheKeyPrefix}{result.Id}";
        await _cacheService.SetAsync(cacheKey, result, UserProfileCacheExpiration);
        _logger.LogDebug("Cached new user profile: {UserId}", result.Id);

        return result;
    }

    public async Task<AuthTokenDto> LoginAsync(LoginCommand command)
    {
        // Login doesn't need caching as it involves authentication logic
        // and should always validate against the current state
        return await _userService.LoginAsync(command);
    }

    public async Task<UserDto> GetUserProfileAsync(Guid userId)
    {
        var cacheKey = $"{UserProfileCacheKeyPrefix}{userId}";
        
        var cachedProfile = await _cacheService.GetAsync<UserDto>(cacheKey);
        if (cachedProfile != null)
        {
            _logger.LogDebug("Retrieved user profile from cache: {UserId}", userId);
            return cachedProfile;
        }

        var profile = await _userService.GetUserProfileAsync(userId);
        
        if (profile != null)
        {
            await _cacheService.SetAsync(cacheKey, profile, UserProfileCacheExpiration);
            _logger.LogDebug("Cached user profile: {UserId}", userId);
        }

        return profile;
    }

    public async Task<UserDto> UpdateProfileAsync(Guid userId, UpdateUserCommand command)
    {
        var result = await _userService.UpdateProfileAsync(userId, command);
        
        // Invalidate the cached user profile since it has been updated
        await InvalidateUserProfileCache(userId);
        
        // Cache the updated profile
        var cacheKey = $"{UserProfileCacheKeyPrefix}{userId}";
        await _cacheService.SetAsync(cacheKey, result, UserProfileCacheExpiration);
        _logger.LogDebug("Updated and cached user profile: {UserId}", userId);

        return result;
    }

    private async Task InvalidateUserProfileCache(Guid userId)
    {
        var cacheKey = $"{UserProfileCacheKeyPrefix}{userId}";
        await _cacheService.RemoveAsync(cacheKey);
        _logger.LogDebug("Invalidated user profile cache: {UserId}", userId);
    }
}