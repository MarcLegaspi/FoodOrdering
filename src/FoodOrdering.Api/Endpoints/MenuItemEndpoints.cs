using FoodOrdering.Api.Filters;
using FoodOrdering.Application.Catalog;
using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Domain.Stores;

namespace FoodOrdering.Api.Endpoints;

public static class MenuItemEndpoints
{
    public static IEndpointRouteBuilder MapMenuItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/menu-items")
            .WithTags("Menu Items");

        group.MapGet("/", async (
            Guid? categoryPublicId,
            bool? availableOnly,
            ITenantContext tenantContext,
            MenuItemService menuItemService,
            CancellationToken ct) =>
        {
            if (tenantContext.StoreId is null)
            {
                return Results.BadRequest(new { error = "Missing X-Store-Slug header." });
            }

            var items = await menuItemService.GetAllAsync(categoryPublicId, availableOnly, ct);
            return Results.Ok(items);
        });

        group.MapPost("/", async (
            CreateMenuItemRequest request,
            MenuItemService menuItemService,
            CancellationToken ct) =>
        {
            var result = await menuItemService.CreateAsync(request, ct);
            return ToActionResult(result, createdRoute: true);
        })
        .RequireStoreRole(StoreRole.Staff);

        group.MapPut("/{publicId:guid}", async (
            Guid publicId,
            UpdateMenuItemRequest request,
            MenuItemService menuItemService,
            CancellationToken ct) =>
        {
            var result = await menuItemService.UpdateAsync(publicId, request, ct);
            return ToActionResult(result, createdRoute: false);
        })
        .RequireStoreRole(StoreRole.Staff);

        group.MapDelete("/{publicId:guid}", async (
            Guid publicId,
            MenuItemService menuItemService,
            CancellationToken ct) =>
        {
            var result = await menuItemService.DeleteAsync(publicId, ct);
            if (result.IsNotFound)
            {
                return Results.NotFound(new { error = result.Error });
            }

            return Results.NoContent();
        })
        .RequireStoreRole(StoreRole.Staff);

        return app;
    }

    private static IResult ToActionResult(MenuItemResult result, bool createdRoute)
    {
        if (result.IsNotFound)
        {
            return Results.NotFound(new { error = result.Error });
        }

        if (result.IsConflict)
        {
            return Results.Conflict(new { error = result.Error });
        }

        if (result.Error is not null || result.Item is null)
        {
            return Results.BadRequest(new { error = result.Error });
        }

        return createdRoute
            ? Results.Created($"/api/menu-items/{result.Item.PublicId}", result.Item)
            : Results.Ok(result.Item);
    }
}
