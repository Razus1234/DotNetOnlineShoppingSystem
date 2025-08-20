namespace OnlineShoppingSystem.Application.DTOs;

public class AdminDashboardDto
{
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int LowStockProducts { get; set; }
    public List<OrderDto> RecentOrders { get; set; } = new();
    public List<ProductDto> LowStockItems { get; set; } = new();
}