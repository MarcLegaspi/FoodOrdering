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

    public async Task<CreateCategoryResult> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        if (tenantContext.StoreId is null)
        {
            return CreateCategoryResult.ValidationError("Store tenant context is required.");
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return CreateCategoryResult.ValidationError("Category name is required.");
        }

        var exists = await db.Categories.AnyAsync(c => c.Name == name, ct);
        if (exists)
        {
            return CreateCategoryResult.Conflict($"Category '{name}' already exists for this store.");
        }

        var category = new Category
        {
            StoreId = tenantContext.StoreId.Value,
            Name = name,
            SortOrder = request.SortOrder
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        return CreateCategoryResult.Created(ToResponse(category));
    }

    private static CategoryResponse ToResponse(Category category) =>
        new(
            category.PublicId,
            category.Name,
            category.SortOrder,
            category.IsActive,
            category.CreatedAtUtc);
}

public sealed record CreateCategoryResult(
    CategoryResponse? Category,
    string? Error,
    bool IsConflict)
{
    public static CreateCategoryResult Created(CategoryResponse category) => new(category, null, false);
    public static CreateCategoryResult ValidationError(string error) => new(null, error, false);
    public static CreateCategoryResult Conflict(string error) => new(null, error, true);
}
