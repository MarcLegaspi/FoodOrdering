using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Infrastructure.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Api.Middleware;

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Store-Slug";

    public async Task InvokeAsync(
        HttpContext context,
        IAppDbContext db,
        TenantContext tenantContext)
    {
        var rawSlug = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? context.Request.Query["storeSlug"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(rawSlug))
        {
            var slug = rawSlug.Trim().ToLowerInvariant();

            var store = await db.Stores
                .AsNoTracking()
                .Where(s => s.Slug == slug && s.IsActive)
                .Select(s => new { s.Id, s.Slug })
                .FirstOrDefaultAsync(context.RequestAborted);

            if (store is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsJsonAsync(
                    new { error = $"Active store '{slug}' was not found." },
                    context.RequestAborted);
                return;
            }

            tenantContext.SetTenant(store.Id, store.Slug);
        }

        await next(context);
    }
}
