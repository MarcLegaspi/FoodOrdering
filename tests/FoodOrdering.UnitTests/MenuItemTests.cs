using FoodOrdering.Application.Catalog;
using Xunit;

namespace FoodOrdering.UnitTests;

public sealed class MenuItemTests
{
    [Fact]
    public async Task MenuItems_Are_Isolated_By_Tenant_And_Cannot_Use_Other_Store_Category()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = new TestTenantContext();
        Guid store1CategoryId;
        Guid store2CategoryId;

        await using (var setupDb = TestDbFactory.Create(tenant, dbName))
        {
            await TestDbFactory.SeedTwoStoresAsync(setupDb);

            tenant.StoreId = 1;
            var cat1 = await new CategoryService(setupDb, tenant)
                .CreateAsync(new CreateCategoryRequest("Burgers", 1));
            store1CategoryId = cat1.Category!.PublicId;

            tenant.StoreId = 2;
            var cat2 = await new CategoryService(setupDb, tenant)
                .CreateAsync(new CreateCategoryRequest("Pizzas", 1));
            store2CategoryId = cat2.Category!.PublicId;

            tenant.StoreId = 1;
            var menu1 = new MenuItemService(setupDb, tenant);
            var created = await menu1.CreateAsync(new CreateMenuItemRequest(
                store1CategoryId,
                "Classic Burger",
                "Beef patty with cheddar",
                9.99m));
            Assert.NotNull(created.Item);

            // Store 1 must NOT be able to attach an item to Store 2's category
            var crossTenantAttempt = await menu1.CreateAsync(new CreateMenuItemRequest(
                store2CategoryId,
                "Sneaky Pizza",
                "Cross-tenant test",
                12.00m));
            Assert.True(crossTenantAttempt.IsNotFound);
        }

        await using var readDb = TestDbFactory.Create(tenant, dbName);

        tenant.StoreId = 1;
        var store1Items = await new MenuItemService(readDb, tenant).GetAllAsync();
        Assert.Single(store1Items);
        Assert.Equal("Classic Burger", store1Items[0].Name);

        tenant.StoreId = 2;
        var store2Items = await new MenuItemService(readDb, tenant).GetAllAsync();
        Assert.Empty(store2Items);
    }

    [Fact]
    public async Task Create_Update_And_Delete_MenuItem_Enforces_Validation_And_Availability_Filter()
    {
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        await TestDbFactory.SeedTwoStoresAsync(db);

        var catResult = await new CategoryService(db, tenant)
            .CreateAsync(new CreateCategoryRequest("Drinks", 1));
        var categoryId = catResult.Category!.PublicId;

        var service = new MenuItemService(db, tenant);

        var negativePrice = await service.CreateAsync(new CreateMenuItemRequest(
            categoryId, "Iced Tea", "Cold brewed", -1.00m));
        Assert.Null(negativePrice.Item);
        Assert.Contains("negative", negativePrice.Error, StringComparison.OrdinalIgnoreCase);

        var created = await service.CreateAsync(new CreateMenuItemRequest(
            categoryId, "Iced Tea", "Cold brewed", 3.50m, IsAvailable: true, SortOrder: 1));
        Assert.NotNull(created.Item);

        var duplicate = await service.CreateAsync(new CreateMenuItemRequest(
            categoryId, "Iced Tea", "Duplicate", 3.50m));
        Assert.True(duplicate.IsConflict);

        var updated = await service.UpdateAsync(created.Item.PublicId, new UpdateMenuItemRequest(
            categoryId, "Lemon Iced Tea", "Fresh lemon", 4.00m, IsAvailable: false, SortOrder: 2));
        Assert.NotNull(updated.Item);
        Assert.Equal("Lemon Iced Tea", updated.Item.Name);
        Assert.False(updated.Item.IsAvailable);

        var availableOnly = await service.GetAllAsync(availableOnly: true);
        Assert.Empty(availableOnly);

        var deleted = await service.DeleteAsync(created.Item.PublicId);
        Assert.False(deleted.IsNotFound);
        Assert.Empty(await service.GetAllAsync());
    }
}
