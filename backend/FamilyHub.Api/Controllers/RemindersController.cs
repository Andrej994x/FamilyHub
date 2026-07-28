using FamilyHub.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reminders")]
public class RemindersController : ApiControllerBase
{
    private readonly IReminderService _reminders;
    private readonly IWebHostEnvironment _environment;

    public RemindersController(IReminderService reminders, IWebHostEnvironment environment)
    {
        _reminders = reminders;
        _environment = environment;
    }

    /// <summary>
    /// Development-only: runs the reminder pass on demand. An optional <paramref name="date"/>
    /// (yyyy-MM-dd) overrides "today" so specific milestones can be exercised deterministically.
    /// Returns 404 outside Development.
    /// </summary>
    [HttpPost("run")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Run([FromQuery] DateOnly? date)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var today = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var count = await _reminders.ProcessRemindersAsync(today);
        return Ok(new { date = today, remindersSent = count });
    }
}
