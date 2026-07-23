using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.DTOs.Documents;

/// <summary>
/// Optional query-string filters for listing documents. All filters are applied server-side.
/// </summary>
public class DocumentFilter
{
    public DocumentType? DocumentType { get; set; }

    public Guid? FamilyMemberId { get; set; }

    public Guid? ChildProfileId { get; set; }
}
