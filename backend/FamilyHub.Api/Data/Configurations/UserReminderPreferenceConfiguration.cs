using System.Text.Json;
using FamilyHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyHub.Api.Data.Configurations;

public class UserReminderPreferenceConfiguration : IEntityTypeConfiguration<UserReminderPreference>
{
    public void Configure(EntityTypeBuilder<UserReminderPreference> builder)
    {
        builder.ToTable("UserReminderPreferences");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.Category)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        // Store the offsets as a compact JSON array in a single column.
        builder.Property(p => p.ReminderOffsetsDays)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<int[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<int>())
            .Metadata.SetValueComparer(new ValueComparer<int[]>(
                (a, b) => a!.SequenceEqual(b!),
                v => v.Aggregate(0, (hash, i) => HashCode.Combine(hash, i)),
                v => v.ToArray()));

        builder.Property(p => p.ReminderOffsetsDays)
            .HasColumnType("nvarchar(200)");

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One preference per user per category.
        builder.HasIndex(p => new { p.UserId, p.Category }).IsUnique();
    }
}
