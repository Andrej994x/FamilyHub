using FamilyHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyHub.Api.Data.Configurations;

public class ExpenseBudgetConfiguration : IEntityTypeConfiguration<ExpenseBudget>
{
    public void Configure(EntityTypeBuilder<ExpenseBudget> builder)
    {
        builder.ToTable("ExpenseBudgets", t =>
        {
            t.HasCheckConstraint("CK_ExpenseBudgets_Amount_Positive", "[Amount] IS NULL OR [Amount] > 0");
            t.HasCheckConstraint("CK_ExpenseBudgets_EffectiveFrom_FirstOfMonth", "DATEPART(day, [EffectiveFrom]) = 1");
        });

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Amount)
            .HasPrecision(12, 2);

        builder.Property(b => b.UpdatedByUserId)
            .IsRequired();

        builder.Property(b => b.UpdatedAt)
            .IsRequired();

        // Deleting a family cascades to its budgets.
        builder.HasOne(b => b.Family)
            .WithMany()
            .HasForeignKey(b => b.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Budget rows of a deleted category are removed in code first.
        builder.HasOne(b => b.Category)
            .WithMany()
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(b => b.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // One row per family, category (null = overall) and month. HasFilter(null) removes the
        // default "IS NOT NULL" filter EF adds for nullable columns, so the overall budget
        // (CategoryId = null) is unique too.
        builder.HasIndex(b => new { b.FamilyId, b.CategoryId, b.EffectiveFrom })
            .IsUnique()
            .HasFilter(null);
    }
}
