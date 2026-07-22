using FamilyHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyHub.Api.Data.Configurations;

public class ShoppingListConfiguration : IEntityTypeConfiguration<ShoppingList>
{
    public void Configure(EntityTypeBuilder<ShoppingList> builder)
    {
        builder.ToTable("ShoppingLists");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.CreatedAt)
            .IsRequired();

        // Deleting a family cascades to its shopping lists.
        builder.HasOne(l => l.Family)
            .WithMany()
            .HasForeignKey(l => l.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Deleting a list cascades to its items.
        builder.HasMany(l => l.Items)
            .WithOne(i => i.ShoppingList)
            .HasForeignKey(i => i.ShoppingListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => l.FamilyId);
    }
}
