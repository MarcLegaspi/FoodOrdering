namespace FoodOrdering.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "FoodOrdering.Api";

    public string Audience { get; init; } = "FoodOrdering.Web";

    public string SecretKey { get; init; } = string.Empty;

    public int ExpirationMinutes { get; init; } = 60;
}
