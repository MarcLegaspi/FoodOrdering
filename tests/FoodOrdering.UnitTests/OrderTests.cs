using FoodOrdering.Application.Catalog;
using FoodOrdering.Application.Orders;
using FoodOrdering.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FoodOrdering.UnitTests;

public sealed class OrderTests
{
    [Fact]
    public async Task PlaceOrder_Computes_Server_Total_Merges_Duplicates_And_Snapshots_Price()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = new TestTenantContext(storeId: 1);
        var customerId = Guid.NewGuid();
        Guid burgerPublicId;

        await using (var setupDb = TestDbFactory.Create(tenant, dbName))
        {
            await TestDbFactory.SeedTwoStoresAsync(setupDb);

            var category = (await new CategoryService(setupDb, tenant)
                .CreateAsync(new CreateCategoryRequest("Burgers", 1))).Category!;

            var menuItem = (await new MenuItemService(setupDb, tenant)
                .CreateAsync(new CreateMenuItemRequest(
                    category.PublicId, "Classic Burger", "100% beef", 10.00m))).Item!;

            burgerPublicId = menuItem.PublicId;
        }

        await using (var orderDb = TestDbFactory.Create(tenant, dbName))
        {
            var customer = new TestCurrentUser(customerId, isAuthenticated: true, "Customer");
            var orderService = new OrderService(orderDb, tenant, customer);

            var placed = await orderService.PlaceOrderAsync(new PlaceOrderRequest(
                [
                    new OrderItemInput(burgerPublicId, 2),
                    new OrderItemInput(burgerPublicId, 1)
                ],
                OrderType.Pickup,
                CustomerPhone: "+1-555-0199",
                Notes: "No onions"));

            Assert.NotNull(placed.Order);
            Assert.Equal(OrderStatus.Pending, placed.Order.Status);
            Assert.Equal(30.00m, placed.Order.TotalAmount);
            Assert.Single(placed.Order.Items);
            Assert.Equal(3, placed.Order.Items[0].Quantity);

            // Change MenuItem price afterwards — existing order receipt must keep $10.00 snapshot
            var trackedItem = await orderDb.MenuItems.SingleAsync(m => m.PublicId == burgerPublicId);
            trackedItem.Price = 15.00m;
            await orderDb.SaveChangesAsync();
        }

        await using var readDb = TestDbFactory.Create(tenant, dbName);
        var orders = await new OrderService(
            readDb,
            tenant,
            new TestCurrentUser(customerId, isAuthenticated: true, "Customer")).GetMyOrdersAsync();

