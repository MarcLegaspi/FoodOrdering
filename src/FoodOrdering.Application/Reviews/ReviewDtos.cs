namespace FoodOrdering.Application.Reviews;

public sealed record CreateReviewRequest(
    int Rating,
    string? Comment);

public sealed record OrderReviewResponse(
    Guid PublicId,
    int Rating,
    string? Comment,
    string CustomerName,
    DateTime CreatedAtUtc);

public sealed record StoreReviewSummary(
    double? AverageRating,
    int ReviewCount,
    IReadOnlyList<OrderReviewResponse> RecentReviews);

public sealed record CreateReviewResult(
    OrderReviewResponse? Review,
    string? Error,
    bool IsNotFound = false,
    bool IsForbidden = false,
    bool IsConflict = false)
{
    public static CreateReviewResult Success(OrderReviewResponse review) => new(review, null);
    public static CreateReviewResult ValidationError(string error) => new(null, error);
    public static CreateReviewResult NotFound(string error) => new(null, error, IsNotFound: true);
    public static CreateReviewResult Forbidden(string error) => new(null, error, IsForbidden: true);
    public static CreateReviewResult Conflict(string error) => new(null, error, IsConflict: true);
}
