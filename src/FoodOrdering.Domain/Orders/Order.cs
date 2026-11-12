using FoodOrdering.Domain.Common;
using FoodOrdering.Domain.Stores;

namespace FoodOrdering.Domain.Orders;

public sealed class Order : AuditableEntity, ITenantEntity
{
    public long Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public long StoreId { get; set; }

    public Store Store { get; set; } = null!;

    public CustomerType CustomerType { get; set; } = CustomerType.Registered;

    public Guid? CustomerUserId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string? CustomerEmail { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public OrderType OrderType { get; set; } = OrderType.Pickup;

    public string CustomerPhone { get; set; } = string.Empty;

    public string? DeliveryAddress { get; set; }

    public decimal TotalAmount { get; set; }

    public string Notes { get; set; } = string.Empty;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
