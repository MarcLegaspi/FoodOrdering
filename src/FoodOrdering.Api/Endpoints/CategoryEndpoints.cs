using FoodOrdering.Application.Catalog;
using FoodOrdering.Application.Common.Interfaces;

namespace FoodOrdering.Api.Endpoints;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categories")
            .WithTags("Categories");

        group.MapGet("/", async (
            ITenantContext tenantContext,
            CategoryService categoryService,
            CancellationToken ct) =>
        {
            if (tenantContext.StoreId is null)
            {
                return Results.BadRequest(new { error = "Missing X-Store-Slug header." });
            }

            var categories = await categoryService.GetAllAsync(ct);
            return Results.Ok(categories);
        });

        group.MapPost("/", async (
            CreateCategoryRequest request,
            ITenantContext tenantContext,
            CategoryService categoryService,
            CancellationToken ct) =>
        {
            if (tenantContext.StoreId is null)
            {
                return Results.BadRequest(new { error = "Missing X-Store-Slug header." });
            }

            var result = await categoryService.CreateAsync(request, ct);

            if (result.IsConflict)
            {
                return Results.Conflict(new { error = result.Error });
            }

            if (result.Error is not null || result.Category is null)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            return Results.Created($"/api/categories/{result.Category.PublicId}", result.Category);
        })
        .RequireAuthorization();

        return app;
    }
}
