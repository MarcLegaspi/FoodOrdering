namespace FoodOrdering.Domain.Orders;

public sealed class OrderItemOption
{
    public long Id { get; set; }

    public long OrderItemId { get; set; }

    public OrderItem OrderItem { get; set; } = null!;

    public string GroupNameSnapshot { get; set; } = string.Empty;

    public string OptionNameSnapshot { get; set; } = string.Empty;

    public decimal PriceDeltaSnapshot { get; set; }
}
