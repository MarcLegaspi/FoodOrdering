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
    }
}
