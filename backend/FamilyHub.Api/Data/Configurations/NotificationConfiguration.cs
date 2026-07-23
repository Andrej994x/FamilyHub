using FamilyHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyHub.Api.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.UserId)
            .IsRequired();

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(n => n.RelatedUrl)
            .HasMaxLength(500);

        builder.Property(n => n.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        // Recipient -> ApplicationUser. Restrict avoids cascade paths into AspNetUsers.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Supports listing and the unread-count query.
        builder.HasIndex(n => new { n.UserId, n.IsRead });

        // Supports a future push worker scanning for undelivered notifications.
        builder.HasIndex(n => n.IsPushSent);
    }
}
