using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Domain.Stores;
using Microsoft.EntityFrameworkCore;

namespace FoodOrdering.Infrastructure.Database;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<Store> Stores => Set<Store>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Automatically applies all IEntityTypeConfiguration<T> classes in this assembly (like StoreConfiguration)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
