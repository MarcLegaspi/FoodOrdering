using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Domain.Stores;
using FoodOrdering.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.UnitTests;

internal sealed class TestTenantContext(long? storeId = null, string? storeSlug = null) : ITenantContext
{
    public long? StoreId { get; set; } = storeId;

    public string? StoreSlug { get; set; } = storeSlug;
}

internal sealed class TestCurrentUser(
    Guid? userId = null,
    bool isAuthenticated = false,
    params string[] roles) : ICurrentUser
{
    private readonly HashSet<string> _roles = new(roles, StringComparer.OrdinalIgnoreCase);

    public Guid? UserId { get; set; } = userId;

    public bool IsAuthenticated { get; set; } = isAuthenticated;

    public bool IsInRole(string role) => _roles.Contains(role);
}

internal sealed class FakeUserDirectory : IUserDirectory
{
    private readonly Dictionary<string, UserSummary> _byEmail = new(StringComparer.OrdinalIgnoreCase);

    public UserSummary AddExisting(string email, string fullName, Guid? id = null)
    {
        var summary = new UserSummary(id ?? Guid.NewGuid(), fullName, email);
        _byEmail[email] = summary;
        return summary;
    }

    public Task<UserSummary?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        _byEmail.TryGetValue(email.Trim(), out var user);
        return Task.FromResult(user);
    }

    public Task<IReadOnlyDictionary<Guid, UserSummary>> GetByIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken ct = default)
    {
        var result = _byEmail.Values
            .Where(u => userIds.Contains(u.Id))
            .ToDictionary(u => u.Id);

        return Task.FromResult<IReadOnlyDictionary<Guid, UserSummary>>(result);
    }

    public Task<(UserSummary? User, IReadOnlyList<string> Errors)> CreateCustomerAsync(
        string email,
        string fullName,
        string password,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult<(UserSummary?, IReadOnlyList<string>)>(
                (null, ["Password is required."]));
        }

        var summary = new UserSummary(Guid.NewGuid(), fullName.Trim(), email.Trim());
        _byEmail[summary.Email] = summary;
        return Task.FromResult<(UserSummary?, IReadOnlyList<string>)>((summary, []));
    }
}

internal static class TestDbFactory
{
    public static AppDbContext Create(ITenantContext tenantContext, string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, tenantContext);
    }

    public static async Task<(Store First, Store Second)> SeedTwoStoresAsync(AppDbContext db)
    {
        var first = new Store { Id = 1, Slug = "burger-barn", Name = "Burger Barn" };
        var second = new Store { Id = 2, Slug = "pizza-palace", Name = "Pizza Palace" };
        db.Stores.AddRange(first, second);
        await db.SaveChangesAsync();
        return (first, second);
    }
}
