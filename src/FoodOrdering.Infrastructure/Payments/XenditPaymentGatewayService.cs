using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodOrdering.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodOrdering.Infrastructure.Payments;

public sealed class XenditPaymentGatewayService(
    HttpClient httpClient,
    IOptions<XenditOptions> options,
    ILogger<XenditPaymentGatewayService> logger) : IPaymentGatewayService
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.SecretKey);

    public async Task<CreatePaymentInvoiceResult?> CreateGcashInvoiceAsync(
        string orderNumber,
        decimal amount,
        string storeSlug,
        string storeName,
        Guid orderPublicId,
        string customerName,
        string customerPhone,
        string? customerEmail,
        CancellationToken ct = default)
    {
        var secretKey = options.Value.SecretKey?.Trim();
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return null;
        }

        var webBaseUrl = string.IsNullOrWhiteSpace(options.Value.WebBaseUrl)
            ? "https://food-ordering-web.pages.dev"
            : options.Value.WebBaseUrl.Trim().TrimEnd('/');

        var successRedirect = $"{webBaseUrl}/{storeSlug}/orders/{orderPublicId}?payment=success";
        var failureRedirect = $"{webBaseUrl}/{storeSlug}/orders/{orderPublicId}?payment=failed";

        var payload = new
        {
            external_id = orderNumber,
            amount = amount,
            description = $"Payment for Order #{orderNumber} at {storeName}",
            payer_email = !string.IsNullOrWhiteSpace(customerEmail) && customerEmail.Contains('@')
                ? customerEmail.Trim()
                : "customer@foodordering.com",
            currency = "PHP",
            payment_methods = new[] { "GCASH", "PAYMAYA" },
            customer = new
            {
                given_names = !string.IsNullOrWhiteSpace(customerName) ? customerName.Trim() : "Customer",
                mobile_number = FormatPhPhone(customerPhone)
            },
            success_redirect_url = successRedirect,
            failure_redirect_url = failureRedirect
        };

        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.xendit.co/v2/invoices")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var basicAuthToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuthToken);

        try
        {
            logger.LogInformation("Creating Xendit GCash Invoice for Order #{OrderNumber} (Amount: {Amount})", orderNumber, amount);

            var response = await httpClient.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Xendit Invoice creation failed ({StatusCode}): {Body}", response.StatusCode, responseBody);
                return null;
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            var invoiceId = root.GetProperty("id").GetString();
            var invoiceUrl = root.GetProperty("invoice_url").GetString();

            if (string.IsNullOrWhiteSpace(invoiceId) || string.IsNullOrWhiteSpace(invoiceUrl))
            {
                logger.LogWarning("Xendit Invoice response missing id or invoice_url");
                return null;
            }

            logger.LogInformation("Xendit Invoice created: ID={InvoiceId}, URL={InvoiceUrl}", invoiceId, invoiceUrl);
            return new CreatePaymentInvoiceResult(invoiceId, invoiceUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating Xendit invoice for Order #{OrderNumber}", orderNumber);
            return null;
        }
    }

    private static string FormatPhPhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("09") && digits.Length == 11)
        {
            return $"+63{digits[1..]}";
        }
        if (digits.StartsWith("639") && digits.Length == 12)
        {
            return $"+{digits}";
        }
        if (digits.StartsWith("9") && digits.Length == 10)
        {
            return $"+63{digits}";
        }
        return phone;
    }
}
