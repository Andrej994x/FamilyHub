using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.Common;

/// <summary>
/// System default reminder lead times (in days) per category, used when a user has no saved
/// preference for that category. Offsets are unique, non-negative and sorted descending.
/// </summary>
public static class ReminderCategoryDefaults
{
    private static readonly IReadOnlyDictionary<ReminderCategory, int[]> Defaults =
        new Dictionary<ReminderCategory, int[]>
        {
            [ReminderCategory.Tasks] = [1, 0],
            [ReminderCategory.CalendarEvents] = [7, 1, 0],
            [ReminderCategory.Documents] = [30, 7, 1, 0],
            [ReminderCategory.Vehicles] = [30, 7, 1, 0],
            [ReminderCategory.Pets] = [30, 7, 1, 0],
            [ReminderCategory.Home] = [30, 7, 1, 0],
            [ReminderCategory.Warranties] = [30, 7, 1, 0],
            [ReminderCategory.OtherVaultItems] = [30, 7, 1, 0],
        };

    /// <summary>All supported categories.</summary>
    public static IEnumerable<ReminderCategory> Categories => Defaults.Keys;

    /// <summary>The default offsets for a category (a fresh copy so callers can't mutate it).</summary>
    public static int[] For(ReminderCategory category) =>
        Defaults.TryGetValue(category, out var offsets) ? (int[])offsets.Clone() : [];

    /// <summary>The largest default offset across all categories (bounds the scan horizon).</summary>
    public static int MaxDefaultOffset => Defaults.Values.SelectMany(o => o).DefaultIfEmpty(0).Max();
}
