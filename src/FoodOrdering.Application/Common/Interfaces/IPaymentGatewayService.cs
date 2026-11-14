namespace FoodOrdering.Application.Common.Interfaces;

public sealed record CreatePaymentInvoiceResult(
    string InvoiceId,
    string CheckoutUrl);

/// <summary>
/// Service to create external payment gateway invoices/checkout sessions (e.g. Xendit GCash).
/// </summary>
public interface IPaymentGatewayService
{
    bool IsConfigured { get; }

    Task<CreatePaymentInvoiceResult?> CreateGcashInvoiceAsync(
        string orderNumber,
        decimal amount,
        string storeSlug,
        string storeName,
        Guid orderPublicId,
        string customerName,
        string customerPhone,
        string? customerEmail,
        CancellationToken ct = default);
}
