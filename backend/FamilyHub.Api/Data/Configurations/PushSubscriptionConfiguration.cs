using FamilyHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyHub.Api.Data.Configurations;

public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.ToTable("PushSubscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.Property(s => s.Endpoint)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(s => s.EndpointHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(s => s.P256dh)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(s => s.Auth)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(s => s.UserAgent)
            .HasMaxLength(400);

        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();

        // Owner -> ApplicationUser. Cascade so a user's devices are cleaned up with the user.
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One row per device endpoint; register upserts on this key. Hashed because the raw
        // endpoint is too long to be a SQL unique-index key.
        builder.HasIndex(s => s.EndpointHash).IsUnique();

        // Supports "active devices for this user" lookups when sending.
        builder.HasIndex(s => new { s.UserId, s.IsActive });
    }
}
