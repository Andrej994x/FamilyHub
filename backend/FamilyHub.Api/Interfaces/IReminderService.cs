namespace FamilyHub.Api.Interfaces;

/// <summary>
/// Scans the family's important dates and delivers due reminders (in-app + push), once each.
/// Called daily by the background service; also invokable directly for testing.
/// </summary>
public interface IReminderService
{
    /// <summary>
    /// Processes all reminder sources relative to <paramref name="today"/> and returns the
    /// number of reminders sent in this run.
    /// </summary>
    Task<int> ProcessRemindersAsync(DateOnly today, CancellationToken cancellationToken = default);
}
