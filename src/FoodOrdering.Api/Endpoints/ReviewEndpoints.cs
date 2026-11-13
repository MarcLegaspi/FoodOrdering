using FoodOrdering.Application.Reviews;

namespace FoodOrdering.Api.Endpoints;

public static class ReviewEndpoints
{
    public static IEndpointRouteBuilder MapReviewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api")
            .WithTags("Reviews");

        // Submit review for an order
        group.MapPost("/orders/{publicId:guid}/review", async (
            Guid publicId,
            CreateReviewRequest request,
            ReviewService reviewService,
            CancellationToken ct) =>
        {
            var result = await reviewService.CreateOrderReviewAsync(publicId, request, ct);

            if (result.IsNotFound)
            {
                return Results.NotFound(new { error = result.Error });
            }

            if (result.IsForbidden)
            {
                return Results.Forbid();
            }

            if (result.IsConflict)
            {
                return Results.Conflict(new { error = result.Error });
            }

            if (result.Error is not null || result.Review is null)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            return Results.Ok(result.Review);
        })
        .RequireAuthorization();

        // Get reviews for a store
        group.MapGet("/stores/{slug}/reviews", async (
            string slug,
            ReviewService reviewService,
            CancellationToken ct) =>
        {
            var summary = await reviewService.GetStoreReviewsAsync(slug, 20, ct);
            if (summary is null)
            {
                return Results.NotFound(new { error = $"Store '{slug}' was not found." });
            }

            return Results.Ok(summary);
        });

        return app;
    }
}
