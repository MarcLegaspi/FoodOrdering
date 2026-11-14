namespace FoodOrdering.Domain.Orders;

/// <summary>
/// Method selected by customer to pay for their order.
/// </summary>
public enum PaymentMethod
{
    /// <summary>Customer pays rider upon delivery via cash or rider's GCash QR.</summary>
    PayOnDelivery = 1,

    /// <summary>Pre-paid online via GCash for express priority pickup/fulfillment.</summary>
    GCashOnline = 2,

    /// <summary>Customer pays cashier at counter when picking up their order.</summary>
    PayAtCounter = 3
}
