using FoodOrdering.Domain.Common;
using FoodOrdering.Domain.Stores;

namespace FoodOrdering.Domain.Catalog;

public sealed class Category : AuditableEntity, ITenantEntity
{
    public long Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public long StoreId { get; set; }

    public Store Store { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
