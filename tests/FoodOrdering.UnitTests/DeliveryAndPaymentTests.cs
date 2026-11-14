using FoodOrdering.Application.Catalog;
using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Application.Orders;
using FoodOrdering.Domain.Orders;
using Xunit;

namespace FoodOrdering.UnitTests;

public sealed class DeliveryAndPaymentTests
{
    [Fact]
    public async Task PlaceOrder_PayOnDelivery_Sets_Pending_And_CashChange()
    {
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        var (store, _) = await TestDbFactory.SeedTwoStoresAsync(db);

        var user = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Customer");
        var orderService = new OrderService(db, tenant, user);

        var category = (await new CategoryService(db, tenant)
            .CreateAsync(new CreateCategoryRequest("Food", 1))).Category!;
        var item = (await new MenuItemService(db, tenant)
            .CreateAsync(new CreateMenuItemRequest(category.PublicId, "Burger", "Yum", 15m))).Item!;

        var result = await orderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(item.PublicId, 1)],
            OrderType.Delivery,
            CustomerPhone: "09171112222",
            DeliveryAddress: "123 Main St",
            PaymentMethod: PaymentMethod.PayOnDelivery,
            CashChangeFor: 100m));

        Assert.NotNull(result.Order);
        Assert.Equal(PaymentMethod.PayOnDelivery, result.Order.PaymentMethod);
        Assert.Equal(PaymentStatus.Pending, result.Order.PaymentStatus);
        Assert.Equal(100m, result.Order.CashChangeFor);
        Assert.Null(result.Order.PaidAtUtc);
    }

    [Fact]
    public async Task PlaceOrder_GCashOnline_Sets_Paid_Status_And_Reference()
    {
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        var (store, _) = await TestDbFactory.SeedTwoStoresAsync(db);

        var user = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Customer");
        var orderService = new OrderService(db, tenant, user);

        var category = (await new CategoryService(db, tenant)
            .CreateAsync(new CreateCategoryRequest("Food", 1))).Category!;
        var item = (await new MenuItemService(db, tenant)
            .CreateAsync(new CreateMenuItemRequest(category.PublicId, "Salad", "Healthy", 20m))).Item!;

        var result = await orderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(item.PublicId, 1)],
            OrderType.Pickup,
            CustomerPhone: "09171112222",
            PaymentMethod: PaymentMethod.GCashOnline));

        Assert.NotNull(result.Order);
        Assert.Equal(PaymentMethod.GCashOnline, result.Order.PaymentMethod);
        Assert.Equal(PaymentStatus.Paid, result.Order.PaymentStatus);
        Assert.NotNull(result.Order.PaidAtUtc);
        Assert.NotNull(result.Order.PaymentReference);
        Assert.StartsWith("GCASH-", result.Order.PaymentReference);
    }

    [Fact]
    public async Task PlaceOrder_GCashOnline_WithConfiguredGateway_Sets_Pending_And_Returns_CheckoutUrl()
    {
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        var (store, _) = await TestDbFactory.SeedTwoStoresAsync(db);

        var user = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Customer");
        var fakeGateway = new FakePaymentGatewayService(
            isConfigured: true,
            stubResult: new CreatePaymentInvoiceResult("inv_test_123", "https://checkout.xendit.co/web/inv_test_123"));

        var orderService = new OrderService(db, tenant, user, fakeGateway);

        var category = (await new CategoryService(db, tenant)
            .CreateAsync(new CreateCategoryRequest("Food", 1))).Category!;
        var item = (await new MenuItemService(db, tenant)
            .CreateAsync(new CreateMenuItemRequest(category.PublicId, "Salad", "Healthy", 20m))).Item!;

        var result = await orderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(item.PublicId, 1)],
            OrderType.Pickup,
            CustomerPhone: "09171112222",
            PaymentMethod: PaymentMethod.GCashOnline));

        Assert.NotNull(result.Order);
        Assert.Equal(PaymentMethod.GCashOnline, result.Order.PaymentMethod);
        Assert.Equal(PaymentStatus.Pending, result.Order.PaymentStatus);
        Assert.Null(result.Order.PaidAtUtc);
        Assert.Equal("inv_test_123", result.Order.PaymentReference);
        Assert.Equal("https://checkout.xendit.co/web/inv_test_123", result.Order.CheckoutUrl);
    }

    private sealed class FakePaymentGatewayService(bool isConfigured, CreatePaymentInvoiceResult? stubResult) : IPaymentGatewayService
    {
        public bool IsConfigured => isConfigured;

        public Task<CreatePaymentInvoiceResult?> CreateGcashInvoiceAsync(
            string orderNumber,
            decimal amount,
            string storeSlug,
            string storeName,
            Guid orderPublicId,
            string customerName,
            string customerPhone,
            string? customerEmail,
            CancellationToken ct = default)
        {
            return Task.FromResult(stubResult);
        }
    }

    [Fact]
    public async Task AssignDriver_ValidDeliveryOrder_Transitions_To_OutForDelivery()
    {
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        var (store, _) = await TestDbFactory.SeedTwoStoresAsync(db);

        var user = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Customer");
        var orderService = new OrderService(db, tenant, user);

        var category = (await new CategoryService(db, tenant)
            .CreateAsync(new CreateCategoryRequest("Food", 1))).Category!;
        var item = (await new MenuItemService(db, tenant)
            .CreateAsync(new CreateMenuItemRequest(category.PublicId, "Pizza", "Cheesy", 25m))).Item!;

        var order = (await orderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(item.PublicId, 1)],
            OrderType.Delivery,
            CustomerPhone: "09171112222",
            DeliveryAddress: "456 Elm St",
            PaymentMethod: PaymentMethod.PayOnDelivery))).Order!;

        await orderService.UpdateStatusAsync(order.PublicId, OrderStatus.Confirmed);
        await orderService.UpdateStatusAsync(order.PublicId, OrderStatus.Preparing);
        await orderService.UpdateStatusAsync(order.PublicId, OrderStatus.Ready);

        var assignResult = await orderService.AssignDriverAsync(
            order.PublicId,
            new AssignDriverRequest("Alex Rider", "0917-555-1234"));

        Assert.NotNull(assignResult.Order);
        Assert.Equal(OrderStatus.OutForDelivery, assignResult.Order.Status);
        Assert.Equal("Alex Rider", assignResult.Order.DriverName);
        Assert.Equal("0917-555-1234", assignResult.Order.DriverPhone);
        Assert.NotNull(assignResult.Order.DispatchedAtUtc);
    }

    [Fact]
    public async Task AssignDriver_PickupOrder_Returns_ValidationError()
    {
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        var (store, _) = await TestDbFactory.SeedTwoStoresAsync(db);

        var user = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Customer");
        var orderService = new OrderService(db, tenant, user);

        var category = (await new CategoryService(db, tenant)
            .CreateAsync(new CreateCategoryRequest("Food", 1))).Category!;
        var item = (await new MenuItemService(db, tenant)
            .CreateAsync(new CreateMenuItemRequest(category.PublicId, "Fries", "Crispy", 5m))).Item!;

        var order = (await orderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(item.PublicId, 1)],
            OrderType.Pickup,
            CustomerPhone: "09171112222",
            PaymentMethod: PaymentMethod.PayAtCounter))).Order!;

        await orderService.UpdateStatusAsync(order.PublicId, OrderStatus.Confirmed);
        await orderService.UpdateStatusAsync(order.PublicId, OrderStatus.Preparing);
        await orderService.UpdateStatusAsync(order.PublicId, OrderStatus.Ready);

        var assignResult = await orderService.AssignDriverAsync(
            order.PublicId,
            new AssignDriverRequest("Dave Rider", "0917-000-0000"));

        Assert.NotNull(assignResult.Error);
        Assert.Contains("Drivers can only be assigned to delivery orders", assignResult.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompleteDelivery_Records_PaymentCollectedVia_And_Marks_Paid()
    {
        var tenant = new TestTenantContext(storeId: 1);
        await using var db = TestDbFactory.Create(tenant);
        var (store, _) = await TestDbFactory.SeedTwoStoresAsync(db);

        var user = new TestCurrentUser(Guid.NewGuid(), isAuthenticated: true, "Customer");
        var orderService = new OrderService(db, tenant, user);

        var category = (await new CategoryService(db, tenant)
            .CreateAsync(new CreateCategoryRequest("Food", 1))).Category!;
        var item = (await new MenuItemService(db, tenant)
            .CreateAsync(new CreateMenuItemRequest(category.PublicId, "Ramen", "Warm", 18m))).Item!;

        var order = (await orderService.PlaceOrderAsync(new PlaceOrderRequest(
            [new OrderItemInput(item.PublicId, 1)],
            OrderType.Delivery,
            CustomerPhone: "09171112222",
            DeliveryAddress: "789 Pine Rd",
            PaymentMethod: PaymentMethod.PayOnDelivery))).Order!;

        await orderService.UpdateStatusAsync(order.PublicId, OrderStatus.Confirmed);
        await orderService.UpdateStatusAsync(order.PublicId, OrderStatus.Preparing);
        await orderService.AssignDriverAsync(order.PublicId, new AssignDriverRequest("Leo Rider", "0917-777-8888"));

        var invalidPinResult = await orderService.UpdateStatusAsync(
            order.PublicId,
            new UpdateOrderStatusRequest(OrderStatus.Completed, PaymentCollectionMethod.GCash, DeliveryPin: "0000"));
        Assert.NotNull(invalidPinResult.Error);
        Assert.Contains("Invalid delivery PIN", invalidPinResult.Error, StringComparison.OrdinalIgnoreCase);

        var completeResult = await orderService.UpdateStatusAsync(
            order.PublicId,
            new UpdateOrderStatusRequest(OrderStatus.Completed, PaymentCollectionMethod.GCash, DeliveryPin: order.DeliveryPin));

        Assert.NotNull(completeResult.Order);
        Assert.Equal(OrderStatus.Completed, completeResult.Order.Status);
        Assert.Equal(PaymentStatus.Paid, completeResult.Order.PaymentStatus);
        Assert.Equal(PaymentCollectionMethod.GCash, completeResult.Order.PaymentCollectedVia);
        Assert.NotNull(completeResult.Order.DeliveredAtUtc);
        Assert.NotNull(completeResult.Order.PaidAtUtc);
    }
}
