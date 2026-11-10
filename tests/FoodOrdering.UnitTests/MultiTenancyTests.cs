using FoodOrdering.Application.Catalog;
using Xunit;

namespace FoodOrdering.UnitTests;

public sealed class MultiTenancyTests
{
    [Fact]
    public async Task Categories_Are_Isolated_By_Tenant()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = new TestTenantContext();

        await using (var setupDb = TestDbFactory.Create(tenant, dbName))
        {
            await TestDbFactory.SeedTwoStoresAsync(setupDb);

            tenant.StoreId = 1;
            var store1Service = new CategoryService(setupDb, tenant);
            await store1Service.CreateAsync(new CreateCategoryRequest("Burgers", 1));

            tenant.StoreId = 2;
            var store2Service = new CategoryService(setupDb, tenant);
            await store2Service.CreateAsync(new CreateCategoryRequest("Pizzas", 1));
        }

        await using var readDb = TestDbFactory.Create(tenant, dbName);

        tenant.StoreId = 1;
        var store1Categories = await new CategoryService(readDb, tenant).GetAllAsync();
        Assert.Single(store1Categories);
        Assert.Equal("Burgers", store1Categories[0].Name);

        tenant.StoreId = 2;
        var store2Categories = await new CategoryService(readDb, tenant).GetAllAsync();
        Assert.Single(store2Categories);
        Assert.Equal("Pizzas", store2Categories[0].Name);
    }

    [Fact]
    public async Task Categories_Return_Empty_When_Tenant_Is_Null()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = new TestTenantContext();

        await using (var setupDb = TestDbFactory.Create(tenant, dbName))
        {
            await TestDbFactory.SeedTwoStoresAsync(setupDb);
            tenant.StoreId = 1;
            await new CategoryService(setupDb, tenant).CreateAsync(new CreateCategoryRequest("Burgers", 1));
        }

        tenant.StoreId = null;
        await using var readDb = TestDbFactory.Create(tenant, dbName);
        var categories = await new CategoryService(readDb, tenant).GetAllAsync();

        Assert.Empty(categories);
    }

    [Fact]
    public async Task CreateCategory_Rejects_Duplicate_In_Same_Store_But_Allows_Same_Name_In_Other_Store()
    {
        var tenant = new TestTenantContext();
        await using var db = TestDbFactory.Create(tenant);
        await TestDbFactory.SeedTwoStoresAsync(db);

        var service = new CategoryService(db, tenant);

        tenant.StoreId = 1;
        var first = await service.CreateAsync(new CreateCategoryRequest("Drinks", 1));
        var duplicate = await service.CreateAsync(new CreateCategoryRequest("Drinks", 2));

        tenant.StoreId = 2;
        var otherStore = await service.CreateAsync(new CreateCategoryRequest("Drinks", 1));

        Assert.NotNull(first.Category);
        Assert.True(duplicate.IsConflict);
        Assert.NotNull(otherStore.Category);
    }
}
