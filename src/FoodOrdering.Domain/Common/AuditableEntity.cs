namespace FoodOrdering.Domain.Common;

public abstract class AuditableEntity
{
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
