using FoodOrdering.Domain.Orders;
using FoodOrdering.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodOrdering.Infrastructure.Database.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", t =>
            t.HasCheckConstraint("CK_orders_TotalAmount_NonNegative", "\"TotalAmount\" >= 0"));

        builder.HasKey(o => o.Id);

        builder.Property(o => o.PublicId)
            .IsRequired();

        builder.HasIndex(o => o.PublicId)
            .IsUnique();

        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(32);

        builder.HasIndex(o => new { o.StoreId, o.OrderNumber })
            .IsUnique();

        builder.Property(o => o.Status)
            .IsRequired();

        builder.Property(o => o.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(o => o.Notes)
            .IsRequired()
            .HasMaxLength(500)
            .HasDefaultValue(string.Empty);

        builder.HasOne(o => o.Store)
            .WithMany(s => s.Orders)
            .HasForeignKey(o => o.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(o => o.CustomerUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
