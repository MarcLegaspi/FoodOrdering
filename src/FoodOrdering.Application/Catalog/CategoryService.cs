using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Application.Catalog;

public sealed class CategoryService(IAppDbContext db, ITenantContext tenantContext)
{
    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken ct = default)
    {
        // Global query filter in AppDbContext automatically filters by tenantContext.StoreId
        return await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => ToResponse(c))
            .ToListAsync(ct);
    }

    public async Task<CategoryResult> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        if (tenantContext.StoreId is null)
        {
            return CategoryResult.ValidationError("Store tenant context is required.");
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return CategoryResult.ValidationError("Category name is required.");
        }

        var exists = await db.Categories.AnyAsync(c => c.Name == name, ct);
        if (exists)
        {
            return CategoryResult.Conflict($"Category '{name}' already exists for this store.");
        }

        var category = new Category
        {
            StoreId = tenantContext.StoreId.Value,
            Name = name,
            SortOrder = request.SortOrder
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        return CategoryResult.Ok(ToResponse(category));
    }

    public async Task<CategoryResult> UpdateAsync(
        Guid publicId,
        UpdateCategoryRequest request,
        CancellationToken ct = default)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.PublicId == publicId, ct);
        if (category is null)
        {
            return CategoryResult.NotFound("Category was not found for this store.");
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return CategoryResult.ValidationError("Category name is required.");
        }

        var duplicate = await db.Categories.AnyAsync(c => c.Id != category.Id && c.Name == name, ct);
        if (duplicate)
        {
            return CategoryResult.Conflict($"Category '{name}' already exists for this store.");
        }

        category.Name = name;
        category.SortOrder = request.SortOrder;
        category.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct);

        return CategoryResult.Ok(ToResponse(category));
    }

    public async Task<ReorderCategoriesResult> ReorderAsync(
        ReorderCategoriesRequest request,
        CancellationToken ct = default)
    {
        if (tenantContext.StoreId is null)
        {
            return ReorderCategoriesResult.ValidationError("Store tenant context is required.");
        }

        if (request.OrderedPublicIds is null || request.OrderedPublicIds.Count == 0)
        {
            return ReorderCategoriesResult.ValidationError("Ordered category list is required.");
        }

        var categories = await db.Categories
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

        var byPublicId = categories.ToDictionary(c => c.PublicId);
        var seen = new HashSet<Guid>();
        var ordered = new List<Category>(categories.Count);

        foreach (var publicId in request.OrderedPublicIds)
        {
            if (!byPublicId.TryGetValue(publicId, out var category))
            {
                return ReorderCategoriesResult.NotFound("One or more categories were not found for this store.");
            }

            if (seen.Add(publicId))
            {
                ordered.Add(category);
            }
        }

        foreach (var remaining in categories)
        {
            if (!seen.Contains(remaining.PublicId))
            {
                ordered.Add(remaining);
            }
        }

        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].SortOrder = i + 1;
        }

        await db.SaveChangesAsync(ct);

        return ReorderCategoriesResult.Ok(ordered.Select(ToResponse).ToList());
    }

    public async Task<CategoryResult> DeleteAsync(Guid publicId, CancellationToken ct = default)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.PublicId == publicId, ct);
        if (category is null)
        {
            return CategoryResult.NotFound("Category was not found for this store.");
        }

        var hasMenuItems = await db.MenuItems.AnyAsync(m => m.CategoryId == category.Id, ct);
        if (hasMenuItems)
        {
            return CategoryResult.Conflict("Cannot delete a category that still has menu items.");
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);

        return CategoryResult.Deleted();
    }

    private static CategoryResponse ToResponse(Category category) =>
        new(
            category.PublicId,
            category.Name,
            category.SortOrder,
            category.IsActive,
            category.CreatedAtUtc);
}

public sealed record CategoryResult(
    CategoryResponse? Category,
    string? Error,
    bool IsConflict,
    bool IsNotFound)
{
    public static CategoryResult Ok(CategoryResponse category) => new(category, null, false, false);
    public static CategoryResult Deleted() => new(null, null, false, false);
    public static CategoryResult ValidationError(string error) => new(null, error, false, false);
    public static CategoryResult Conflict(string error) => new(null, error, true, false);
    public static CategoryResult NotFound(string error) => new(null, error, false, true);
}

public sealed record ReorderCategoriesResult(
    IReadOnlyList<CategoryResponse>? Categories,
    string? Error,
    bool IsNotFound)
{
    public static ReorderCategoriesResult Ok(IReadOnlyList<CategoryResponse> categories) => new(categories, null, false);
    public static ReorderCategoriesResult ValidationError(string error) => new(null, error, false);
    public static ReorderCategoriesResult NotFound(string error) => new(null, error, true);
}
