using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Application.Stores;
using FoodOrdering.Domain.Stores;

namespace FoodOrdering.Api.Filters;

public static class StoreAuthorizationExtensions
{
    public static RouteHandlerBuilder RequireStoreRole(
        this RouteHandlerBuilder builder,
        StoreRole minimumRole)
    {
        return builder
            .RequireAuthorization()
            .AddEndpointFilter(async (context, next) =>
            {
                var tenantContext = context.HttpContext.RequestServices.GetRequiredService<ITenantContext>();
                if (tenantContext.StoreId is null)
                {
                    return Results.BadRequest(new { error = "Missing X-Store-Slug header." });
                }

                var storeAccess = context.HttpContext.RequestServices.GetRequiredService<StoreAccessService>();
                if (!await storeAccess.HasRoleAsync(minimumRole, context.HttpContext.RequestAborted))
                {
                    return Results.Forbid();
                }

                return await next(context);
            });
    }
}
