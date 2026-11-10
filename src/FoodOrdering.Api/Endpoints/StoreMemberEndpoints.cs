using FoodOrdering.Api.Filters;
using FoodOrdering.Application.Stores;
using FoodOrdering.Domain.Stores;

namespace FoodOrdering.Api.Endpoints;

public static class StoreMemberEndpoints
{
    public static IEndpointRouteBuilder MapStoreMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/store-members")
            .WithTags("Store Members");

        group.MapGet("/", async (StoreMemberService memberService, CancellationToken ct) =>
        {
            var members = await memberService.GetAllAsync(ct);
            return Results.Ok(members);
        })
        .RequireStoreRole(StoreRole.Manager);

        group.MapPost("/", async (
            AddStoreMemberRequest request,
            StoreMemberService memberService,
            CancellationToken ct) =>
        {
            var result = await memberService.AddAsync(request, ct);

            if (result.IsForbidden)
            {
                return Results.Forbid();
            }

            if (result.IsConflict)
            {
                return Results.Conflict(new { errors = result.Errors });
            }

            if (result.Errors.Count > 0 || result.Member is null)
            {
                return Results.BadRequest(new { errors = result.Errors });
            }

            return Results.Created($"/api/store-members/{result.Member.UserId}", result.Member);
        })
        .RequireStoreRole(StoreRole.Manager);

        group.MapDelete("/{userId:guid}", async (
            Guid userId,
            StoreMemberService memberService,
            CancellationToken ct) =>
        {
            var result = await memberService.RemoveAsync(userId, ct);

            if (result.IsForbidden)
            {
                return Results.Forbid();
            }

            if (result.IsNotFound)
            {
                return Results.NotFound(new { error = result.Error });
            }

            if (!result.Succeeded)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            return Results.NoContent();
        })
        .RequireStoreRole(StoreRole.Manager);

        return app;
    }
}
