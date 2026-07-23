namespace FamilyHub.Api.Models;

public class Vehicle
{
    public Guid Id { get; set; }

    public Guid FamilyId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Make { get; set; }

    public string? Model { get; set; }

    public string? RegistrationNumber { get; set; }

    public DateTimeOffset? RegistrationExpiry { get; set; }

    public DateTimeOffset? InsuranceExpiry { get; set; }

    public DateTimeOffset? NextServiceDate { get; set; }

    public int? NextServiceMileage { get; set; }

    public string? Notes { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Family? Family { get; set; }
}
