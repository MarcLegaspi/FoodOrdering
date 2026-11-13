using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Domain.Orders;
using FoodOrdering.Domain.Reviews;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Application.Reviews;

public sealed class ReviewService(
    IAppDbContext db,
    ICurrentUser currentUser)
{
    public async Task<CreateReviewResult> CreateOrderReviewAsync(
        Guid orderPublicId,
        CreateReviewRequest request,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is not Guid customerUserId)
        {
            return CreateReviewResult.Forbidden("You must be signed in to review an order.");
        }

        if (request.Rating < 1 || request.Rating > 5)
        {
            return CreateReviewResult.ValidationError("Rating must be between 1 and 5 stars.");
        }

        var comment = request.Comment?.Trim();
        if (comment?.Length > 500)
        {
            return CreateReviewResult.ValidationError("Review comment cannot exceed 500 characters.");
        }

        var order = await db.Orders
            .IgnoreQueryFilters()
            .Include(o => o.Store)
            .Include(o => o.Review)
            .FirstOrDefaultAsync(o => o.PublicId == orderPublicId, ct);

        if (order is null)
        {
            return CreateReviewResult.NotFound("Order was not found.");
        }

        if (order.CustomerUserId != customerUserId)
        {
            return CreateReviewResult.Forbidden("You can only review your own orders.");
        }

        if (order.Status != OrderStatus.Completed)
        {
            return CreateReviewResult.ValidationError("Only completed orders can be reviewed.");
        }

        if (order.Review is not null)
        {
            return CreateReviewResult.Conflict("You have already reviewed this order.");
        }

        var review = new OrderReview
        {
            StoreId = order.StoreId,
            OrderId = order.Id,
            CustomerUserId = customerUserId,
            CustomerName = !string.IsNullOrWhiteSpace(order.CustomerName) ? order.CustomerName : "Customer",
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment
        };

        db.OrderReviews.Add(review);

        var existingRatings = await db.OrderReviews
            .IgnoreQueryFilters()
            .Where(r => r.StoreId == order.StoreId)
            .Select(r => r.Rating)
            .ToListAsync(ct);

        existingRatings.Add(request.Rating);
        order.Store.ReviewCount = existingRatings.Count;
        order.Store.AverageRating = Math.Round(existingRatings.Average(), 1);

        await db.SaveChangesAsync(ct);

        return CreateReviewResult.Success(new OrderReviewResponse(
            review.PublicId,
            review.Rating,
            review.Comment,
            review.CustomerName,
            review.CreatedAtUtc));
    }

    public async Task<StoreReviewSummary?> GetStoreReviewsAsync(
        string slug,
        int limit = 20,
        CancellationToken ct = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var store = await db.Stores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == normalizedSlug, ct);

        if (store is null)
        {
            return null;
        }

        var reviews = await db.OrderReviews
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(r => r.StoreId == store.Id)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(limit)
            .Select(r => new OrderReviewResponse(
                r.PublicId,
                r.Rating,
                r.Comment,
                r.CustomerName,
                r.CreatedAtUtc))
            .ToListAsync(ct);

        return new StoreReviewSummary(store.AverageRating, store.ReviewCount, reviews);
    }
}
