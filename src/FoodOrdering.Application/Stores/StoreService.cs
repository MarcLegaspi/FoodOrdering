using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Domain.Stores;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Application.Stores;

public sealed class StoreService(IAppDbContext db)
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

        var store = new Store
        {
            Slug = slug,
            Name = name,
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim()
        };

        db.Stores.Add(store);
        await db.SaveChangesAsync(ct);

        return CreateStoreResult.Created(ToResponse(store));
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
