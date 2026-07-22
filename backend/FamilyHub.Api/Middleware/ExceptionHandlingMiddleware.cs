using System.Net;
using FamilyHub.Api.Common;

namespace FamilyHub.Api.Middleware;

/// <summary>
/// Catches unhandled exceptions, logs them, and returns a consistent JSON error response.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new ErrorResponse(
                context.Response.StatusCode,
                "An unexpected error occurred.",
                _environment.IsDevelopment() ? ex.ToString() : null);

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
