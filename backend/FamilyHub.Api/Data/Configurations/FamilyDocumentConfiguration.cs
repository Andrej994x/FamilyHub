using FamilyHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyHub.Api.Data.Configurations;

public class FamilyDocumentConfiguration : IEntityTypeConfiguration<FamilyDocument>
{
    public void Configure(EntityTypeBuilder<FamilyDocument> builder)
    {
        builder.ToTable("FamilyDocuments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DocumentType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(d => d.DocumentNumber)
            .HasMaxLength(100);

        builder.Property(d => d.Notes)
            .HasMaxLength(2000);

        builder.Property(d => d.AttachmentPath)
            .HasMaxLength(500);

        builder.Property(d => d.CreatedByUserId)
            .IsRequired();

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        // Deleting a family cascades to its documents.
        builder.HasOne(d => d.Family)
            .WithMany()
            .HasForeignKey(d => d.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optional subject references. NoAction avoids additional cascade paths into
        // FamilyDocuments; dependent documents are removed in code before a member or
        // child is deleted.
        builder.HasOne(d => d.FamilyMember)
            .WithMany()
            .HasForeignKey(d => d.FamilyMemberId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(d => d.ChildProfile)
            .WithMany()
            .HasForeignKey(d => d.ChildProfileId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(d => d.FamilyId);
        builder.HasIndex(d => d.FamilyMemberId);
        builder.HasIndex(d => d.ChildProfileId);
        builder.HasIndex(d => d.DocumentType);
    }
}
