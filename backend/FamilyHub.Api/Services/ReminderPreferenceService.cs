using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Reminders;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Services;

public class ReminderPreferenceService : IReminderPreferenceService
{
    private readonly AppDbContext _db;

    public ReminderPreferenceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ReminderPreferenceResponse>> GetAllAsync(string userId)
    {
        var saved = await _db.UserReminderPreferences
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.Category);

        return ReminderCategoryDefaults.Categories
            .Select(category => saved.TryGetValue(category, out var p) ? ToResponse(p) : DefaultResponse(category))
            .ToList();
    }

    public async Task<ReminderPreferenceResponse> GetAsync(string userId, ReminderCategory category)
    {
        var preference = await _db.UserReminderPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Category == category);

        return preference is null ? DefaultResponse(category) : ToResponse(preference);
    }

    public async Task<ReminderPreferenceResponse> UpdateAsync(
        string userId, ReminderCategory category, UpdateReminderPreferenceRequest request)
    {
        // Defensive normalisation: unique + sorted descending (validation already rejects bad input).
        var offsets = request.ReminderOffsetsDays.Distinct().OrderByDescending(v => v).ToArray();
        var now = DateTimeOffset.UtcNow;

        var preference = await _db.UserReminderPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Category == category);

        if (preference is null)
        {
            preference = new UserReminderPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Category = category,
                CreatedAt = now,
            };
            _db.UserReminderPreferences.Add(preference);
        }

        preference.IsEnabled = request.IsEnabled;
        preference.ReminderOffsetsDays = offsets;
        preference.UpdatedAt = now;

        await _db.SaveChangesAsync();

        return ToResponse(preference);
    }

    public Task ResetDefaultsAsync(string userId) =>
        _db.UserReminderPreferences.Where(p => p.UserId == userId).ExecuteDeleteAsync();

    private static ReminderPreferenceResponse DefaultResponse(ReminderCategory category) =>
        new(category.ToString(), IsEnabled: true, ReminderCategoryDefaults.For(category), IsDefault: true);

    private static ReminderPreferenceResponse ToResponse(UserReminderPreference p) =>
        new(p.Category.ToString(), p.IsEnabled, p.ReminderOffsetsDays, IsDefault: false);
}
