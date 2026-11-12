using FoodOrdering.Domain.Catalog;
using FoodOrdering.Domain.Common;
using FoodOrdering.Domain.Orders;

namespace FoodOrdering.Domain.Stores;

public sealed class Store : AuditableEntity
{
    public long Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string OpeningHours { get; set; } = string.Empty;

    public decimal DeliveryFee { get; set; } = 0m;

    public decimal MinimumDeliveryOrder { get; set; } = 0m;

    public decimal? FreeDeliveryThreshold { get; set; }

    public string EstimatedDeliveryTime { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool IsAcceptingOrders { get; set; } = true;

    public ICollection<Category> Categories { get; set; } = new List<Category>();

    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();

    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public ICollection<StoreMember> Members { get; set; } = new List<StoreMember>();
}
