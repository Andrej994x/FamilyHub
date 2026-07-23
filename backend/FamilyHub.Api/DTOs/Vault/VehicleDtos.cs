namespace FamilyHub.Api.DTOs.Vault;

public class CreateVehicleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? RegistrationNumber { get; set; }
    public DateTimeOffset? RegistrationExpiry { get; set; }
    public DateTimeOffset? InsuranceExpiry { get; set; }
    public DateTimeOffset? NextServiceDate { get; set; }
    public int? NextServiceMileage { get; set; }
    public string? Notes { get; set; }
    public List<IFormFile>? Attachments { get; set; }
}

public class UpdateVehicleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? RegistrationNumber { get; set; }
    public DateTimeOffset? RegistrationExpiry { get; set; }
    public DateTimeOffset? InsuranceExpiry { get; set; }
    public DateTimeOffset? NextServiceDate { get; set; }
    public int? NextServiceMileage { get; set; }
    public string? Notes { get; set; }
    /// <summary>New files to append to the vehicle.</summary>
    public List<IFormFile>? Attachments { get; set; }
}

public record VehicleResponse(
    Guid Id,
    Guid FamilyId,
    string Name,
    string? Make,
    string? Model,
    string? RegistrationNumber,
    DateTimeOffset? RegistrationExpiry,
    DateTimeOffset? InsuranceExpiry,
    DateTimeOffset? NextServiceDate,
    int? NextServiceMileage,
    string? Notes,
    string CreatedByUserId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<VaultAttachmentResponse> Attachments);
