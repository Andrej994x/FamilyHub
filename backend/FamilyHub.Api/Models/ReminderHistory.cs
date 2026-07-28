using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.Models;

/// <summary>
/// Record that a reminder for a specific milestone has been delivered, so the daily reminder
/// run never sends the same one twice. One row per (source, date field, due date, days-before).
/// If the underlying date changes, the due-date component changes and fresh reminders can fire.
/// </summary>
public class ReminderHistory
{
    public Guid Id { get; set; }

    public Guid FamilyId { get; set; }

    /// <summary>The user this reminder was delivered to (reminders are decided per recipient).</summary>
    public string RecipientUserId { get; set; } = string.Empty;

    public ReminderSourceType SourceType { get; set; }

    /// <summary>Id of the source entity (task, vehicle, document, …).</summary>
    public Guid SourceId { get; set; }

    /// <summary>Which date on the entity this reminder is about, e.g. "InsuranceExpiry".</summary>
    public string DateKind { get; set; } = string.Empty;

    /// <summary>The target date the reminder counts down to.</summary>
    public DateOnly DueDate { get; set; }

    /// <summary>How many days before <see cref="DueDate"/> this reminder fired (30, 7, 1 or 0).</summary>
    public int DaysBefore { get; set; }

    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
}
