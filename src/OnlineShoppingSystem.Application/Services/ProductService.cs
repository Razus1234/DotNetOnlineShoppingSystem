using AutoMapper;
using Microsoft.Extensions.Logging;
using OnlineShoppingSystem.Application.Commands.Product;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Queries;
using OnlineShoppingSystem.Domain.Entities;
using OnlineShoppingSystem.Domain.Exceptions;
using OnlineShoppingSystem.Domain.ValueObjects;

namespace OnlineShoppingSystem.Application.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ProductService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResultDto<ProductDto>> GetProductsAsync(ProductQuery query)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        _logger.LogDebug("Retrieving products with query: Page={PageNumber}, Size={PageSize}, Category={Category}, Keyword={Keyword}", 
            query.PageNumber, query.PageSize, query.Category, query.Keyword);

        try
        {
            // Validate pagination parameters
            if (query.PageNumber < 1)
                query.PageNumber = 1;
            
            if (query.PageSize < 1 || query.PageSize > 100)
                query.PageSize = 10;

            // Get paged results from repository
            var pagedResult = await _unitOfWork.Products.GetPagedAsync(query);

            _logger.LogInformation("Retrieved {Count} products out of {TotalCount} total products", 
                pagedResult.Items.Count(), pagedResult.TotalCount);

            // Map to DTOs
            var productDtos = _mapper.Map<IEnumerable<ProductDto>>(pagedResult.Items);

            return new PagedResultDto<ProductDto>
            {
                Items = productDtos.ToList(),
                TotalCount = pagedResult.TotalCount,
                PageNumber = pagedResult.Page,
                PageSize = pagedResult.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve products with query: Page={PageNumber}, Size={PageSize}", 
                query.PageNumber, query.PageSize);
            throw;
        }
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid productId)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        
        return product == null ? null : _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductCommand command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        // Create product entity
        var product = Product.Create(
            command.Name,
            command.Description,
            command.Price,
            command.Stock,
            command.Category);

        // Add image URLs if provided
        foreach (var imageUrl in command.ImageUrls)
        {
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                product.AddImageUrl(imageUrl);
            }
        }

        // Add to repository
        await _unitOfWork.Products.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> UpdateProductAsync(Guid productId, UpdateProductCommand command)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null)
        {
            throw new ProductNotFoundException(productId);
        }

        // Update product details
        product.UpdateDetails(
            command.Name,
            command.Description,
            new Money(command.Price),
            command.Category);

        // Update image URLs - remove existing and add new ones
        var existingImageUrls = product.ImageUrls.ToList();
        foreach (var existingUrl in existingImageUrls)
        {
            product.RemoveImageUrl(existingUrl);
        }

        foreach (var imageUrl in command.ImageUrls)
        {
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                product.AddImageUrl(imageUrl);
            }
        }

        // Save changes
        await _unitOfWork.Products.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ProductDto>(product);
    }

    public async Task DeleteProductAsync(Guid productId)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        var exists = await _unitOfWork.Products.ExistsAsync(productId);
        if (!exists)
        {
            throw new ProductNotFoundException(productId);
        }

        await _unitOfWork.Products.DeleteAsync(productId);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<ProductDto> UpdateStockAsync(Guid productId, int newStock)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        if (newStock < 0)
            throw new ArgumentException("Stock cannot be negative", nameof(newStock));

        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null)
        {
            throw new ProductNotFoundException(productId);
        }

        product.UpdateStock(newStock);

        await _unitOfWork.Products.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<bool> IsProductInStockAsync(Guid productId, int requestedQuantity = 1)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        if (requestedQuantity <= 0)
            return false;

        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null)
        {
            return false;
        }

        return product.IsInStock(requestedQuantity);
    }

    public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return new List<ProductDto>();
        }

        var products = await _unitOfWork.Products.SearchAsync(keyword.Trim());
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return new List<ProductDto>();
        }

        var products = await _unitOfWork.Products.GetByCategoryAsync(category.Trim());
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }
}