using FamilyHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyHub.Api.Data.Configurations;

public class ShoppingItemConfiguration : IEntityTypeConfiguration<ShoppingItem>
{
    public void Configure(EntityTypeBuilder<ShoppingItem> builder)
    {
        builder.ToTable("ShoppingItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Quantity)
            .HasMaxLength(50);

        builder.Property(i => i.Category)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.AddedByUserId)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        // The list -> items relationship (incl. cascade delete) is configured in
        // ShoppingListConfiguration.

        // User references use Restrict to avoid cascade paths into AspNetUsers.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(i => i.AddedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(i => i.PurchasedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.ShoppingListId);
        builder.HasIndex(i => i.IsPurchased);
    }
}
