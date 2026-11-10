namespace FoodOrdering.Application.Auth;

public sealed record RegisterRequest(
    string FullName,
    string Email,
    string Password);

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record UserInfoResponse(
    Guid Id,
    string FullName,
    string Email,
    IReadOnlyList<string> Roles);

public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    UserInfoResponse User);

public sealed record AuthResult(
    AuthResponse? Data,
    IReadOnlyList<string> Errors,
    bool IsConflict = false,
    bool IsUnauthorized = false)
{
    public static AuthResult Success(AuthResponse data) => new(data, []);
    public static AuthResult ValidationError(params string[] errors) => new(null, errors);
    public static AuthResult Conflict(string error) => new(null, [error], IsConflict: true);
    public static AuthResult Unauthorized(string error) => new(null, [error], IsUnauthorized: true);
}
