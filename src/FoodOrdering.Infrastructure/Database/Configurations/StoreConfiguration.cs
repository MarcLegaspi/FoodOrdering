using FoodOrdering.Domain.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodOrdering.Infrastructure.Database.Configurations;

public sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("stores");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.PublicId)
            .IsRequired();

        builder.HasIndex(s => s.PublicId)
            .IsUnique();

        builder.Property(s => s.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(s => s.Slug)
            .IsUnique();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.Email)
            .HasMaxLength(255);

        builder.Property(s => s.Phone)
            .HasMaxLength(50);

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
    }
}
