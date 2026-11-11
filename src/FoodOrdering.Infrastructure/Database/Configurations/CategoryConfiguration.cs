using FoodOrdering.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodOrdering.Infrastructure.Database.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);

        // Start identity at 1100 so seeded Ids (1001..1006) never collide with existing or new rows
        builder.Property(c => c.Id)
            .HasIdentityOptions(startValue: 1100);

        builder.Property(c => c.PublicId)
            .IsRequired();

        builder.HasIndex(c => c.PublicId)
            .IsUnique();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(120);

        // A category name must be unique within a store, while different stores can share the same name
        builder.HasIndex(c => new { c.StoreId, c.Name })
            .IsUnique();

        builder.Property(c => c.SortOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne(c => c.Store)
            .WithMany(s => s.Categories)
            .HasForeignKey(c => c.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            new Category
            {
                Id = 1001,
                PublicId = Guid.Parse("a1111111-1111-1111-1111-111111111111"),
                StoreId = 1,
                Name = "Burgers",
                SortOrder = 1,
                IsActive = true,
                CreatedAtUtc = seedDate
            },
            new Category
            {
                Id = 1002,
                PublicId = Guid.Parse("a2222222-2222-2222-2222-222222222222"),
                StoreId = 1,
                Name = "Sides & Drinks",
                SortOrder = 2,
                IsActive = true,
                CreatedAtUtc = seedDate
            },
            new Category
            {
                Id = 1003,
                PublicId = Guid.Parse("b1111111-1111-1111-1111-111111111111"),
                StoreId = 2,
                Name = "Classic Pizzas",
                SortOrder = 1,
                IsActive = true,
                CreatedAtUtc = seedDate
            },
            new Category
            {
                Id = 1004,
                PublicId = Guid.Parse("b2222222-2222-2222-2222-222222222222"),
                StoreId = 2,
                Name = "Sides & Drinks",
                SortOrder = 2,
                IsActive = true,
                CreatedAtUtc = seedDate
            },
            new Category
            {
                Id = 1005,
                PublicId = Guid.Parse("c1111111-1111-1111-1111-111111111111"),
                StoreId = 3,
                Name = "Signature Rolls",
                SortOrder = 1,
                IsActive = true,
                CreatedAtUtc = seedDate
            },
            new Category
            {
                Id = 1006,
                PublicId = Guid.Parse("c2222222-2222-2222-2222-222222222222"),
                StoreId = 3,
                Name = "Soups & Sides",
                SortOrder = 2,
                IsActive = true,
                CreatedAtUtc = seedDate
            });
    }
}
