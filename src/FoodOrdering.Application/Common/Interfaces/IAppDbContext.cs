using FoodOrdering.Domain.Catalog;
using FoodOrdering.Domain.Orders;
using FoodOrdering.Domain.Stores;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Store> Stores { get; }

    DbSet<StoreMember> StoreMembers { get; }

    DbSet<Category> Categories { get; }

    DbSet<MenuItem> MenuItems { get; }

    DbSet<Order> Orders { get; }

    DbSet<OrderItem> OrderItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
