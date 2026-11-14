using FoodOrdering.Api.Filters;
using FoodOrdering.Api.Hubs;
using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Application.Orders;
using FoodOrdering.Domain.Orders;
using FoodOrdering.Domain.Stores;
using Microsoft.AspNetCore.SignalR;

namespace FoodOrdering.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders");

        group.MapPost("/", async (
            PlaceOrderRequest request,
            ITenantContext tenantContext,
            OrderService orderService,
            IHubContext<OrderHub> hubContext,
            CancellationToken ct) =>
        {
            if (tenantContext.StoreId is null)
            {
                return Results.BadRequest(new { error = "Missing X-Store-Slug header." });
            }

            var result = await orderService.PlaceOrderAsync(request, ct);
            if (result.Order is not null)
            {
                await BroadcastOrderEventAsync(hubContext, "OrderPlaced", result, ct);
            }

            return ToActionResult(result, createdRoute: true);
        })
        .AllowAnonymous();

        group.MapGet("/{publicId:guid}", async (
            Guid publicId,
            OrderService orderService,
            CancellationToken ct) =>
        {
            var result = await orderService.GetOrderByPublicIdAsync(publicId, ct);
            return ToActionResult(result, createdRoute: false);
        })
        .AllowAnonymous();

        group.MapGet("/my", async (OrderService orderService, CancellationToken ct) =>
        {
            var orders = await orderService.GetMyOrdersAsync(ct);
            return Results.Ok(orders);
        })
        .RequireAuthorization();

        group.MapPost("/{publicId:guid}/cancel", async (
            Guid publicId,
            OrderService orderService,
            IHubContext<OrderHub> hubContext,
            CancellationToken ct) =>
        {
            var result = await orderService.CancelMyOrderAsync(publicId, ct);
            if (result.Order is not null)
            {
                await BroadcastOrderEventAsync(hubContext, "OrderUpdated", result, ct);
            }

            return ToActionResult(result, createdRoute: false);
        })
        .RequireAuthorization();

        group.MapGet("/", async (
            OrderStatus? status,
            OrderService orderService,
            CancellationToken ct) =>
        {
            var orders = await orderService.GetStoreOrdersAsync(status, ct);
            return Results.Ok(orders);
        })
        .RequireStoreRole(StoreRole.Staff);

        // PATCH /api/orders/{publicId}/status
        // Advances order status (e.g. Preparing -> Ready -> Completed). When completing delivery orders,
        // validates the customer's 4-digit Delivery PIN and payment settlement.
        group.MapPatch("/{publicId:guid}/status", async (
            Guid publicId,
            UpdateOrderStatusRequest request,
            OrderService orderService,
            IHubContext<OrderHub> hubContext,
            CancellationToken ct) =>
        {
            var result = await orderService.UpdateStatusAsync(publicId, request, ct);
            if (result.Order is not null)
            {
                // Push real-time update to store kitchen screen and customer tracking screen
                await BroadcastOrderEventAsync(hubContext, "OrderUpdated", result, ct);
            }

            return ToActionResult(result, createdRoute: false);
        })
        .RequireStoreRole(StoreRole.Staff);

        // POST /api/orders/{publicId}/assign-driver
        // Assigns rider name and phone to a delivery order, records DispatchedAtUtc, transitions
        // status to OutForDelivery, and notifies customer tracking screen via SignalR.
        group.MapPost("/{publicId:guid}/assign-driver", async (
            Guid publicId,
            AssignDriverRequest request,
            OrderService orderService,
            IHubContext<OrderHub> hubContext,
            CancellationToken ct) =>
        {
            var result = await orderService.AssignDriverAsync(publicId, request, ct);
            if (result.Order is not null)
            {
                await BroadcastOrderEventAsync(hubContext, "OrderUpdated", result, ct);
            }

            return ToActionResult(result, createdRoute: false);
        })
        .RequireStoreRole(StoreRole.Staff);

        // POST /api/orders/{publicId}/verify-payment
        // Allows staff to verify and approve manual GCash payments (matching the customer's last 4 digits).
        group.MapPost("/{publicId:guid}/verify-payment", async (
            Guid publicId,
            OrderService orderService,
            IHubContext<OrderHub> hubContext,
            CancellationToken ct) =>
        {
            var result = await orderService.VerifyPaymentAsync(publicId, ct);
            if (result.Order is not null)
            {
                await BroadcastOrderEventAsync(hubContext, "OrderUpdated", result, ct);
            }

            return ToActionResult(result, createdRoute: false);
        })
        .RequireStoreRole(StoreRole.Staff);

        return app;
    }

    private static async Task BroadcastOrderEventAsync(
        IHubContext<OrderHub> hubContext,
        string storeEvent,
        OrderResult result,
        CancellationToken ct)
    {
        if (result.Order is null)
        {
            return;
        }

        await hubContext.Clients
            .Group(OrderHub.StoreGroup(result.Order.StoreSlug))
            .SendAsync(storeEvent, result.Order, ct);

        await hubContext.Clients
            .Group(OrderHub.OrderGroup(result.Order.PublicId))
            .SendAsync("OrderUpdated", result.Order, ct);

        if (result.CustomerUserId is Guid customerUserId)
        {
            await hubContext.Clients
                .Group(OrderHub.UserGroup(customerUserId))
                .SendAsync("OrderUpdated", result.Order, ct);
        }
    }

    private static IResult ToActionResult(OrderResult result, bool createdRoute)
    {
        if (result.IsForbidden)
        {
            return Results.Forbid();
        }

        if (result.IsNotFound)
        {
            return Results.NotFound(new { error = result.Error });
        }

        if (result.Error is not null || result.Order is null)
        {
            return Results.BadRequest(new { error = result.Error });
        }

        return createdRoute
            ? Results.Created($"/api/orders/{result.Order.PublicId}", result.Order)
            : Results.Ok(result.Order);
    }
}
