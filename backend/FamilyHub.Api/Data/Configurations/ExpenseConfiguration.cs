using FamilyHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyHub.Api.Data.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses", t =>
            t.HasCheckConstraint("CK_Expenses_Amount_Positive", "[Amount] > 0"));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Amount)
            .HasPrecision(12, 2);

        // Date is a DateOnly and maps to SQL 'date'.

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.PaidByUserId)
            .IsRequired();

        builder.Property(e => e.CreatedByUserId)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // Deleting a family cascades to its expenses.
        builder.HasOne(e => e.Family)
            .WithMany()
            .HasForeignKey(e => e.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        // A category in use can't be deleted (it is archived instead), which keeps history intact.
        builder.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // User references use Restrict to avoid cascade paths into AspNetUsers. Users are
        // never deleted, so the payer and creator survive a member leaving the family.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(e => e.PaidByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Monthly history and totals filter on family + date range.
        builder.HasIndex(e => new { e.FamilyId, e.Date });
    }
}
