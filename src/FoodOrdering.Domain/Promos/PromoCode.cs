using FoodOrdering.Domain.Common;
using FoodOrdering.Domain.Stores;

namespace FoodOrdering.Domain.Promos;

public sealed class PromoCode : AuditableEntity, ITenantEntity
{
    public long Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public long StoreId { get; set; }

    public Store Store { get; set; } = null!;

    public string Code { get; set; } = string.Empty;

    public PromoDiscountType DiscountType { get; set; } = PromoDiscountType.FixedAmount;

    public decimal DiscountValue { get; set; }

    public int? MaxUses { get; set; }

    public int UsedCount { get; set; }

    public DateTime? StartsAtUtc { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    public bool IsActive { get; set; } = true;
}
