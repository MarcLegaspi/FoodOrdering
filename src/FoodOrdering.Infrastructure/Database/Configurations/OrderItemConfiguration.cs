using FoodOrdering.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodOrdering.Infrastructure.Database.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items", t =>
        {
            t.HasCheckConstraint("CK_order_items_Quantity_Positive", "\"Quantity\" > 0");
            t.HasCheckConstraint("CK_order_items_UnitPriceSnapshot_NonNegative", "\"UnitPriceSnapshot\" >= 0");
        });

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ItemNameSnapshot)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(i => i.UnitPriceSnapshot)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(i => i.Quantity)
            .IsRequired();

        builder.HasOne(i => i.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.MenuItem)
            .WithMany()
            .HasForeignKey(i => i.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
