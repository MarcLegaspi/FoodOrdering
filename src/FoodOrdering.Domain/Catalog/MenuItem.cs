using FoodOrdering.Domain.Common;
using FoodOrdering.Domain.Stores;

namespace FoodOrdering.Domain.Catalog;

public sealed class MenuItem : AuditableEntity, ITenantEntity
{
    public long Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public long StoreId { get; set; }

    public Store Store { get; set; } = null!;

    public long CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; } = true;

    public int SortOrder { get; set; }
}
