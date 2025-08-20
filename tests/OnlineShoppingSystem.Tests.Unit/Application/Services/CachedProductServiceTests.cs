using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OnlineShoppingSystem.Application.Commands.Product;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Queries;
using OnlineShoppingSystem.Application.Services;

namespace OnlineShoppingSystem.Tests.Unit.Application.Services;

[TestClass]
public class CachedProductServiceTests
{
    private Mock<IProductService> _mockProductService;
    private Mock<ICacheService> _mockCacheService;
    private Mock<ILogger<CachedProductService>> _mockLogger;
    private CachedProductService _cachedProductService;

    [TestInitialize]
    public void Setup()
    {
        _mockProductService = new Mock<IProductService>();
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<CachedProductService>>();
        _cachedProductService = new CachedProductService(
            _mockProductService.Object,
            _mockCacheService.Object,
            _mockLogger.Object);
    }

    [TestMethod]
    public async Task GetProductByIdAsync_WithCachedProduct_ReturnsCachedValue()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var cachedProduct = new ProductDto { Id = productId, Name = "Cached Product" };
        var cacheKey = $"product:{productId}";

        _mockCacheService.Setup(x => x.GetAsync<ProductDto>(cacheKey))
            .ReturnsAsync(cachedProduct);

        // Act
        var result = await _cachedProductService.GetProductByIdAsync(productId);

        // Assert
        Assert.AreEqual(cachedProduct, result);
        _mockProductService.Verify(x => x.GetProductByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mockCacheService.Verify(x => x.GetAsync<ProductDto>(cacheKey), Times.Once);
    }

    [TestMethod]
    public async Task GetProductByIdAsync_WithoutCachedProduct_CallsServiceAndCachesResult()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new ProductDto { Id = productId, Name = "Product" };
        var cacheKey = $"product:{productId}";

        _mockCacheService.Setup(x => x.GetAsync<ProductDto>(cacheKey))
            .ReturnsAsync((ProductDto?)null);
        _mockProductService.Setup(x => x.GetProductByIdAsync(productId))
            .ReturnsAsync(product);

        // Act
        var result = await _cachedProductService.GetProductByIdAsync(productId);

