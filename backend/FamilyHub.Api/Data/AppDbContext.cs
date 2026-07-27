using FamilyHub.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Data;

/// <summary>
/// Application database context backed by ASP.NET Core Identity.
/// Additional entity sets and configurations will be added as the domain model grows.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Family> Families => Set<Family>();

    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();

    public DbSet<FamilyInvitation> FamilyInvitations => Set<FamilyInvitation>();

    public DbSet<ChildProfile> ChildProfiles => Set<ChildProfile>();

    public DbSet<FamilyTask> FamilyTasks => Set<FamilyTask>();

    public DbSet<FamilyEvent> FamilyEvents => Set<FamilyEvent>();

    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();

    public DbSet<ShoppingItem> ShoppingItems => Set<ShoppingItem>();

    public DbSet<PickupSchedule> PickupSchedules => Set<PickupSchedule>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();

    public DbSet<FamilyDocument> FamilyDocuments => Set<FamilyDocument>();

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<Pet> Pets => Set<Pet>();

    public DbSet<HomeRecord> HomeRecords => Set<HomeRecord>();

    public DbSet<Warranty> Warranties => Set<Warranty>();

    public DbSet<OtherRecord> OtherRecords => Set<OtherRecord>();

    public DbSet<VaultAttachment> VaultAttachments => Set<VaultAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Applies any IEntityTypeConfiguration<T> defined in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
