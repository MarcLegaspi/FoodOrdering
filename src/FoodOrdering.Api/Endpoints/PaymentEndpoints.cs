using System.Text.Json;
using System.Text.Json.Serialization;
using FoodOrdering.Api.Hubs;
using FoodOrdering.Application.Orders;
using FoodOrdering.Domain.Orders;
using FoodOrdering.Infrastructure.Database;
using FoodOrdering.Infrastructure.Payments;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FoodOrdering.Api.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments")
            .WithTags("Payments");

        // Public diagnostic status endpoint to verify payment gateway configuration
        group.MapGet("/status", (IOptions<XenditOptions> xenditOpts) =>
        {
            var key = xenditOpts.Value.SecretKey?.Trim();
            var isConfigured = !string.IsNullOrWhiteSpace(key);
            string maskedKey;
            if (isConfigured && key != null)
            {
                var takeStart = Math.Min(8, key.Length);
                var takeEnd = Math.Min(4, key.Length);
                maskedKey = key.Length > 12
                    ? $"{key[..takeStart]}...{key[^takeEnd..]}"
                    : $"{key[..Math.Min(4, key.Length)]}***";
            }
            else
            {
                maskedKey = "Not Configured";
            }

            return Results.Ok(new
            {
                configured = isConfigured,
                gateway = "Xendit",
                keyPreview = maskedKey,
                webBaseUrl = xenditOpts.Value.WebBaseUrl
            });
        })
        .AllowAnonymous();

        // Webhook receiver for Xendit payment updates
        group.MapPost("/xendit-webhook", async (
            HttpContext context,
            AppDbContext db,
            IHubContext<OrderHub> hubContext,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("XenditWebhook");

            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync(ct);

            logger.LogInformation("Received Xendit Webhook: {Payload}", body);

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                var status = root.TryGetProperty("status", out var s) ? s.GetString() : null;
                var externalId = root.TryGetProperty("external_id", out var ext) ? ext.GetString() : null;
                var invoiceId = root.TryGetProperty("id", out var id) ? id.GetString() : null;

                if (string.Equals(status, "PAID", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(status, "SETTLED", StringComparison.OrdinalIgnoreCase))
                {
                    var order = await db.Orders
                        .Include(o => o.Store)
                        .Include(o => o.Items)
                            .ThenInclude(i => i.MenuItem)
                        .Include(o => o.Items)
                            .ThenInclude(i => i.Options)
                        .FirstOrDefaultAsync(o =>
                            (externalId != null && o.OrderNumber == externalId) ||
                            (invoiceId != null && o.PaymentReference == invoiceId), ct);

                    if (order != null)
                    {
                        order.PaymentStatus = PaymentStatus.Paid;
                        order.PaidAtUtc ??= DateTime.UtcNow;
                        await db.SaveChangesAsync(ct);

                        logger.LogInformation("Order #{OrderNumber} marked as PAID via Xendit webhook", order.OrderNumber);

                        var response = new OrderResponse(
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
                            order.PaymentCheckoutUrl);

                        // Broadcast real-time live notification to kitchen and customer tracking
                        await hubContext.Clients
                            .Group(OrderHub.StoreGroup(order.Store.Slug))
                            .SendAsync("OrderUpdated", response, ct);

                        await hubContext.Clients
                            .Group(OrderHub.OrderGroup(order.PublicId))
                            .SendAsync("OrderUpdated", response, ct);
                    }
                }

                return Results.Ok(new { message = "Webhook processed successfully" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process Xendit webhook payload");
                return Results.Ok(new { message = "Error logged" });
            }
        })
        .AllowAnonymous();

        return app;
    }
}
