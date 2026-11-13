using FoodOrdering.Domain.Catalog;
using FoodOrdering.Domain.Orders;
using FoodOrdering.Domain.Promos;
using FoodOrdering.Domain.Reviews;
using FoodOrdering.Domain.Stores;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Store> Stores { get; }

    DbSet<StoreMember> StoreMembers { get; }

    DbSet<Category> Categories { get; }

    DbSet<MenuItem> MenuItems { get; }

    DbSet<MenuItemOptionGroup> MenuItemOptionGroups { get; }

    DbSet<MenuItemOption> MenuItemOptions { get; }

    DbSet<PromoCode> PromoCodes { get; }

    DbSet<Order> Orders { get; }

    DbSet<OrderItem> OrderItems { get; }

    DbSet<OrderItemOption> OrderItemOptions { get; }

    DbSet<OrderReview> OrderReviews { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
