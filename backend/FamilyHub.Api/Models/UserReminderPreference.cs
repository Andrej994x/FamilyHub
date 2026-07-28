using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.Models;

/// <summary>
/// A user's reminder settings for one category/module. Absence of a row means "use the system
/// defaults" for that category. Offsets are stored as a JSON array (e.g. [30,7,1,0]) — unique,
/// non-negative and sorted descending.
/// </summary>
public class UserReminderPreference
{
    public Guid Id { get; set; }

    /// <summary>Owner — the <see cref="ApplicationUser"/> whose preference this is.</summary>
    public string UserId { get; set; } = string.Empty;

    public ReminderCategory Category { get; set; }

    /// <summary>Master switch — when false, no reminders fire for this user in this category.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Lead times in days, e.g. [30, 7, 1, 0]. Stored as a JSON array.</summary>
    public int[] ReminderOffsetsDays { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ApplicationUser? User { get; set; }
}
