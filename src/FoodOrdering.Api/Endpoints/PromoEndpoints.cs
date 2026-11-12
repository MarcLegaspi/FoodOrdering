using FoodOrdering.Api.Filters;
using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Application.Promos;
using FoodOrdering.Domain.Stores;

namespace FoodOrdering.Api.Endpoints;

public static class PromoEndpoints
{
    public static IEndpointRouteBuilder MapPromoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/promos")
            .WithTags("Promos");

        group.MapPost("/validate", async (
            ValidatePromoRequest request,
            ITenantContext tenantContext,
            PromoService promoService,
            CancellationToken ct) =>
        {
            if (tenantContext.StoreId is null)
            {
                return Results.BadRequest(new { error = "Missing X-Store-Slug header." });
            }

            var result = await promoService.ValidatePromoAsync(request.Code, request.Subtotal, ct);
            return Results.Ok(result);
        })
        .RequireAuthorization();

        group.MapGet("/", async (
            PromoService promoService,
            CancellationToken ct) =>
        {
            var promos = await promoService.GetStorePromosAsync(ct);
            return Results.Ok(promos);
        })
        .RequireStoreRole(StoreRole.Staff);

        group.MapPost("/", async (
            CreatePromoCodeRequest request,
            ITenantContext tenantContext,
            PromoService promoService,
            CancellationToken ct) =>
        {
            if (tenantContext.StoreId is null)
            {
                return Results.BadRequest(new { error = "Missing X-Store-Slug header." });
            }

            var result = await promoService.CreatePromoAsync(request, ct);
            if (result.IsConflict)
            {
                return Results.Conflict(new { error = result.Error });
            }

            if (result.Error is not null || result.Promo is null)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            return Results.Created($"/api/promos/{result.Promo.PublicId}", result.Promo);
        })
        .RequireStoreRole(StoreRole.Manager);

        group.MapPatch("/{publicId:guid}/toggle", async (
            Guid publicId,
            PromoService promoService,
            CancellationToken ct) =>
        {
            var result = await promoService.TogglePromoStatusAsync(publicId, ct);
            if (result.IsNotFound)
            {
                return Results.NotFound(new { error = result.Error });
            }

            return Results.Ok(result.Promo);
        })
        .RequireStoreRole(StoreRole.Manager);

        group.MapDelete("/{publicId:guid}", async (
            Guid publicId,
            PromoService promoService,
            CancellationToken ct) =>
        {
            var deleted = await promoService.DeletePromoAsync(publicId, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireStoreRole(StoreRole.Manager);

        return app;
    }
}
