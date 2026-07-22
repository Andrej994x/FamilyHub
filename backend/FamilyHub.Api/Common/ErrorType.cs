namespace FamilyHub.Api.Common;

/// <summary>
/// Classifies a service failure so the API layer can map it to an HTTP status code.
/// </summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Forbidden,
    Conflict
}
