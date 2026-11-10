namespace FoodOrdering.Application.Common.Interfaces;

public interface ITenantContext
{
    long? StoreId { get; }

    string? StoreSlug { get; }
}
