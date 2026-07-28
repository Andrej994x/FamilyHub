using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using TaskStatus = FamilyHub.Api.Models.Enums.TaskStatus;

namespace FamilyHub.Api.Services;

public class ReminderService : IReminderService
{
    private const string VaultUrl = "/vault";
    private const string CalendarUrl = "/calendar";
    private const string TasksUrl = "/tasks";

    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ILogger<ReminderService> _logger;

    // Per-run caches (a fresh service instance is created per run, so these are safe).
    private DateOnly _today;
    private HashSet<string> _sentKeys = new();
    private Dictionary<Guid, List<MemberInfo>> _members = new();
    private Dictionary<(string UserId, ReminderCategory Category), (bool Enabled, int[] Offsets)> _prefs = new();

    public ReminderService(AppDbContext db, INotificationService notifications, ILogger<ReminderService> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    private readonly record struct MemberInfo(Guid MemberId, string UserId, FamilyRole Role);

    public async Task<int> ProcessRemindersAsync(DateOnly today, CancellationToken cancellationToken = default)
    {
        _today = today;

        // --- Load everything the run needs up front (point 7: no per-record preference queries) ---
        await LoadMembersAsync(cancellationToken);
        await LoadPreferencesAsync(cancellationToken);
        _sentKeys = await LoadSentKeysAsync(today, cancellationToken);

        // The horizon spans the widest enabled offset across defaults and saved preferences.
        var maxPrefOffset = _prefs.Values
            .Where(p => p.Enabled)
            .SelectMany(p => p.Offsets)
            .DefaultIfEmpty(0)
            .Max();
        var maxOffset = Math.Max(ReminderCategoryDefaults.MaxDefaultOffset, maxPrefOffset);

        var lowerUtc = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var upperUtc = new DateTimeOffset(today.AddDays(maxOffset + 1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var sent = 0;
        sent += await ProcessTasksAsync(lowerUtc, upperUtc, cancellationToken);
        sent += await ProcessEventsAsync(lowerUtc, upperUtc, cancellationToken);
        sent += await ProcessDocumentsAsync(lowerUtc, upperUtc, cancellationToken);
        sent += await ProcessVehiclesAsync(lowerUtc, upperUtc, cancellationToken);
        sent += await ProcessPetsAsync(lowerUtc, upperUtc, cancellationToken);
        sent += await ProcessHomeRecordsAsync(lowerUtc, upperUtc, cancellationToken);
        sent += await ProcessWarrantiesAsync(lowerUtc, upperUtc, cancellationToken);
        sent += await ProcessOtherRecordsAsync(lowerUtc, upperUtc, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return sent;
    }

    // ---- Sources ----

    private async Task<int> ProcessTasksAsync(DateTimeOffset lowerUtc, DateTimeOffset upperUtc, CancellationToken ct)
    {
        var tasks = await _db.FamilyTasks
            .Where(t => t.DueDate != null && t.DueDate >= lowerUtc && t.DueDate < upperUtc
                && (t.Status == TaskStatus.Pending || t.Status == TaskStatus.InProgress))
            .Select(t => new { t.Id, t.FamilyId, t.Title, Due = t.DueDate!.Value, t.AssignedToMemberId })
            .ToListAsync(ct);

        var sent = 0;
        foreach (var t in tasks)
        {
            var due = DateOnly.FromDateTime(t.Due.UtcDateTime);
            foreach (var userId in AssigneeOrFamily(t.FamilyId, t.AssignedToMemberId))
            {
                sent += await RemindUserAsync(
                    userId, ReminderCategory.Tasks, ReminderSourceType.Task, t.Id, t.FamilyId, "DueDate", due,
                    NotificationType.Task, TasksUrl, "Task reminder",
                    d => $"Task \"{t.Title}\" is due {WhenPhrase(d)}.");
            }
        }
        return sent;
    }

    private async Task<int> ProcessEventsAsync(DateTimeOffset lowerUtc, DateTimeOffset upperUtc, CancellationToken ct)
    {
        var events = await _db.FamilyEvents
            .Where(e => e.StartDateTime >= lowerUtc && e.StartDateTime < upperUtc)
            .Select(e => new { e.Id, e.FamilyId, e.Title, e.StartDateTime, e.AssignedMemberId })
            .ToListAsync(ct);

        var sent = 0;
        foreach (var e in events)
        {
            var due = DateOnly.FromDateTime(e.StartDateTime.UtcDateTime);
            foreach (var userId in AssigneeOrFamily(e.FamilyId, e.AssignedMemberId))
            {
                sent += await RemindUserAsync(
                    userId, ReminderCategory.CalendarEvents, ReminderSourceType.CalendarEvent, e.Id, e.FamilyId,
                    "StartDateTime", due, NotificationType.Calendar, CalendarUrl, "Event reminder",
                    d => $"\"{e.Title}\" is {WhenPhrase(d)}.");
            }
        }
        return sent;
    }

    private async Task<int> ProcessDocumentsAsync(DateTimeOffset lowerUtc, DateTimeOffset upperUtc, CancellationToken ct)
    {
        var docs = await _db.FamilyDocuments
            .Where(d => d.ExpiryDate != null && d.ExpiryDate >= lowerUtc && d.ExpiryDate < upperUtc)
            .Select(d => new { d.Id, d.FamilyId, d.DocumentType, Expiry = d.ExpiryDate!.Value })
            .ToListAsync(ct);

        var sent = 0;
        foreach (var d in docs)
        {
            var due = DateOnly.FromDateTime(d.Expiry.UtcDateTime);
            foreach (var userId in OwnerParent(d.FamilyId))
            {
                sent += await RemindUserAsync(
                    userId, ReminderCategory.Documents, ReminderSourceType.FamilyDocument, d.Id, d.FamilyId,
                    "ExpiryDate", due, NotificationType.FamilyVault, VaultUrl, "Document expiring",
                    day => $"Your {d.DocumentType} document expires {WhenPhrase(day)}.");
            }
        }
        return sent;
    }

    private async Task<int> ProcessVehiclesAsync(DateTimeOffset lowerUtc, DateTimeOffset upperUtc, CancellationToken ct)
    {
        var vehicles = await _db.Vehicles
            .Where(v =>
                (v.RegistrationExpiry != null && v.RegistrationExpiry >= lowerUtc && v.RegistrationExpiry < upperUtc) ||
                (v.InsuranceExpiry != null && v.InsuranceExpiry >= lowerUtc && v.InsuranceExpiry < upperUtc) ||
                (v.NextServiceDate != null && v.NextServiceDate >= lowerUtc && v.NextServiceDate < upperUtc))
            .Select(v => new { v.Id, v.FamilyId, v.Name, v.RegistrationExpiry, v.InsuranceExpiry, v.NextServiceDate })
            .ToListAsync(ct);

        var sent = 0;
        foreach (var v in vehicles)
        {
            sent += await RemindVehicleDateAsync(v.Id, v.FamilyId, "RegistrationExpiry",
                v.RegistrationExpiry, $"Registration for \"{v.Name}\" expires");
            sent += await RemindVehicleDateAsync(v.Id, v.FamilyId, "InsuranceExpiry",
                v.InsuranceExpiry, $"Insurance for \"{v.Name}\" expires");
            sent += await RemindVehicleDateAsync(v.Id, v.FamilyId, "NextServiceDate",
                v.NextServiceDate, $"Service for \"{v.Name}\" is due");
        }
        return sent;
    }

    private async Task<int> RemindVehicleDateAsync(
        Guid id, Guid familyId, string dateKind, DateTimeOffset? date, string lead)
    {
        if (date is not DateTimeOffset value)
        {
            return 0;
        }
        var due = DateOnly.FromDateTime(value.UtcDateTime);
        var sent = 0;
        foreach (var userId in OwnerParent(familyId))
        {
            sent += await RemindUserAsync(
                userId, ReminderCategory.Vehicles, ReminderSourceType.Vehicle, id, familyId, dateKind, due,
                NotificationType.FamilyVault, VaultUrl, "Vehicle reminder", d => $"{lead} {WhenPhrase(d)}.");
        }
        return sent;
    }

    private async Task<int> ProcessPetsAsync(DateTimeOffset lowerUtc, DateTimeOffset upperUtc, CancellationToken ct)
    {
        var pets = await _db.Pets
            .Where(p => p.NextVaccinationDate != null && p.NextVaccinationDate >= lowerUtc && p.NextVaccinationDate < upperUtc)
            .Select(p => new { p.Id, p.FamilyId, p.Name, Next = p.NextVaccinationDate!.Value })
            .ToListAsync(ct);

        var sent = 0;
        foreach (var p in pets)
        {
            var due = DateOnly.FromDateTime(p.Next.UtcDateTime);
            foreach (var userId in OwnerParent(p.FamilyId))
            {
                sent += await RemindUserAsync(
                    userId, ReminderCategory.Pets, ReminderSourceType.Pet, p.Id, p.FamilyId, "NextVaccinationDate",
                    due, NotificationType.FamilyVault, VaultUrl, "Pet vaccination due",
                    d => $"{p.Name}'s vaccination is due {WhenPhrase(d)}.");
            }
        }
        return sent;
    }

    private async Task<int> ProcessHomeRecordsAsync(DateTimeOffset lowerUtc, DateTimeOffset upperUtc, CancellationToken ct)
    {
        var records = await _db.HomeRecords
            .Where(h => h.RenewalDate != null && h.RenewalDate >= lowerUtc && h.RenewalDate < upperUtc)
            .Select(h => new { h.Id, h.FamilyId, h.Title, Renewal = h.RenewalDate!.Value })
            .ToListAsync(ct);

        var sent = 0;
        foreach (var h in records)
        {
            var due = DateOnly.FromDateTime(h.Renewal.UtcDateTime);
            foreach (var userId in OwnerParent(h.FamilyId))
            {
                sent += await RemindUserAsync(
                    userId, ReminderCategory.Home, ReminderSourceType.HomeRecord, h.Id, h.FamilyId, "RenewalDate",
                    due, NotificationType.FamilyVault, VaultUrl, "Home record renewal",
                    d => $"\"{h.Title}\" renews {WhenPhrase(d)}.");
            }
        }
        return sent;
    }

    private async Task<int> ProcessWarrantiesAsync(DateTimeOffset lowerUtc, DateTimeOffset upperUtc, CancellationToken ct)
    {
        var warranties = await _db.Warranties
            .Where(w => w.WarrantyExpiryDate != null && w.WarrantyExpiryDate >= lowerUtc && w.WarrantyExpiryDate < upperUtc)
            .Select(w => new { w.Id, w.FamilyId, w.ProductName, Expiry = w.WarrantyExpiryDate!.Value })
            .ToListAsync(ct);

        var sent = 0;
        foreach (var w in warranties)
        {
            var due = DateOnly.FromDateTime(w.Expiry.UtcDateTime);
            foreach (var userId in OwnerParent(w.FamilyId))
            {
                sent += await RemindUserAsync(
                    userId, ReminderCategory.Warranties, ReminderSourceType.Warranty, w.Id, w.FamilyId,
                    "WarrantyExpiryDate", due, NotificationType.FamilyVault, VaultUrl, "Warranty expiring",
                    d => $"Warranty for \"{w.ProductName}\" expires {WhenPhrase(d)}.");
            }
        }
        return sent;
    }

    private async Task<int> ProcessOtherRecordsAsync(DateTimeOffset lowerUtc, DateTimeOffset upperUtc, CancellationToken ct)
    {
        var records = await _db.OtherRecords
            .Where(o =>
                (o.ExpiryDate != null && o.ExpiryDate >= lowerUtc && o.ExpiryDate < upperUtc) ||
                (o.ImportantDate != null && o.ImportantDate >= lowerUtc && o.ImportantDate < upperUtc))
            .Select(o => new { o.Id, o.FamilyId, o.Title, o.ExpiryDate, o.ImportantDate })
            .ToListAsync(ct);

        var sent = 0;
        foreach (var o in records)
        {
            foreach (var userId in OwnerParent(o.FamilyId))
            {
                if (o.ExpiryDate is DateTimeOffset expiry)
                {
                    sent += await RemindUserAsync(
                        userId, ReminderCategory.OtherVaultItems, ReminderSourceType.OtherRecord, o.Id, o.FamilyId,
                        "ExpiryDate", DateOnly.FromDateTime(expiry.UtcDateTime), NotificationType.FamilyVault, VaultUrl,
                        "Vault item reminder", d => $"\"{o.Title}\" expires {WhenPhrase(d)}.");
                }
                if (o.ImportantDate is DateTimeOffset important)
                {
                    sent += await RemindUserAsync(
                        userId, ReminderCategory.OtherVaultItems, ReminderSourceType.OtherRecord, o.Id, o.FamilyId,
                        "ImportantDate", DateOnly.FromDateTime(important.UtcDateTime), NotificationType.FamilyVault, VaultUrl,
                        "Vault item reminder", d => $"\"{o.Title}\" — important date {WhenPhrase(d)}.");
                }
            }
        }
        return sent;
    }

    // ---- Core per-recipient logic ----

    /// <summary>
    /// Sends one reminder to <paramref name="userId"/> if their preference for the category is
    /// enabled, today is one of their configured offsets, and it hasn't already been delivered.
    /// </summary>
    private async Task<int> RemindUserAsync(
        string userId, ReminderCategory category, ReminderSourceType sourceType, Guid sourceId, Guid familyId,
        string dateKind, DateOnly due, NotificationType notificationType, string url, string title,
        Func<int, string> message)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return 0;
        }

        var (enabled, offsets) = EffectivePreference(userId, category);
        if (!enabled)
        {
            return 0; // user disabled this category
        }

        var daysBefore = due.DayNumber - _today.DayNumber;
        if (daysBefore < 0 || !offsets.Contains(daysBefore))
        {
            return 0; // not one of this user's lead times
        }

        var key = ReminderKey(userId, sourceType, sourceId, dateKind, due, daysBefore);
        if (!_sentKeys.Add(key))
        {
            return 0; // already delivered to this user
        }

        try
        {
            await _notifications.CreateForUserAsync(userId, notificationType, title, message(daysBefore), familyId, url);
        }
        catch (Exception ex)
        {
            _sentKeys.Remove(key);
            _logger.LogWarning(ex, "Failed to send reminder to {UserId} for {Type} {SourceId}.", userId, sourceType, sourceId);
            return 0;
        }

        _db.ReminderHistory.Add(new ReminderHistory
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            RecipientUserId = userId,
            SourceType = sourceType,
            SourceId = sourceId,
            DateKind = dateKind,
            DueDate = due,
            DaysBefore = daysBefore,
            SentAt = DateTimeOffset.UtcNow,
        });
        return 1;
    }

    private (bool Enabled, int[] Offsets) EffectivePreference(string userId, ReminderCategory category) =>
        _prefs.TryGetValue((userId, category), out var pref)
            ? pref
            : (true, ReminderCategoryDefaults.For(category));

    // ---- Recipient resolution (from the cached family membership) ----

    private IEnumerable<string> AssigneeOrFamily(Guid familyId, Guid? assignedMemberId)
    {
        if (!_members.TryGetValue(familyId, out var members))
        {
            return [];
        }
        if (assignedMemberId is Guid memberId)
        {
            var match = members.FirstOrDefault(m => m.MemberId == memberId);
            return match.UserId is null ? [] : [match.UserId];
        }
        return members.Select(m => m.UserId).Distinct();
    }

    private IEnumerable<string> OwnerParent(Guid familyId) =>
        _members.TryGetValue(familyId, out var members)
            ? members.Where(m => m.Role is FamilyRole.Owner or FamilyRole.Parent).Select(m => m.UserId).Distinct()
            : [];

    // ---- Caches ----

    private async Task LoadMembersAsync(CancellationToken ct)
    {
        var members = await _db.FamilyMembers
            .Select(m => new { m.FamilyId, m.Id, m.UserId, m.Role })
            .ToListAsync(ct);

        _members = members
            .GroupBy(m => m.FamilyId)
            .ToDictionary(g => g.Key, g => g.Select(m => new MemberInfo(m.Id, m.UserId, m.Role)).ToList());
    }

    private async Task LoadPreferencesAsync(CancellationToken ct)
    {
        var prefs = await _db.UserReminderPreferences
            .Select(p => new { p.UserId, p.Category, p.IsEnabled, p.ReminderOffsetsDays })
            .ToListAsync(ct);

        _prefs = prefs.ToDictionary(
            p => (p.UserId, p.Category),
            p => (p.IsEnabled, p.ReminderOffsetsDays));
    }

    private async Task<HashSet<string>> LoadSentKeysAsync(DateOnly today, CancellationToken ct)
    {
        var rows = await _db.ReminderHistory
            .Where(r => r.DueDate >= today)
            .Select(r => new { r.RecipientUserId, r.SourceType, r.SourceId, r.DateKind, r.DueDate, r.DaysBefore })
            .ToListAsync(ct);

        var set = new HashSet<string>();
        foreach (var r in rows)
        {
            set.Add(ReminderKey(r.RecipientUserId, r.SourceType, r.SourceId, r.DateKind, r.DueDate, r.DaysBefore));
        }
        return set;
    }

    // ---- Helpers ----

    private static string WhenPhrase(int daysBefore) => daysBefore switch
    {
        0 => "today",
        1 => "tomorrow",
        _ => $"in {daysBefore} days",
    };

    private static string ReminderKey(
        string userId, ReminderSourceType type, Guid sourceId, string dateKind, DateOnly due, int daysBefore) =>
        $"{userId}|{type}|{sourceId}|{dateKind}|{due:yyyy-MM-dd}|{daysBefore}";
}
