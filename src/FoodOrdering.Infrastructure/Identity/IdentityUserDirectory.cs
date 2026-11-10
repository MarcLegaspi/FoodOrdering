using FoodOrdering.Application.Auth;
using FoodOrdering.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Infrastructure.Identity;

public sealed class IdentityUserDirectory(
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager) : IUserDirectory
{
    public async Task<UserSummary?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        return user is null
            ? null
            : new UserSummary(user.Id, user.FullName, user.Email ?? string.Empty);
    }

    public async Task<IReadOnlyDictionary<Guid, UserSummary>> GetByIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken ct = default)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, UserSummary>();
        }

        return await userManager.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(
                u => u.Id,
                u => new UserSummary(u.Id, u.FullName, u.Email ?? string.Empty),
                ct);
    }

    public async Task<(UserSummary? User, IReadOnlyList<string> Errors)> CreateCustomerAsync(
        string email,
        string fullName,
        string password,
        CancellationToken ct = default)
    {
        var trimmedEmail = email.Trim();
        var trimmedName = fullName.Trim();

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = trimmedEmail,
            Email = trimmedEmail,
            FullName = trimmedName
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            return (null, createResult.Errors.Select(e => e.Description).ToArray());
        }

        await EnsureRoleExistsAsync(roleManager, AppRoles.Customer);
        await userManager.AddToRoleAsync(user, AppRoles.Customer);

        return (new UserSummary(user.Id, user.FullName, user.Email!), []);
    }

    internal static async Task EnsureRoleExistsAsync(RoleManager<IdentityRole<Guid>> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        }
    }
}
