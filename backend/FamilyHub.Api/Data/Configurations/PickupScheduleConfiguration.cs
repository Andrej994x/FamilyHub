using FamilyHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyHub.Api.Data.Configurations;

public class PickupScheduleConfiguration : IEntityTypeConfiguration<PickupSchedule>
{
    public void Configure(EntityTypeBuilder<PickupSchedule> builder)
    {
        builder.ToTable("PickupSchedules");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PickupDateTime)
            .IsRequired();

        builder.Property(p => p.Location)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(p => p.Notes)
            .HasMaxLength(2000);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.CreatedByUserId)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        // Deleting a family cascades to its pickups.
        builder.HasOne(p => p.Family)
            .WithMany()
            .HasForeignKey(p => p.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Required child/member references. NoAction avoids additional cascade paths
        // into PickupSchedules; dependent pickups are removed in code before a child
        // or member is deleted.
        builder.HasOne(p => p.ChildProfile)
            .WithMany()
            .HasForeignKey(p => p.ChildProfileId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(p => p.AssignedMember)
            .WithMany()
            .HasForeignKey(p => p.AssignedMemberId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(p => p.FamilyId);
        builder.HasIndex(p => p.PickupDateTime);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.AssignedMemberId);
        builder.HasIndex(p => p.ChildProfileId);
    }
}
