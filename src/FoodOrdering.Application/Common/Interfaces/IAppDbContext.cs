using FoodOrdering.Domain.Catalog;
using FoodOrdering.Domain.Stores;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Store> Stores { get; }

    DbSet<Category> Categories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
