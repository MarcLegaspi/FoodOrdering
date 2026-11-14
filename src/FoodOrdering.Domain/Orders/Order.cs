using FoodOrdering.Domain.Common;
using FoodOrdering.Domain.Reviews;
using FoodOrdering.Domain.Stores;

namespace FoodOrdering.Domain.Orders;

public sealed class Order : AuditableEntity, ITenantEntity
{
    public long Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public long StoreId { get; set; }

    public Store Store { get; set; } = null!;

    public CustomerType CustomerType { get; set; } = CustomerType.Registered;

    public Guid? CustomerUserId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string? CustomerEmail { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public OrderType OrderType { get; set; } = OrderType.Pickup;

    public string CustomerPhone { get; set; } = string.Empty;

    public string? DeliveryAddress { get; set; }

    public string? DeliveryLandmark { get; set; }

    public decimal Subtotal { get; set; }

    public string? PromoCodeSnapshot { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal DeliveryFee { get; set; }

    public decimal TotalAmount { get; set; }

    public string EstimatedDeliveryTime { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Chosen payment option: PayOnDelivery (Cash or GCash QR to rider), GCashOnline (express pre-paid), or PayAtCounter.
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.PayOnDelivery;

    /// <summary>
    /// Lifecycle payment state: Pending (due upon delivery/pickup) or Paid.
    /// </summary>
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    /// <summary>
    /// Optional customer note when paying cash on delivery (e.g., 500, 1000) so rider brings change.
    /// </summary>
    public decimal? CashChangeFor { get; set; }

    /// <summary>
    /// Name of assigned delivery rider/driver.
    /// </summary>
    public string? DriverName { get; set; }

    /// <summary>
    /// Contact phone number of assigned delivery rider/driver.
    /// </summary>
    public string? DriverPhone { get; set; }

    /// <summary>
    /// UTC timestamp when driver leaves the store and order enters OutForDelivery status.
    /// </summary>
    public DateTime? DispatchedAtUtc { get; set; }

    /// <summary>
    /// UTC timestamp when customer actually receives delivery upon PIN verification.
    /// </summary>
    public DateTime? DeliveredAtUtc { get; set; }

    /// <summary>
    /// Method rider collected payment on delivery: Cash or GCash QR.
    /// </summary>
    public PaymentCollectionMethod? PaymentCollectedVia { get; set; }

    /// <summary>
    /// UTC timestamp when payment was recorded/verified.
    /// </summary>
    public DateTime? PaidAtUtc { get; set; }

    /// <summary>
    /// Transaction reference for online payments (e.g. GCash reference ID).
    /// </summary>
    public string? PaymentReference { get; set; }

    /// <summary>
    /// Hosted checkout redirect URL from payment gateway (e.g. Xendit Invoice URL for GCash).
    /// </summary>
    public string? PaymentCheckoutUrl { get; set; }

    /// <summary>
    /// 4-digit security PIN generated for delivery orders. Customer shows this to rider at doorstep as proof-of-delivery.
    /// </summary>
    public string? DeliveryPin { get; set; }

    /// <summary>
    /// Optional URL of photo proof-of-delivery taken by rider (reserved for Phase 2 cloud storage).
    /// </summary>
    public string? ProofOfDeliveryImageUrl { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    public OrderReview? Review { get; set; }
}
