using FoodOrdering.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodOrdering.Infrastructure.Database.Configurations;

public sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("menu_items", t =>
            t.HasCheckConstraint("CK_menu_items_Price_NonNegative", "\"Price\" >= 0"));

        builder.HasKey(m => m.Id);

        // Start identity at 1100 so seeded Ids (1001..1006) never collide with existing or new rows
        builder.Property(m => m.Id)
            .HasIdentityOptions(startValue: 1100);

        builder.Property(m => m.PublicId)
            .IsRequired();

        builder.HasIndex(m => m.PublicId)
            .IsUnique();

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(m => new { m.StoreId, m.CategoryId, m.Name })
            .IsUnique();

        builder.Property(m => m.Description)
            .IsRequired()
            .HasMaxLength(500)
            .HasDefaultValue(string.Empty);

        builder.Property(m => m.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(m => m.ImageUrl)
            .HasMaxLength(500);

        builder.Property(m => m.IsAvailable)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(m => m.SortOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne(m => m.Store)
            .WithMany(s => s.MenuItems)
            .HasForeignKey(m => m.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Category)
            .WithMany(c => c.MenuItems)
            .HasForeignKey(m => m.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            new MenuItem
            {
                Id = 1001,
                PublicId = Guid.Parse("d1111111-1111-1111-1111-111111111111"),
                StoreId = 1,
                CategoryId = 1001,
                Name = "Classic Cheeseburger",
                Description = "Grilled beef patty, cheddar, lettuce, tomato, house sauce",
                Price = 9.99m,
                ImageUrl = "/images/menu/cheeseburger.jpg",
                IsAvailable = true,
                SortOrder = 1,
                CreatedAtUtc = seedDate
            },
            new MenuItem
            {
                Id = 1002,
                PublicId = Guid.Parse("d2222222-2222-2222-2222-222222222222"),
                StoreId = 1,
                CategoryId = 1002,
                Name = "Crispy Fries",
                Description = "Golden sea-salted shoestring fries",
                Price = 3.99m,
                ImageUrl = "/images/menu/fries.jpg",
                IsAvailable = true,
                SortOrder = 1,
                CreatedAtUtc = seedDate
            },
            new MenuItem
            {
                Id = 1003,
                PublicId = Guid.Parse("e1111111-1111-1111-1111-111111111111"),
                StoreId = 2,
                CategoryId = 1003,
                Name = "Margherita Pizza",
                Description = "San Marzano tomato, fresh mozzarella, sweet basil",
                Price = 13.50m,
                ImageUrl = "/images/menu/margherita-pizza.jpg",
                IsAvailable = true,
                SortOrder = 1,
                CreatedAtUtc = seedDate
            },
            new MenuItem
            {
                Id = 1004,
                PublicId = Guid.Parse("e2222222-2222-2222-2222-222222222222"),
                StoreId = 2,
                CategoryId = 1004,
                Name = "Garlic Knots",
                Description = "Warm oven-baked knots brushed with herb garlic butter",
                Price = 5.50m,
                ImageUrl = "/images/menu/garlic-knots.jpg",
                IsAvailable = true,
                SortOrder = 1,
                CreatedAtUtc = seedDate
            },
            new MenuItem
            {
                Id = 1005,
                PublicId = Guid.Parse("f1111111-1111-1111-1111-111111111111"),
                StoreId = 3,
                CategoryId = 1005,
                Name = "Salmon Avocado Roll",
                Description = "Fresh Atlantic salmon, ripe avocado, toasted sesame",
                Price = 11.50m,
                ImageUrl = "/images/menu/salmon-roll.jpg",
                IsAvailable = true,
                SortOrder = 1,
                CreatedAtUtc = seedDate
            },
            new MenuItem
            {
                Id = 1006,
                PublicId = Guid.Parse("f2222222-2222-2222-2222-222222222222"),
                StoreId = 3,
                CategoryId = 1006,
                Name = "Miso Soup",
                Description = "Traditional dashi broth with tofu, wakame, and scallions",
                Price = 3.50m,
                ImageUrl = "/images/menu/miso-soup.jpg",
                IsAvailable = true,
                SortOrder = 1,
                CreatedAtUtc = seedDate
            });
    }
}
