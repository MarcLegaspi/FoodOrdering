using FoodOrdering.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodOrdering.Infrastructure.Database.Configurations;

public sealed class OrderReviewConfiguration : IEntityTypeConfiguration<OrderReview>
{
    public void Configure(EntityTypeBuilder<OrderReview> builder)
    {
        builder.ToTable("order_reviews", t =>
        {
            t.HasCheckConstraint("CK_order_reviews_Rating_Range", "\"Rating\" >= 1 AND \"Rating\" <= 5");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.PublicId)
            .IsRequired();

        builder.HasIndex(r => r.PublicId)
            .IsUnique();

        builder.Property(r => r.CustomerUserId)
            .IsRequired();

        builder.Property(r => r.CustomerName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(r => r.Rating)
            .IsRequired();

        builder.Property(r => r.Comment)
            .HasMaxLength(500);

        builder.HasIndex(r => r.OrderId)
            .IsUnique();

        builder.HasIndex(r => new { r.StoreId, r.CreatedAtUtc });

        builder.HasOne(r => r.Order)
            .WithOne(o => o.Review)
            .HasForeignKey<OrderReview>(r => r.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Store)
            .WithMany(s => s.Reviews)
            .HasForeignKey(r => r.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
