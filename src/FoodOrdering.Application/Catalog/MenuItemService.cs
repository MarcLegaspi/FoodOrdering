using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Application.Catalog;

public sealed class MenuItemService(IAppDbContext db, ITenantContext tenantContext)
{
    public async Task<IReadOnlyList<MenuItemResponse>> GetAllAsync(
        Guid? categoryPublicId = null,
        bool? availableOnly = null,
        CancellationToken ct = default)
    {
        var query = db.MenuItems
            .AsNoTracking()
            .AsQueryable();

        if (categoryPublicId is Guid catId)
        {
            query = query.Where(m => m.Category.PublicId == catId);
        }

        if (availableOnly == true)
        {
            query = query.Where(m => m.IsAvailable);
        }

        return await query
            .OrderBy(m => m.Category.SortOrder)
            .ThenBy(m => m.SortOrder)
            .ThenBy(m => m.Name)
            .Select(m => new MenuItemResponse(
                m.PublicId,
                m.Category.PublicId,
                m.Category.Name,
                m.Name,
                m.Description,
                m.Price,
                m.ImageUrl,
                m.IsAvailable,
                m.SortOrder,
                m.CreatedAtUtc,
                m.OptionGroups
                    .OrderBy(g => g.SortOrder)
                    .ThenBy(g => g.Name)
                    .Select(g => new MenuItemOptionGroupResponse(
                        g.PublicId,
                        g.Name,
                        g.IsRequired,
                        g.AllowMultiple,
                        g.SortOrder,
                        g.Options
                            .OrderBy(o => o.SortOrder)
                            .ThenBy(o => o.Name)
                            .Select(o => new MenuItemOptionResponse(
                                o.PublicId,
                                o.Name,
                                o.PriceDelta,
                                o.SortOrder))
                            .ToList()))
                    .ToList()))
            .ToListAsync(ct);
    }

    public async Task<MenuItemResult> CreateAsync(CreateMenuItemRequest request, CancellationToken ct = default)
    {
        if (tenantContext.StoreId is null)
        {
            return MenuItemResult.ValidationError("Store tenant context is required.");
        }

        var validation = await ValidateAndResolveCategoryAsync(
            request.CategoryPublicId,
            request.Name,
            request.Price,
            excludeItemId: null,
            ct);

        if (validation.ErrorResult is not null)
        {
            return validation.ErrorResult;
        }

        var (optionGroups, optionError) = BuildValidatedOptionGroups(request.OptionGroups);
        if (optionError is not null)
        {
            return MenuItemResult.ValidationError(optionError);
        }

        var category = validation.Category!;
        var item = new MenuItem
        {
            StoreId = tenantContext.StoreId.Value,
            CategoryId = category.Id,
            Name = validation.NormalizedName,
            Description = request.Description.Trim(),
            Price = request.Price,
            ImageUrl = NormalizeOptionalUrl(request.ImageUrl),
            IsAvailable = request.IsAvailable,
            SortOrder = request.SortOrder,
            OptionGroups = optionGroups ?? []
        };

        db.MenuItems.Add(item);
        await db.SaveChangesAsync(ct);

        return MenuItemResult.Success(ToResponse(item, category));
    }

    public async Task<MenuItemResult> UpdateAsync(
        Guid publicId,
        UpdateMenuItemRequest request,
        CancellationToken ct = default)
    {
        var item = await db.MenuItems
            .Include(m => m.OptionGroups)
            .ThenInclude(g => g.Options)
            .FirstOrDefaultAsync(m => m.PublicId == publicId, ct);

        if (item is null)
        {
            return MenuItemResult.NotFound("Menu item was not found for this store.");
        }

        var validation = await ValidateAndResolveCategoryAsync(
            request.CategoryPublicId,
            request.Name,
            request.Price,
            excludeItemId: item.Id,
            ct);

        if (validation.ErrorResult is not null)
        {
            return validation.ErrorResult;
        }

        var (optionGroups, optionError) = BuildValidatedOptionGroups(request.OptionGroups);
        if (optionError is not null)
        {
            return MenuItemResult.ValidationError(optionError);
        }

        var category = validation.Category!;
        item.CategoryId = category.Id;
        item.Name = validation.NormalizedName;
        item.Description = request.Description.Trim();
        item.Price = request.Price;
        item.ImageUrl = NormalizeOptionalUrl(request.ImageUrl);
        item.IsAvailable = request.IsAvailable;
        item.SortOrder = request.SortOrder;

        if (optionGroups is not null)
        {
            db.MenuItemOptionGroups.RemoveRange(item.OptionGroups);
            item.OptionGroups.Clear();
            foreach (var group in optionGroups)
            {
                item.OptionGroups.Add(group);
            }
        }

        await db.SaveChangesAsync(ct);

        return MenuItemResult.Success(ToResponse(item, category));
    }

