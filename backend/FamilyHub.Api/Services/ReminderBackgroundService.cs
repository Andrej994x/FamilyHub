using FamilyHub.Api.Common;
using FamilyHub.Api.Interfaces;
using Microsoft.Extensions.Options;

namespace FamilyHub.Api.Services;

/// <summary>
/// Runs the reminder pass once shortly after startup, then daily at the configured UTC hour.
/// The engine dedups via reminder history, so an extra run (e.g. on restart) is harmless.
/// </summary>
public class ReminderBackgroundService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(15);

    private readonly IServiceProvider _services;
    private readonly ReminderSettings _settings;
    private readonly ILogger<ReminderBackgroundService> _logger;

    public ReminderBackgroundService(
        IServiceProvider services,
        IOptions<ReminderSettings> settings,
        ILogger<ReminderBackgroundService> logger)
    {
        _services = services;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let startup (and migrations/seeding) settle before the first pass.
        if (!await DelayAsync(StartupDelay, stoppingToken))
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceAsync(stoppingToken);

            if (!await DelayAsync(TimeUntilNextRun(), stoppingToken))
            {
                return;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _services.CreateScope();
            var reminders = scope.ServiceProvider.GetRequiredService<IReminderService>();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var count = await reminders.ProcessRemindersAsync(today, ct);
            _logger.LogInformation("Reminder run complete for {Date}: {Count} reminder(s) sent.", today, count);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Reminder run failed.");
        }
    }

    private TimeSpan TimeUntilNextRun()
    {
        var now = DateTime.UtcNow;
        var hour = Math.Clamp(_settings.RunAtHourUtc, 0, 23);
        var next = now.Date.AddHours(hour);
        if (next <= now)
        {
            next = next.AddDays(1);
        }
        return next - now;
    }

    /// <summary>Delays, returning false if cancellation was requested during the wait.</summary>
    private static async Task<bool> DelayAsync(TimeSpan delay, CancellationToken ct)
    {
        try
        {
            await Task.Delay(delay, ct);
            return true;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }
}
