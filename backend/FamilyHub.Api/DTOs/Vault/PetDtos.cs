namespace FamilyHub.Api.DTOs.Vault;

public class CreatePetRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Breed { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? MicrochipNumber { get; set; }
    public string? VaccinationName { get; set; }
    public DateTimeOffset? LastVaccinationDate { get; set; }
    public DateTimeOffset? NextVaccinationDate { get; set; }
    public string? Veterinarian { get; set; }
    public string? Notes { get; set; }
    public bool IsImportant { get; set; }
    public Guid? RelatedMemberId { get; set; }
    public List<IFormFile>? Attachments { get; set; }
}

public class UpdatePetRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Breed { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? MicrochipNumber { get; set; }
    public string? VaccinationName { get; set; }
    public DateTimeOffset? LastVaccinationDate { get; set; }
    public DateTimeOffset? NextVaccinationDate { get; set; }
    public string? Veterinarian { get; set; }
    public string? Notes { get; set; }
    public bool IsImportant { get; set; }
    public Guid? RelatedMemberId { get; set; }
    /// <summary>New files to append to the pet.</summary>
    public List<IFormFile>? Attachments { get; set; }
}

public record PetResponse(
    Guid Id,
    Guid FamilyId,
    string Name,
    string? Type,
    string? Breed,
    DateOnly? DateOfBirth,
    string? MicrochipNumber,
    string? VaccinationName,
    DateTimeOffset? LastVaccinationDate,
    DateTimeOffset? NextVaccinationDate,
    string? Veterinarian,
    string? Notes,
    bool IsImportant,
    Guid? RelatedMemberId,
    string CreatedByUserId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<VaultAttachmentResponse> Attachments);
