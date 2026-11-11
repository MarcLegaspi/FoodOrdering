using FoodOrdering.Application.Stores;
using FoodOrdering.Domain.Stores;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FoodOrdering.UnitTests;

public sealed class StoreServiceTests
{
    [Fact]
    public async Task CreateAsync_Normalizes_Slug_And_Assigns_Owner()
    {
        var tenant = new TestTenantContext();
        await using var db = TestDbFactory.Create(tenant);
        var users = new FakeUserDirectory();
        var service = new StoreService(db, tenant, users);

        var result = await service.CreateAsync(new CreateStoreRequest(
            Slug: "  Taco-Town  ",
            Name: "Taco Town",
            Email: "hello@tacotown.local",
            Phone: "+1-555-0104",
            OwnerEmail: "owner@tacotown.local",
            OwnerFullName: "Taco Owner",
            OwnerPassword: "Password123"));

        Assert.NotNull(result.Store);
        Assert.Equal("taco-town", result.Store.Slug);
        Assert.True(result.Store.IsAcceptingOrders);

        var ownerMember = await db.StoreMembers
            .IgnoreQueryFilters()
            .SingleAsync();

        Assert.Equal(StoreRole.Owner, ownerMember.Role);
    }

    [Fact]
    public async Task CreateAsync_Returns_Conflict_On_Duplicate_Slug()
    {
        var tenant = new TestTenantContext();
        await using var db = TestDbFactory.Create(tenant);
        await TestDbFactory.SeedTwoStoresAsync(db);

        var service = new StoreService(db, tenant, new FakeUserDirectory());
        var result = await service.CreateAsync(new CreateStoreRequest(
            Slug: "BURGER-BARN",
            Name: "Duplicate Burger Barn",
            Email: "dup@test.local",
            Phone: "123"));

        Assert.True(result.IsConflict);
        Assert.Null(result.Store);
    }

    [Fact]
    public async Task UpdateCurrentStoreSettingsAsync_Toggles_IsAcceptingOrders()
    {
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        await TestDbFactory.SeedTwoStoresAsync(db);

        var service = new StoreService(db, tenant, new FakeUserDirectory());

        var closed = await service.UpdateCurrentStoreSettingsAsync(
            new UpdateStoreSettingsRequest(IsAcceptingOrders: false));
        Assert.NotNull(closed);
        Assert.False(closed.IsAcceptingOrders);

        var reopened = await service.UpdateCurrentStoreSettingsAsync(
            new UpdateStoreSettingsRequest(IsAcceptingOrders: true));
        Assert.NotNull(reopened);
        Assert.True(reopened.IsAcceptingOrders);
    }

    [Fact]
    public async Task UpdateCurrentStoreProfileAsync_Updates_Profile_And_Rejects_Blank_Name()
    {
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        await TestDbFactory.SeedTwoStoresAsync(db);

        var service = new StoreService(db, tenant, new FakeUserDirectory());

        var invalid = await service.UpdateCurrentStoreProfileAsync(new UpdateStoreProfileRequest(
            Name: "   ",
            Email: "new@burgerbarn.local",
            Phone: "+1-555-9999",
            Address: "99 Main St",
            OpeningHours: "9 AM - 9 PM"));
        Assert.NotNull(invalid.Error);

        var updated = await service.UpdateCurrentStoreProfileAsync(new UpdateStoreProfileRequest(
            Name: "Burger Barn Flagship",
            Email: "new@burgerbarn.local",
            Phone: "+1-555-9999",
            Address: "99 Main St",
            OpeningHours: "9 AM - 9 PM"));

        Assert.Null(updated.Error);
        Assert.NotNull(updated.Store);
        Assert.Equal("Burger Barn Flagship", updated.Store!.Name);
        Assert.Equal("new@burgerbarn.local", updated.Store.Email);
        Assert.Equal("+1-555-9999", updated.Store.Phone);
        Assert.Equal("99 Main St", updated.Store.Address);
        Assert.Equal("9 AM - 9 PM", updated.Store.OpeningHours);
    }
}
