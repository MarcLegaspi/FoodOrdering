using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Application.Orders;

public sealed class OrderService(
    IAppDbContext db,
    ITenantContext tenantContext,
    ICurrentUser currentUser)
{
    public async Task<OrderResult> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct = default)
    {
        if (tenantContext.StoreId is not long storeId)
        {
            return OrderResult.ValidationError("Store tenant context is required.");
        }

        if (!currentUser.IsAuthenticated || currentUser.UserId is not Guid customerUserId)
        {
            return OrderResult.Forbidden("Authentication is required to place an order.");
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            return OrderResult.ValidationError("At least one menu item is required.");
        }

        if (request.Items.Any(i => i.Quantity <= 0))
        {
            return OrderResult.ValidationError("All item quantities must be at least 1.");
        }

        if (!Enum.IsDefined(request.OrderType))
        {
            return OrderResult.ValidationError("Invalid order type.");
        }

        var customerPhone = request.CustomerPhone?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(customerPhone))
        {
            return OrderResult.ValidationError("Customer phone number is required.");
        }

        var deliveryAddress = request.OrderType == OrderType.Delivery
            ? request.DeliveryAddress?.Trim()
            : null;

        if (request.OrderType == OrderType.Delivery && string.IsNullOrWhiteSpace(deliveryAddress))
        {
            return OrderResult.ValidationError("Delivery address is required for delivery orders.");
        }

        // Merge duplicate menu item lines so each menu item appears once per order
        var quantitiesByPublicId = request.Items
            .GroupBy(i => i.MenuItemPublicId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        var requestedIds = quantitiesByPublicId.Keys.ToList();

        // Tenant filter on MenuItems ensures items belong strictly to the active store
        var menuItems = await db.MenuItems
            .AsNoTracking()
            .Where(m => requestedIds.Contains(m.PublicId))
            .ToListAsync(ct);

        if (menuItems.Count != requestedIds.Count)
        {
            return OrderResult.NotFound("One or more menu items were not found for this store.");
        }

        var unavailableItem = menuItems.FirstOrDefault(m => !m.IsAvailable);
        if (unavailableItem is not null)
        {
            return OrderResult.ValidationError($"'{unavailableItem.Name}' is currently unavailable.");
        }

        var store = await db.Stores
            .AsNoTracking()
            .Where(s => s.Id == storeId)
            .Select(s => new { s.Slug, s.Name })
            .FirstOrDefaultAsync(ct);

        if (store is null)
        {
            return OrderResult.NotFound("Active store was not found.");
        }

        var order = new Order
        {
            StoreId = storeId,
            CustomerUserId = customerUserId,
            OrderNumber = GenerateOrderNumber(),
            Status = OrderStatus.Pending,
            OrderType = request.OrderType,
            CustomerPhone = customerPhone,
            DeliveryAddress = deliveryAddress,
            Notes = request.Notes?.Trim() ?? string.Empty
        };

        decimal totalAmount = 0m;
        foreach (var menuItem in menuItems)
        {
            var quantity = quantitiesByPublicId[menuItem.PublicId];
            totalAmount += menuItem.Price * quantity;

            order.Items.Add(new OrderItem
            {
                MenuItemId = menuItem.Id,
                ItemNameSnapshot = menuItem.Name,
                UnitPriceSnapshot = menuItem.Price,
                Quantity = quantity
            });
        }

        order.TotalAmount = totalAmount;

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        var itemsById = menuItems.ToDictionary(m => m.Id, m => m.PublicId);
        return OrderResult.Success(new OrderResponse(
            order.PublicId,
            store.Slug,
            store.Name,
            order.OrderNumber,
            order.Status,
            order.OrderType,
            order.CustomerPhone,
            order.DeliveryAddress,
            order.TotalAmount,
            order.Notes,
            order.CreatedAtUtc,
            order.Items.Select(i => new OrderItemResponse(
                itemsById[i.MenuItemId],
                i.ItemNameSnapshot,
                i.UnitPriceSnapshot,
                i.Quantity,
                i.UnitPriceSnapshot * i.Quantity)).ToList()));
    }

    public async Task<IReadOnlyList<OrderResponse>> GetMyOrdersAsync(CancellationToken ct = default)
    {
        if (currentUser.UserId is not Guid customerUserId)
        {
            return [];
        }

        // Customers can view their order history across all stores (or narrowed if X-Store-Slug is sent)
        var query = db.Orders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o => o.CustomerUserId == customerUserId);

        if (tenantContext.StoreId is long storeId)
        {
            query = query.Where(o => o.StoreId == storeId);
        }

        return await ProjectOrders(query.OrderByDescending(o => o.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<OrderResponse>> GetStoreOrdersAsync(
        OrderStatus? status = null,
        CancellationToken ct = default)
    {
        var query = db.Orders
            .AsNoTracking()
            .AsQueryable();

        if (status is OrderStatus filterStatus)
        {
            query = query.Where(o => o.Status == filterStatus);
        }

        return await ProjectOrders(query.OrderByDescending(o => o.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<OrderResult> UpdateStatusAsync(
        Guid publicId,
        OrderStatus newStatus,
        CancellationToken ct = default)
    {
        if (!Enum.IsDefined(newStatus))
        {
            return OrderResult.ValidationError("Invalid order status.");
        }

        var order = await db.Orders
            .Include(o => o.Store)
            .Include(o => o.Items)
            .ThenInclude(i => i.MenuItem)
            .FirstOrDefaultAsync(o => o.PublicId == publicId, ct);

        if (order is null)
        {
            return OrderResult.NotFound("Order was not found for this store.");
        }

        if (!IsValidTransition(order.Status, newStatus))
        {
            return OrderResult.ValidationError(
                $"Cannot change order status from {order.Status} to {newStatus}.");
        }

        order.Status = newStatus;
        await db.SaveChangesAsync(ct);

        return OrderResult.Success(ToResponse(order));
    }

    public async Task<OrderResult> CancelMyOrderAsync(Guid publicId, CancellationToken ct = default)
    {
        if (currentUser.UserId is not Guid customerUserId)
        {
            return OrderResult.Forbidden("Authentication is required.");
        }

        var order = await db.Orders
            .IgnoreQueryFilters()
            .Include(o => o.Store)
            .Include(o => o.Items)
            .ThenInclude(i => i.MenuItem)
            .FirstOrDefaultAsync(o => o.PublicId == publicId, ct);

        if (order is null)
        {
            return OrderResult.NotFound("Order was not found.");
        }

        if (order.CustomerUserId != customerUserId)
        {
            return OrderResult.Forbidden("You can only cancel your own orders.");
        }

        if (order.Status != OrderStatus.Pending)
        {
            return OrderResult.ValidationError("Only pending orders can be cancelled by the customer.");
        }

        order.Status = OrderStatus.Cancelled;
        await db.SaveChangesAsync(ct);

        return OrderResult.Success(ToResponse(order));
    }

    private static bool IsValidTransition(OrderStatus current, OrderStatus next)
    {
        if (current is OrderStatus.Completed or OrderStatus.Cancelled)
        {
            return false;
        }

        if (next == OrderStatus.Cancelled)
        {
            return true;
        }

        return next > current;
    }

    private static string GenerateOrderNumber() =>
        $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    private static IQueryable<OrderResponse> ProjectOrders(IQueryable<Order> query) =>
        query.Select(o => new OrderResponse(
            o.PublicId,
            o.Store.Slug,
            o.Store.Name,
            o.OrderNumber,
            o.Status,
            o.OrderType,
            o.CustomerPhone,
            o.DeliveryAddress,
            o.TotalAmount,
            o.Notes,
            o.CreatedAtUtc,
            o.Items.Select(i => new OrderItemResponse(
                i.MenuItem.PublicId,
                i.ItemNameSnapshot,
                i.UnitPriceSnapshot,
                i.Quantity,
                i.UnitPriceSnapshot * i.Quantity)).ToList()));

    private static OrderResponse ToResponse(Order order) =>
        new(
            order.PublicId,
            order.Store.Slug,
            order.Store.Name,
            order.OrderNumber,
            order.Status,
            order.OrderType,
            order.CustomerPhone,
            order.DeliveryAddress,
            order.TotalAmount,
            order.Notes,
            order.CreatedAtUtc,
            order.Items.Select(i => new OrderItemResponse(
                i.MenuItem.PublicId,
                i.ItemNameSnapshot,
                i.UnitPriceSnapshot,
                i.Quantity,
                i.UnitPriceSnapshot * i.Quantity)).ToList());
}
