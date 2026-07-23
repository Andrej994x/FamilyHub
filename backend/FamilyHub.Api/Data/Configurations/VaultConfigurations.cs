using FamilyHub.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyHub.Api.Data.Configurations;

// All Family Vault record types belong to a single family (cascade on family delete) and
// carry no member/child references, so no extra cleanup is needed when those are removed.
// Attachments reference their owner polymorphically and are scoped to the family directly.

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name).IsRequired().HasMaxLength(200);
        builder.Property(v => v.Make).HasMaxLength(100);
        builder.Property(v => v.Model).HasMaxLength(100);
        builder.Property(v => v.RegistrationNumber).HasMaxLength(50);
        builder.Property(v => v.Notes).HasMaxLength(2000);
        builder.Property(v => v.CreatedByUserId).IsRequired();
        builder.Property(v => v.CreatedAt).IsRequired();

        builder.HasOne(v => v.Family)
            .WithMany()
            .HasForeignKey(v => v.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => v.FamilyId);
    }
}

public class PetConfiguration : IEntityTypeConfiguration<Pet>
{
    public void Configure(EntityTypeBuilder<Pet> builder)
    {
        builder.ToTable("Pets");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Type).HasMaxLength(100);
        builder.Property(p => p.Breed).HasMaxLength(100);
        builder.Property(p => p.MicrochipNumber).HasMaxLength(100);
        builder.Property(p => p.VaccinationName).HasMaxLength(200);
        builder.Property(p => p.Veterinarian).HasMaxLength(200);
        builder.Property(p => p.Notes).HasMaxLength(2000);
        builder.Property(p => p.CreatedByUserId).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();
        // DateOfBirth is DateOnly? and maps to SQL 'date'.

        builder.HasOne(p => p.Family)
            .WithMany()
            .HasForeignKey(p => p.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.FamilyId);
    }
}

public class HomeRecordConfiguration : IEntityTypeConfiguration<HomeRecord>
{
    public void Configure(EntityTypeBuilder<HomeRecord> builder)
    {
        builder.ToTable("HomeRecords");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Title).IsRequired().HasMaxLength(200);
        builder.Property(h => h.Type).HasMaxLength(100);
        builder.Property(h => h.Provider).HasMaxLength(200);
        builder.Property(h => h.Notes).HasMaxLength(2000);
        builder.Property(h => h.CreatedByUserId).IsRequired();
        builder.Property(h => h.CreatedAt).IsRequired();

        builder.HasOne(h => h.Family)
            .WithMany()
            .HasForeignKey(h => h.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => h.FamilyId);
    }
}

public class WarrantyConfiguration : IEntityTypeConfiguration<Warranty>
{
    public void Configure(EntityTypeBuilder<Warranty> builder)
    {
        builder.ToTable("Warranties");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(w => w.Store).HasMaxLength(200);
        builder.Property(w => w.SerialNumber).HasMaxLength(100);
        builder.Property(w => w.Notes).HasMaxLength(2000);
        builder.Property(w => w.CreatedByUserId).IsRequired();
        builder.Property(w => w.CreatedAt).IsRequired();

        builder.HasOne(w => w.Family)
            .WithMany()
            .HasForeignKey(w => w.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => w.FamilyId);
    }
}

public class OtherRecordConfiguration : IEntityTypeConfiguration<OtherRecord>
{
    public void Configure(EntityTypeBuilder<OtherRecord> builder)
    {
        builder.ToTable("OtherRecords");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Title).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Description).HasMaxLength(2000);
        builder.Property(o => o.CreatedByUserId).IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();

        builder.HasOne(o => o.Family)
            .WithMany()
            .HasForeignKey(o => o.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.FamilyId);
    }
}

public class VaultAttachmentConfiguration : IEntityTypeConfiguration<VaultAttachment>
{
    public void Configure(EntityTypeBuilder<VaultAttachment> builder)
    {
        builder.ToTable("VaultAttachments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.OwnerType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.FilePath).IsRequired().HasMaxLength(500);
        builder.Property(a => a.FileName).HasMaxLength(260);
        builder.Property(a => a.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(a => a.CreatedByUserId).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();

        // Single cascade path: Family -> VaultAttachment. Owners are referenced
        // polymorphically (no FK), so an owner delete cleans up rows/files in code.
        builder.HasOne(a => a.Family)
            .WithMany()
            .HasForeignKey(a => a.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.FamilyId);
        builder.HasIndex(a => new { a.OwnerType, a.OwnerId });
    }
}
