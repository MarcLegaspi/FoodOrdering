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
        {
            t.HasCheckConstraint("CK_orders_TotalAmount_NonNegative", "\"TotalAmount\" >= 0");
            t.HasCheckConstraint("CK_orders_Subtotal_NonNegative", "\"Subtotal\" >= 0");
            t.HasCheckConstraint("CK_orders_DeliveryFee_NonNegative", "\"DeliveryFee\" >= 0");
            t.HasCheckConstraint("CK_orders_DiscountAmount_NonNegative", "\"DiscountAmount\" >= 0");
        });

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

        builder.Property(o => o.CustomerType)
            .IsRequired()
            .HasDefaultValue(CustomerType.Registered);

        builder.HasIndex(o => o.CustomerType);

        builder.Property(o => o.CustomerName)
            .IsRequired()
            .HasMaxLength(150)
            .HasDefaultValue(string.Empty);

        builder.Property(o => o.CustomerEmail)
            .HasMaxLength(256);

        builder.Property(o => o.CustomerUserId)
            .IsRequired(false);

        builder.Property(o => o.OrderType)
            .IsRequired()
            .HasDefaultValue(OrderType.Pickup);

        builder.Property(o => o.CustomerPhone)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue(string.Empty);

        builder.Property(o => o.DeliveryAddress)
            .HasMaxLength(500);

        builder.Property(o => o.DeliveryLandmark)
            .HasMaxLength(300);

        builder.Property(o => o.Subtotal)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(o => o.PromoCodeSnapshot)
            .HasMaxLength(32);

        builder.Property(o => o.DiscountAmount)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(o => o.DeliveryFee)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(o => o.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(o => o.EstimatedDeliveryTime)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue(string.Empty);

        builder.Property(o => o.Notes)
            .IsRequired()
            .HasMaxLength(500)
            .HasDefaultValue(string.Empty);

        builder.Property(o => o.PaymentMethod)
            .IsRequired()
            .HasDefaultValue(PaymentMethod.PayOnDelivery);

        builder.Property(o => o.PaymentStatus)
            .IsRequired()
            .HasDefaultValue(PaymentStatus.Pending);

        builder.Property(o => o.CashChangeFor)
            .HasPrecision(18, 2);

        builder.Property(o => o.DriverName)
            .HasMaxLength(150);

        builder.Property(o => o.DriverPhone)
            .HasMaxLength(50);

        builder.Property(o => o.PaymentReference)
            .HasMaxLength(100);

        builder.Property(o => o.PaymentCheckoutUrl)
            .HasMaxLength(1000);

        builder.Property(o => o.DeliveryPin)
            .HasMaxLength(10);

        builder.Property(o => o.ProofOfDeliveryImageUrl)
            .HasMaxLength(500);

        builder.HasOne(o => o.Store)
            .WithMany(s => s.Orders)
            .HasForeignKey(o => o.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(o => o.CustomerUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
