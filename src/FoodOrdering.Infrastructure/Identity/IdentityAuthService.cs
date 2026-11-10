using FoodOrdering.Application.Auth;
using Microsoft.AspNetCore.Identity;

namespace FoodOrdering.Infrastructure.Identity;

public sealed class IdentityAuthService(
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    JwtTokenGenerator tokenGenerator) : IAuthService
{
    private const string DefaultRole = "Customer";

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

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FullName = fullName
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return AuthResult.ValidationError(
                createResult.Errors.Select(e => e.Description).ToArray());
        }

        if (!await roleManager.RoleExistsAsync(DefaultRole))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(DefaultRole));
        }

        await userManager.AddToRoleAsync(user, DefaultRole);

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var (token, expiresAtUtc) = tokenGenerator.Generate(user, roles);

        var userInfo = new UserInfoResponse(user.Id, user.FullName, user.Email!, roles);
        return AuthResult.Success(new AuthResponse(token, expiresAtUtc, userInfo));
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim();
        var user = await userManager.FindByEmailAsync(email);

        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return AuthResult.Unauthorized("Invalid email or password.");
        }

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var (token, expiresAtUtc) = tokenGenerator.Generate(user, roles);

        var userInfo = new UserInfoResponse(user.Id, user.FullName, user.Email!, roles);
        return AuthResult.Success(new AuthResponse(token, expiresAtUtc, userInfo));
    }

    public async Task<UserInfoResponse?> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        return new UserInfoResponse(user.Id, user.FullName, user.Email ?? string.Empty, roles);
    }
}
