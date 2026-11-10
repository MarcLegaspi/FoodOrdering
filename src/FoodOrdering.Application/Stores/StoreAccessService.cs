using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Domain.Stores;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Application.Stores;

public sealed class StoreAccessService(
    IAppDbContext db,
    ITenantContext tenantContext,
    ICurrentUser currentUser)
{
    public async Task<StoreRole?> GetCurrentUserRoleAsync(CancellationToken ct = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not Guid userId)
        {
            return null;
        }

        if (currentUser.IsInRole("Admin"))
        {
            return StoreRole.Owner;
        }

        if (tenantContext.StoreId is null)
        {
            return null;
        }

        // Global query filter on StoreMember already scopes this to tenantContext.StoreId
        return await db.StoreMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => (StoreRole?)m.Role)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> HasRoleAsync(StoreRole minimumRole, CancellationToken ct = default)
    {
        var role = await GetCurrentUserRoleAsync(ct);
        return role is not null && role >= minimumRole;
    }
}
