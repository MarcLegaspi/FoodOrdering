namespace FoodOrdering.Application.Stores;

public sealed record CreateStoreRequest(
    string Slug,
    string Name,
    string Email,
    string Phone);

public sealed record StoreResponse(
    Guid PublicId,
    string Slug,
    string Name,
    string Email,
    string Phone,
    bool IsActive,
    DateTime CreatedAtUtc);
