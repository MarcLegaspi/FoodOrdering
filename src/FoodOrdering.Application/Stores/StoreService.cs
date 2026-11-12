using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Domain.Stores;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Application.Stores;

public sealed class StoreService(
    IAppDbContext db,
    ITenantContext tenantContext,
    IUserDirectory userDirectory)
{
    public async Task<IReadOnlyList<StoreResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Stores
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => ToResponse(s))
            .ToListAsync(ct);
    }

    public async Task<StoreResponse?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var normalizedSlug = NormalizeSlug(slug);

        return await db.Stores
            .AsNoTracking()
            .Where(s => s.Slug == normalizedSlug)
            .Select(s => ToResponse(s))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<CreateStoreResult> CreateAsync(CreateStoreRequest request, CancellationToken ct = default)
    {
        var slug = NormalizeSlug(request.Slug);
        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(name))
        {
            return CreateStoreResult.ValidationError("Slug and Name are required.");
        }

        var exists = await db.Stores.AnyAsync(s => s.Slug == slug, ct);
        if (exists)
        {
            return CreateStoreResult.Conflict($"A store with slug '{slug}' already exists.");
        }

        UserSummary? owner = null;
        if (!string.IsNullOrWhiteSpace(request.OwnerEmail))
        {
            var ownerEmail = request.OwnerEmail.Trim();
            owner = await userDirectory.FindByEmailAsync(ownerEmail, ct);

            if (owner is null)
            {
                if (string.IsNullOrWhiteSpace(request.OwnerFullName) || string.IsNullOrWhiteSpace(request.OwnerPassword))
                {
                    return CreateStoreResult.ValidationError(
                        $"User '{ownerEmail}' does not exist yet. Provide OwnerFullName and OwnerPassword to create them.");
                }

                var (createdOwner, errors) = await userDirectory.CreateCustomerAsync(
                    ownerEmail,
                    request.OwnerFullName,
                    request.OwnerPassword,
                    ct);

                if (createdOwner is null)
                {
                    return CreateStoreResult.ValidationError(string.Join(" ", errors));
                }

                owner = createdOwner;
            }
        }

        var store = new Store
        {
            Slug = slug,
            Name = name,
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            Address = request.Address?.Trim() ?? string.Empty,
            OpeningHours = request.OpeningHours?.Trim() ?? string.Empty,
            DeliveryFee = request.DeliveryFee < 0 ? 0m : request.DeliveryFee,
            MinimumDeliveryOrder = request.MinimumDeliveryOrder < 0 ? 0m : request.MinimumDeliveryOrder,
            FreeDeliveryThreshold = request.FreeDeliveryThreshold is { } ft && ft >= 0 ? ft : null,
            EstimatedDeliveryTime = request.EstimatedDeliveryTime?.Trim() ?? string.Empty
        };

        db.Stores.Add(store);

        if (owner is not null)
        {
            db.StoreMembers.Add(new StoreMember
            {
                Store = store,
                UserId = owner.Id,
                Role = StoreRole.Owner
            });
        }

        await db.SaveChangesAsync(ct);

        return CreateStoreResult.Created(ToResponse(store));
    }

    public async Task<StoreResponse?> UpdateCurrentStoreSettingsAsync(
        UpdateStoreSettingsRequest request,
        CancellationToken ct = default)
    {
        if (tenantContext.StoreId is not long storeId)
        {
            return null;
        }

        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == storeId, ct);
        if (store is null)
        {
            return null;
        }

        store.IsAcceptingOrders = request.IsAcceptingOrders;
        await db.SaveChangesAsync(ct);

        return ToResponse(store);
    }

    public async Task<StoreProfileResult> UpdateCurrentStoreProfileAsync(
        UpdateStoreProfileRequest request,
        CancellationToken ct = default)
    {
        if (tenantContext.StoreId is not long storeId)
        {
            return StoreProfileResult.NotFound("Store tenant context is required.");
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return StoreProfileResult.ValidationError("Store name is required.");
        }

        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == storeId, ct);
        if (store is null)
        {
            return StoreProfileResult.NotFound("Active store was not found.");
        }

        store.Name = name;
        store.Email = request.Email.Trim();
        store.Phone = request.Phone.Trim();
        store.Address = request.Address.Trim();
        store.OpeningHours = request.OpeningHours.Trim();
        store.DeliveryFee = request.DeliveryFee < 0 ? 0m : request.DeliveryFee;
        store.MinimumDeliveryOrder = request.MinimumDeliveryOrder < 0 ? 0m : request.MinimumDeliveryOrder;
        store.FreeDeliveryThreshold = request.FreeDeliveryThreshold is { } threshold && threshold >= 0 ? threshold : null;
        store.EstimatedDeliveryTime = request.EstimatedDeliveryTime?.Trim() ?? string.Empty;

        await db.SaveChangesAsync(ct);

        return StoreProfileResult.Success(ToResponse(store));
    }

    private static string NormalizeSlug(string slug) =>
        slug.Trim().ToLowerInvariant();

    private static StoreResponse ToResponse(Store store) =>
        new(
            store.PublicId,
            store.Slug,
            store.Name,
            store.Email,
            store.Phone,
            store.Address,
            store.OpeningHours,
            store.IsActive,
            store.IsAcceptingOrders,
            store.CreatedAtUtc,
            store.DeliveryFee,
            store.MinimumDeliveryOrder,
            store.FreeDeliveryThreshold,
            store.EstimatedDeliveryTime);
}

public sealed record StoreProfileResult(
    StoreResponse? Store,
    string? Error,
    bool IsNotFound = false)
{
    public static StoreProfileResult Success(StoreResponse store) => new(store, null);
    public static StoreProfileResult ValidationError(string error) => new(null, error);
    public static StoreProfileResult NotFound(string error) => new(null, error, IsNotFound: true);
}

public sealed record CreateStoreResult(
    StoreResponse? Store,
    string? Error,
    bool IsConflict)
{
    public static CreateStoreResult Created(StoreResponse store) => new(store, null, false);
    public static CreateStoreResult ValidationError(string error) => new(null, error, false);
    public static CreateStoreResult Conflict(string error) => new(null, error, true);
}
