using FoodOrdering.Application.Auth;
using FoodOrdering.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Infrastructure.Identity;

public sealed class IdentityAuthService(
    UserManager<AppUser> userManager,
    IUserDirectory userDirectory,
    IAppDbContext db,
    JwtTokenGenerator tokenGenerator) : IAuthService
{
    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var fullName = request.FullName.Trim();
        var email = request.Email.Trim();

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
        {
            return AuthResult.ValidationError("FullName and Email are required.");
        }

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return AuthResult.Conflict($"An account with email '{email}' already exists.");
        }

        var (createdUser, errors) = await userDirectory.CreateCustomerAsync(
            email,
            fullName,
            request.Password,
            ct);

        if (createdUser is null)
        {
            return AuthResult.ValidationError(errors.ToArray());
        }

        var appUser = await userManager.FindByIdAsync(createdUser.Id.ToString());
        if (appUser is null)
        {
            return AuthResult.ValidationError("Failed to load created user.");
        }

        return await BuildAuthSuccessAsync(appUser, ct);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim();
        var user = await userManager.FindByEmailAsync(email);

        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return AuthResult.Unauthorized("Invalid email or password.");
        }

        return await BuildAuthSuccessAsync(user, ct);
    }

    public async Task<UserInfoResponse?> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var managedStores = await GetManagedStoresAsync(user.Id, ct);

        return new UserInfoResponse(
            user.Id,
            user.FullName,
            user.Email ?? string.Empty,
            roles,
            managedStores);
    }

    private async Task<AuthResult> BuildAuthSuccessAsync(AppUser user, CancellationToken ct)
    {
        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var (token, expiresAtUtc) = tokenGenerator.Generate(user, roles);
        var managedStores = await GetManagedStoresAsync(user.Id, ct);

        var userInfo = new UserInfoResponse(
            user.Id,
            user.FullName,
            user.Email ?? string.Empty,
            roles,
            managedStores);

        return AuthResult.Success(new AuthResponse(token, expiresAtUtc, userInfo));
    }

    private async Task<IReadOnlyList<ManagedStoreResponse>> GetManagedStoresAsync(
        Guid userId,
        CancellationToken ct)
    {
        return await db.StoreMembers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.Store.Name)
            .Select(m => new ManagedStoreResponse(
                m.Store.PublicId,
                m.Store.Slug,
                m.Store.Name,
                m.Role))
            .ToListAsync(ct);
    }
}
