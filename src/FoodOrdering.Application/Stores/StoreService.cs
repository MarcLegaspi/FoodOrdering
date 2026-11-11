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
            Phone = request.Phone.Trim()
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

    private static string NormalizeSlug(string slug) =>
        slug.Trim().ToLowerInvariant();

    private static StoreResponse ToResponse(Store store) =>
        new(
            store.PublicId,
            store.Slug,
            store.Name,
            store.Email,
            store.Phone,
            store.IsActive,
            store.IsAcceptingOrders,
            store.CreatedAtUtc);
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
