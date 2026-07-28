using FamilyHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyHub.Api.Data.Configurations;

public class ReminderHistoryConfiguration : IEntityTypeConfiguration<ReminderHistory>
{
    public void Configure(EntityTypeBuilder<ReminderHistory> builder)
    {
        builder.ToTable("ReminderHistory");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RecipientUserId)
            .IsRequired();

        builder.Property(r => r.SourceType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(r => r.DateKind)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.DueDate).IsRequired();
        builder.Property(r => r.DaysBefore).IsRequired();
        builder.Property(r => r.SentAt).IsRequired();

        // Each milestone is delivered to each recipient exactly once. This unique key both drives
        // the in-memory dedup and guards against duplicates at the database level.
        builder.HasIndex(r => new { r.RecipientUserId, r.SourceType, r.SourceId, r.DateKind, r.DueDate, r.DaysBefore })
            .IsUnique();
    }
}
