using FoodOrdering.Domain.Orders;

namespace FoodOrdering.Application.Orders;

public sealed record OrderItemInput(
    Guid MenuItemPublicId,
    int Quantity);

public sealed record PlaceOrderRequest(
    IReadOnlyList<OrderItemInput> Items,
    string? Notes = null);

public sealed record UpdateOrderStatusRequest(OrderStatus Status);

public sealed record OrderItemResponse(
    Guid MenuItemPublicId,
    string ItemName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public sealed record OrderResponse(
    Guid PublicId,
    string StoreSlug,
    string StoreName,
    string OrderNumber,
    OrderStatus Status,
    decimal TotalAmount,
    string Notes,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record OrderResult(
    OrderResponse? Order,
    string? Error,
    bool IsNotFound = false,
    bool IsForbidden = false)
{
    public static OrderResult Success(OrderResponse order) => new(order, null);
    public static OrderResult ValidationError(string error) => new(null, error);
    public static OrderResult NotFound(string error) => new(null, error, IsNotFound: true);
    public static OrderResult Forbidden(string error) => new(null, error, IsForbidden: true);
}
