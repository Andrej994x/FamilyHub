namespace FamilyHub.Api.Common;

/// <summary>
/// Reminder engine configuration, bound from the "Reminders" section of appsettings.
/// Lead times are configured per user per category (see reminder preferences), not here.
/// </summary>
public class ReminderSettings
{
    /// <summary>Hour of day (UTC, 0-23) the daily reminder pass runs.</summary>
    public int RunAtHourUtc { get; set; } = 8;
}
