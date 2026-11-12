using FoodOrdering.Application.Catalog;
using FoodOrdering.Application.Orders;
using FoodOrdering.Application.Promos;
using FoodOrdering.Domain.Orders;
using FoodOrdering.Domain.Promos;
using Xunit;

namespace FoodOrdering.UnitTests;

public sealed class PromoTests
{
    [Fact]
    public async Task PromoService_Creates_And_Validates_Promos_For_Registered_Users_Only()
    {
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        await TestDbFactory.SeedTwoStoresAsync(db);

        var registeredUser = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Customer");
        var guestUser = new TestCurrentUser(userId: null, isAuthenticated: false);

        var promoServiceForUser = new PromoService(db, tenant, registeredUser);
        var promoServiceForGuest = new PromoService(db, tenant, guestUser);

        // 1. Create a 20% off promo with 2 max uses
        var createResult = await promoServiceForUser.CreatePromoAsync(new CreatePromoCodeRequest(
            "SAVE20",
            PromoDiscountType.Percentage,
            20m,
            MaxUses: 2));

        Assert.NotNull(createResult.Promo);
        Assert.Equal("SAVE20", createResult.Promo.Code);
        Assert.Equal(PromoDiscountType.Percentage, createResult.Promo.DiscountType);
        Assert.Equal(20m, createResult.Promo.DiscountValue);
        Assert.Equal(2, createResult.Promo.MaxUses);
        Assert.Equal(0, createResult.Promo.UsedCount);

        // 2. Guest user cannot validate promo code
        var guestVal = await promoServiceForGuest.ValidatePromoAsync("SAVE20", 50m);
        Assert.False(guestVal.IsValid);
        Assert.Contains("registered members", guestVal.Error, StringComparison.OrdinalIgnoreCase);

        // 3. Registered user validates promo code successfully (20% of $50 = $10)
        var userVal = await promoServiceForUser.ValidatePromoAsync("SAVE20", 50m);
        Assert.True(userVal.IsValid);
        Assert.Equal(10.00m, userVal.DiscountAmount);

        // 4. Duplicate promo code rejected
        var duplicate = await promoServiceForUser.CreatePromoAsync(new CreatePromoCodeRequest(
            "save20",
            PromoDiscountType.FixedAmount,
            5m));
        Assert.True(duplicate.IsConflict);
    }

