using FoodOrdering.Domain.Stores;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Store> Stores { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
