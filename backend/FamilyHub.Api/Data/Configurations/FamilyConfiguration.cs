using FamilyHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyHub.Api.Data.Configurations;

public class FamilyConfiguration : IEntityTypeConfiguration<Family>
{
    public void Configure(EntityTypeBuilder<Family> builder)
    {
        builder.ToTable("Families");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.CreatedByUserId)
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .IsRequired();

        // Family creator -> ApplicationUser. Restrict so deleting a user never
        // silently deletes the families they created.
        builder.HasOne(f => f.CreatedByUser)
            .WithMany()
            .HasForeignKey(f => f.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Deleting a family cascades to its members, invitations, and children.
        builder.HasMany(f => f.Members)
            .WithOne(m => m.Family)
            .HasForeignKey(m => m.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.Invitations)
            .WithOne(i => i.Family)
            .HasForeignKey(i => i.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.Children)
            .WithOne(c => c.Family)
            .HasForeignKey(c => c.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
