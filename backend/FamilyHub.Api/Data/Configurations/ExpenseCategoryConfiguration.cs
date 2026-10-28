using FamilyHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyHub.Api.Data.Configurations;

public class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("ExpenseCategories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.DefaultCategory)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // Deleting a family cascades to its categories.
        builder.HasOne(c => c.Family)
            .WithMany()
            .HasForeignKey(c => c.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Names are unique per family, archived categories included (restore instead of
        // re-creating). Also makes concurrent default seeding fail instead of duplicating.
        builder.HasIndex(c => new { c.FamilyId, c.Name })
            .IsUnique();
    }
}