    public async Task<MenuItemResult> DeleteAsync(Guid publicId, CancellationToken ct = default)
    {
        var item = await db.MenuItems.FirstOrDefaultAsync(m => m.PublicId == publicId, ct);
        if (item is null)
        {
            return MenuItemResult.NotFound("Menu item was not found for this store.");
        }

        db.MenuItems.Remove(item);
        await db.SaveChangesAsync(ct);

        return new MenuItemResult(null, null);
    }

    private async Task<(Category? Category, string NormalizedName, MenuItemResult? ErrorResult)> ValidateAndResolveCategoryAsync(
        Guid categoryPublicId,
        string rawName,
        decimal price,
        long? excludeItemId,
        CancellationToken ct)
    {
        var name = rawName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return (null, name, MenuItemResult.ValidationError("Menu item name is required."));
        }

        if (price < 0)
        {
            return (null, name, MenuItemResult.ValidationError("Price cannot be negative."));
        }

        // Tenant filter on Categories ensures the category belongs to the active store
        var category = await db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.PublicId == categoryPublicId, ct);

        if (category is null)
        {
            return (null, name, MenuItemResult.NotFound("Category was not found for this store."));
        }

        var duplicate = await db.MenuItems.AnyAsync(
            m => m.CategoryId == category.Id
                && m.Name == name
                && (excludeItemId == null || m.Id != excludeItemId.Value),
            ct);

        if (duplicate)
        {
            return (null, name, MenuItemResult.Conflict($"Menu item '{name}' already exists in '{category.Name}'."));
        }

        return (category, name, null);
    }

    private static (List<MenuItemOptionGroup>? Groups, string? Error) BuildValidatedOptionGroups(
        IReadOnlyList<SaveMenuItemOptionGroupRequest>? rawGroups)
    {
        if (rawGroups is null)
        {
            return (null, null);
        }

        var groups = new List<MenuItemOptionGroup>(rawGroups.Count);
        for (var gIndex = 0; gIndex < rawGroups.Count; gIndex++)
        {
            var rawGroup = rawGroups[gIndex];
            var groupName = rawGroup.Name.Trim();
            if (string.IsNullOrWhiteSpace(groupName))
            {
                return (null, "Option group name is required.");
            }

            if (rawGroup.Options is null || rawGroup.Options.Count == 0)
            {
                return (null, $"Option group '{groupName}' must contain at least one option.");
            }

            var options = new List<MenuItemOption>(rawGroup.Options.Count);
            for (var oIndex = 0; oIndex < rawGroup.Options.Count; oIndex++)
            {
                var rawOption = rawGroup.Options[oIndex];
                var optionName = rawOption.Name.Trim();
                if (string.IsNullOrWhiteSpace(optionName))
                {
                    return (null, $"All options in '{groupName}' must have a name.");
                }

                if (rawOption.PriceDelta < 0)
                {
                    return (null, $"Option '{optionName}' price add-on cannot be negative.");
                }

                options.Add(new MenuItemOption
                {
                    Name = optionName,
                    PriceDelta = rawOption.PriceDelta,
                    SortOrder = rawOption.SortOrder > 0 ? rawOption.SortOrder : oIndex + 1
                });
            }

            groups.Add(new MenuItemOptionGroup
            {
                Name = groupName,
                IsRequired = rawGroup.IsRequired,
                AllowMultiple = rawGroup.AllowMultiple,
                SortOrder = rawGroup.SortOrder > 0 ? rawGroup.SortOrder : gIndex + 1,
                Options = options
            });
        }

        return (groups, null);
    }

    private static string? NormalizeOptionalUrl(string? url) =>
        string.IsNullOrWhiteSpace(url) ? null : url.Trim();

    private static MenuItemResponse ToResponse(MenuItem item, Category category) =>
        new(
            item.PublicId,
            category.PublicId,
            category.Name,
            item.Name,
            item.Description,
            item.Price,
            item.ImageUrl,
            item.IsAvailable,
            item.SortOrder,
            item.CreatedAtUtc,
            item.OptionGroups
                .OrderBy(g => g.SortOrder)
                .ThenBy(g => g.Name)
                .Select(g => new MenuItemOptionGroupResponse(
                    g.PublicId,
                    g.Name,
                    g.IsRequired,
                    g.AllowMultiple,
                    g.SortOrder,
                    g.Options
                        .OrderBy(o => o.SortOrder)
                        .ThenBy(o => o.Name)
                        .Select(o => new MenuItemOptionResponse(
                            o.PublicId,
                            o.Name,
                            o.PriceDelta,
                            o.SortOrder))
                        .ToList()))
                .ToList());
}
