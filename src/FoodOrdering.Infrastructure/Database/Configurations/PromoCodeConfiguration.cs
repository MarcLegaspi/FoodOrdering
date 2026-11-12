using FoodOrdering.Domain.Promos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodOrdering.Infrastructure.Database.Configurations;

public sealed class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
{
    public void Configure(EntityTypeBuilder<PromoCode> builder)
    {
        builder.ToTable("promo_codes", t =>
        {
            t.HasCheckConstraint("CK_promo_codes_DiscountValue_Positive", "\"DiscountValue\" > 0");
            t.HasCheckConstraint("CK_promo_codes_UsedCount_NonNegative", "\"UsedCount\" >= 0");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PublicId)
            .IsRequired();

        builder.HasIndex(p => p.PublicId)
            .IsUnique();

        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(32);

        builder.HasIndex(p => new { p.StoreId, p.Code })
            .IsUnique();

        builder.Property(p => p.DiscountType)
            .IsRequired();

        builder.Property(p => p.DiscountValue)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.UsedCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne(p => p.Store)
            .WithMany()
            .HasForeignKey(p => p.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
