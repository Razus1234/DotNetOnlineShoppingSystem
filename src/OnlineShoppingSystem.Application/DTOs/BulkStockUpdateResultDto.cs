namespace OnlineShoppingSystem.Application.DTOs;

public class BulkStockUpdateResultDto
{
    public int TotalUpdates { get; set; }
    public int SuccessfulUpdates { get; set; }
    public int FailedUpdates { get; set; }
    public List<StockUpdateResult> Results { get; set; } = new();
}

public class StockUpdateResult
{
    public Guid ProductId { get; set; }
    public bool Success { get; set; }
    public int OldStock { get; set; }
    public int NewStock { get; set; }
    public string? ErrorMessage { get; set; }
}