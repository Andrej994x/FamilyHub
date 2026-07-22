using Microsoft.AspNetCore.Mvc;

namespace FamilyHub.Api.Controllers;

/// <summary>
/// Lightweight liveness endpoint used to verify the API is running.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() =>
        Ok(new { status = "Healthy", timestamp = DateTimeOffset.UtcNow });
}
