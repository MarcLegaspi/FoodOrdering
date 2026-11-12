using FoodOrdering.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodOrdering.Infrastructure.Database.Configurations;

public sealed class OrderItemOptionConfiguration : IEntityTypeConfiguration<OrderItemOption>
{
    public void Configure(EntityTypeBuilder<OrderItemOption> builder)
    {
        builder.ToTable("order_item_options", t =>
            t.HasCheckConstraint("CK_order_item_options_PriceDeltaSnapshot_NonNegative", "\"PriceDeltaSnapshot\" >= 0"));

        builder.HasKey(o => o.Id);

        builder.Property(o => o.GroupNameSnapshot)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.OptionNameSnapshot)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.PriceDeltaSnapshot)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.HasOne(o => o.OrderItem)
            .WithMany(i => i.Options)
            .HasForeignKey(o => o.OrderItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
