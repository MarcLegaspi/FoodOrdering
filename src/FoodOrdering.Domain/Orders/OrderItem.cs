using FoodOrdering.Domain.Catalog;

namespace FoodOrdering.Domain.Orders;

public sealed class OrderItem
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public long MenuItemId { get; set; }

    public MenuItem MenuItem { get; set; } = null!;

    public string ItemNameSnapshot { get; set; } = string.Empty;

    public decimal UnitPriceSnapshot { get; set; }

    public int Quantity { get; set; }
}
