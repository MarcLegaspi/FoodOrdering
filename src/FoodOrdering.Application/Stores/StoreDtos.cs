namespace FoodOrdering.Application.Stores;

public sealed record CreateStoreRequest(
    string Slug,
    string Name,
    string Email,
    string Phone,
    string? OwnerEmail = null,
    string? OwnerFullName = null,
    string? OwnerPassword = null);

public sealed record StoreResponse(
    Guid PublicId,
    string Slug,
    string Name,
    string Email,
    string Phone,
    bool IsActive,
    DateTime CreatedAtUtc);
