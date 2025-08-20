using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineShoppingSystem.API.Attributes;
using OnlineShoppingSystem.Application.Commands.Product;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Queries;

namespace OnlineShoppingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get paginated list of products with optional filtering
    /// </summary>
    /// <param name="query">Product query parameters</param>
    /// <returns>Paginated product list</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ProductDto>>> GetProducts([FromQuery] ProductQuery query)
    {
        _logger.LogInformation("Getting products with query: {@Query}", query);

        var products = await _productService.GetProductsAsync(query);

        return Ok(products);
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Product details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
    {
        _logger.LogInformation("Getting product with ID: {ProductId}", id);

        var product = await _productService.GetProductByIdAsync(id);
        
        if (product == null)
        {
            _logger.LogWarning("Product not found: {ProductId}", id);
            return NotFound($"Product with ID {id} not found");
        }

        return Ok(product);
    }

    /// <summary>
    /// Search products by keyword
    /// </summary>
    /// <param name="keyword">Search keyword</param>
    /// <returns>List of matching products</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> SearchProducts([FromQuery] string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return BadRequest("Search keyword cannot be empty");
        }

        _logger.LogInformation("Searching products with keyword: {Keyword}", keyword);

        var products = await _productService.SearchProductsAsync(keyword);

        return Ok(products);
    }

    /// <summary>
    /// Get products by category
    /// </summary>
    /// <param name="category">Product category</param>
    /// <returns>List of products in the category</returns>
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProductsByCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return BadRequest("Category cannot be empty");
        }

        _logger.LogInformation("Getting products by category: {Category}", category);

        var products = await _productService.GetProductsByCategoryAsync(category);

        return Ok(products);
    }

    /// <summary>
    /// Create a new product (Admin only)
    /// </summary>
    /// <param name="command">Product creation details</param>
    /// <returns>Created product</returns>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [SanitizeInput]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductCommand command)
    {
        _logger.LogInformation("Creating new product: {ProductName}", command.Name);

        var product = await _productService.CreateProductAsync(command);

        _logger.LogInformation("Product created successfully with ID: {ProductId}", product.Id);

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    /// <summary>
    /// Update an existing product (Admin only)
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="command">Product update details</param>
    /// <returns>Updated product</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [SanitizeInput]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command)
    {
        _logger.LogInformation("Updating product with ID: {ProductId}", id);

        var product = await _productService.UpdateProductAsync(id, command);

        _logger.LogInformation("Product updated successfully: {ProductId}", id);

        return Ok(product);
    }

    /// <summary>
    /// Delete a product (Admin only)
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        _logger.LogInformation("Deleting product with ID: {ProductId}", id);

        await _productService.DeleteProductAsync(id);

        _logger.LogInformation("Product deleted successfully: {ProductId}", id);

        return NoContent();
    }

    /// <summary>
    /// Update product stock (Admin only)
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="request">Stock update request</param>
    /// <returns>Updated product</returns>
    [HttpPatch("{id:guid}/stock")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> UpdateStock(Guid id, [FromBody] UpdateStockRequest request)
    {
        if (request.Stock < 0)
        {
            return BadRequest("Stock cannot be negative");
        }

        _logger.LogInformation("Updating stock for product {ProductId} to {Stock}", id, request.Stock);

        var product = await _productService.UpdateStockAsync(id, request.Stock);

        _logger.LogInformation("Stock updated successfully for product: {ProductId}", id);

        return Ok(product);
    }

    /// <summary>
    /// Check if product is in stock
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="quantity">Requested quantity (default: 1)</param>
    /// <returns>Stock availability status</returns>
    [HttpGet("{id:guid}/stock")]
    [ProducesResponseType(typeof(StockCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockCheckResponse>> CheckStock(Guid id, [FromQuery] int quantity = 1)
    {
        if (quantity <= 0)
        {
            return BadRequest("Quantity must be greater than zero");
        }

        _logger.LogInformation("Checking stock for product {ProductId}, quantity: {Quantity}", id, quantity);

        var isInStock = await _productService.IsProductInStockAsync(id, quantity);

        return Ok(new StockCheckResponse
        {
            ProductId = id,
            RequestedQuantity = quantity,
            IsInStock = isInStock
        });
    }

    public class UpdateStockRequest
    {
        public int Stock { get; set; }
    }

    public class StockCheckResponse
    {
        public Guid ProductId { get; set; }
        public int RequestedQuantity { get; set; }
        public bool IsInStock { get; set; }
    }
}