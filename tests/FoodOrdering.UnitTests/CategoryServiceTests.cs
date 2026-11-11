using FoodOrdering.Application.Catalog;
using Xunit;

namespace FoodOrdering.UnitTests;

public sealed class CategoryServiceTests
{
    [Fact]
    public async Task UpdateAsync_Updates_Name_And_SortOrder_And_Rejects_Duplicates()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = new TestTenantContext { StoreId = 1 };

        await using var db = TestDbFactory.Create(tenant, dbName);
        await TestDbFactory.SeedTwoStoresAsync(db);

        var service = new CategoryService(db, tenant);
        var cat1 = (await service.CreateAsync(new CreateCategoryRequest("Burgers", 1))).Category!;
        var cat2 = (await service.CreateAsync(new CreateCategoryRequest("Sides", 1))).Category!;

        var duplicate = await service.UpdateAsync(cat2.PublicId, new UpdateCategoryRequest("Burgers", 2));
        Assert.True(duplicate.IsConflict);

        var updated = await service.UpdateAsync(cat2.PublicId, new UpdateCategoryRequest("Snacks", 5));
        Assert.NotNull(updated.Category);
        Assert.Equal("Snacks", updated.Category!.Name);
        Assert.Equal(5, updated.Category.SortOrder);
    }

    [Fact]
    public async Task ReorderAsync_Resequences_Duplicate_SortOrders()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = new TestTenantContext { StoreId = 1 };

        await using var db = TestDbFactory.Create(tenant, dbName);
        await TestDbFactory.SeedTwoStoresAsync(db);

        var service = new CategoryService(db, tenant);
        var burgers = (await service.CreateAsync(new CreateCategoryRequest("Burgers", 1))).Category!;
        var drinks = (await service.CreateAsync(new CreateCategoryRequest("Drinks", 1))).Category!;

        var reordered = await service.ReorderAsync(new ReorderCategoriesRequest([drinks.PublicId, burgers.PublicId]));
        Assert.NotNull(reordered.Categories);
        Assert.Equal(["Drinks", "Burgers"], reordered.Categories!.Select(c => c.Name).ToArray());
        Assert.Equal([1, 2], reordered.Categories.Select(c => c.SortOrder).ToArray());
    }

    [Fact]
    public async Task DeleteAsync_Rejects_When_Category_Has_MenuItems_And_Deletes_When_Empty()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = new TestTenantContext { StoreId = 1 };

        await using var db = TestDbFactory.Create(tenant, dbName);
        await TestDbFactory.SeedTwoStoresAsync(db);

        var categoryService = new CategoryService(db, tenant);
        var menuItemService = new MenuItemService(db, tenant);

        var cat = (await categoryService.CreateAsync(new CreateCategoryRequest("Burgers", 1))).Category!;
        var item = (await menuItemService.CreateAsync(new CreateMenuItemRequest(cat.PublicId, "Burger", "", 8.50m))).Item!;

        var conflictDelete = await categoryService.DeleteAsync(cat.PublicId);
        Assert.True(conflictDelete.IsConflict);

        await menuItemService.DeleteAsync(item.PublicId);
        var deleted = await categoryService.DeleteAsync(cat.PublicId);
        Assert.Null(deleted.Error);
        Assert.Empty(await categoryService.GetAllAsync());
    }
}
