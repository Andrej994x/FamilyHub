using FamilyHub.Api.DTOs.Reminders;
using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.Interfaces;

/// <summary>
/// Reads and updates the current user's reminder preferences. Every method is scoped to the
/// given <c>userId</c>, so a user can only ever see or change their own settings. GET operations
/// return effective values (system defaults when no row exists).
/// </summary>
public interface IReminderPreferenceService
{
    Task<IReadOnlyList<ReminderPreferenceResponse>> GetAllAsync(string userId);

    Task<ReminderPreferenceResponse> GetAsync(string userId, ReminderCategory category);

    Task<ReminderPreferenceResponse> UpdateAsync(
        string userId, ReminderCategory category, UpdateReminderPreferenceRequest request);

    Task ResetDefaultsAsync(string userId);
}
