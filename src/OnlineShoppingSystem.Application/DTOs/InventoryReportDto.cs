namespace OnlineShoppingSystem.Application.DTOs;

public class InventoryReportDto
{
    public int TotalProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public int LowStockProducts { get; set; }
    public decimal TotalStockValue { get; set; }
    public int LowStockThreshold { get; set; }
    public List<LowStockProductDto> LowStockItems { get; set; } = new();
    public List<CategoryStockDto> CategoryBreakdown { get; set; } = new();
}

public class LowStockProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int RecommendedReorder { get; set; }
}

public class CategoryStockDto
{
    public string Category { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public int TotalStock { get; set; }
    public decimal TotalValue { get; set; }
}