    [Fact]
    public async Task PlaceOrder_With_PromoCode_Applies_Discount_And_Increments_Claim_Count()
    {
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        var (store, _) = await TestDbFactory.SeedTwoStoresAsync(db);
        store.DeliveryFee = 3.00m;
        await db.SaveChangesAsync();

        var category = (await new CategoryService(db, tenant)
            .CreateAsync(new CreateCategoryRequest("Meals", 1))).Category!;
        var item = (await new MenuItemService(db, tenant)
            .CreateAsync(new CreateMenuItemRequest(
                category.PublicId, "Burger Combo", "Juicy", 20.00m))).Item!;

        var registeredUser = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Customer");
        var guestUser = new TestCurrentUser(userId: null, isAuthenticated: false);

        var promoService = new PromoService(db, tenant, registeredUser);

        // Create promo: $5 cash off, max 1 use
        var created = await promoService.CreatePromoAsync(new CreatePromoCodeRequest(
            "LUNCH5",
            PromoDiscountType.FixedAmount,
            5.00m,
            MaxUses: 1));
        Assert.NotNull(created.Promo);

        var guestOrderService = new OrderService(db, tenant, guestUser);
        var userOrderService = new OrderService(db, tenant, registeredUser);

        // Guest attempting to use promo is rejected
        var guestAttempt = await guestOrderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(item.PublicId, 1)],
            OrderType.Pickup,
            CustomerPhone: "09170001122",
            CustomerName: "Guest User",
            PromoCode: "LUNCH5"));
        Assert.Null(guestAttempt.Order);
        Assert.Contains("registered members", guestAttempt.Error, StringComparison.OrdinalIgnoreCase);

        // Registered user applies promo (Subtotal $20, Discount $5, Pickup Delivery $0 -> Total $15)
        var userOrder = await userOrderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(item.PublicId, 1)],
            OrderType.Pickup,
            CustomerPhone: "09170001122",
            CustomerName: "John Member",
            PromoCode: "LUNCH5"));

        Assert.NotNull(userOrder.Order);
        Assert.Equal(20.00m, userOrder.Order.Subtotal);
        Assert.Equal("LUNCH5", userOrder.Order.PromoCode);
        Assert.Equal(5.00m, userOrder.Order.DiscountAmount);
        Assert.Equal(15.00m, userOrder.Order.TotalAmount);

        // Second attempt with same code by same user fails (already redeemed)
        var secondAttempt = await userOrderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(item.PublicId, 1)],
            OrderType.Pickup,
            CustomerPhone: "09170001122",
            CustomerName: "John Member",
            PromoCode: "LUNCH5"));

        Assert.Null(secondAttempt.Order);
        Assert.NotNull(secondAttempt.Error);
        Assert.True(
            secondAttempt.Error.Contains("already used", StringComparison.OrdinalIgnoreCase) ||
            secondAttempt.Error.Contains("maximum claim limit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PromoService_Enforces_One_Use_Per_Customer_And_Restores_On_Cancellation()
    {
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        var (store, _) = await TestDbFactory.SeedTwoStoresAsync(db);

        var category = (await new CategoryService(db, tenant)
            .CreateAsync(new CreateCategoryRequest("Meals", 1))).Category!;
        var item = (await new MenuItemService(db, tenant)
            .CreateAsync(new CreateMenuItemRequest(
                category.PublicId, "Burger Combo", "Juicy", 20.00m))).Item!;

        var userA = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Customer");
        var userB = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Customer");

        var promoServiceA = new PromoService(db, tenant, userA);
        var promoServiceB = new PromoService(db, tenant, userB);

        // Create promo with 10 claims allowed
        await promoServiceA.CreatePromoAsync(new CreatePromoCodeRequest(
            "VIP10",
            PromoDiscountType.Percentage,
            10.00m,
            MaxUses: 10));

        var orderServiceA = new OrderService(db, tenant, userA);
        var orderServiceB = new OrderService(db, tenant, userB);

        // 1. User A places an order with VIP10 -> SUCCESS
        var orderA1 = await orderServiceA.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(item.PublicId, 1)],
            OrderType.Pickup,
            CustomerPhone: "09170000001",
            PromoCode: "VIP10"));
        Assert.NotNull(orderA1.Order);

        // 2. User A tries to apply VIP10 in cart again -> REJECTED (Already used)
        var valA = await promoServiceA.ValidatePromoAsync("VIP10", 20m);
        Assert.False(valA.IsValid);
        Assert.Contains("already used", valA.Error, StringComparison.OrdinalIgnoreCase);

        // 3. User A tries to place a second order with VIP10 -> REJECTED
        var orderA2 = await orderServiceA.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(item.PublicId, 1)],
            OrderType.Pickup,
            CustomerPhone: "09170000001",
            PromoCode: "VIP10"));
        Assert.Null(orderA2.Order);
        Assert.Contains("already used", orderA2.Error, StringComparison.OrdinalIgnoreCase);

        // 4. User B can still use VIP10 (different customer) -> SUCCESS
        var valB = await promoServiceB.ValidatePromoAsync("VIP10", 20m);
        Assert.True(valB.IsValid);

        var orderB = await orderServiceB.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(item.PublicId, 1)],
            OrderType.Pickup,
            CustomerPhone: "09170000002",
            PromoCode: "VIP10"));
        Assert.NotNull(orderB.Order);

        // 5. User A cancels their first pending order -> Claim is restored!
        var cancelResult = await orderServiceA.CancelMyOrderAsync(orderA1.Order.PublicId);
        Assert.NotNull(cancelResult.Order);
        Assert.Equal(OrderStatus.Cancelled, cancelResult.Order.Status);

        // Now User A can use VIP10 again
        var valAAfterCancel = await promoServiceA.ValidatePromoAsync("VIP10", 20m);
        Assert.True(valAAfterCancel.IsValid);
    }

    [Fact]
    public async Task PromoService_Honors_StartsAtUtc_Effectivity_Window()
    {
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        await TestDbFactory.SeedTwoStoresAsync(db);

        var registeredUser = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Customer");
        var promoService = new PromoService(db, tenant, registeredUser);

        // 1. StartsAt later than ExpiresAt is rejected
        var invalidDates = await promoService.CreatePromoAsync(new CreatePromoCodeRequest(
            "FUTUREINVALID",
            PromoDiscountType.FixedAmount,
            5.00m,
            StartsAtUtc: DateTime.UtcNow.AddDays(2),
            ExpiresAtUtc: DateTime.UtcNow.AddDays(1)));
        Assert.NotNull(invalidDates.Error);
        Assert.Contains("start date cannot be later than expiration date", invalidDates.Error, StringComparison.OrdinalIgnoreCase);

        // 2. Future promo (starts tomorrow)
        var futurePromo = await promoService.CreatePromoAsync(new CreatePromoCodeRequest(
            "TOMORROW10",
            PromoDiscountType.Percentage,
            10.00m,
            StartsAtUtc: DateTime.UtcNow.AddDays(1),
            ExpiresAtUtc: DateTime.UtcNow.AddDays(7)));
        Assert.NotNull(futurePromo.Promo);
        Assert.NotNull(futurePromo.Promo.StartsAtUtc);

        // Validation today fails with "not active yet"
        var futureVal = await promoService.ValidatePromoAsync("TOMORROW10", 100m);
        Assert.False(futureVal.IsValid);
        Assert.Contains("not active yet", futureVal.Error, StringComparison.OrdinalIgnoreCase);

        // 3. Active promo (started yesterday, expires next week)
        var activePromo = await promoService.CreatePromoAsync(new CreatePromoCodeRequest(
            "ACTIVE10",
            PromoDiscountType.Percentage,
            10.00m,
            StartsAtUtc: DateTime.UtcNow.AddDays(-1),
            ExpiresAtUtc: DateTime.UtcNow.AddDays(7)));
        Assert.NotNull(activePromo.Promo);

        var activeVal = await promoService.ValidatePromoAsync("ACTIVE10", 100m);
        Assert.True(activeVal.IsValid);
        Assert.Equal(10.00m, activeVal.DiscountAmount);
    }
}
