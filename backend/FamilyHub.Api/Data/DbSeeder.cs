using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskStatus = FamilyHub.Api.Models.Enums.TaskStatus;

namespace FamilyHub.Api.Data;

/// <summary>
/// Development-only sample data. Populates a complete, self-consistent family so the
/// app can be explored without manual data entry.
///
/// Everything uses fixed identifiers and is keyed on <see cref="FamilyId"/>: if that
/// family already exists the seeder is a no-op, so it is safe to run on every startup.
/// Never call this outside the Development environment.
/// </summary>
public static class DbSeeder
{
    // Sign-in credentials for the two seeded parents.
    public const string Parent1Email = "alice@familyhub.dev";
    public const string Parent2Email = "bob@familyhub.dev";
    public const string SeedPassword = "Password123!";

    private const string Parent1UserId = "11111111-0000-0000-0000-000000000001";
    private const string Parent2UserId = "11111111-0000-0000-0000-000000000002";

    private static readonly Guid FamilyId = new("a0000000-0000-0000-0000-000000000001");
    private static readonly Guid Member1Id = new("b0000000-0000-0000-0000-000000000001");
    private static readonly Guid Member2Id = new("b0000000-0000-0000-0000-000000000002");
    private static readonly Guid ChildId = new("c0000000-0000-0000-0000-000000000001");
    private static readonly Guid ShoppingListId = new("d0000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbSeeder));

        try
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

            // Make sure the schema exists / is up to date before touching data.
            await db.Database.MigrateAsync();

            if (await db.Families.AnyAsync(f => f.Id == FamilyId))
            {
                logger.LogInformation("Seed data already present; skipping.");
                return;
            }

            // --- Users (Identity hashes the passwords) ---
            var alice = await GetOrCreateUserAsync(
                userManager, Parent1UserId, Parent1Email, "Alice", "Rivera");
            var bob = await GetOrCreateUserAsync(
                userManager, Parent2UserId, Parent2Email, "Bob", "Rivera");

            var now = DateTimeOffset.UtcNow;
            var todayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset At(int days, int hour, int minute = 0) =>
                todayStart.AddDays(days).AddHours(hour).AddMinutes(minute);

            // --- Family + members (Alice created it, so she is the Owner; Bob is a Parent) ---
            var family = new Family
            {
                Id = FamilyId,
                Name = "The Rivera Family",
                CreatedByUserId = alice.Id,
                CreatedAt = now.AddMonths(-2),
            };

            var member1 = new FamilyMember
            {
                Id = Member1Id,
                FamilyId = FamilyId,
                UserId = alice.Id,
                Role = FamilyRole.Owner,
                JoinedAt = now.AddMonths(-2),
            };
            var member2 = new FamilyMember
            {
                Id = Member2Id,
                FamilyId = FamilyId,
                UserId = bob.Id,
                Role = FamilyRole.Parent,
                JoinedAt = now.AddMonths(-2).AddDays(1),
            };

            var child = new ChildProfile
            {
                Id = ChildId,
                FamilyId = FamilyId,
                FirstName = "Mia",
                LastName = "Rivera",
                DateOfBirth = new DateOnly(2017, 4, 12),
                Notes = "Allergic to peanuts.",
                CreatedAt = now.AddMonths(-2),
            };

            // --- Five tasks (varied priority, status and assignment) ---
            var tasks = new List<FamilyTask>
            {
                new()
                {
                    Id = Guid.NewGuid(), FamilyId = FamilyId,
                    Title = "Buy groceries for the week",
                    Description = "Check the shopping list before leaving.",
                    AssignedToMemberId = Member2Id, DueDate = At(0, 18),
                    Priority = TaskPriority.High, Status = TaskStatus.Pending,
                    CreatedByUserId = alice.Id, CreatedAt = now.AddDays(-1),
                },
                new()
                {
                    Id = Guid.NewGuid(), FamilyId = FamilyId,
                    Title = "Pay the electricity bill",
                    AssignedToMemberId = Member1Id, DueDate = At(2, 12),
                    Priority = TaskPriority.Medium, Status = TaskStatus.Pending,
                    CreatedByUserId = alice.Id, CreatedAt = now.AddDays(-1),
                },
                new()
                {
                    Id = Guid.NewGuid(), FamilyId = FamilyId,
                    Title = "Take out the recycling",
                    AssignedToMemberId = Member1Id, DueDate = At(-1, 8),
                    Priority = TaskPriority.Low, Status = TaskStatus.Completed,
                    CreatedByUserId = bob.Id, CreatedAt = now.AddDays(-3),
                    CompletedAt = now.AddDays(-1),
                },
                new()
                {
                    Id = Guid.NewGuid(), FamilyId = FamilyId,
                    Title = "Book Mia's dentist appointment",
                    Description = "Prefer a morning slot.",
                    AssignedToMemberId = Member2Id, DueDate = At(3, 12),
                    Priority = TaskPriority.Medium, Status = TaskStatus.InProgress,
                    CreatedByUserId = bob.Id, CreatedAt = now.AddDays(-2),
                },
                new()
                {
                    Id = Guid.NewGuid(), FamilyId = FamilyId,
                    Title = "Fix the leaking kitchen tap",
                    AssignedToMemberId = null, DueDate = At(5, 17),
                    Priority = TaskPriority.High, Status = TaskStatus.Pending,
                    CreatedByUserId = alice.Id, CreatedAt = now.AddDays(-1),
                },
            };

            // --- Five calendar events ---
            var events = new List<FamilyEvent>
            {
                new()
                {
                    Id = Guid.NewGuid(), FamilyId = FamilyId,
                    Title = "School drop-off", EventType = EventType.School,
                    StartDateTime = At(0, 8), EndDateTime = At(0, 8, 30),
                    Location = "Maple Elementary", AssignedMemberId = Member1Id,
                    ChildProfileId = ChildId, CreatedByUserId = alice.Id, CreatedAt = now.AddDays(-5),
                },
                new()
                {
                    Id = Guid.NewGuid(), FamilyId = FamilyId,
                    Title = "Mia — doctor check-up", EventType = EventType.Doctor,
                    StartDateTime = At(1, 10), EndDateTime = At(1, 10, 45),
                    Location = "City Health Clinic", AssignedMemberId = Member2Id,
                    ChildProfileId = ChildId, CreatedByUserId = bob.Id, CreatedAt = now.AddDays(-4),
                },
                new()
                {
                    Id = Guid.NewGuid(), FamilyId = FamilyId,
                    Title = "Football training", EventType = EventType.Training,
                    StartDateTime = At(2, 14), EndDateTime = At(2, 15, 30),
                    Location = "Community Sports Park", ChildProfileId = ChildId,
                    CreatedByUserId = alice.Id, CreatedAt = now.AddDays(-3),
                },
                new()
                {
                    Id = Guid.NewGuid(), FamilyId = FamilyId,
                    Title = "Grandma's birthday dinner", EventType = EventType.Birthday,
                    StartDateTime = At(4, 18), EndDateTime = At(4, 21),
                    Location = "Grandma's house", CreatedByUserId = alice.Id, CreatedAt = now.AddDays(-6),
                },
                new()
                {
                    Id = Guid.NewGuid(), FamilyId = FamilyId,
                    Title = "Family movie night", EventType = EventType.Family,
                    StartDateTime = At(0, 19), EndDateTime = At(0, 21),
                    Location = "Home", CreatedByUserId = bob.Id, CreatedAt = now.AddDays(-1),
                },
            };

            // --- One shopping list with a mix of purchased / unpurchased items ---
            var shoppingList = new ShoppingList
            {
                Id = ShoppingListId,
                FamilyId = FamilyId,
                Name = "Weekly groceries",
                CreatedAt = now.AddDays(-2),
                Items = new List<ShoppingItem>
                {
                    NewItem("Milk", "2 L", ItemCategory.Grocery, false, alice.Id, null, now),
                    NewItem("Bread", "1 loaf", ItemCategory.Grocery, false, bob.Id, null, now),
                    NewItem("Apples", "1 kg", ItemCategory.Grocery, true, alice.Id, alice.Id, now),
                    NewItem("Toothpaste", null, ItemCategory.Pharmacy, false, bob.Id, null, now),
                    NewItem("Dish soap", "1 bottle", ItemCategory.Home, true, bob.Id, bob.Id, now),
                    NewItem("Crayons", "1 pack", ItemCategory.Child, false, alice.Id, null, now),
                },
            };

            // --- Three pickup schedules (one per status of interest) ---
            var pickups = new List<PickupSchedule>
            {
                new()
                {
                    Id = Guid.NewGuid(), FamilyId = FamilyId, ChildProfileId = ChildId,
                    AssignedMemberId = Member1Id, PickupDateTime = At(0, 15, 30),
                    Location = "Maple Elementary — main gate", Notes = "Bring the booster seat.",
                    Status = PickupStatus.Pending, CreatedByUserId = alice.Id, CreatedAt = now.AddDays(-1),
                },
                new()
                {
                    Id = Guid.NewGuid(), FamilyId = FamilyId, ChildProfileId = ChildId,
                    AssignedMemberId = Member2Id, PickupDateTime = At(1, 15, 30),
                    Location = "After-school club", Status = PickupStatus.Confirmed,
                    CreatedByUserId = alice.Id, CreatedAt = now.AddDays(-1),
                },
                new()
                {
                    Id = Guid.NewGuid(), FamilyId = FamilyId, ChildProfileId = ChildId,
                    AssignedMemberId = Member1Id, PickupDateTime = At(2, 16),
                    Location = "Community Sports Park", Notes = "Running late — needs cover.",
                    Status = PickupStatus.CannotAttend, CreatedByUserId = bob.Id, CreatedAt = now.AddDays(-1),
                },
            };

            var confirmedPickup = pickups[1];
            var rejectedPickup = pickups[2];
            var groceriesTask = tasks[0];

            // --- Several notifications (both users, mixed types and read states) ---
            var notifications = new List<Notification>
            {
                new()
                {
                    Id = Guid.NewGuid(), UserId = bob.Id, FamilyId = FamilyId, Type = NotificationType.Calendar,
                    Title = "Pickup assigned",
                    Message = $"You have been assigned a pickup at {confirmedPickup.Location}.",
                    RelatedUrl = "/pickups", IsRead = false, CreatedAt = now.AddHours(-3),
                },
                new()
                {
                    Id = Guid.NewGuid(), UserId = bob.Id, FamilyId = FamilyId, Type = NotificationType.Task,
                    Title = "New task assigned",
                    Message = $"You have been assigned the task \"{groceriesTask.Title}\".",
                    RelatedUrl = "/tasks", IsRead = false, CreatedAt = now.AddHours(-2),
                },
                new()
                {
                    Id = Guid.NewGuid(), UserId = bob.Id, FamilyId = FamilyId, Type = NotificationType.Calendar,
                    Title = "Pickup rejected",
                    Message = $"The assigned member cannot attend the pickup at {rejectedPickup.Location}.",
                    RelatedUrl = "/pickups", IsRead = false, CreatedAt = now.AddHours(-1),
                },
                new()
                {
                    Id = Guid.NewGuid(), UserId = alice.Id, FamilyId = FamilyId, Type = NotificationType.Family,
                    Title = "New family member",
                    Message = "Bob has joined The Rivera Family.",
                    RelatedUrl = "/family", IsRead = true, CreatedAt = now.AddMonths(-2).AddDays(1),
                },
                new()
                {
                    Id = Guid.NewGuid(), UserId = alice.Id, FamilyId = FamilyId, Type = NotificationType.Calendar,
                    Title = "Pickup taken over",
                    Message = $"Another member has taken over the pickup at {rejectedPickup.Location}.",
                    RelatedUrl = "/pickups", IsRead = true, CreatedAt = now.AddMinutes(-30),
                },
            };

            db.Families.Add(family);
            db.FamilyMembers.AddRange(member1, member2);
            db.ChildProfiles.Add(child);
            db.FamilyTasks.AddRange(tasks);
            db.FamilyEvents.AddRange(events);
            db.ShoppingLists.Add(shoppingList);
            db.PickupSchedules.AddRange(pickups);
            db.Notifications.AddRange(notifications);

            await db.SaveChangesAsync();

            logger.LogInformation(
                "Seeded development data: family {FamilyId}, users {Email1} / {Email2} (password: {Password}).",
                FamilyId, Parent1Email, Parent2Email, SeedPassword);
        }
        catch (Exception ex)
        {
            // A seeding failure should not prevent the API from starting in development.
            logger.LogError(ex, "Development data seeding failed.");
        }
    }

    private static async Task<ApplicationUser> GetOrCreateUserAsync(
        UserManager<ApplicationUser> userManager,
        string id,
        string email,
        string firstName,
        string lastName)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return existing;
        }

        var user = new ApplicationUser
        {
            Id = id,
            Email = email,
            UserName = email,
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-2),
        };

        var result = await userManager.CreateAsync(user, SeedPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create seed user {email}: {errors}");
        }

        return user;
    }

    private static ShoppingItem NewItem(
        string name,
        string? quantity,
        ItemCategory category,
        bool purchased,
        string addedByUserId,
        string? purchasedByUserId,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Quantity = quantity,
            Category = category,
            IsPurchased = purchased,
            AddedByUserId = addedByUserId,
            PurchasedByUserId = purchased ? purchasedByUserId : null,
            CreatedAt = now.AddDays(-2),
            PurchasedAt = purchased ? now.AddHours(-5) : null,
        };
}