        Assert.Single(orders);
        Assert.Equal(10.00m, orders[0].Items[0].UnitPrice);
        Assert.Equal(30.00m, orders[0].TotalAmount);
    }

    [Fact]
    public async Task PlaceOrder_Rejects_Unavailable_Or_Other_Store_Items()
    {
        var tenant = new TestTenantContext();
        await using var db = TestDbFactory.Create(tenant);
        await TestDbFactory.SeedTwoStoresAsync(db);

        tenant.StoreId = 1;
        var cat1 = (await new CategoryService(db, tenant)
            .CreateAsync(new CreateCategoryRequest("Burgers", 1))).Category!;
        var unavailableItem = (await new MenuItemService(db, tenant)
            .CreateAsync(new CreateMenuItemRequest(
                cat1.PublicId, "Sold Out Burger", "Out", 12.00m, IsAvailable: false))).Item!;

        tenant.StoreId = 2;
        var cat2 = (await new CategoryService(db, tenant)
            .CreateAsync(new CreateCategoryRequest("Pizzas", 1))).Category!;
        var store2Item = (await new MenuItemService(db, tenant)
            .CreateAsync(new CreateMenuItemRequest(
                cat2.PublicId, "Pepperoni Pizza", "Hot", 14.00m))).Item!;

        var customer = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Customer");
        tenant.StoreId = 1;
        var orderService = new OrderService(db, tenant, customer);

        var unavailableResult = await orderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(unavailableItem.PublicId, 1)],
            OrderType.Pickup,
            CustomerPhone: "+1-555-0199"));
        Assert.Contains("unavailable", unavailableResult.Error, StringComparison.OrdinalIgnoreCase);

        var crossTenantResult = await orderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(store2Item.PublicId, 1)],
            OrderType.Pickup,
            CustomerPhone: "+1-555-0199"));
        Assert.True(crossTenantResult.IsNotFound);
    }

    [Fact]
    public async Task Customer_Cancel_And_Staff_Status_Transitions_Are_Enforced()
    {
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        await TestDbFactory.SeedTwoStoresAsync(db);

        var category = (await new CategoryService(db, tenant)
            .CreateAsync(new CreateCategoryRequest("Tacos", 1))).Category!;
        var taco = (await new MenuItemService(db, tenant)
            .CreateAsync(new CreateMenuItemRequest(
                category.PublicId, "Al Pastor", "Pork & pineapple", 4.50m))).Item!;

        var customer1Id = Guid.NewGuid();
        var customer2Id = Guid.NewGuid();

        var customer1Service = new OrderService(
            db, tenant, new TestCurrentUser(customer1Id, isAuthenticated: true, "Customer"));
        var customer2Service = new OrderService(
            db, tenant, new TestCurrentUser(customer2Id, isAuthenticated: true, "Customer"));

        var order1 = (await customer1Service.PlaceOrderAsync(
            new PlaceOrderRequest(
                [new OrderItemInput(taco.PublicId, 2)],
                OrderType.Pickup,
                CustomerPhone: "+1-555-0199"))).Order!;

        // Other customer cannot cancel customer1's order
        var hijackAttempt = await customer2Service.CancelMyOrderAsync(order1.PublicId);
        Assert.True(hijackAttempt.IsForbidden);

        // Staff advances Pending -> Confirmed -> Preparing
        var staffService = new OrderService(
            db, tenant, new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Customer"));
        var confirmed = await staffService.UpdateStatusAsync(order1.PublicId, OrderStatus.Confirmed);
        Assert.Equal(OrderStatus.Confirmed, confirmed.Order!.Status);

        // Customer cannot cancel once Confirmed
        var lateCancel = await customer1Service.CancelMyOrderAsync(order1.PublicId);
        Assert.NotNull(lateCancel.Error);

        // Cannot move backwards Preparing -> Confirmed
        await staffService.UpdateStatusAsync(order1.PublicId, OrderStatus.Preparing);
        var backwards = await staffService.UpdateStatusAsync(order1.PublicId, OrderStatus.Confirmed);
        Assert.NotNull(backwards.Error);

        // Store 2 cannot see Store 1's order
        tenant.StoreId = 2;
        var store2Orders = await new OrderService(db, tenant, new TestCurrentUser()).GetStoreOrdersAsync();
        Assert.Empty(store2Orders);
    }

    [Fact]
    public async Task PlaceOrder_Validates_Fulfillment_Details()
    {
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        await TestDbFactory.SeedTwoStoresAsync(db);

        var category = (await new CategoryService(db, tenant)
            .CreateAsync(new CreateCategoryRequest("Burgers", 1))).Category!;
        var burger = (await new MenuItemService(db, tenant)
            .CreateAsync(new CreateMenuItemRequest(
                category.PublicId, "Cheeseburger", "Classic", 9.99m))).Item!;

        var customer = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Customer");
        var orderService = new OrderService(db, tenant, customer);

        var missingPhone = await orderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(burger.PublicId, 1)],
            OrderType.Pickup,
            CustomerPhone: ""));
        Assert.Contains("phone", missingPhone.Error, StringComparison.OrdinalIgnoreCase);

        var missingAddress = await orderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(burger.PublicId, 1)],
            OrderType.Delivery,
            CustomerPhone: "09171234567",
            DeliveryAddress: "  "));
        Assert.Contains("address", missingAddress.Error, StringComparison.OrdinalIgnoreCase);

        var pickupClearsAddress = await orderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(burger.PublicId, 1)],
            OrderType.Pickup,
            CustomerPhone: "09171234567",
            DeliveryAddress: "Should be ignored"));
        Assert.NotNull(pickupClearsAddress.Order);
        Assert.Equal(OrderType.Pickup, pickupClearsAddress.Order.OrderType);
        Assert.Null(pickupClearsAddress.Order.DeliveryAddress);

        var delivery = await orderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(burger.PublicId, 1)],
            OrderType.Delivery,
            CustomerPhone: "09171234567",
            DeliveryAddress: "123 Main St, Makati"));
        Assert.NotNull(delivery.Order);
        Assert.Equal(OrderType.Delivery, delivery.Order.OrderType);
        Assert.Equal("09171234567", delivery.Order.CustomerPhone);
        Assert.Equal("123 Main St, Makati", delivery.Order.DeliveryAddress);
    }

    [Fact]
    public async Task PlaceOrder_Rejects_When_Store_Is_Not_Accepting_Orders()
    {
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        var (first, _) = await TestDbFactory.SeedTwoStoresAsync(db);
        first.IsAcceptingOrders = false;
        await db.SaveChangesAsync();

        var category = (await new CategoryService(db, tenant)
            .CreateAsync(new CreateCategoryRequest("Burgers", 1))).Category!;
        var burger = (await new MenuItemService(db, tenant)
            .CreateAsync(new CreateMenuItemRequest(
                category.PublicId, "Cheeseburger", "Classic", 9.99m))).Item!;

        var customer = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Customer");
        var result = await new OrderService(db, tenant, customer).PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(burger.PublicId, 1)],
            OrderType.Pickup,
            CustomerPhone: "09171234567"));

        Assert.Null(result.Order);
        Assert.Contains("closed", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PlaceOrder_Allows_Guest_When_CustomerName_Provided_And_Sets_CustomerType_Guest()
    {
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        await TestDbFactory.SeedTwoStoresAsync(db);

        var category = (await new CategoryService(db, tenant)
            .CreateAsync(new CreateCategoryRequest("Burgers", 1))).Category!;
        var burger = (await new MenuItemService(db, tenant)
            .CreateAsync(new CreateMenuItemRequest(
                category.PublicId, "Cheeseburger", "Classic", 10.00m))).Item!;

        // Unauthenticated guest user
        var guestUser = new TestCurrentUser(userId: null, isAuthenticated: false);
        var orderService = new OrderService(db, tenant, guestUser);

        // Missing guest customer name rejected
        var rejected = await orderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(burger.PublicId, 1)],
            OrderType.Pickup,
            CustomerPhone: "09170001122",
            CustomerName: ""));
        Assert.Null(rejected.Order);
        Assert.Contains("name is required", rejected.Error, StringComparison.OrdinalIgnoreCase);

        // Guest with name succeeds
        var placed = await orderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(burger.PublicId, 2)],
            OrderType.Pickup,
            CustomerPhone: "09170001122",
            CustomerName: "Alex Guest",
            CustomerEmail: "alex@example.com"));

        Assert.NotNull(placed.Order);
        Assert.Equal(CustomerType.Guest, placed.Order.CustomerType);
        Assert.Equal("Alex Guest", placed.Order.CustomerName);
        Assert.Equal("alex@example.com", placed.Order.CustomerEmail);
        Assert.Equal(20.00m, placed.Order.TotalAmount);
        Assert.Null(placed.CustomerUserId);

        // Lookup by public ID succeeds without auth
        var lookup = await orderService.GetOrderByPublicIdAsync(placed.Order.PublicId);
        Assert.NotNull(lookup.Order);
        Assert.Equal(placed.Order.OrderNumber, lookup.Order.OrderNumber);
        Assert.Equal("Alex Guest", lookup.Order.CustomerName);
    }

    [Fact]
    public async Task PlaceOrder_Enforces_MinimumDeliveryOrder_And_Applies_DeliveryFee_Or_FreeDeliveryThreshold()
    {
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        var (store, _) = await TestDbFactory.SeedTwoStoresAsync(db);

        // Configure store delivery rules: Min $15, Delivery Fee $3, Free over $30
        store.MinimumDeliveryOrder = 15.00m;
        store.DeliveryFee = 3.00m;
        store.FreeDeliveryThreshold = 30.00m;
        store.EstimatedDeliveryTime = "25-35 mins";
        await db.SaveChangesAsync();

        var category = (await new CategoryService(db, tenant)
            .CreateAsync(new CreateCategoryRequest("Pizzas", 1))).Category!;
        var pizza = (await new MenuItemService(db, tenant)
            .CreateAsync(new CreateMenuItemRequest(
                category.PublicId, "Pepperoni Pizza", "Delicious", 12.00m))).Item!;

        var customer = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Customer");
        var orderService = new OrderService(db, tenant, customer);

        // 1. Order subtotal is $12 (< $15 min): must be rejected with informative error
        var belowMin = await orderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(pizza.PublicId, 1)],
            OrderType.Delivery,
            CustomerPhone: "09170001122",
            DeliveryAddress: "123 Elm St",
            DeliveryLandmark: "Gate 4, buzzer #12"));

        Assert.Null(belowMin.Order);
        Assert.Contains("Minimum order for delivery is", belowMin.Error);

        // 2. Order subtotal is $24 (>= $15 min, < $30 free threshold): charges $3 delivery fee
        var withDeliveryFee = await orderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(pizza.PublicId, 2)],
            OrderType.Delivery,
            CustomerPhone: "09170001122",
            DeliveryAddress: "123 Elm St",
            DeliveryLandmark: "Gate 4, buzzer #12"));

        Assert.NotNull(withDeliveryFee.Order);
        Assert.Equal(24.00m, withDeliveryFee.Order.Subtotal);
        Assert.Equal(3.00m, withDeliveryFee.Order.DeliveryFee);
        Assert.Equal(27.00m, withDeliveryFee.Order.TotalAmount);
        Assert.Equal("123 Elm St", withDeliveryFee.Order.DeliveryAddress);
        Assert.Equal("Gate 4, buzzer #12", withDeliveryFee.Order.DeliveryLandmark);
        Assert.Equal("25-35 mins", withDeliveryFee.Order.EstimatedDeliveryTime);

        // 3. Order subtotal is $36 (>= $30 free threshold): delivery fee is waived ($0.00)
        var freeDelivery = await orderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(pizza.PublicId, 3)],
            OrderType.Delivery,
            CustomerPhone: "09170001122",
            DeliveryAddress: "123 Elm St"));

        Assert.NotNull(freeDelivery.Order);
        Assert.Equal(36.00m, freeDelivery.Order.Subtotal);
        Assert.Equal(0.00m, freeDelivery.Order.DeliveryFee);
        Assert.Equal(36.00m, freeDelivery.Order.TotalAmount);

        // 4. Pickup order never incurs delivery fee even if below delivery minimum
        var pickup = await orderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(pizza.PublicId, 1)],
            OrderType.Pickup,
            CustomerPhone: "09170001122"));

        Assert.NotNull(pickup.Order);
        Assert.Equal(12.00m, pickup.Order.Subtotal);
        Assert.Equal(0.00m, pickup.Order.DeliveryFee);
        Assert.Equal(12.00m, pickup.Order.TotalAmount);
    }
}
