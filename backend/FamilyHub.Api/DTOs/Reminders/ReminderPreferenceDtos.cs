namespace FamilyHub.Api.DTOs.Reminders;

/// <summary>
/// A user's effective reminder settings for one category. <see cref="IsDefault"/> is true when
/// the user has no saved row and these are the system defaults.
/// </summary>
public record ReminderPreferenceResponse(
    string Category,
    bool IsEnabled,
    int[] ReminderOffsetsDays,
    bool IsDefault);

/// <summary>Updates a single category's reminder settings for the current user.</summary>
public record UpdateReminderPreferenceRequest(
    bool IsEnabled,
    int[] ReminderOffsetsDays);
