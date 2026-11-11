using FoodOrdering.Api.Filters;
using FoodOrdering.Application.Auth;
using FoodOrdering.Application.Stores;
using FoodOrdering.Domain.Stores;

namespace FoodOrdering.Api.Endpoints;

public static class StoreEndpoints
{
    public static IEndpointRouteBuilder MapStoreEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stores")
            .WithTags("Stores");

        group.MapGet("/", async (StoreService storeService, CancellationToken ct) =>
        {
            var stores = await storeService.GetAllAsync(ct);
            return Results.Ok(stores);
        });

        group.MapGet("/{slug}", async (string slug, StoreService storeService, CancellationToken ct) =>
        {
            var store = await storeService.GetBySlugAsync(slug, ct);
            return store is null ? Results.NotFound() : Results.Ok(store);
        });

        group.MapPost("/", async (CreateStoreRequest request, StoreService storeService, CancellationToken ct) =>
        {
            var result = await storeService.CreateAsync(request, ct);

            if (result.IsConflict)
            {
                return Results.Conflict(new { error = result.Error });
            }

            if (result.Error is not null || result.Store is null)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            return Results.Created($"/api/stores/{result.Store.Slug}", result.Store);
        })
        .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin));

        group.MapPatch("/current/settings", async (
            UpdateStoreSettingsRequest request,
            StoreService storeService,
            CancellationToken ct) =>
        {
            var updated = await storeService.UpdateCurrentStoreSettingsAsync(request, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        })
        .RequireStoreRole(StoreRole.Manager);

        return app;
    }
}
