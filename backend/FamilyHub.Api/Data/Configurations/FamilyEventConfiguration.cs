using FamilyHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyHub.Api.Data.Configurations;

public class FamilyEventConfiguration : IEntityTypeConfiguration<FamilyEvent>
{
    public void Configure(EntityTypeBuilder<FamilyEvent> builder)
    {
        builder.ToTable("FamilyEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.StartDateTime)
            .IsRequired();

        builder.Property(e => e.Location)
            .HasMaxLength(300);

        builder.Property(e => e.CreatedByUserId)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // Deleting a family cascades to its events.
        builder.HasOne(e => e.Family)
            .WithMany()
            .HasForeignKey(e => e.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optional related member/child. NoAction avoids additional cascade paths into
        // FamilyEvents; these references are cleared in code before a member or child
        // is removed.
        builder.HasOne(e => e.AssignedMember)
            .WithMany()
            .HasForeignKey(e => e.AssignedMemberId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.ChildProfile)
            .WithMany()
            .HasForeignKey(e => e.ChildProfileId)
            .OnDelete(DeleteBehavior.NoAction);

        // Indexes to support the list filters and ordering.
        builder.HasIndex(e => e.FamilyId);
        builder.HasIndex(e => e.StartDateTime);
        builder.HasIndex(e => e.EventType);
        builder.HasIndex(e => e.AssignedMemberId);
        builder.HasIndex(e => e.ChildProfileId);
    }
}
