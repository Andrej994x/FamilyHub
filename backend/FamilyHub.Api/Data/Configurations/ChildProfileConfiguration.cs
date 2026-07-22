using FamilyHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyHub.Api.Data.Configurations;

public class ChildProfileConfiguration : IEntityTypeConfiguration<ChildProfile>
{
    public void Configure(EntityTypeBuilder<ChildProfile> builder)
    {
        builder.ToTable("ChildProfiles");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Notes)
            .HasMaxLength(2000);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // DateOfBirth is nullable (DateOnly?) and maps to SQL 'date'.

        // The Family -> Children relationship (incl. delete behavior) is configured
        // in FamilyConfiguration.
    }
}
