namespace FoodOrdering.Application.Catalog;

public sealed record SaveMenuItemOptionRequest(
    string Name,
    decimal PriceDelta = 0m,
    int SortOrder = 0);

public sealed record SaveMenuItemOptionGroupRequest(
    string Name,
    bool IsRequired = false,
    bool AllowMultiple = false,
    int SortOrder = 0,
    IReadOnlyList<SaveMenuItemOptionRequest>? Options = null);

public sealed record CreateMenuItemRequest(
    Guid CategoryPublicId,
    string Name,
    string Description,
    decimal Price,
    string? ImageUrl = null,
    bool IsAvailable = true,
    int SortOrder = 0,
    IReadOnlyList<SaveMenuItemOptionGroupRequest>? OptionGroups = null);

public sealed record UpdateMenuItemRequest(
    Guid CategoryPublicId,
    string Name,
    string Description,
    decimal Price,
    string? ImageUrl = null,
    bool IsAvailable = true,
    int SortOrder = 0,
    IReadOnlyList<SaveMenuItemOptionGroupRequest>? OptionGroups = null);

public sealed record MenuItemOptionResponse(
    Guid PublicId,
    string Name,
    decimal PriceDelta,
    int SortOrder);

public sealed record MenuItemOptionGroupResponse(
    Guid PublicId,
    string Name,
    bool IsRequired,
    bool AllowMultiple,
    int SortOrder,
    IReadOnlyList<MenuItemOptionResponse> Options);

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
    DateTime CreatedAtUtc,
    IReadOnlyList<MenuItemOptionGroupResponse> OptionGroups);

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
