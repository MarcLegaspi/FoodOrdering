using FoodOrdering.Domain.Stores;

namespace FoodOrdering.Application.Stores;

public sealed record AddStoreMemberRequest(
    string Email,
    StoreRole Role = StoreRole.Staff,
    string? FullName = null,
    string? Password = null);

public sealed record StoreMemberResponse(
    Guid UserId,
    string FullName,
    string Email,
    StoreRole Role,
    DateTime CreatedAtUtc);

public sealed record AddStoreMemberResult(
    StoreMemberResponse? Member,
    IReadOnlyList<string> Errors,
    bool IsConflict = false,
    bool IsForbidden = false)
{
    public static AddStoreMemberResult Created(StoreMemberResponse member) => new(member, []);
    public static AddStoreMemberResult ValidationError(params string[] errors) => new(null, errors);
    public static AddStoreMemberResult Conflict(string error) => new(null, [error], IsConflict: true);
    public static AddStoreMemberResult Forbidden(string error) => new(null, [error], IsForbidden: true);
}

public sealed record RemoveStoreMemberResult(
    bool Succeeded,
    string? Error,
    bool IsNotFound = false,
    bool IsForbidden = false)
{
    public static RemoveStoreMemberResult Removed() => new(true, null);
    public static RemoveStoreMemberResult NotFound(string error) => new(false, error, IsNotFound: true);
    public static RemoveStoreMemberResult Forbidden(string error) => new(false, error, IsForbidden: true);
    public static RemoveStoreMemberResult ValidationError(string error) => new(false, error);
}
