using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OnlineShoppingSystem.Application.Commands.User;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Services;

namespace OnlineShoppingSystem.Tests.Unit.Application.Services;

[TestClass]
public class CachedUserServiceTests
{
    private Mock<IUserService> _mockUserService;
    private Mock<ICacheService> _mockCacheService;
    private Mock<ILogger<CachedUserService>> _mockLogger;
    private CachedUserService _cachedUserService;

    [TestInitialize]
    public void Setup()
    {
        _mockUserService = new Mock<IUserService>();
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<CachedUserService>>();
        _cachedUserService = new CachedUserService(
            _mockUserService.Object,
            _mockCacheService.Object,
            _mockLogger.Object);
    }

    [TestMethod]
    public async Task RegisterAsync_CallsServiceAndCachesResult()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "test@example.com",
            Password = "password123",
            FullName = "Test User"
        };
        var registeredUser = new UserDto 
        { 
            Id = Guid.NewGuid(), 
            Email = command.Email, 
            FullName = command.FullName 
        };
        var cacheKey = $"user:profile:{registeredUser.Id}";

        _mockUserService.Setup(x => x.RegisterAsync(command))
            .ReturnsAsync(registeredUser);

        // Act
        var result = await _cachedUserService.RegisterAsync(command);

        // Assert
        Assert.AreEqual(registeredUser, result);
        _mockUserService.Verify(x => x.RegisterAsync(command), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(cacheKey, registeredUser, It.IsAny<TimeSpan>()), Times.Once);
    }

    [TestMethod]
    public async Task LoginAsync_CallsServiceDirectlyWithoutCaching()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "test@example.com",
            Password = "password123"
        };
        var authToken = new AuthTokenDto
        {
            Token = "jwt-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        _mockUserService.Setup(x => x.LoginAsync(command))
            .ReturnsAsync(authToken);

        // Act
        var result = await _cachedUserService.LoginAsync(command);

        // Assert
        Assert.AreEqual(authToken, result);
        _mockUserService.Verify(x => x.LoginAsync(command), Times.Once);
        
        // Verify no cache operations are performed for login
        _mockCacheService.Verify(x => x.GetAsync<AuthTokenDto>(It.IsAny<string>()), Times.Never);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<AuthTokenDto>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [TestMethod]
    public async Task GetUserProfileAsync_WithCachedProfile_ReturnsCachedValue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cachedProfile = new UserDto 
        { 
            Id = userId, 
            Email = "test@example.com", 
            FullName = "Test User" 
        };
        var cacheKey = $"user:profile:{userId}";

        _mockCacheService.Setup(x => x.GetAsync<UserDto>(cacheKey))
            .ReturnsAsync(cachedProfile);

        // Act
        var result = await _cachedUserService.GetUserProfileAsync(userId);

        // Assert
        Assert.AreEqual(cachedProfile, result);
        _mockUserService.Verify(x => x.GetUserProfileAsync(It.IsAny<Guid>()), Times.Never);
        _mockCacheService.Verify(x => x.GetAsync<UserDto>(cacheKey), Times.Once);
    }

    [TestMethod]
    public async Task GetUserProfileAsync_WithoutCachedProfile_CallsServiceAndCachesResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userProfile = new UserDto 
        { 
            Id = userId, 
            Email = "test@example.com", 
            FullName = "Test User" 
        };
        var cacheKey = $"user:profile:{userId}";

        _mockCacheService.Setup(x => x.GetAsync<UserDto>(cacheKey))
            .ReturnsAsync((UserDto?)null);
        _mockUserService.Setup(x => x.GetUserProfileAsync(userId))
            .ReturnsAsync(userProfile);

        // Act
        var result = await _cachedUserService.GetUserProfileAsync(userId);

        // Assert
        Assert.AreEqual(userProfile, result);
        _mockUserService.Verify(x => x.GetUserProfileAsync(userId), Times.Once);
        _mockCacheService.Verify(x => x.GetAsync<UserDto>(cacheKey), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(cacheKey, userProfile, It.IsAny<TimeSpan>()), Times.Once);
    }

    [TestMethod]
    public async Task GetUserProfileAsync_WithNullProfile_DoesNotCache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cacheKey = $"user:profile:{userId}";

        _mockCacheService.Setup(x => x.GetAsync<UserDto>(cacheKey))
            .ReturnsAsync((UserDto?)null);
        _mockUserService.Setup(x => x.GetUserProfileAsync(userId))
            .ReturnsAsync((UserDto?)null);

        // Act
        var result = await _cachedUserService.GetUserProfileAsync(userId);

        // Assert
        Assert.IsNull(result);
        _mockUserService.Verify(x => x.GetUserProfileAsync(userId), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<UserDto>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateProfileAsync_InvalidatesCacheAndCachesUpdatedProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdateUserCommand
        {
            FullName = "Updated User"
        };
        var updatedProfile = new UserDto 
        { 
            Id = userId, 
            Email = "updated@example.com", 
            FullName = command.FullName 
        };
        var cacheKey = $"user:profile:{userId}";

        _mockUserService.Setup(x => x.UpdateProfileAsync(userId, command))
            .ReturnsAsync(updatedProfile);

        // Act
        var result = await _cachedUserService.UpdateProfileAsync(userId, command);

        // Assert
        Assert.AreEqual(updatedProfile, result);
        _mockUserService.Verify(x => x.UpdateProfileAsync(userId, command), Times.Once);
        
        // Verify cache invalidation and re-caching
        _mockCacheService.Verify(x => x.RemoveAsync(cacheKey), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(cacheKey, updatedProfile, It.IsAny<TimeSpan>()), Times.Once);
    }

    [TestMethod]
    public void Constructor_WithNullUserService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new CachedUserService(null!, _mockCacheService.Object, _mockLogger.Object));
    }

    [TestMethod]
    public void Constructor_WithNullCacheService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new CachedUserService(_mockUserService.Object, null!, _mockLogger.Object));
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            new CachedUserService(_mockUserService.Object, _mockCacheService.Object, null!));
    }
}