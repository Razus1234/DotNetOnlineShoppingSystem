using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OnlineShoppingSystem.Application.Commands.Product;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.Common.Models;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Mappings;
using OnlineShoppingSystem.Application.Queries;
using OnlineShoppingSystem.Application.Services;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.Exceptions;
using OnlineShoppingSystem.Domain.ValueObjects;

namespace OnlineShoppingSystem.Tests.Unit.Application.Services;

[TestClass]
public class ProductServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<IProductRepository> _mockProductRepository;
    private IMapper _mapper;
    private ProductService _productService;

    [TestInitialize]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockProductRepository = new Mock<IProductRepository>();
        
        // Setup AutoMapper
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ProductMappingProfile>();
        });
        _mapper = configuration.CreateMapper();

        // Setup UnitOfWork to return the mock repository
        _mockUnitOfWork.Setup(x => x.Products).Returns(_mockProductRepository.Object);

        _productService = new ProductService(_mockUnitOfWork.Object, _mapper, Mock.Of<ILogger<ProductService>>());
    }

    [TestMethod]
    public async Task GetProductsAsync_ValidQuery_ReturnsPagedResults()
    {
        // Arrange
        var query = new ProductQuery { PageNumber = 1, PageSize = 10 };
        var products = new List<Product>
        {
            Product.Create("Test Product 1", "Description 1", 10.99m, 5, "Electronics"),
            Product.Create("Test Product 2", "Description 2", 20.99m, 3, "Books")
        };
        
        var pagedResult = new PagedResult<Product>(products, 2, 1, 10);
        _mockProductRepository.Setup(x => x.GetPagedAsync(It.IsAny<ProductQuery>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _productService.GetProductsAsync(query);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Items.Count);
        Assert.AreEqual(2, result.TotalCount);
        Assert.AreEqual(1, result.PageNumber);
        Assert.AreEqual(10, result.PageSize);
        Assert.AreEqual("Test Product 1", result.Items[0].Name);
        Assert.AreEqual("Test Product 2", result.Items[1].Name);
    }

    [TestMethod]
    public async Task GetProductsAsync_NullQuery_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => _productService.GetProductsAsync(null!));
    }

    [TestMethod]
    public async Task GetProductsAsync_InvalidPageNumber_CorrectsPagination()
    {
        // Arrange
        var query = new ProductQuery { PageNumber = 0, PageSize = 10 };
        var pagedResult = new PagedResult<Product>(new List<Product>(), 0, 1, 10);
        _mockProductRepository.Setup(x => x.GetPagedAsync(It.IsAny<ProductQuery>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _productService.GetProductsAsync(query);

        // Assert
        Assert.AreEqual(1, result.PageNumber);
        _mockProductRepository.Verify(x => x.GetPagedAsync(It.Is<ProductQuery>(q => q.PageNumber == 1)), Times.Once);
    }

    [TestMethod]
    public async Task GetProductsAsync_InvalidPageSize_CorrectsPagination()
    {
        // Arrange
        var query = new ProductQuery { PageNumber = 1, PageSize = 0 };
        var pagedResult = new PagedResult<Product>(new List<Product>(), 0, 1, 10);
        _mockProductRepository.Setup(x => x.GetPagedAsync(It.IsAny<ProductQuery>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _productService.GetProductsAsync(query);

        // Assert
        Assert.AreEqual(10, result.PageSize);
        _mockProductRepository.Verify(x => x.GetPagedAsync(It.Is<ProductQuery>(q => q.PageSize == 10)), Times.Once);
    }

    [TestMethod]
    public async Task GetProductByIdAsync_ValidId_ReturnsProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("Test Product", "Description", 10.99m, 5, "Electronics");
        _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(product);

        // Act
        var result = await _productService.GetProductByIdAsync(productId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Test Product", result.Name);
        Assert.AreEqual("Description", result.Description);
        Assert.AreEqual(10.99m, result.Price);
        Assert.AreEqual(5, result.Stock);
        Assert.AreEqual("Electronics", result.Category);
    }

    [TestMethod]
    public async Task GetProductByIdAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _productService.GetProductByIdAsync(productId);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetProductByIdAsync_EmptyId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _productService.GetProductByIdAsync(Guid.Empty));
    }

    [TestMethod]
    public async Task CreateProductAsync_ValidCommand_ReturnsCreatedProduct()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "New Product",
            Description = "New Description",
            Price = 15.99m,
            Stock = 10,
            Category = "Electronics",
            ImageUrls = new List<string> { "https://example.com/image1.jpg" }
        };

        _mockProductRepository.Setup(x => x.AddAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => p);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _productService.CreateProductAsync(command);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(command.Name, result.Name);
        Assert.AreEqual(command.Description, result.Description);
        Assert.AreEqual(command.Price, result.Price);
        Assert.AreEqual(command.Stock, result.Stock);
        Assert.AreEqual(command.Category, result.Category);
        Assert.AreEqual(1, result.ImageUrls.Count);
        Assert.AreEqual("https://example.com/image1.jpg", result.ImageUrls[0]);

        _mockProductRepository.Verify(x => x.AddAsync(It.IsAny<Product>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task CreateProductAsync_NullCommand_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => _productService.CreateProductAsync(null!));
    }

    [TestMethod]
    public async Task UpdateProductAsync_ValidCommand_ReturnsUpdatedProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = Product.Create("Old Product", "Old Description", 10.99m, 5, "Electronics");
        var command = new UpdateProductCommand
        {
            Name = "Updated Product",
            Description = "Updated Description",
            Price = 20.99m,
            Category = "Books",
            ImageUrls = new List<string> { "https://example.com/updated.jpg" }
        };

        _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(existingProduct);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _productService.UpdateProductAsync(productId, command);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(command.Name, result.Name);
        Assert.AreEqual(command.Description, result.Description);
        Assert.AreEqual(command.Price, result.Price);
        Assert.AreEqual(command.Category, result.Category);

        _mockProductRepository.Verify(x => x.UpdateAsync(It.IsAny<Product>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task UpdateProductAsync_NonExistentProduct_ThrowsProductNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new UpdateProductCommand
        {
            Name = "Updated Product",
            Description = "Updated Description",
            Price = 20.99m,
            Category = "Books"
        };

        _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ProductNotFoundException>(
            () => _productService.UpdateProductAsync(productId, command));
    }

    [TestMethod]
    public async Task UpdateProductAsync_EmptyId_ThrowsArgumentException()
    {
        // Arrange
        var command = new UpdateProductCommand
        {
            Name = "Updated Product",
            Description = "Updated Description",
            Price = 20.99m,
            Category = "Books"
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _productService.UpdateProductAsync(Guid.Empty, command));
    }

    [TestMethod]
    public async Task UpdateProductAsync_NullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => _productService.UpdateProductAsync(productId, null!));
    }

    [TestMethod]
    public async Task DeleteProductAsync_ExistingProduct_DeletesSuccessfully()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockProductRepository.Setup(x => x.ExistsAsync(productId))
            .ReturnsAsync(true);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _productService.DeleteProductAsync(productId);

        // Assert
        _mockProductRepository.Verify(x => x.DeleteAsync(productId), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task DeleteProductAsync_NonExistentProduct_ThrowsProductNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockProductRepository.Setup(x => x.ExistsAsync(productId))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ProductNotFoundException>(
            () => _productService.DeleteProductAsync(productId));
    }

    [TestMethod]
    public async Task DeleteProductAsync_EmptyId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _productService.DeleteProductAsync(Guid.Empty));
    }

    [TestMethod]
    public async Task UpdateStockAsync_ValidParameters_ReturnsUpdatedProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("Test Product", "Description", 10.99m, 5, "Electronics");
        var newStock = 15;

        _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(product);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _productService.UpdateStockAsync(productId, newStock);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(newStock, result.Stock);

        _mockProductRepository.Verify(x => x.UpdateAsync(It.IsAny<Product>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task UpdateStockAsync_NegativeStock_ThrowsArgumentException()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _productService.UpdateStockAsync(productId, -1));
    }

    [TestMethod]
    public async Task UpdateStockAsync_NonExistentProduct_ThrowsProductNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ProductNotFoundException>(
            () => _productService.UpdateStockAsync(productId, 10));
    }

    [TestMethod]
    public async Task IsProductInStockAsync_ProductInStock_ReturnsTrue()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("Test Product", "Description", 10.99m, 5, "Electronics");

        _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(product);

        // Act
        var result = await _productService.IsProductInStockAsync(productId, 3);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task IsProductInStockAsync_ProductOutOfStock_ReturnsFalse()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("Test Product", "Description", 10.99m, 2, "Electronics");

        _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(product);

        // Act
        var result = await _productService.IsProductInStockAsync(productId, 5);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task IsProductInStockAsync_NonExistentProduct_ReturnsFalse()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _productService.IsProductInStockAsync(productId, 1);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task IsProductInStockAsync_ZeroQuantity_ReturnsFalse()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var result = await _productService.IsProductInStockAsync(productId, 0);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task SearchProductsAsync_ValidKeyword_ReturnsMatchingProducts()
    {
        // Arrange
        var keyword = "laptop";
        var products = new List<Product>
        {
            Product.Create("Gaming Laptop", "High-end gaming laptop", 999.99m, 3, "Electronics"),
            Product.Create("Business Laptop", "Professional laptop", 799.99m, 5, "Electronics")
        };

        _mockProductRepository.Setup(x => x.SearchAsync(keyword))
            .ReturnsAsync(products);

        // Act
        var result = await _productService.SearchProductsAsync(keyword);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count());
        Assert.IsTrue(result.Any(p => p.Name == "Gaming Laptop"));
        Assert.IsTrue(result.Any(p => p.Name == "Business Laptop"));
    }

    [TestMethod]
    public async Task SearchProductsAsync_EmptyKeyword_ReturnsEmptyList()
    {
        // Act
        var result = await _productService.SearchProductsAsync("");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count());
        _mockProductRepository.Verify(x => x.SearchAsync(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task SearchProductsAsync_NullKeyword_ReturnsEmptyList()
    {
        // Act
        var result = await _productService.SearchProductsAsync(null!);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count());
        _mockProductRepository.Verify(x => x.SearchAsync(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task GetProductsByCategoryAsync_ValidCategory_ReturnsMatchingProducts()
    {
        // Arrange
        var category = "Electronics";
        var products = new List<Product>
        {
            Product.Create("Laptop", "Gaming laptop", 999.99m, 3, "Electronics"),
            Product.Create("Phone", "Smartphone", 599.99m, 10, "Electronics")
        };

        _mockProductRepository.Setup(x => x.GetByCategoryAsync(category))
            .ReturnsAsync(products);

        // Act
        var result = await _productService.GetProductsByCategoryAsync(category);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count());
        Assert.IsTrue(result.All(p => p.Category == "Electronics"));
    }

    [TestMethod]
    public async Task GetProductsByCategoryAsync_EmptyCategory_ReturnsEmptyList()
    {
        // Act
        var result = await _productService.GetProductsByCategoryAsync("");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count());
        _mockProductRepository.Verify(x => x.GetByCategoryAsync(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task GetProductsByCategoryAsync_NullCategory_ReturnsEmptyList()
    {
        // Act
        var result = await _productService.GetProductsByCategoryAsync(null!);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count());
        _mockProductRepository.Verify(x => x.GetByCategoryAsync(It.IsAny<string>()), Times.Never);
    }
}