namespace FamilyHub.Api.Common;

/// <summary>
/// Standard error payload returned by the API for unhandled failures.
/// </summary>
public record ErrorResponse(int Status, string Message, string? Detail = null);
