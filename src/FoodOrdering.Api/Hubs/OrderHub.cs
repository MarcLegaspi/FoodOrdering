using System.Security.Claims;
using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Domain.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Api.Hubs;

[Authorize]
public sealed class OrderHub(IAppDbContext db) : Hub
{
    public static string StoreGroup(string storeSlug) => $"store:{storeSlug.Trim().ToLowerInvariant()}";

    public static string UserGroup(Guid userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        if (TryGetUserId(out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinStoreKitchen(string storeSlug)
    {
        if (!TryGetUserId(out var userId))
        {
            throw new HubException("Authentication is required.");
        }

        var normalizedSlug = storeSlug.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            throw new HubException("Store slug is required.");
        }

        var store = await db.Stores
            .AsNoTracking()
            .Where(s => s.Slug == normalizedSlug && s.IsActive)
            .Select(s => new { s.Id, s.Slug })
            .FirstOrDefaultAsync(Context.ConnectionAborted);

        if (store is null)
        {
            throw new HubException($"Active store '{normalizedSlug}' was not found.");
        }

        var isAdmin = Context.User?.IsInRole("Admin") ?? false;
        if (!isAdmin)
        {
            var isStaffOrAbove = await db.StoreMembers
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(
                    m => m.StoreId == store.Id && m.UserId == userId && m.Role >= StoreRole.Staff,
                    Context.ConnectionAborted);

            if (!isStaffOrAbove)
            {
                throw new HubException("You do not have permission to access this store kitchen.");
            }
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, StoreGroup(store.Slug));
    }

    public Task LeaveStoreKitchen(string storeSlug)
    {
        if (string.IsNullOrWhiteSpace(storeSlug))
        {
            return Task.CompletedTask;
        }

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, StoreGroup(storeSlug));
    }

    private bool TryGetUserId(out Guid userId)
    {
        var raw = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }
}

