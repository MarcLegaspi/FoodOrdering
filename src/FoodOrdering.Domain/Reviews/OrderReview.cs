using FoodOrdering.Domain.Common;
using FoodOrdering.Domain.Orders;
using FoodOrdering.Domain.Stores;

namespace FoodOrdering.Domain.Reviews;

public sealed class OrderReview : AuditableEntity, ITenantEntity
{
    public long Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public long StoreId { get; set; }

    public Store Store { get; set; } = null!;

    public long OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public Guid CustomerUserId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string? Comment { get; set; }
}
