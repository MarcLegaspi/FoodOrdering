using FoodOrdering.Application.Reviews;
using FoodOrdering.Domain.Orders;

namespace FoodOrdering.Application.Orders;

public sealed record OrderItemInput(
    Guid MenuItemPublicId,
    int Quantity,
    IReadOnlyList<Guid>? SelectedOptionPublicIds = null);

/// <summary>
/// Payload to submit a new customer order.
/// </summary>
public sealed record PlaceOrderRequest(
    IReadOnlyList<OrderItemInput> Items,
    OrderType OrderType = OrderType.Pickup,
    string CustomerPhone = "",
    string? DeliveryAddress = null,
    string? Notes = null,
    string CustomerName = "",
    string? CustomerEmail = null,
    string? DeliveryLandmark = null,
    string? PromoCode = null,
    PaymentMethod PaymentMethod = PaymentMethod.PayOnDelivery,
    decimal? CashChangeFor = null,
    string? PaymentRefLast4 = null);

/// <summary>
/// Payload for store staff to transition order status.
/// </summary>
/// <param name="Status">Target order status.</param>
/// <param name="PaymentCollectedVia">Required if transitioning a PayOnDelivery order to Completed (Cash or GCash).</param>
/// <param name="DeliveryPin">Required 4-digit PIN when completing delivery orders (proof-of-delivery).</param>
public sealed record UpdateOrderStatusRequest(
    OrderStatus Status,
    PaymentCollectionMethod? PaymentCollectedVia = null,
    string? DeliveryPin = null);

/// <summary>
/// Payload to assign a delivery rider and dispatch the order.
/// </summary>
public sealed record AssignDriverRequest(
    string DriverName,
    string DriverPhone);

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
    IReadOnlyList<OrderItemResponse> Items,
    PaymentMethod PaymentMethod = PaymentMethod.PayOnDelivery,
    PaymentStatus PaymentStatus = PaymentStatus.Pending,
    decimal? CashChangeFor = null,
    string? DriverName = null,
    string? DriverPhone = null,
    DateTime? DispatchedAtUtc = null,
    DateTime? DeliveredAtUtc = null,
    PaymentCollectionMethod? PaymentCollectedVia = null,
    DateTime? PaidAtUtc = null,
    string? PaymentReference = null,
    string? DeliveryPin = null,
    string? ProofOfDeliveryImageUrl = null,
    string? CheckoutUrl = null,
    OrderReviewResponse? Review = null);

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
