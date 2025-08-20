using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OnlineShoppingSystem.Infrastructure.Services;
using System.Text.Json;

namespace OnlineShoppingSystem.Tests.Unit.Infrastructure.Services;

[TestClass]
public class InMemoryCacheServiceTests
{
    private IMemoryCache _memoryCache;
    private Mock<ILogger<InMemoryCacheService>> _mockLogger;
    private InMemoryCacheService _cacheService;

    [TestInitialize]
    public void Setup()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<InMemoryCacheService>>();
        _cacheService = new InMemoryCacheService(_memoryCache, _mockLogger.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _memoryCache?.Dispose();
    }

    [TestMethod]
    public async Task GetAsync_WithValidKey_ReturnsValue()
    {
        // Arrange
        var key = "test-key";
        var value = new TestObject { Id = 1, Name = "Test" };
        var jsonValue = JsonSerializer.Serialize(value);
        _memoryCache.Set(key, jsonValue);

        // Act
        var result = await _cacheService.GetAsync<TestObject>(key);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(value.Id, result.Id);
        Assert.AreEqual(value.Name, result.Name);
    }

    [TestMethod]
    public async Task GetAsync_WithNonExistentKey_ReturnsNull()
    {
        // Arrange
        var key = "non-existent-key";

        // Act
        var result = await _cacheService.GetAsync<TestObject>(key);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetAsync_WithNullOrEmptyKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => _cacheService.GetAsync<TestObject>(null));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => _cacheService.GetAsync<TestObject>(""));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => _cacheService.GetAsync<TestObject>("   "));
    }

    [TestMethod]
    public async Task SetAsync_WithValidData_StoresValue()
    {
        // Arrange
        var key = "test-key";
        var value = new TestObject { Id = 1, Name = "Test" };
        var expiration = TimeSpan.FromMinutes(5);

        // Act
        await _cacheService.SetAsync(key, value, expiration);

        // Assert
        var exists = await _cacheService.ExistsAsync(key);
        Assert.IsTrue(exists);

        var retrievedValue = await _cacheService.GetAsync<TestObject>(key);
        Assert.IsNotNull(retrievedValue);
        Assert.AreEqual(value.Id, retrievedValue.Id);
        Assert.AreEqual(value.Name, retrievedValue.Name);
    }

    [TestMethod]
    public async Task SetAsync_WithNullKey_ThrowsArgumentException()
    {
        // Arrange
        var value = new TestObject { Id = 1, Name = "Test" };
        var expiration = TimeSpan.FromMinutes(5);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => 
            _cacheService.SetAsync(null, value, expiration));
    }

    [TestMethod]
    public async Task SetAsync_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var key = "test-key";
        var expiration = TimeSpan.FromMinutes(5);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => 
            _cacheService.SetAsync<TestObject>(key, null, expiration));
    }

    [TestMethod]
    public async Task SetAsync_WithZeroExpiration_ThrowsArgumentException()
    {
        // Arrange
        var key = "test-key";
        var value = new TestObject { Id = 1, Name = "Test" };
        var expiration = TimeSpan.Zero;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => 
            _cacheService.SetAsync(key, value, expiration));
    }

    [TestMethod]
    public async Task RemoveAsync_WithExistingKey_RemovesValue()
    {
        // Arrange
        var key = "test-key";
        var value = new TestObject { Id = 1, Name = "Test" };
        await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(5));

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        var exists = await _cacheService.ExistsAsync(key);
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task RemoveAsync_WithNullKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => _cacheService.RemoveAsync(null));
    }

    [TestMethod]
    public async Task RemoveByPatternAsync_WithMatchingPattern_RemovesMatchingKeys()
    {
        // Arrange
        var keys = new[] { "product:1", "product:2", "user:1", "product:3" };
        var value = new TestObject { Id = 1, Name = "Test" };
        
        foreach (var key in keys)
        {
            await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(5));
        }

        // Act
        await _cacheService.RemoveByPatternAsync("^product:.*");

        // Assert
        var productKeysExist = await Task.WhenAll(
            _cacheService.ExistsAsync("product:1"),
            _cacheService.ExistsAsync("product:2"),
            _cacheService.ExistsAsync("product:3")
        );
        
        var userKeyExists = await _cacheService.ExistsAsync("user:1");

        Assert.IsTrue(productKeysExist.All(exists => !exists), "Product keys should be removed");
        Assert.IsTrue(userKeyExists, "User key should remain");
    }

    [TestMethod]
    public async Task RemoveByPatternAsync_WithNullPattern_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => 
            _cacheService.RemoveByPatternAsync(null));
    }

    [TestMethod]
    public async Task ExistsAsync_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        var key = "test-key";
        var value = new TestObject { Id = 1, Name = "Test" };
        await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(5));

        // Act
        var exists = await _cacheService.ExistsAsync(key);

        // Assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsAsync_WithNonExistentKey_ReturnsFalse()
    {
        // Arrange
        var key = "non-existent-key";

        // Act
        var exists = await _cacheService.ExistsAsync(key);

        // Assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsAsync_WithNullKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => _cacheService.ExistsAsync(null));
    }

    [TestMethod]
    public async Task SetAsync_WithStringValue_StoresAndRetrievesCorrectly()
    {
        // Arrange
        var key = "string-key";
        var value = "test string value";
        var expiration = TimeSpan.FromMinutes(5);

        // Act
        await _cacheService.SetAsync(key, value, expiration);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        Assert.AreEqual(value, result);
    }

    private class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}