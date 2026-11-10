namespace FoodOrdering.Application.Catalog;

public sealed record CreateCategoryRequest(
    string Name,
    int SortOrder = 0);

public sealed record CategoryResponse(
    Guid PublicId,
    string Name,
    int SortOrder,
    bool IsActive,
    DateTime CreatedAtUtc);
