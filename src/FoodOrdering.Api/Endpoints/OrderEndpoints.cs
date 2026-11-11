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
        .RequireAuthorization();

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

        group.MapPatch("/{publicId:guid}/status", async (
            Guid publicId,
            UpdateOrderStatusRequest request,
            OrderService orderService,
            IHubContext<OrderHub> hubContext,
            CancellationToken ct) =>
        {
            var result = await orderService.UpdateStatusAsync(publicId, request.Status, ct);
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
