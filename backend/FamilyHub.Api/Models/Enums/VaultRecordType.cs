namespace FamilyHub.Api.Models.Enums;

/// <summary>
/// The kind of Family Vault record an attachment belongs to. Documents are excluded
/// because they carry a single attachment column of their own.
/// </summary>
public enum VaultRecordType
{
    Vehicle,
    Pet,
    Home,
    Warranty,
    Other
}
