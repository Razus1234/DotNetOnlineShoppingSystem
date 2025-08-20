using OnlineShoppingSystem.Domain.Enums;

namespace OnlineShoppingSystem.Application.Queries;

public class AdminOrderQuery : OrderQuery
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public new OrderStatus? Status { get; set; }
    public Guid? UserId { get; set; }
    public decimal? MinTotal { get; set; }
    public decimal? MaxTotal { get; set; }
}