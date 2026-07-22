using System.Security.Claims;
using FamilyHub.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace FamilyHub.Api.Controllers;

/// <summary>
/// Base controller with shared helpers for translating <see cref="Result"/> outcomes
/// into HTTP responses and reading the authenticated user id.
/// </summary>
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Id of the authenticated user (from the JWT 'sub' claim).</summary>
    protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    protected IActionResult HandleResult<T>(Result<T> result) =>
        result.Succeeded ? Ok(result.Value) : Error(result);

    protected IActionResult Error(Result result)
    {
        var status = result.ErrorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(status, new ErrorResponse(status, result.Error ?? "Request failed."));
    }
}
