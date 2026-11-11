namespace FoodOrdering.Application.Catalog;

public sealed record CreateMenuItemRequest(
    Guid CategoryPublicId,
    string Name,
    string Description,
    decimal Price,
    string? ImageUrl = null,
    bool IsAvailable = true,
    int SortOrder = 0);

public sealed record UpdateMenuItemRequest(
    Guid CategoryPublicId,
    string Name,
    string Description,
    decimal Price,
    string? ImageUrl = null,
    bool IsAvailable = true,
    int SortOrder = 0);

public sealed record MenuItemResponse(
    Guid PublicId,
    Guid CategoryPublicId,
    string CategoryName,
    string Name,
    string Description,
    decimal Price,
    string? ImageUrl,
    bool IsAvailable,
    int SortOrder,
    DateTime CreatedAtUtc);

public sealed record MenuItemResult(
    MenuItemResponse? Item,
    string? Error,
    bool IsConflict = false,
    bool IsNotFound = false)
{
    public static MenuItemResult Success(MenuItemResponse item) => new(item, null);
    public static MenuItemResult ValidationError(string error) => new(null, error);
    public static MenuItemResult Conflict(string error) => new(null, error, IsConflict: true);
    public static MenuItemResult NotFound(string error) => new(null, error, IsNotFound: true);
}
