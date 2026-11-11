namespace FoodOrdering.Domain.Orders;

public enum OrderStatus
{
    Pending = 1,
    Confirmed = 2,
    Preparing = 3,
    Ready = 4,
    Completed = 5,
    Cancelled = 6
}