        // Assert
        Assert.AreEqual(product, result);
        _mockProductService.Verify(x => x.GetProductByIdAsync(productId), Times.Once);
        _mockCacheService.Verify(x => x.GetAsync<ProductDto>(cacheKey), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(cacheKey, product, It.IsAny<TimeSpan>()), Times.Once);
    }

    [TestMethod]
    public async Task GetProductByIdAsync_WithNullProduct_DoesNotCache()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var cacheKey = $"product:{productId}";

        _mockCacheService.Setup(x => x.GetAsync<ProductDto>(cacheKey))
            .ReturnsAsync((ProductDto?)null);
        _mockProductService.Setup(x => x.GetProductByIdAsync(productId))
            .ReturnsAsync((ProductDto?)null);

        // Act
        var result = await _cachedProductService.GetProductByIdAsync(productId);

        // Assert
        Assert.IsNull(result);
        _mockProductService.Verify(x => x.GetProductByIdAsync(productId), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<ProductDto>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [TestMethod]
    public async Task GetProductsAsync_WithCachedResults_ReturnsCachedValue()
    {
        // Arrange
        var query = new ProductQuery { PageNumber = 1, PageSize = 10 };
        var cachedResult = new PagedResultDto<ProductDto>
        {
            Items = new List<ProductDto> { new() { Id = Guid.NewGuid(), Name = "Product" } },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _mockCacheService.Setup(x => x.GetAsync<PagedResultDto<ProductDto>>(It.IsAny<string>()))
            .ReturnsAsync(cachedResult);

        // Act
        var result = await _cachedProductService.GetProductsAsync(query);

        // Assert
        Assert.AreEqual(cachedResult, result);
        _mockProductService.Verify(x => x.GetProductsAsync(It.IsAny<ProductQuery>()), Times.Never);
    }

    [TestMethod]
    public async Task GetProductsAsync_WithoutCachedResults_CallsServiceAndCachesResult()
    {
        // Arrange
        var query = new ProductQuery { PageNumber = 1, PageSize = 10 };
        var serviceResult = new PagedResultDto<ProductDto>
        {
            Items = new List<ProductDto> { new() { Id = Guid.NewGuid(), Name = "Product" } },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _mockCacheService.Setup(x => x.GetAsync<PagedResultDto<ProductDto>>(It.IsAny<string>()))
            .ReturnsAsync((PagedResultDto<ProductDto>?)null);
        _mockProductService.Setup(x => x.GetProductsAsync(query))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _cachedProductService.GetProductsAsync(query);

        // Assert
        Assert.AreEqual(serviceResult, result);
        _mockProductService.Verify(x => x.GetProductsAsync(query), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), serviceResult, It.IsAny<TimeSpan>()), Times.Once);
    }

    [TestMethod]
    public async Task CreateProductAsync_InvalidatesCaches()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "New Product",
            Description = "Description",
            Price = 10.99m,
            Stock = 100,
            Category = "Electronics",
            ImageUrls = new List<string>()
        };
        var createdProduct = new ProductDto { Id = Guid.NewGuid(), Name = "New Product" };

        _mockProductService.Setup(x => x.CreateProductAsync(command))
            .ReturnsAsync(createdProduct);

        // Act
        var result = await _cachedProductService.CreateProductAsync(command);

        // Assert
        Assert.AreEqual(createdProduct, result);
        _mockProductService.Verify(x => x.CreateProductAsync(command), Times.Once);
        
        // Verify cache invalidation patterns
        _mockCacheService.Verify(x => x.RemoveByPatternAsync("^products:.*"), Times.Once);
        _mockCacheService.Verify(x => x.RemoveByPatternAsync("^search:.*"), Times.Once);
        _mockCacheService.Verify(x => x.RemoveByPatternAsync("^category:.*"), Times.Once);
    }

    [TestMethod]
    public async Task UpdateProductAsync_InvalidatesSpecificProductAndRelatedCaches()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new UpdateProductCommand
        {
            Name = "Updated Product",
            Description = "Updated Description",
            Price = 15.99m,
            Category = "Electronics",
            ImageUrls = new List<string>()
        };
        var updatedProduct = new ProductDto { Id = productId, Name = "Updated Product" };

        _mockProductService.Setup(x => x.UpdateProductAsync(productId, command))
            .ReturnsAsync(updatedProduct);

        // Act
        var result = await _cachedProductService.UpdateProductAsync(productId, command);

        // Assert
        Assert.AreEqual(updatedProduct, result);
        _mockProductService.Verify(x => x.UpdateProductAsync(productId, command), Times.Once);
        
        // Verify specific product cache invalidation
        _mockCacheService.Verify(x => x.RemoveAsync($"product:{productId}"), Times.Once);
        
        // Verify related cache invalidation patterns
        _mockCacheService.Verify(x => x.RemoveByPatternAsync("^products:.*"), Times.Once);
        _mockCacheService.Verify(x => x.RemoveByPatternAsync("^search:.*"), Times.Once);
        _mockCacheService.Verify(x => x.RemoveByPatternAsync("^category:.*"), Times.Once);
    }

    [TestMethod]
    public async Task DeleteProductAsync_InvalidatesSpecificProductAndRelatedCaches()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        await _cachedProductService.DeleteProductAsync(productId);

        // Assert
        _mockProductService.Verify(x => x.DeleteProductAsync(productId), Times.Once);
        
        // Verify specific product cache invalidation
        _mockCacheService.Verify(x => x.RemoveAsync($"product:{productId}"), Times.Once);
        
        // Verify related cache invalidation patterns
        _mockCacheService.Verify(x => x.RemoveByPatternAsync("^products:.*"), Times.Once);
        _mockCacheService.Verify(x => x.RemoveByPatternAsync("^search:.*"), Times.Once);
        _mockCacheService.Verify(x => x.RemoveByPatternAsync("^category:.*"), Times.Once);
    }

    [TestMethod]
    public async Task UpdateStockAsync_InvalidatesSpecificProductCache()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var newStock = 50;
        var updatedProduct = new ProductDto { Id = productId, Stock = newStock };

        _mockProductService.Setup(x => x.UpdateStockAsync(productId, newStock))
            .ReturnsAsync(updatedProduct);

        // Act
        var result = await _cachedProductService.UpdateStockAsync(productId, newStock);

        // Assert
        Assert.AreEqual(updatedProduct, result);
        _mockProductService.Verify(x => x.UpdateStockAsync(productId, newStock), Times.Once);
        
        // Verify only specific product cache is invalidated (not all caches)
        _mockCacheService.Verify(x => x.RemoveAsync($"product:{productId}"), Times.Once);
        _mockCacheService.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task IsProductInStockAsync_DoesNotUseCache()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var requestedQuantity = 5;

        _mockProductService.Setup(x => x.IsProductInStockAsync(productId, requestedQuantity))
            .ReturnsAsync(true);

        // Act
        var result = await _cachedProductService.IsProductInStockAsync(productId, requestedQuantity);

        // Assert
        Assert.IsTrue(result);
        _mockProductService.Verify(x => x.IsProductInStockAsync(productId, requestedQuantity), Times.Once);
        
        // Verify no cache operations are performed for stock checks (stock checks bypass cache)
        _mockCacheService.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task SearchProductsAsync_WithCachedResults_ReturnsCachedValue()
    {
        // Arrange
        var keyword = "laptop";
        var cachedResults = new List<ProductDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Gaming Laptop" }
        };
        var cacheKey = $"search:{keyword}";

        _mockCacheService.Setup(x => x.GetAsync<IEnumerable<ProductDto>>(cacheKey))
            .ReturnsAsync(cachedResults);

        // Act
        var result = await _cachedProductService.SearchProductsAsync(keyword);

        // Assert
        Assert.AreEqual(cachedResults, result);
        _mockProductService.Verify(x => x.SearchProductsAsync(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task SearchProductsAsync_WithoutCachedResults_CallsServiceAndCachesResult()
    {
        // Arrange
        var keyword = "laptop";
        var searchResults = new List<ProductDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Gaming Laptop" }
        };
        var cacheKey = $"search:{keyword}";

        _mockCacheService.Setup(x => x.GetAsync<IEnumerable<ProductDto>>(cacheKey))
            .ReturnsAsync((IEnumerable<ProductDto>?)null);
        _mockProductService.Setup(x => x.SearchProductsAsync(keyword))
            .ReturnsAsync(searchResults);

        // Act
        var result = await _cachedProductService.SearchProductsAsync(keyword);

        // Assert
        Assert.AreEqual(searchResults, result);
        _mockProductService.Verify(x => x.SearchProductsAsync(keyword), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(cacheKey, searchResults, It.IsAny<TimeSpan>()), Times.Once);
    }

    [TestMethod]
    public async Task SearchProductsAsync_WithEmptyResults_DoesNotCache()
    {
        // Arrange
        var keyword = "nonexistent";
        var emptyResults = new List<ProductDto>();
        var cacheKey = $"search:{keyword}";

        _mockCacheService.Setup(x => x.GetAsync<IEnumerable<ProductDto>>(cacheKey))
            .ReturnsAsync((IEnumerable<ProductDto>?)null);
        _mockProductService.Setup(x => x.SearchProductsAsync(keyword))
            .ReturnsAsync(emptyResults);

        // Act
        var result = await _cachedProductService.SearchProductsAsync(keyword);

        // Assert
        Assert.AreEqual(emptyResults, result);
        _mockProductService.Verify(x => x.SearchProductsAsync(keyword), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<IEnumerable<ProductDto>>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [TestMethod]
    public async Task GetProductsByCategoryAsync_WithCachedResults_ReturnsCachedValue()
    {
        // Arrange
        var category = "Electronics";
        var cachedResults = new List<ProductDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Laptop", Category = category }
        };
        var cacheKey = $"category:{category.ToLowerInvariant()}";

        _mockCacheService.Setup(x => x.GetAsync<IEnumerable<ProductDto>>(cacheKey))
            .ReturnsAsync(cachedResults);

        // Act
        var result = await _cachedProductService.GetProductsByCategoryAsync(category);

        // Assert
        Assert.AreEqual(cachedResults, result);
        _mockProductService.Verify(x => x.GetProductsByCategoryAsync(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task GetProductsByCategoryAsync_WithoutCachedResults_CallsServiceAndCachesResult()
    {
        // Arrange
        var category = "Electronics";
        var categoryResults = new List<ProductDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Laptop", Category = category }
        };
        var cacheKey = $"category:{category.ToLowerInvariant()}";

        _mockCacheService.Setup(x => x.GetAsync<IEnumerable<ProductDto>>(cacheKey))
            .ReturnsAsync((IEnumerable<ProductDto>?)null);
        _mockProductService.Setup(x => x.GetProductsByCategoryAsync(category))
            .ReturnsAsync(categoryResults);

        // Act
        var result = await _cachedProductService.GetProductsByCategoryAsync(category);

        // Assert
        Assert.AreEqual(categoryResults, result);
        _mockProductService.Verify(x => x.GetProductsByCategoryAsync(category), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(cacheKey, categoryResults, It.IsAny<TimeSpan>()), Times.Once);
    }
}