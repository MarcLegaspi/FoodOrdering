namespace FoodOrdering.Infrastructure.Payments;

public sealed class XenditOptions
{
    public const string SectionName = "Xendit";

    /// <summary>
    /// Xendit API Secret Key (e.g. "xnd_development_...").
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// Optional webhook verification token configured in Xendit dashboard.
    /// </summary>
    public string? WebhookToken { get; set; }

    /// <summary>
    /// Base URL of frontend Angular application for redirect URLs.
    /// </summary>
    public string WebBaseUrl { get; set; } = "https://food-ordering-web.pages.dev";
}
