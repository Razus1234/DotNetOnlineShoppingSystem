namespace OnlineShoppingSystem.Application.Queries;

public class ProductQuery
{
    public string? Keyword { get; set; }
    public string? Category { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinStock { get; set; }
    public int? MaxStock { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; } = false;
}