namespace FoodOrdering.Domain.Orders;

/// <summary>
/// Order lifecycle statuses.
/// </summary>
public enum OrderStatus
{
    Pending = 1,
    Confirmed = 2,
    Preparing = 3,
    Ready = 4,
    Completed = 5,
    Cancelled = 6,

    /// <summary>Driver assigned and dispatched; rider is on the way to customer.</summary>
    OutForDelivery = 7
}
