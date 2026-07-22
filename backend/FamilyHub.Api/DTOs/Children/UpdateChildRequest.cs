namespace FamilyHub.Api.DTOs.Children;

/// <summary>
/// Basic, non-sensitive child details. Do not use Notes for medical or other
/// sensitive personal data in this version.
/// </summary>
public record UpdateChildRequest(
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? Notes);
