using FoodOrdering.Api.Filters;
using FoodOrdering.Application.Catalog;
using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Domain.Stores;

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
            return ToActionResult(result, createdRoute: true);
        })
        .RequireStoreRole(StoreRole.Staff);

        group.MapPut("/{publicId:guid}", async (
            Guid publicId,
            UpdateCategoryRequest request,
            CategoryService categoryService,
            CancellationToken ct) =>
        {
            var result = await categoryService.UpdateAsync(publicId, request, ct);
            return ToActionResult(result, createdRoute: false);
        })
        .RequireStoreRole(StoreRole.Staff);

        group.MapPut("/reorder", async (
            ReorderCategoriesRequest request,
            CategoryService categoryService,
            CancellationToken ct) =>
        {
            var result = await categoryService.ReorderAsync(request, ct);
            if (result.IsNotFound)
            {
                return Results.NotFound(new { error = result.Error });
            }

            if (result.Error is not null || result.Categories is null)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            return Results.Ok(result.Categories);
        })
        .RequireStoreRole(StoreRole.Staff);

        group.MapDelete("/{publicId:guid}", async (
            Guid publicId,
            CategoryService categoryService,
            CancellationToken ct) =>
        {
            var result = await categoryService.DeleteAsync(publicId, ct);
            if (result.IsNotFound)
            {
                return Results.NotFound(new { error = result.Error });
            }

            if (result.IsConflict)
            {
                return Results.Conflict(new { error = result.Error });
            }

            return Results.NoContent();
        })
        .RequireStoreRole(StoreRole.Staff);

        return app;
    }

    private static IResult ToActionResult(CategoryResult result, bool createdRoute)
    {
        if (result.IsNotFound)
        {
            return Results.NotFound(new { error = result.Error });
        }

        if (result.IsConflict)
        {
            return Results.Conflict(new { error = result.Error });
        }

        if (result.Error is not null || result.Category is null)
        {
            return Results.BadRequest(new { error = result.Error });
        }

        return createdRoute
            ? Results.Created($"/api/categories/{result.Category.PublicId}", result.Category)
            : Results.Ok(result.Category);
    }
}
