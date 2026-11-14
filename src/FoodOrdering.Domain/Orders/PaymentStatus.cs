namespace FoodOrdering.Domain.Orders;

/// <summary>
/// Status of payment for the order lifecycle.
/// </summary>
public enum PaymentStatus
{
    /// <summary>Payment is pending collection upon delivery or pickup.</summary>
    Pending = 1,

    /// <summary>Payment has been received and verified.</summary>
    Paid = 2,

    /// <summary>Payment attempt failed.</summary>
    Failed = 3
}
