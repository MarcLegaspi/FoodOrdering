using FoodOrdering.Domain.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodOrdering.Infrastructure.Database.Configurations;

public sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("stores");

        builder.HasKey(s => s.Id);

        // Start identity at 4 so seeded Ids (1..3) never collide with new inserts
        builder.Property(s => s.Id)
            .HasIdentityOptions(startValue: 4);

        builder.Property(s => s.PublicId)
            .IsRequired();

        builder.HasIndex(s => s.PublicId)
            .IsUnique();

        builder.Property(s => s.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(s => s.Slug)
            .IsUnique();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.Email)
            .HasMaxLength(255);

        builder.Property(s => s.Phone)
            .HasMaxLength(50);

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.IsAcceptingOrders)
            .IsRequired()
            .HasDefaultValue(true);

        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            new Store
            {
                Id = 1,
                PublicId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Slug = "burger-barn",
                Name = "Burger Barn",
                Email = "hello@burgerbarn.local",
                Phone = "+1-555-0101",
                IsActive = true,
                IsAcceptingOrders = true,
                CreatedAtUtc = seedDate
            },
            new Store
            {
                Id = 2,
                PublicId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Slug = "pizza-palace",
                Name = "Pizza Palace",
                Email = "hello@pizzapalace.local",
                Phone = "+1-555-0102",
                IsActive = true,
                IsAcceptingOrders = true,
                CreatedAtUtc = seedDate
            },
            new Store
            {
                Id = 3,
                PublicId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Slug = "sushi-spot",
                Name = "Sushi Spot",
                Email = "hello@sushispot.local",
                Phone = "+1-555-0103",
                IsActive = true,
                IsAcceptingOrders = true,
                CreatedAtUtc = seedDate
            });
    }
}
