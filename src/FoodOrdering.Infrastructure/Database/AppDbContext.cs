using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Domain.Catalog;
using FoodOrdering.Domain.Common;
using FoodOrdering.Domain.Orders;
using FoodOrdering.Domain.Promos;
using FoodOrdering.Domain.Reviews;
using FoodOrdering.Domain.Stores;
using FoodOrdering.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Infrastructure.Database;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ITenantContext tenantContext)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options), IAppDbContext
{
    private readonly ITenantContext _tenantContext = tenantContext;

    public DbSet<Store> Stores => Set<Store>();

    public DbSet<StoreMember> StoreMembers => Set<StoreMember>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    public DbSet<MenuItemOptionGroup> MenuItemOptionGroups => Set<MenuItemOptionGroup>();

    public DbSet<MenuItemOption> MenuItemOptions => Set<MenuItemOption>();

    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<OrderItemOption> OrderItemOptions => Set<OrderItemOption>();

    public DbSet<OrderReview> OrderReviews => Set<OrderReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Fail-closed multi-tenant filters: queries only return rows for the active store
        modelBuilder.Entity<Category>()
            .HasQueryFilter(c => _tenantContext.StoreId != null && c.StoreId == _tenantContext.StoreId);

        modelBuilder.Entity<MenuItem>()
            .HasQueryFilter(m => _tenantContext.StoreId != null && m.StoreId == _tenantContext.StoreId);

        modelBuilder.Entity<MenuItemOptionGroup>()
            .HasQueryFilter(g => _tenantContext.StoreId != null && g.MenuItem.StoreId == _tenantContext.StoreId);

        modelBuilder.Entity<MenuItemOption>()
            .HasQueryFilter(o => _tenantContext.StoreId != null && o.OptionGroup.MenuItem.StoreId == _tenantContext.StoreId);

        modelBuilder.Entity<PromoCode>()
            .HasQueryFilter(p => _tenantContext.StoreId != null && p.StoreId == _tenantContext.StoreId);

        modelBuilder.Entity<Order>()
            .HasQueryFilter(o => _tenantContext.StoreId != null && o.StoreId == _tenantContext.StoreId);

        modelBuilder.Entity<OrderItem>()
            .HasQueryFilter(i => _tenantContext.StoreId != null && i.Order.StoreId == _tenantContext.StoreId);

        modelBuilder.Entity<OrderItemOption>()
            .HasQueryFilter(o => _tenantContext.StoreId != null && o.OrderItem.Order.StoreId == _tenantContext.StoreId);

        modelBuilder.Entity<StoreMember>()
            .HasQueryFilter(m => _tenantContext.StoreId != null && m.StoreId == _tenantContext.StoreId);

        modelBuilder.Entity<OrderReview>()
            .HasQueryFilter(r => _tenantContext.StoreId != null && r.StoreId == _tenantContext.StoreId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.CreatedAtUtc == default)
            {
                entry.Entity.CreatedAtUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
