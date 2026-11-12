namespace FoodOrdering.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid? UserId { get; }

    string? Email { get; }

    bool IsAuthenticated { get; }

    bool IsInRole(string role);
}
