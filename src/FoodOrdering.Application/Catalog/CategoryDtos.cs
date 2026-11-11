namespace FoodOrdering.Application.Catalog;

public sealed record CreateCategoryRequest(
    string Name,
    int SortOrder = 0);

public sealed record UpdateCategoryRequest(
    string Name,
    int SortOrder,
    bool IsActive = true);

public sealed record ReorderCategoriesRequest(
    IReadOnlyList<Guid> OrderedPublicIds);

public sealed record CategoryResponse(
    Guid PublicId,
    string Name,
    int SortOrder,
    bool IsActive,
    DateTime CreatedAtUtc);
