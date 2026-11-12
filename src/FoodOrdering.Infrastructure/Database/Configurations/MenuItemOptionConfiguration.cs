using FoodOrdering.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodOrdering.Infrastructure.Database.Configurations;

public sealed class MenuItemOptionConfiguration : IEntityTypeConfiguration<MenuItemOption>
{
    public void Configure(EntityTypeBuilder<MenuItemOption> builder)
    {
        builder.ToTable("menu_item_options", t =>
            t.HasCheckConstraint("CK_menu_item_options_PriceDelta_NonNegative", "\"PriceDelta\" >= 0"));

        builder.HasKey(o => o.Id);

        builder.Property(o => o.PublicId)
            .IsRequired();

        builder.HasIndex(o => o.PublicId)
            .IsUnique();

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.PriceDelta)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(o => o.SortOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne(o => o.OptionGroup)
            .WithMany(g => g.Options)
            .HasForeignKey(o => o.MenuItemOptionGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
