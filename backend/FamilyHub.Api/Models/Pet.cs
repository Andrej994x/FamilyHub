namespace FamilyHub.Api.Models;

public class Pet
{
    public Guid Id { get; set; }

    public Guid FamilyId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Free-form kind, e.g. "Dog", "Cat".</summary>
    public string? Type { get; set; }

    public string? Breed { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? MicrochipNumber { get; set; }

    public string? VaccinationName { get; set; }

    public DateTimeOffset? LastVaccinationDate { get; set; }

    public DateTimeOffset? NextVaccinationDate { get; set; }

    public string? Veterinarian { get; set; }

    public string? Notes { get; set; }

    /// <summary>When true, creating the record notifies Owner/Parent members (and the related person).</summary>
    public bool IsImportant { get; set; }

    /// <summary>Optional <see cref="FamilyMember"/> this record concerns (the "related person").</summary>
    public Guid? RelatedMemberId { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Family? Family { get; set; }
}
