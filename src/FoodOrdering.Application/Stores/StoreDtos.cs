namespace FoodOrdering.Application.Stores;

public sealed record CreateStoreRequest(
    string Slug,
    string Name,
    string Email,
    string Phone,
    string? OwnerEmail = null,
    string? OwnerFullName = null,
    string? OwnerPassword = null,
    string? Address = null,
    string? OpeningHours = null,
    decimal DeliveryFee = 0m,
    decimal MinimumDeliveryOrder = 0m,
    decimal? FreeDeliveryThreshold = null,
    string EstimatedDeliveryTime = "");

public sealed record UpdateStoreSettingsRequest(bool IsAcceptingOrders);

public sealed record UpdateStoreProfileRequest(
    string Name,
    string Email = "",
    string Phone = "",
    string Address = "",
    string OpeningHours = "",
    decimal DeliveryFee = 0m,
    decimal MinimumDeliveryOrder = 0m,
    decimal? FreeDeliveryThreshold = null,
    string EstimatedDeliveryTime = "");

public sealed record StoreResponse(
    Guid PublicId,
    string Slug,
    string Name,
    string Email,
    string Phone,
    string Address,
    string OpeningHours,
    bool IsActive,
    bool IsAcceptingOrders,
    DateTime CreatedAtUtc,
    decimal DeliveryFee = 0m,
    decimal MinimumDeliveryOrder = 0m,
    decimal? FreeDeliveryThreshold = null,
    string EstimatedDeliveryTime = "");
