namespace FoodOrdering.Domain.Orders;

/// <summary>
/// Specific method used by customer to settle payment directly with the delivery rider.
/// </summary>
public enum PaymentCollectionMethod
{
    /// <summary>Customer paid rider with physical cash currency.</summary>
    Cash = 1,

    /// <summary>Customer paid by scanning the rider's personal/store GCash QR code.</summary>
    GCash = 2
}
