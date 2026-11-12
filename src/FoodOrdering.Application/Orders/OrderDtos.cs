using FoodOrdering.Domain.Orders;

namespace FoodOrdering.Application.Orders;

public sealed record OrderItemInput(
    Guid MenuItemPublicId,
    int Quantity,
    IReadOnlyList<Guid>? SelectedOptionPublicIds = null);

public sealed record PlaceOrderRequest(
    IReadOnlyList<OrderItemInput> Items,
    OrderType OrderType = OrderType.Pickup,
    string CustomerPhone = "",
    string? DeliveryAddress = null,
    string? Notes = null,
    string CustomerName = "",
    string? CustomerEmail = null,
    string? DeliveryLandmark = null,
    string? PromoCode = null);

public sealed record UpdateOrderStatusRequest(OrderStatus Status);

public sealed record OrderItemOptionResponse(
    string GroupName,
    string OptionName,
    decimal PriceDelta);

public sealed record OrderItemResponse(
    Guid MenuItemPublicId,
    string ItemName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    IReadOnlyList<OrderItemOptionResponse> Options);

public sealed record OrderResponse(
    Guid PublicId,
    string StoreSlug,
    string StoreName,
    string OrderNumber,
    CustomerType CustomerType,
    string CustomerName,
    OrderStatus Status,
    OrderType OrderType,
    string CustomerPhone,
    string? CustomerEmail,
    string? DeliveryAddress,
    string? DeliveryLandmark,
    decimal Subtotal,
    string? PromoCode,
    decimal DiscountAmount,
    decimal DeliveryFee,
    decimal TotalAmount,
    string EstimatedDeliveryTime,
    string Notes,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderItemResponse> Items);

public sealed record OrderResult(
    OrderResponse? Order,
    string? Error,
    bool IsNotFound = false,
    bool IsForbidden = false,
    Guid? CustomerUserId = null)
{
    public static OrderResult Success(OrderResponse order, Guid? customerUserId = null) =>
        new(order, null, CustomerUserId: customerUserId);
    public static OrderResult ValidationError(string error) => new(null, error);
    public static OrderResult NotFound(string error) => new(null, error, IsNotFound: true);
    public static OrderResult Forbidden(string error) => new(null, error, IsForbidden: true);
}
