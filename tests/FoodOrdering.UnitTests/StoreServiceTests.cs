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
        var service = new StoreService(db, users);

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

        var service = new StoreService(db, new FakeUserDirectory());
        var result = await service.CreateAsync(new CreateStoreRequest(
            Slug: "BURGER-BARN",
            Name: "Duplicate Burger Barn",
            Email: "dup@test.local",
            Phone: "123"));

        Assert.True(result.IsConflict);
        Assert.Null(result.Store);
    }
}
