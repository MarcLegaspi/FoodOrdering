using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Domain.Stores;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Application.Stores;

public sealed class StoreMemberService(
    IAppDbContext db,
    ITenantContext tenantContext,
    StoreAccessService storeAccess,
    IUserDirectory userDirectory)
{
    public async Task<IReadOnlyList<StoreMemberResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var members = await db.StoreMembers
            .AsNoTracking()
            .OrderByDescending(m => m.Role)
            .ThenBy(m => m.CreatedAtUtc)
            .ToListAsync(ct);

        var users = await userDirectory.GetByIdsAsync(
            members.Select(m => m.UserId).Distinct().ToList(),
            ct);

        return members
            .Select(m =>
            {
                users.TryGetValue(m.UserId, out var user);
                return new StoreMemberResponse(
                    m.UserId,
                    user?.FullName ?? string.Empty,
                    user?.Email ?? string.Empty,
                    m.Role,
                    m.CreatedAtUtc);
            })
            .ToList();
    }

    public async Task<AddStoreMemberResult> AddAsync(AddStoreMemberRequest request, CancellationToken ct = default)
    {
        if (tenantContext.StoreId is null)
        {
            return AddStoreMemberResult.ValidationError("Store tenant context is required.");
        }

        var callerRole = await storeAccess.GetCurrentUserRoleAsync(ct);
        if (callerRole is null || callerRole < StoreRole.Manager)
        {
            return AddStoreMemberResult.Forbidden("Only a Store Manager or Owner can add store members.");
        }

        if (!Enum.IsDefined(request.Role))
        {
            return AddStoreMemberResult.ValidationError("Invalid store role.");
        }

        if (request.Role > callerRole)
        {
            return AddStoreMemberResult.Forbidden("You cannot assign a store role higher than your own.");
        }

        var email = request.Email.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return AddStoreMemberResult.ValidationError("Email is required.");
        }

        var user = await userDirectory.FindByEmailAsync(email, ct);
        if (user is null)
        {
            if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Password))
            {
                return AddStoreMemberResult.ValidationError(
                    $"User '{email}' does not exist yet. Provide FullName and Password to create their account.");
            }

            var (createdUser, errors) = await userDirectory.CreateCustomerAsync(
                email,
                request.FullName,
                request.Password,
                ct);

            if (createdUser is null)
            {
                return AddStoreMemberResult.ValidationError(errors.ToArray());
            }

            user = createdUser;
        }

        var alreadyMember = await db.StoreMembers.AnyAsync(m => m.UserId == user.Id, ct);
        if (alreadyMember)
        {
            return AddStoreMemberResult.Conflict($"'{user.Email}' is already a member of this store.");
        }

        var member = new StoreMember
        {
            StoreId = tenantContext.StoreId.Value,
            UserId = user.Id,
            Role = request.Role
        };

        db.StoreMembers.Add(member);
        await db.SaveChangesAsync(ct);

        return AddStoreMemberResult.Created(
            new StoreMemberResponse(user.Id, user.FullName, user.Email, member.Role, member.CreatedAtUtc));
    }

    public async Task<RemoveStoreMemberResult> RemoveAsync(Guid userId, CancellationToken ct = default)
    {
        var callerRole = await storeAccess.GetCurrentUserRoleAsync(ct);
        if (callerRole is null || callerRole < StoreRole.Manager)
        {
            return RemoveStoreMemberResult.Forbidden("Only a Store Manager or Owner can remove store members.");
        }

        var member = await db.StoreMembers.FirstOrDefaultAsync(m => m.UserId == userId, ct);
        if (member is null)
        {
            return RemoveStoreMemberResult.NotFound("Store member not found.");
        }

        if (member.Role > callerRole)
        {
            return RemoveStoreMemberResult.Forbidden("You cannot remove a store member with a higher role.");
        }

        if (member.Role == StoreRole.Owner)
        {
            var ownerCount = await db.StoreMembers.CountAsync(m => m.Role == StoreRole.Owner, ct);
            if (ownerCount <= 1)
            {
                return RemoveStoreMemberResult.ValidationError("A store must keep at least one Owner.");
            }
        }

        db.StoreMembers.Remove(member);
        await db.SaveChangesAsync(ct);

        return RemoveStoreMemberResult.Removed();
    }
}
