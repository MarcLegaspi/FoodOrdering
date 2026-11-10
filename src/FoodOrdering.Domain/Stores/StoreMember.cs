using FoodOrdering.Domain.Common;

namespace FoodOrdering.Domain.Stores;

public sealed class StoreMember : AuditableEntity, ITenantEntity
{
    public long Id { get; set; }

    public long StoreId { get; set; }

    public Store Store { get; set; } = null!;

    public Guid UserId { get; set; }

    public StoreRole Role { get; set; } = StoreRole.Staff;
}
