using FoodOrdering.Domain.Promos;

namespace FoodOrdering.Application.Promos;

public sealed record CreatePromoCodeRequest(
    string Code,
    PromoDiscountType DiscountType,
    decimal DiscountValue,
    int? MaxUses = null,
    DateTime? StartsAtUtc = null,
    DateTime? ExpiresAtUtc = null);

public sealed record ValidatePromoRequest(
    string Code,
    decimal Subtotal);

public sealed record PromoCodeResponse(
    Guid PublicId,
    string Code,
    PromoDiscountType DiscountType,
    decimal DiscountValue,
    int? MaxUses,
    int UsedCount,
    DateTime? StartsAtUtc,
    DateTime? ExpiresAtUtc,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record ValidatePromoResponse(
    bool IsValid,
    string Code,
    PromoDiscountType DiscountType,
    decimal DiscountValue,
    decimal DiscountAmount,
    string? Error = null)
{
    public string? Message => Error;
}

public sealed record PromoResult(
    PromoCodeResponse? Promo,
    string? Error,
    bool IsNotFound = false,
    bool IsConflict = false)
{
    public static PromoResult Success(PromoCodeResponse promo) => new(promo, null);
    public static PromoResult ValidationError(string error) => new(null, error);
    public static PromoResult NotFound(string error) => new(null, error, IsNotFound: true);
    public static PromoResult Conflict(string error) => new(null, error, IsConflict: true);
}
