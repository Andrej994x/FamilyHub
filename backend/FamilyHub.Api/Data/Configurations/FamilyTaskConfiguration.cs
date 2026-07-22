using FamilyHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyHub.Api.Data.Configurations;

public class FamilyTaskConfiguration : IEntityTypeConfiguration<FamilyTask>
{
    public void Configure(EntityTypeBuilder<FamilyTask> builder)
    {
        builder.ToTable("FamilyTasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(2000);

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.CreatedByUserId)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        // Deleting a family cascades to its tasks.
        builder.HasOne(t => t.Family)
            .WithMany()
            .HasForeignKey(t => t.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optional assignee. NoAction avoids a second cascade path into FamilyTasks
        // (the family already cascades into both members and tasks). Tasks are
        // explicitly unassigned in code before a member is removed.
        builder.HasOne(t => t.AssignedToMember)
            .WithMany()
            .HasForeignKey(t => t.AssignedToMemberId)
            .OnDelete(DeleteBehavior.NoAction);

        // Indexes to support the list filters.
        builder.HasIndex(t => t.FamilyId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.AssignedToMemberId);
    }
}
