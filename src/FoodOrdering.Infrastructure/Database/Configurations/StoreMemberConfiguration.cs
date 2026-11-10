using FoodOrdering.Domain.Stores;
using FoodOrdering.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodOrdering.Infrastructure.Database.Configurations;

public sealed class StoreMemberConfiguration : IEntityTypeConfiguration<StoreMember>
{
    public void Configure(EntityTypeBuilder<StoreMember> builder)
    {
        builder.ToTable("store_members");

        builder.HasKey(m => m.Id);

        builder.HasIndex(m => new { m.StoreId, m.UserId })
            .IsUnique();

        builder.Property(m => m.Role)
            .IsRequired();

        builder.HasOne(m => m.Store)
            .WithMany(s => s.Members)
            .HasForeignKey(m => m.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
