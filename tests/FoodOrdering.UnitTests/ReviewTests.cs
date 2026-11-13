using FoodOrdering.Application.Catalog;
using FoodOrdering.Application.Orders;
using FoodOrdering.Application.Reviews;
using FoodOrdering.Domain.Orders;
using Xunit;

namespace FoodOrdering.UnitTests;

public sealed class ReviewTests
{
    [Fact]
    public async Task CreateOrderReview_Requires_Completed_Status()
    {
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        var (store, _) = await TestDbFactory.SeedTwoStoresAsync(db);

        var user = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Customer");
        var orderService = new OrderService(db, tenant, user);
        var reviewService = new ReviewService(db, user);

        var category = (await new CategoryService(db, tenant)
            .CreateAsync(new CreateCategoryRequest("Food", 1))).Category!;
        var item = (await new MenuItemService(db, tenant)
            .CreateAsync(new CreateMenuItemRequest(category.PublicId, "Tacos", "Yum", 10m))).Item!;

        // Order placed (Status: Pending)
        var order = (await orderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(item.PublicId, 1)],
            OrderType.Pickup,
            CustomerPhone: "09170000000"))).Order!;

        var reviewResult = await reviewService.CreateOrderReviewAsync(
            order.PublicId,
            new CreateReviewRequest(5, "Loved it!"));

        Assert.NotNull(reviewResult.Error);
        Assert.Contains("Only completed orders can be reviewed", reviewResult.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateOrderReview_Requires_Order_Owner()
    {
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        var (store, _) = await TestDbFactory.SeedTwoStoresAsync(db);

        var userA = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Customer");
        var userB = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Customer");

        var orderServiceA = new OrderService(db, tenant, userA);
        var reviewServiceB = new ReviewService(db, userB);

        var category = (await new CategoryService(db, tenant)
            .CreateAsync(new CreateCategoryRequest("Food", 1))).Category!;
        var item = (await new MenuItemService(db, tenant)
            .CreateAsync(new CreateMenuItemRequest(category.PublicId, "Tacos", "Yum", 10m))).Item!;

        var order = (await orderServiceA.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(item.PublicId, 1)],
            OrderType.Pickup,
            CustomerPhone: "09170000000"))).Order!;

        // Staff completes order
        var staffUser = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Staff");
        var staffOrderService = new OrderService(db, tenant, staffUser);
        await staffOrderService.UpdateStatusAsync(order.PublicId, OrderStatus.Confirmed);
        await staffOrderService.UpdateStatusAsync(order.PublicId, OrderStatus.Preparing);
        await staffOrderService.UpdateStatusAsync(order.PublicId, OrderStatus.Ready);
        await staffOrderService.UpdateStatusAsync(order.PublicId, OrderStatus.Completed);

        // User B tries to review User A's order -> Forbidden
        var reviewResult = await reviewServiceB.CreateOrderReviewAsync(
            order.PublicId,
            new CreateReviewRequest(5, "Hacked review"));

        Assert.True(reviewResult.IsForbidden);
    }

    [Fact]
    public async Task CreateOrderReview_Validates_Rating_Range()
    {
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        await TestDbFactory.SeedTwoStoresAsync(db);

        var user = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Customer");
        var reviewService = new ReviewService(db, user);

        var resultUnder = await reviewService.CreateOrderReviewAsync(Guid.NewGuid(), new CreateReviewRequest(0, null));
        Assert.Contains("between 1 and 5", resultUnder.Error, StringComparison.OrdinalIgnoreCase);

        var resultOver = await reviewService.CreateOrderReviewAsync(Guid.NewGuid(), new CreateReviewRequest(6, null));
        Assert.Contains("between 1 and 5", resultOver.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateOrderReview_Calculates_Store_Average_And_Rejects_Duplicates()
    {
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        var (store, _) = await TestDbFactory.SeedTwoStoresAsync(db);

        var userA = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Customer");
        var userB = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Customer");

        var orderServiceA = new OrderService(db, tenant, userA);
        var orderServiceB = new OrderService(db, tenant, userB);

        var reviewServiceA = new ReviewService(db, userA);
        var reviewServiceB = new ReviewService(db, userB);

        var category = (await new CategoryService(db, tenant)
            .CreateAsync(new CreateCategoryRequest("Food", 1))).Category!;
        var item = (await new MenuItemService(db, tenant)
            .CreateAsync(new CreateMenuItemRequest(category.PublicId, "Tacos", "Yum", 10m))).Item!;

        // Place and complete Order 1 for User A
        var order1 = (await orderServiceA.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(item.PublicId, 1)],
            OrderType.Pickup,
            CustomerPhone: "09170000000",
            CustomerName: "Alice"))).Order!;

        var staffUser = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Staff");
        var staffOrderService = new OrderService(db, tenant, staffUser);
        await staffOrderService.UpdateStatusAsync(order1.PublicId, OrderStatus.Confirmed);
        await staffOrderService.UpdateStatusAsync(order1.PublicId, OrderStatus.Preparing);
        await staffOrderService.UpdateStatusAsync(order1.PublicId, OrderStatus.Ready);
        await staffOrderService.UpdateStatusAsync(order1.PublicId, OrderStatus.Completed);

        // Place and complete Order 2 for User B
        var order2 = (await orderServiceB.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(item.PublicId, 1)],
            OrderType.Pickup,
            CustomerPhone: "09170000001",
            CustomerName: "Bob"))).Order!;

        await staffOrderService.UpdateStatusAsync(order2.PublicId, OrderStatus.Confirmed);
        await staffOrderService.UpdateStatusAsync(order2.PublicId, OrderStatus.Preparing);
        await staffOrderService.UpdateStatusAsync(order2.PublicId, OrderStatus.Ready);
        await staffOrderService.UpdateStatusAsync(order2.PublicId, OrderStatus.Completed);

        // 1. User A reviews Order 1 with 5 stars
        var rev1 = await reviewServiceA.CreateOrderReviewAsync(
            order1.PublicId,
            new CreateReviewRequest(5, "Super delicious!"));

        Assert.NotNull(rev1.Review);
        Assert.Equal(5, rev1.Review.Rating);
        Assert.Equal("Super delicious!", rev1.Review.Comment);
        Assert.Equal("Alice", rev1.Review.CustomerName);

        // Verify store stats: 1 review, 5.0 avg
        Assert.Equal(1, store.ReviewCount);
        Assert.Equal(5.0, store.AverageRating);

        // 2. User A tries to review Order 1 again -> Conflict
        var dup = await reviewServiceA.CreateOrderReviewAsync(
            order1.PublicId,
            new CreateReviewRequest(4, "Second thought"));
        Assert.True(dup.IsConflict);

        // 3. User B reviews Order 2 with 4 stars
        var rev2 = await reviewServiceB.CreateOrderReviewAsync(
            order2.PublicId,
            new CreateReviewRequest(4, "Good but a bit cold"));

        Assert.NotNull(rev2.Review);

        // Verify store stats: 2 reviews, (5 + 4) / 2 = 4.5 avg
        Assert.Equal(2, store.ReviewCount);
        Assert.Equal(4.5, store.AverageRating);

        // 4. Public store review summary returns both reviews
        var summary = await reviewServiceA.GetStoreReviewsAsync(store.Slug);
        Assert.NotNull(summary);
        Assert.Equal(4.5, summary.AverageRating);
        Assert.Equal(2, summary.ReviewCount);
        Assert.Equal(2, summary.RecentReviews.Count);
    }
}
