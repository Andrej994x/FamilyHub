namespace FamilyHub.Api.DTOs.Children;

public record ChildResponse(
    Guid Id,
    Guid FamilyId,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? Notes,
    DateTimeOffset CreatedAt);
