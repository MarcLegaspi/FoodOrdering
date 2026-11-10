using System.Security.Claims;
using FoodOrdering.Application.Auth;

namespace FoodOrdering.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            IAuthService authService,
            CancellationToken ct) =>
        {
            var result = await authService.RegisterAsync(request, ct);

            if (result.IsConflict)
            {
                return Results.Conflict(new { errors = result.Errors });
            }

            if (result.Errors.Count > 0 || result.Data is null)
            {
                return Results.BadRequest(new { errors = result.Errors });
            }

            return Results.Created($"/api/auth/me", result.Data);
        });

        group.MapPost("/login", async (
            LoginRequest request,
            IAuthService authService,
            CancellationToken ct) =>
        {
            var result = await authService.LoginAsync(request, ct);

            if (result.IsUnauthorized || result.Data is null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(result.Data);
        });

        group.MapGet("/me", async (
            ClaimsPrincipal principal,
            IAuthService authService,
            CancellationToken ct) =>
        {
            var rawUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(rawUserId, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await authService.GetCurrentUserAsync(userId, ct);
            return user is null ? Results.Unauthorized() : Results.Ok(user);
        })
        .RequireAuthorization();

        return app;
    }
}
