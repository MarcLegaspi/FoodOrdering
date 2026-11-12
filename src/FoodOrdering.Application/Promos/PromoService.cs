using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Domain.Orders;
using FoodOrdering.Domain.Promos;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Application.Promos;

public sealed class PromoService(
    IAppDbContext db,
    ITenantContext tenantContext,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<PromoCodeResponse>> GetStorePromosAsync(CancellationToken ct = default)
    {
        var promos = await db.PromoCodes
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(ct);

        return promos.Select(ToResponse).ToList();
    }

    public async Task<PromoResult> CreatePromoAsync(CreatePromoCodeRequest request, CancellationToken ct = default)
    {
        if (tenantContext.StoreId is not long storeId)
        {
            return PromoResult.ValidationError("Store tenant context is required.");
        }

        var normalizedCode = NormalizeCode(request.Code);
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return PromoResult.ValidationError("Promo code is required.");
        }

        if (normalizedCode.Length > 32)
        {
            return PromoResult.ValidationError("Promo code cannot exceed 32 characters.");
        }

        if (!Enum.IsDefined(request.DiscountType))
        {
            return PromoResult.ValidationError("Invalid discount type.");
        }

        if (request.DiscountValue <= 0)
        {
            return PromoResult.ValidationError("Discount value must be greater than zero.");
        }

        if (request.DiscountType == PromoDiscountType.Percentage && request.DiscountValue > 100)
        {
            return PromoResult.ValidationError("Percentage discount cannot exceed 100%.");
        }

        if (request.MaxUses is { } maxUses && maxUses <= 0)
        {
            return PromoResult.ValidationError("Maximum claims must be greater than zero.");
        }

        if (request.StartsAtUtc.HasValue && request.ExpiresAtUtc.HasValue && request.StartsAtUtc > request.ExpiresAtUtc)
        {
            return PromoResult.ValidationError("Promo start date cannot be later than expiration date.");
        }

        var exists = await db.PromoCodes
            .AnyAsync(p => p.Code == normalizedCode, ct);

        if (exists)
        {
            return PromoResult.Conflict($"Promo code '{normalizedCode}' already exists for this store.");
        }

        var promo = new PromoCode
        {
            StoreId = storeId,
            Code = normalizedCode,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MaxUses = request.MaxUses,
            StartsAtUtc = request.StartsAtUtc,
            ExpiresAtUtc = request.ExpiresAtUtc,
            IsActive = true,
            UsedCount = 0
        };

        db.PromoCodes.Add(promo);
        await db.SaveChangesAsync(ct);

        return PromoResult.Success(ToResponse(promo));
    }

    public async Task<PromoResult> TogglePromoStatusAsync(Guid publicId, CancellationToken ct = default)
    {
        var promo = await db.PromoCodes
            .FirstOrDefaultAsync(p => p.PublicId == publicId, ct);

        if (promo is null)
        {
            return PromoResult.NotFound("Promo code was not found.");
        }

        promo.IsActive = !promo.IsActive;
        await db.SaveChangesAsync(ct);

        return PromoResult.Success(ToResponse(promo));
    }

    public async Task<bool> DeletePromoAsync(Guid publicId, CancellationToken ct = default)
    {
        var promo = await db.PromoCodes
            .FirstOrDefaultAsync(p => p.PublicId == publicId, ct);

        if (promo is null)
        {
            return false;
        }

        db.PromoCodes.Remove(promo);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ValidatePromoResponse> ValidatePromoAsync(string rawCode, decimal subtotal, CancellationToken ct = default)
    {
        var code = NormalizeCode(rawCode);
        if (string.IsNullOrWhiteSpace(code))
        {
            return new ValidatePromoResponse(false, string.Empty, PromoDiscountType.FixedAmount, 0, 0, "Promo code is required.");
        }

        if (!currentUser.IsAuthenticated)
        {
            return new ValidatePromoResponse(false, code, PromoDiscountType.FixedAmount, 0, 0, "Promo codes are only available for registered members. Please sign in to apply.");
        }

        var promo = await db.PromoCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == code, ct);

        if (promo is null || !promo.IsActive)
        {
            return new ValidatePromoResponse(false, code, PromoDiscountType.FixedAmount, 0, 0, "Invalid or inactive promo code.");
        }

        if (promo.StartsAtUtc is { } startsAt && DateTime.UtcNow < startsAt)
        {
            return new ValidatePromoResponse(false, code, promo.DiscountType, promo.DiscountValue, 0, $"This promo code is not active yet. It will be valid starting {startsAt:MMM d, yyyy h:mm tt} UTC.");
        }

        if (promo.ExpiresAtUtc is { } expiry && DateTime.UtcNow > expiry)
        {
            return new ValidatePromoResponse(false, code, promo.DiscountType, promo.DiscountValue, 0, "This promo code has expired.");
        }

        if (promo.MaxUses is { } maxUses && promo.UsedCount >= maxUses)
        {
            return new ValidatePromoResponse(false, code, promo.DiscountType, promo.DiscountValue, 0, "This promo code has reached its maximum claim limit.");
        }

        if (currentUser.UserId is { } customerUserId)
        {
            var alreadyUsed = await db.Orders
                .AsNoTracking()
                .AnyAsync(o => o.CustomerUserId == customerUserId
                            && o.PromoCodeSnapshot == promo.Code
                            && o.Status != OrderStatus.Cancelled, ct);

            if (alreadyUsed)
            {
                return new ValidatePromoResponse(false, code, promo.DiscountType, promo.DiscountValue, 0, "You have already used this promo code on a previous order.");
            }
        }

        var discount = CalculateDiscount(promo.DiscountType, promo.DiscountValue, subtotal);
        return new ValidatePromoResponse(true, promo.Code, promo.DiscountType, promo.DiscountValue, discount);
    }

    public static decimal CalculateDiscount(PromoDiscountType type, decimal value, decimal subtotal)
    {
        if (subtotal <= 0 || value <= 0)
        {
            return 0m;
        }

        var discount = type switch
        {
            PromoDiscountType.Percentage => Math.Round(subtotal * (value / 100m), 2),
            PromoDiscountType.FixedAmount => value,
            _ => 0m
        };

        return Math.Min(subtotal, Math.Max(0m, discount));
    }

    private static string NormalizeCode(string code) =>
        code?.Trim().ToUpperInvariant() ?? string.Empty;

    private static PromoCodeResponse ToResponse(PromoCode p) =>
        new(
            p.PublicId,
            p.Code,
            p.DiscountType,
            p.DiscountValue,
            p.MaxUses,
            p.UsedCount,
            p.StartsAtUtc,
            p.ExpiresAtUtc,
            p.IsActive,
            p.CreatedAtUtc);
}
