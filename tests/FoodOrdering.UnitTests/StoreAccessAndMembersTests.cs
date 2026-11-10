using FoodOrdering.Application.Auth;
using FoodOrdering.Application.Stores;
using FoodOrdering.Domain.Stores;
using Xunit;

namespace FoodOrdering.UnitTests;

public sealed class StoreAccessAndMembersTests
{
    [Fact]
    public async Task StoreAccess_Isolates_Permissions_Per_Store_While_Admin_Has_Full_Access()
    {
        var ownerUserId = Guid.NewGuid();
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        await TestDbFactory.SeedTwoStoresAsync(db);

        db.StoreMembers.Add(new StoreMember
        {
            StoreId = 1,
            UserId = ownerUserId,
            Role = StoreRole.Owner
        });
        await db.SaveChangesAsync();

        var ownerUser = new TestCurrentUser(ownerUserId, isAuthenticated: true, AppRoles.Customer);
        var ownerAccess = new StoreAccessService(db, tenant, ownerUser);

        tenant.StoreId = 1;
        Assert.True(await ownerAccess.HasRoleAsync(StoreRole.Staff));
        Assert.True(await ownerAccess.HasRoleAsync(StoreRole.Owner));

        tenant.StoreId = 2;
        Assert.False(await ownerAccess.HasRoleAsync(StoreRole.Staff));

        var adminUser = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, AppRoles.Admin);
        var adminAccess = new StoreAccessService(db, tenant, adminUser);
        Assert.True(await adminAccess.HasRoleAsync(StoreRole.Owner));
    }

    [Fact]
    public async Task StoreMemberService_Enforces_Role_Hierarchy_And_Protects_Last_Owner()
    {
        var ownerId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var tenant = new TestTenantContext(storeId: 1);

        await using var db = TestDbFactory.Create(tenant);
        await TestDbFactory.SeedTwoStoresAsync(db);

        var users = new FakeUserDirectory();
        users.AddExisting("owner@store.local", "Owner", ownerId);
        users.AddExisting("manager@store.local", "Manager", managerId);

        db.StoreMembers.AddRange(
            new StoreMember { StoreId = 1, UserId = ownerId, Role = StoreRole.Owner },
            new StoreMember { StoreId = 1, UserId = managerId, Role = StoreRole.Manager });
        await db.SaveChangesAsync();

        var managerUser = new TestCurrentUser(managerId, isAuthenticated: true, AppRoles.Customer);
        var managerAccess = new StoreAccessService(db, tenant, managerUser);
        var memberService = new StoreMemberService(db, tenant, managerAccess, users);

        var addOwnerAttempt = await memberService.AddAsync(new AddStoreMemberRequest(
            Email: "newowner@store.local",
            Role: StoreRole.Owner,
            FullName: "New Owner",
            Password: "Password123"));
        Assert.True(addOwnerAttempt.IsForbidden);

        var addStaffAttempt = await memberService.AddAsync(new AddStoreMemberRequest(
            Email: "staff@store.local",
            Role: StoreRole.Staff,
            FullName: "Staff User",
            Password: "Password123"));
        Assert.NotNull(addStaffAttempt.Member);
        Assert.Equal(StoreRole.Staff, addStaffAttempt.Member.Role);

        var removeOwnerByManager = await memberService.RemoveAsync(ownerId);
        Assert.True(removeOwnerByManager.IsForbidden);

        var ownerUser = new TestCurrentUser(ownerId, isAuthenticated: true, AppRoles.Customer);
        var ownerAccess = new StoreAccessService(db, tenant, ownerUser);
        var ownerMemberService = new StoreMemberService(db, tenant, ownerAccess, users);

        var removeLastOwner = await ownerMemberService.RemoveAsync(ownerId);
        Assert.False(removeLastOwner.Succeeded);
        Assert.Contains("at least one Owner", removeLastOwner.Error);
    }
}
