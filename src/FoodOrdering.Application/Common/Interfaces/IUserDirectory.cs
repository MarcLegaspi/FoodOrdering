namespace FoodOrdering.Application.Common.Interfaces;

public sealed record UserSummary(Guid Id, string FullName, string Email);

public interface IUserDirectory
{
    Task<UserSummary?> FindByEmailAsync(string email, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, UserSummary>> GetByIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken ct = default);

    Task<(UserSummary? User, IReadOnlyList<string> Errors)> CreateCustomerAsync(
        string email,
        string fullName,
        string password,
        CancellationToken ct = default);
}
