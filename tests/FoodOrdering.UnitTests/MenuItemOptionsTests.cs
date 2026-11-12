using FoodOrdering.Application.Catalog;
using FoodOrdering.Application.Orders;
using FoodOrdering.Domain.Orders;
using Xunit;

namespace FoodOrdering.UnitTests;

public sealed class MenuItemOptionsTests
{
    [Fact]
    public async Task CreateAndUpdateMenuItem_With_Options_And_PlaceOrder_Applies_Option_Pricing_And_Rules()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = new TestTenantContext(storeId: 1);
        var customerId = Guid.NewGuid();
        var user = new TestCurrentUser { UserId = customerId, IsAuthenticated = true };

        await using var db = TestDbFactory.Create(tenant, dbName);
        await TestDbFactory.SeedTwoStoresAsync(db);

        var category = (await new CategoryService(db, tenant)
            .CreateAsync(new CreateCategoryRequest("Burgers", 1))).Category!;

        var menuService = new MenuItemService(db, tenant);
        var created = await menuService.CreateAsync(new CreateMenuItemRequest(
            category.PublicId,
            "Build-Your-Own Burger",
            "Juicy patty",
            8.00m,
            OptionGroups:
            [
                new SaveMenuItemOptionGroupRequest(
                    "Size",
                    IsRequired: true,
                    AllowMultiple: false,
                    SortOrder: 1,
                    Options:
                    [
                        new SaveMenuItemOptionRequest("Regular", 0.00m, 1),
                        new SaveMenuItemOptionRequest("Double", 3.00m, 2)
                    ]),
                new SaveMenuItemOptionGroupRequest(
                    "Extras",
                    IsRequired: false,
                    AllowMultiple: true,
                    SortOrder: 2,
                    Options:
                    [
                        new SaveMenuItemOptionRequest("Extra Cheese", 1.25m, 1),
                        new SaveMenuItemOptionRequest("Bacon", 1.75m, 2)
                    ])
            ]));

        Assert.Null(created.Error);
        var item = created.Item!;
        Assert.Equal(2, item.OptionGroups.Count);

        var sizeGroup = item.OptionGroups.Single(g => g.Name == "Size");
        var extrasGroup = item.OptionGroups.Single(g => g.Name == "Extras");
        var regularId = sizeGroup.Options.Single(o => o.Name == "Regular").PublicId;
        var doubleId = sizeGroup.Options.Single(o => o.Name == "Double").PublicId;
        var cheeseId = extrasGroup.Options.Single(o => o.Name == "Extra Cheese").PublicId;
        var baconId = extrasGroup.Options.Single(o => o.Name == "Bacon").PublicId;

        // Updating availability without passing OptionGroups preserves existing groups
        var toggled = await menuService.UpdateAsync(
            item.PublicId,
            new UpdateMenuItemRequest(item.CategoryPublicId, item.Name, item.Description, item.Price, IsAvailable: true));
        Assert.Equal(2, toggled.Item!.OptionGroups.Count);

        var orderService = new OrderService(db, tenant, user);

        // Missing required group should be rejected
        var missingRequired = await orderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(item.PublicId, 1, [cheeseId])],
            OrderType.Pickup,
            "+1-555-0100"));
        Assert.NotNull(missingRequired.Error);

        // Selecting 2 options from single-select group should be rejected
        var invalidMulti = await orderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(item.PublicId, 1, [regularId, doubleId])],
            OrderType.Pickup,
            "+1-555-0100"));
        Assert.NotNull(invalidMulti.Error);

        // Same item with two different option combinations stays as two separate lines;
        // identical option combinations merge quantity cleanly
        var placed = await orderService.PlaceOrderAsync(new PlaceOrderRequest(
            [
                new OrderItemInput(item.PublicId, 1, [doubleId, cheeseId, baconId]), // 8 + 3 + 1.25 + 1.75 = 14.00
                new OrderItemInput(item.PublicId, 1, [baconId, cheeseId, doubleId]), // merges -> qty 2 @ 14.00 = 28.00
                new OrderItemInput(item.PublicId, 1, [regularId])                    // qty 1 @ 8.00 = 8.00
            ],
            OrderType.Pickup,
            "+1-555-0100"));

        Assert.Null(placed.Error);
        var order = placed.Order!;
        Assert.Equal(36.00m, order.TotalAmount);
        Assert.Equal(2, order.Items.Count);

        var loadedOrder = (await orderService.GetStoreOrdersAsync()).Single();
        var loadedLoadedLine = loadedOrder.Items.Single(i => i.Quantity == 2);
        Assert.Equal(14.00m, loadedLoadedLine.UnitPrice);
        Assert.Equal(3, loadedLoadedLine.Options.Count);
        Assert.Contains(loadedLoadedLine.Options, o => o.GroupName == "Size" && o.OptionName == "Double" && o.PriceDelta == 3.00m);
        Assert.Contains(loadedLoadedLine.Options, o => o.GroupName == "Extras" && o.OptionName == "Extra Cheese");
        Assert.Contains(loadedLoadedLine.Options, o => o.GroupName == "Extras" && o.OptionName == "Bacon");
    }
}
