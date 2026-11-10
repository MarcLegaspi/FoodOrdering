using FoodOrdering.Application.Common.Interfaces;

namespace FoodOrdering.Infrastructure.MultiTenancy;

public sealed class TenantContext : ITenantContext
{
    public long? StoreId { get; private set; }

    public string? StoreSlug { get; private set; }

    public void SetTenant(long storeId, string storeSlug)
    {
        StoreId = storeId;
        StoreSlug = storeSlug;
    }
}
