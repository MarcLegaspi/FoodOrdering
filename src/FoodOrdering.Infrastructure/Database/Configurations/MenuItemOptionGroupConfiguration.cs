using FoodOrdering.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodOrdering.Infrastructure.Database.Configurations;

public sealed class MenuItemOptionGroupConfiguration : IEntityTypeConfiguration<MenuItemOptionGroup>
{
    public void Configure(EntityTypeBuilder<MenuItemOptionGroup> builder)
    {
        builder.ToTable("menu_item_option_groups");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.PublicId)
            .IsRequired();

        builder.HasIndex(g => g.PublicId)
            .IsUnique();

        builder.Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(g => g.IsRequired)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(g => g.AllowMultiple)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(g => g.SortOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne(g => g.MenuItem)
            .WithMany(m => m.OptionGroups)
            .HasForeignKey(g => g.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
