using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Application.Reviews;
using FoodOrdering.Domain.Orders;
using FoodOrdering.Domain.Promos;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Application.Orders;

public sealed class OrderService(
    IAppDbContext db,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IPaymentGatewayService? paymentGateway = null)
{
    public async Task<OrderResult> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct = default)
    {
        if (tenantContext.StoreId is not long storeId)
        {
            return OrderResult.ValidationError("Store tenant context is required.");
        }

        CustomerType customerType;
        Guid? customerUserId = null;
        string customerName;

        if (currentUser.IsAuthenticated && currentUser.UserId is Guid uid)
        {
            customerType = CustomerType.Registered;
            customerUserId = uid;
            customerName = !string.IsNullOrWhiteSpace(request.CustomerName)
                ? request.CustomerName.Trim()
                : (currentUser.Email ?? "Registered Customer");
        }
        else
        {
            customerType = CustomerType.Guest;
            customerUserId = null;
            customerName = request.CustomerName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(customerName))
            {
                return OrderResult.ValidationError("Customer name is required for guest checkout.");
            }
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

        var requestedIds = request.Items
            .Select(i => i.MenuItemPublicId)
            .Distinct()
            .ToList();

        // Tenant filter on MenuItems ensures items belong strictly to the active store
        var menuItems = await db.MenuItems
            .AsNoTracking()
            .Include(m => m.OptionGroups)
            .ThenInclude(g => g.Options)
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

        var menuItemsByPublicId = menuItems.ToDictionary(m => m.PublicId);
        var resolvedLines = new Dictionary<string, (Domain.Catalog.MenuItem MenuItem, List<(string GroupName, string OptionName, decimal PriceDelta)> Options, int Quantity)>();

        foreach (var lineInput in request.Items)
        {
            var menuItem = menuItemsByPublicId[lineInput.MenuItemPublicId];
            var (selectedOptions, lineKey, optionError) = ValidateAndResolveOptions(
                menuItem,
                lineInput.SelectedOptionPublicIds);

            if (optionError is not null)
            {
                return OrderResult.ValidationError(optionError);
            }

            if (resolvedLines.TryGetValue(lineKey, out var existing))
            {
                resolvedLines[lineKey] = (existing.MenuItem, existing.Options, existing.Quantity + lineInput.Quantity);
            }
            else
            {
                resolvedLines[lineKey] = (menuItem, selectedOptions, lineInput.Quantity);
            }
        }

        var store = await db.Stores
            .AsNoTracking()
            .Where(s => s.Id == storeId)
            .Select(s => new
            {
                s.Slug,
                s.Name,
                s.IsAcceptingOrders,
                s.DeliveryFee,
                s.MinimumDeliveryOrder,
                s.FreeDeliveryThreshold,
                s.EstimatedDeliveryTime
            })
            .FirstOrDefaultAsync(ct);

        if (store is null)
        {
            return OrderResult.NotFound("Active store was not found.");
        }

        if (!store.IsAcceptingOrders)
        {
            return OrderResult.ValidationError($"'{store.Name}' is currently closed and not accepting orders.");
        }

        var customerEmail = !string.IsNullOrWhiteSpace(request.CustomerEmail)
            ? request.CustomerEmail.Trim()
            : (currentUser.IsAuthenticated ? currentUser.Email : null);

        decimal subtotal = 0m;
        var orderItems = new List<OrderItem>();

        foreach (var line in resolvedLines.Values)
        {
            var unitPrice = line.MenuItem.Price + line.Options.Sum(o => o.PriceDelta);
            subtotal += unitPrice * line.Quantity;

            var orderItem = new OrderItem
            {
                MenuItemId = line.MenuItem.Id,
                ItemNameSnapshot = line.MenuItem.Name,
                UnitPriceSnapshot = unitPrice,
                Quantity = line.Quantity
            };

            foreach (var opt in line.Options)
            {
                orderItem.Options.Add(new OrderItemOption
                {
                    GroupNameSnapshot = opt.GroupName,
                    OptionNameSnapshot = opt.OptionName,
                    PriceDeltaSnapshot = opt.PriceDelta
                });
            }

            orderItems.Add(orderItem);
        }

        decimal deliveryFee = 0m;
        string? deliveryLandmark = null;
        var estimatedDeliveryTime = store.EstimatedDeliveryTime;

        if (request.OrderType == OrderType.Delivery)
        {
            if (store.MinimumDeliveryOrder > 0 && subtotal < store.MinimumDeliveryOrder)
            {
                var remaining = store.MinimumDeliveryOrder - subtotal;
                return OrderResult.ValidationError(
                    $"Minimum order for delivery is {store.MinimumDeliveryOrder:C}. Please add {remaining:C} more to qualify for delivery.");
            }

            deliveryFee = store.FreeDeliveryThreshold is { } threshold && subtotal >= threshold
                ? 0m
                : store.DeliveryFee;

            deliveryLandmark = request.DeliveryLandmark?.Trim();
        }

        decimal discountAmount = 0m;
        string? promoCodeSnapshot = null;

        if (!string.IsNullOrWhiteSpace(request.PromoCode))
        {
            if (customerType != CustomerType.Registered)
            {
                return OrderResult.ValidationError("Promo codes are only available for registered members. Please sign in to apply.");
            }

            var normalizedPromoCode = request.PromoCode.Trim().ToUpperInvariant();
            var promo = await db.PromoCodes
                .FirstOrDefaultAsync(p => p.Code == normalizedPromoCode, ct);

            if (promo is null || !promo.IsActive)
            {
                return OrderResult.ValidationError("Invalid or inactive promo code.");
            }

            if (promo.StartsAtUtc is { } startsAt && DateTime.UtcNow < startsAt)
            {
                return OrderResult.ValidationError($"This promo code is not active yet. It will be valid starting {startsAt:MMM d, yyyy h:mm tt} UTC.");
            }

            if (promo.ExpiresAtUtc is { } expiry && DateTime.UtcNow > expiry)
            {
                return OrderResult.ValidationError("This promo code has expired.");
            }

            if (promo.MaxUses is { } maxUses && promo.UsedCount >= maxUses)
            {
                return OrderResult.ValidationError("This promo code has reached its maximum claim limit.");
            }

            if (customerUserId is not null)
            {
                var alreadyUsed = await db.Orders
                    .AsNoTracking()
                    .AnyAsync(o => o.CustomerUserId == customerUserId
                                && o.PromoCodeSnapshot == promo.Code
                                && o.Status != OrderStatus.Cancelled, ct);

                if (alreadyUsed)
                {
                    return OrderResult.ValidationError("You have already used this promo code on a previous order.");
                }
            }

            discountAmount = promo.DiscountType switch
            {
                PromoDiscountType.Percentage => Math.Round(subtotal * (promo.DiscountValue / 100m), 2),
                PromoDiscountType.FixedAmount => promo.DiscountValue,
                _ => 0m
            };

            discountAmount = Math.Min(subtotal, Math.Max(0m, discountAmount));
            promoCodeSnapshot = promo.Code;
            promo.UsedCount++;
        }

        var totalAmount = Math.Max(0m, subtotal - discountAmount) + deliveryFee;

        // Payment and Delivery Security Initialization:
        // Delivery orders generate a secure 4-digit PIN that the customer provides to the rider as proof-of-delivery.
        var paymentMethod = request.PaymentMethod;
        var paymentStatus = PaymentStatus.Pending;
        string? paymentReference = null;
        string? checkoutUrl = null;
        DateTime? paidAtUtc = null;
        var deliveryPin = request.OrderType == OrderType.Delivery ? Random.Shared.Next(1000, 10000).ToString() : null;
        var orderNumber = GenerateOrderNumber();
        var orderPublicId = Guid.NewGuid();

        // GCash Online: If a payment gateway (e.g. Xendit) is configured, create a checkout invoice.
        // If not configured, fall back to simulated instant payment for local/demo testing.
        if (paymentMethod == PaymentMethod.GCashOnline)
        {
            if (paymentGateway != null && paymentGateway.IsConfigured)
            {
                var invoice = await paymentGateway.CreateGcashInvoiceAsync(
                    orderNumber,
                    totalAmount,
                    store.Slug,
                    store.Name,
                    orderPublicId,
                    customerName,
                    customerPhone,
                    customerEmail,
                    ct);

                if (invoice != null)
                {
                    checkoutUrl = invoice.CheckoutUrl;
                    paymentReference = invoice.InvoiceId;
                    paymentStatus = PaymentStatus.Pending;
                }
                else
                {
                    paymentStatus = PaymentStatus.Paid;
                    paidAtUtc = DateTime.UtcNow;
                    paymentReference = $"GCASH-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
                }
            }
            else
            {
                paymentStatus = PaymentStatus.Paid;
                paidAtUtc = DateTime.UtcNow;
                paymentReference = $"GCASH-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            }
        }

        var order = new Order
        {
            PublicId = orderPublicId,
            StoreId = storeId,
            CustomerType = customerType,
            CustomerUserId = customerUserId,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            OrderNumber = orderNumber,
            Status = OrderStatus.Pending,
            OrderType = request.OrderType,
            CustomerPhone = customerPhone,
            DeliveryAddress = deliveryAddress,
            DeliveryLandmark = deliveryLandmark,
            Subtotal = subtotal,
            PromoCodeSnapshot = promoCodeSnapshot,
            DiscountAmount = discountAmount,
            DeliveryFee = deliveryFee,
            TotalAmount = totalAmount,
            EstimatedDeliveryTime = estimatedDeliveryTime,
            Notes = request.Notes?.Trim() ?? string.Empty,
            PaymentMethod = paymentMethod,
            PaymentStatus = paymentStatus,
            CashChangeFor = request.CashChangeFor,
            PaidAtUtc = paidAtUtc,
            PaymentReference = paymentReference,
            PaymentCheckoutUrl = checkoutUrl,
            DeliveryPin = deliveryPin,
            Items = orderItems
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        var itemsById = menuItems.ToDictionary(m => m.Id, m => m.PublicId);
        return OrderResult.Success(new OrderResponse(
            order.PublicId,
            store.Slug,
            store.Name,
            order.OrderNumber,
            order.CustomerType,
            order.CustomerName,
            order.Status,
            order.OrderType,
            order.CustomerPhone,
            order.CustomerEmail,
            order.DeliveryAddress,
            order.DeliveryLandmark,
            order.Subtotal,
            order.PromoCodeSnapshot,
            order.DiscountAmount,
            order.DeliveryFee,
            order.TotalAmount,
            order.EstimatedDeliveryTime,
            order.Notes,
            order.CreatedAtUtc,
            order.Items.Select(i => new OrderItemResponse(
                itemsById[i.MenuItemId],
                i.ItemNameSnapshot,
                i.UnitPriceSnapshot,
                i.Quantity,
                i.UnitPriceSnapshot * i.Quantity,
                i.Options.Select(o => new OrderItemOptionResponse(
                    o.GroupNameSnapshot,
                    o.OptionNameSnapshot,
                    o.PriceDeltaSnapshot)).ToList())).ToList(),
            order.PaymentMethod,
            order.PaymentStatus,
            order.CashChangeFor,
            order.DriverName,
            order.DriverPhone,
            order.DispatchedAtUtc,
            order.DeliveredAtUtc,
            order.PaymentCollectedVia,
            order.PaidAtUtc,
            order.PaymentReference,
            order.DeliveryPin,
            order.ProofOfDeliveryImageUrl),
            customerUserId);
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

    public Task<OrderResult> UpdateStatusAsync(
        Guid publicId,
        OrderStatus newStatus,
        CancellationToken ct = default) =>
        UpdateStatusAsync(publicId, new UpdateOrderStatusRequest(newStatus), ct);

    public async Task<OrderResult> UpdateStatusAsync(
        Guid publicId,
        UpdateOrderStatusRequest request,
        CancellationToken ct = default)
    {
        var newStatus = request.Status;
        if (!Enum.IsDefined(newStatus))
        {
            return OrderResult.ValidationError("Invalid order status.");
        }

        var order = await db.Orders
            .Include(o => o.Store)
            .Include(o => o.Review)
            .Include(o => o.Items)
            .ThenInclude(i => i.MenuItem)
            .Include(o => o.Items)
            .ThenInclude(i => i.Options)
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

        if (newStatus == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled && !string.IsNullOrEmpty(order.PromoCodeSnapshot))
        {
            var promo = await db.PromoCodes
                .FirstOrDefaultAsync(p => p.StoreId == order.StoreId && p.Code == order.PromoCodeSnapshot, ct);
            if (promo != null && promo.UsedCount > 0)
            {
                promo.UsedCount--;
            }
        }

        // Critical: Completing a delivery order requires verifying the customer's 4-digit Delivery PIN
        // as proof-of-delivery, stamping DeliveredAtUtc, and marking payment as settled.
        if (newStatus == OrderStatus.Completed)
        {
            if (order.OrderType == OrderType.Delivery && !string.IsNullOrWhiteSpace(order.DeliveryPin))
            {
                var inputPin = request.DeliveryPin?.Trim();
                if (string.IsNullOrWhiteSpace(inputPin) || !string.Equals(inputPin, order.DeliveryPin, StringComparison.Ordinal))
                {
                    return OrderResult.ValidationError("Invalid delivery PIN. Please ask the customer for their 4-digit delivery PIN.");
                }
            }

            // Record actual completion timestamp
            order.DeliveredAtUtc = DateTime.UtcNow;

            // Settle payment status upon successful handoff
            if (request.PaymentCollectedVia.HasValue)
            {
                order.PaymentCollectedVia = request.PaymentCollectedVia.Value;
                order.PaymentStatus = PaymentStatus.Paid;
                order.PaidAtUtc ??= DateTime.UtcNow;
            }
            else if (order.PaymentStatus == PaymentStatus.Pending)
            {
                order.PaymentStatus = PaymentStatus.Paid;
                order.PaidAtUtc ??= DateTime.UtcNow;
            }
        }

        order.Status = newStatus;
        await db.SaveChangesAsync(ct);

        return OrderResult.Success(ToResponse(order), order.CustomerUserId);
    }

    /// <summary>
    /// Assigns a rider/driver to a delivery order, records the dispatch timestamp,
    /// and advances status to OutForDelivery.
    /// </summary>
    public async Task<OrderResult> AssignDriverAsync(
        Guid publicId,
        AssignDriverRequest request,
        CancellationToken ct = default)
    {
        var driverName = request.DriverName?.Trim();
        var driverPhone = request.DriverPhone?.Trim();

        if (string.IsNullOrWhiteSpace(driverName))
        {
            return OrderResult.ValidationError("Driver name is required.");
        }

        var order = await db.Orders
            .Include(o => o.Store)
            .Include(o => o.Review)
            .Include(o => o.Items)
            .ThenInclude(i => i.MenuItem)
            .Include(o => o.Items)
            .ThenInclude(i => i.Options)
            .FirstOrDefaultAsync(o => o.PublicId == publicId, ct);

        if (order is null)
        {
            return OrderResult.NotFound("Order was not found for this store.");
        }

        if (order.OrderType != OrderType.Delivery)
        {
            return OrderResult.ValidationError("Drivers can only be assigned to delivery orders.");
        }

        // Rider can be assigned while order is Preparing, Ready, or re-assigned if already OutForDelivery
        if (order.Status is not (OrderStatus.Preparing or OrderStatus.Ready or OrderStatus.OutForDelivery))
        {
            return OrderResult.ValidationError($"Cannot assign driver when order is in {order.Status} status.");
        }

        order.DriverName = driverName;
        order.DriverPhone = driverPhone;
        order.Status = OrderStatus.OutForDelivery;
        order.DispatchedAtUtc ??= DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return OrderResult.Success(ToResponse(order), order.CustomerUserId);
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
            .Include(o => o.Review)
            .Include(o => o.Items)
            .ThenInclude(i => i.MenuItem)
            .Include(o => o.Items)
            .ThenInclude(i => i.Options)
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
        if (!string.IsNullOrEmpty(order.PromoCodeSnapshot))
        {
            var promo = await db.PromoCodes
                .FirstOrDefaultAsync(p => p.StoreId == order.StoreId && p.Code == order.PromoCodeSnapshot, ct);
            if (promo != null && promo.UsedCount > 0)
            {
                promo.UsedCount--;
            }
        }
        await db.SaveChangesAsync(ct);

        return OrderResult.Success(ToResponse(order), order.CustomerUserId);
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

        return (current, next) switch
        {
            (OrderStatus.Pending, OrderStatus.Confirmed) => true,
            (OrderStatus.Confirmed, OrderStatus.Preparing) => true,
            (OrderStatus.Preparing, OrderStatus.Ready) => true,
            (OrderStatus.Preparing, OrderStatus.OutForDelivery) => true,
            (OrderStatus.Ready, OrderStatus.OutForDelivery) => true,
            (OrderStatus.Ready, OrderStatus.Completed) => true,
            (OrderStatus.OutForDelivery, OrderStatus.Completed) => true,
            _ => false
        };
    }

    private static (
        List<(string GroupName, string OptionName, decimal PriceDelta)> Options,
        string LineKey,
        string? Error) ValidateAndResolveOptions(
        Domain.Catalog.MenuItem menuItem,
        IReadOnlyList<Guid>? rawSelectedOptionIds)
    {
        var distinctRequestedIds = (rawSelectedOptionIds ?? [])
            .Distinct()
            .ToList();

        var optionLookup = menuItem.OptionGroups
            .SelectMany(g => g.Options.Select(o => new { Group = g, Option = o }))
            .ToDictionary(x => x.Option.PublicId);

        var selectedPairs = new List<(Domain.Catalog.MenuItemOptionGroup Group, Domain.Catalog.MenuItemOption Option)>();
        foreach (var optionPublicId in distinctRequestedIds)
        {
            if (!optionLookup.TryGetValue(optionPublicId, out var pair))
            {
                return ([], string.Empty, $"One or more selected options do not belong to '{menuItem.Name}'.");
            }

            selectedPairs.Add((pair.Group, pair.Option));
        }

        foreach (var group in menuItem.OptionGroups)
        {
            var countForGroup = selectedPairs.Count(p => p.Group.Id == group.Id);
            if (group.IsRequired && countForGroup == 0)
            {
                return ([], string.Empty, $"'{menuItem.Name}' requires choosing an option from '{group.Name}'.");
            }

            if (!group.AllowMultiple && countForGroup > 1)
            {
                return ([], string.Empty, $"Only one option may be selected from '{group.Name}' on '{menuItem.Name}'.");
            }
        }

        var orderedPairs = selectedPairs
            .OrderBy(p => p.Group.SortOrder)
            .ThenBy(p => p.Group.Name)
            .ThenBy(p => p.Option.SortOrder)
            .ThenBy(p => p.Option.Name)
            .ThenBy(p => p.Option.PublicId)
            .ToList();

        var lineKey = $"{menuItem.PublicId}:{string.Join(",", orderedPairs.Select(p => p.Option.PublicId))}";
        var resolved = orderedPairs
            .Select(p => (p.Group.Name, p.Option.Name, p.Option.PriceDelta))
            .ToList();

        return (resolved, lineKey, null);
    }

    private static string GenerateOrderNumber() =>
        $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    public async Task<OrderResult> GetOrderByPublicIdAsync(Guid publicId, CancellationToken ct = default)
    {
        var order = await db.Orders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(o => o.Store)
            .Include(o => o.Review)
            .Include(o => o.Items)
                .ThenInclude(i => i.MenuItem)
            .Include(o => o.Items)
                .ThenInclude(i => i.Options)
            .FirstOrDefaultAsync(o => o.PublicId == publicId, ct);

        if (order is null)
        {
            return OrderResult.NotFound("Order was not found.");
        }

        return OrderResult.Success(ToResponse(order), order.CustomerUserId);
    }

    private static IQueryable<OrderResponse> ProjectOrders(IQueryable<Order> query) =>
        query.Select(o => new OrderResponse(
            o.PublicId,
            o.Store.Slug,
            o.Store.Name,
            o.OrderNumber,
            o.CustomerType,
            o.CustomerName,
            o.Status,
            o.OrderType,
            o.CustomerPhone,
            o.CustomerEmail,
            o.DeliveryAddress,
            o.DeliveryLandmark,
            o.Subtotal,
            o.PromoCodeSnapshot,
            o.DiscountAmount,
            o.DeliveryFee,
            o.TotalAmount,
            o.EstimatedDeliveryTime,
            o.Notes,
            o.CreatedAtUtc,
            o.Items.Select(i => new OrderItemResponse(
                i.MenuItem.PublicId,
                i.ItemNameSnapshot,
                i.UnitPriceSnapshot,
                i.Quantity,
                i.UnitPriceSnapshot * i.Quantity,
                i.Options.Select(opt => new OrderItemOptionResponse(
                    opt.GroupNameSnapshot,
                    opt.OptionNameSnapshot,
                    opt.PriceDeltaSnapshot)).ToList())).ToList(),
            o.PaymentMethod,
            o.PaymentStatus,
            o.CashChangeFor,
            o.DriverName,
            o.DriverPhone,
            o.DispatchedAtUtc,
            o.DeliveredAtUtc,
            o.PaymentCollectedVia,
            o.PaidAtUtc,
            o.PaymentReference,
            o.DeliveryPin,
            o.ProofOfDeliveryImageUrl,
            o.PaymentCheckoutUrl,
            o.Review == null
                ? null
                : new OrderReviewResponse(
                    o.Review.PublicId,
                    o.Review.Rating,
                    o.Review.Comment,
                    o.Review.CustomerName,
                    o.Review.CreatedAtUtc)));

    private static OrderResponse ToResponse(Order order) =>
        new(
            order.PublicId,
            order.Store.Slug,
            order.Store.Name,
            order.OrderNumber,
            order.CustomerType,
            order.CustomerName,
            order.Status,
            order.OrderType,
            order.CustomerPhone,
            order.CustomerEmail,
            order.DeliveryAddress,
            order.DeliveryLandmark,
            order.Subtotal,
            order.PromoCodeSnapshot,
            order.DiscountAmount,
            order.DeliveryFee,
            order.TotalAmount,
            order.EstimatedDeliveryTime,
            order.Notes,
            order.CreatedAtUtc,
            order.Items.Select(i => new OrderItemResponse(
                i.MenuItem.PublicId,
                i.ItemNameSnapshot,
                i.UnitPriceSnapshot,
                i.Quantity,
                i.UnitPriceSnapshot * i.Quantity,
                i.Options.Select(opt => new OrderItemOptionResponse(
                    opt.GroupNameSnapshot,
                    opt.OptionNameSnapshot,
                    opt.PriceDeltaSnapshot)).ToList())).ToList(),
            order.PaymentMethod,
            order.PaymentStatus,
            order.CashChangeFor,
            order.DriverName,
            order.DriverPhone,
            order.DispatchedAtUtc,
            order.DeliveredAtUtc,
            order.PaymentCollectedVia,
            order.PaidAtUtc,
            order.PaymentReference,
            order.DeliveryPin,
            order.ProofOfDeliveryImageUrl,
            order.PaymentCheckoutUrl,
            order.Review == null
                ? null
                : new OrderReviewResponse(
                    order.Review.PublicId,
                    order.Review.Rating,
                    order.Review.Comment,
                    order.Review.CustomerName,
                    order.Review.CreatedAtUtc));
}
