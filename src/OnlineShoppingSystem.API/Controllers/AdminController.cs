using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineShoppingSystem.API.Attributes;
using OnlineShoppingSystem.Application.Commands.Order;
using OnlineShoppingSystem.Application.Commands.Product;
using OnlineShoppingSystem.Application.Common.Interfaces;
using OnlineShoppingSystem.Application.DTOs;
using OnlineShoppingSystem.Application.Queries;
using OnlineShoppingSystem.Domain.Enums;

namespace OnlineShoppingSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IOrderService _orderService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IProductService productService,
        IOrderService orderService,
        ILogger<AdminController> logger)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get dashboard overview with key metrics
    /// </summary>
    /// <returns>Dashboard metrics</returns>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(AdminDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminDashboardDto>> GetDashboard()
    {
        _logger.LogInformation("Getting admin dashboard metrics");

        // Get recent orders
        var recentOrdersQuery = new AdminOrderQuery { PageSize = 10, PageNumber = 1 };
        var recentOrders = await _orderService.GetAllOrdersAsync(recentOrdersQuery);

        // Get low stock products
        var lowStockQuery = new ProductQuery { PageSize = 10, PageNumber = 1, MaxStock = 10 };
        var lowStockProducts = await _productService.GetProductsAsync(lowStockQuery);

        var dashboard = new AdminDashboardDto
        {
            TotalOrders = recentOrders.TotalCount,
            PendingOrders = recentOrders.Items.Count(o => o.Status == OrderStatus.Pending),
            LowStockProducts = lowStockProducts.Items.Count(),
            RecentOrders = recentOrders.Items.Take(5).ToList(),
            LowStockItems = lowStockProducts.Items.ToList()
        };

        return Ok(dashboard);
    }

    /// <summary>
    /// Get all orders with filtering and pagination (Admin only)
    /// </summary>
    /// <param name="query">Order query parameters</param>
    /// <returns>Paginated order list</returns>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(PagedResultDto<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResultDto<OrderDto>>> GetAllOrders([FromQuery] AdminOrderQuery query)
    {
        _logger.LogInformation("Admin getting all orders with query: {@Query}", query);

        var orders = await _orderService.GetAllOrdersAsync(query);

        return Ok(orders);
    }

    /// <summary>
    /// Update order status (Admin only)
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <param name="request">Status update request</param>
    /// <returns>Updated order</returns>
    [HttpPatch("orders/{orderId:guid}/status")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> UpdateOrderStatus(Guid orderId, [FromBody] UpdateOrderStatusRequest request)
    {
        if (!Enum.IsDefined(typeof(OrderStatus), request.Status))
        {
            return BadRequest("Invalid order status");
        }

        _logger.LogInformation("Admin updating order {OrderId} status to {Status}", orderId, request.Status);

        var order = await _orderService.UpdateOrderStatusAsync(orderId, request.Status);

        _logger.LogInformation("Order status updated successfully: {OrderId}", orderId);

        return Ok(order);
    }

    /// <summary>
    /// Get sales report with date range filtering
    /// </summary>
    /// <param name="request">Sales report request parameters</param>
    /// <returns>Sales report data</returns>
    [HttpGet("reports/sales")]
    [ProducesResponseType(typeof(SalesReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SalesReportDto>> GetSalesReport([FromQuery] SalesReportRequest request)
    {
        if (request.StartDate > request.EndDate)
        {
            return BadRequest("Start date cannot be after end date");
        }

        if (request.EndDate > DateTime.UtcNow)
        {
            return BadRequest("End date cannot be in the future");
        }

        _logger.LogInformation("Generating sales report from {StartDate} to {EndDate}", 
            request.StartDate, request.EndDate);

        var report = await GenerateSalesReportAsync(request);

        return Ok(report);
    }

    /// <summary>
    /// Get inventory report with low stock alerts
    /// </summary>
    /// <param name="request">Inventory report request</param>
    /// <returns>Inventory report data</returns>
    [HttpGet("reports/inventory")]
    [ProducesResponseType(typeof(InventoryReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InventoryReportDto>> GetInventoryReport([FromQuery] InventoryReportRequest request)
    {
        _logger.LogInformation("Generating inventory report with threshold: {Threshold}", request.LowStockThreshold);

        var report = await GenerateInventoryReportAsync(request);

        return Ok(report);
    }

    /// <summary>
    /// Bulk update product stock levels
    /// </summary>
    /// <param name="request">Bulk stock update request</param>
    /// <returns>Update results</returns>
    [HttpPatch("inventory/bulk-update")]
    [SanitizeInput]
    [ProducesResponseType(typeof(BulkStockUpdateResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BulkStockUpdateResultDto>> BulkUpdateStock([FromBody] BulkStockUpdateRequest request)
    {
        if (request.Updates == null || !request.Updates.Any())
        {
            return BadRequest("No stock updates provided");
        }

        _logger.LogInformation("Processing bulk stock update for {Count} products", request.Updates.Count());

        var results = new List<StockUpdateResult>();
        var successCount = 0;
        var failureCount = 0;

        foreach (var update in request.Updates)
        {
            try
            {
                if (update.NewStock < 0)
                {
                    results.Add(new StockUpdateResult
                    {
                        ProductId = update.ProductId,
                        Success = false,
                        ErrorMessage = "Stock cannot be negative"
                    });
                    failureCount++;
                    continue;
                }

                var product = await _productService.UpdateStockAsync(update.ProductId, update.NewStock);
                results.Add(new StockUpdateResult
                {
                    ProductId = update.ProductId,
                    Success = true,
                    OldStock = update.NewStock, // This would need to be tracked properly
                    NewStock = product.Stock
                });
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update stock for product {ProductId}", update.ProductId);
                results.Add(new StockUpdateResult
                {
                    ProductId = update.ProductId,
                    Success = false,
                    ErrorMessage = ex.Message
                });
                failureCount++;
            }
        }

        var result = new BulkStockUpdateResultDto
        {
            TotalUpdates = request.Updates.Count(),
            SuccessfulUpdates = successCount,
            FailedUpdates = failureCount,
            Results = results
        };

        _logger.LogInformation("Bulk stock update completed: {Success} successful, {Failed} failed", 
            successCount, failureCount);

        return Ok(result);
    }

    private async Task<SalesReportDto> GenerateSalesReportAsync(SalesReportRequest request)
    {
        // Get orders within date range
        var orderQuery = new AdminOrderQuery
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PageSize = int.MaxValue,
            PageNumber = 1
        };

        var orders = await _orderService.GetAllOrdersAsync(orderQuery);
        var completedOrders = orders.Items.Where(o => o.Status == OrderStatus.Delivered).ToList();

        var totalRevenue = completedOrders.Sum(o => o.Total);
        var totalOrders = completedOrders.Count;
        var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        // Group by date for daily sales
        var dailySales = completedOrders
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new DailySalesDto
            {
                Date = g.Key,
                OrderCount = g.Count(),
                Revenue = g.Sum(o => o.Total)
            })
            .OrderBy(d => d.Date)
            .ToList();

        // Top selling products
        var topProducts = completedOrders
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new TopSellingProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                TotalQuantitySold = g.Sum(i => i.Quantity),
                TotalRevenue = g.Sum(i => i.Subtotal)
            })
            .OrderByDescending(p => p.TotalQuantitySold)
            .Take(10)
            .ToList();

        return new SalesReportDto
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            AverageOrderValue = averageOrderValue,
            DailySales = dailySales,
            TopSellingProducts = topProducts
        };
    }

    private async Task<InventoryReportDto> GenerateInventoryReportAsync(InventoryReportRequest request)
    {
        // Get all products
        var productQuery = new ProductQuery
        {
            PageSize = int.MaxValue,
            PageNumber = 1
        };

        var products = await _productService.GetProductsAsync(productQuery);

        var lowStockProducts = products.Items
            .Where(p => p.Stock <= request.LowStockThreshold)
            .Select(p => new LowStockProductDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                Category = p.Category,
                CurrentStock = p.Stock,
                RecommendedReorder = Math.Max(request.LowStockThreshold * 2, 10)
            })
            .OrderBy(p => p.CurrentStock)
            .ToList();

        var outOfStockProducts = products.Items.Where(p => p.Stock == 0).Count();
        var totalProducts = products.Items.Count();
        var totalStockValue = products.Items.Sum(p => p.Price * p.Stock);

        // Category breakdown
        var categoryBreakdown = products.Items
            .GroupBy(p => p.Category)
            .Select(g => new CategoryStockDto
            {
                Category = g.Key,
                ProductCount = g.Count(),
                TotalStock = g.Sum(p => p.Stock),
                TotalValue = g.Sum(p => p.Price * p.Stock)
            })
            .OrderByDescending(c => c.TotalValue)
            .ToList();

        return new InventoryReportDto
        {
            TotalProducts = totalProducts,
            OutOfStockProducts = outOfStockProducts,
            LowStockProducts = lowStockProducts.Count,
            TotalStockValue = totalStockValue,
            LowStockThreshold = request.LowStockThreshold,
            LowStockItems = lowStockProducts,
            CategoryBreakdown = categoryBreakdown
        };
    }

    // Request/Response DTOs
    public class UpdateOrderStatusRequest
    {
        public OrderStatus Status { get; set; }
    }

    public class SalesReportRequest
    {
        public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(-30);
        public DateTime EndDate { get; set; } = DateTime.UtcNow;
    }

    public class InventoryReportRequest
    {
        public int LowStockThreshold { get; set; } = 10;
    }

    public class BulkStockUpdateRequest
    {
        public IEnumerable<StockUpdateItem> Updates { get; set; } = new List<StockUpdateItem>();
    }

    public class StockUpdateItem
    {
        public Guid ProductId { get; set; }
        public int NewStock { get; set; }
    }
}