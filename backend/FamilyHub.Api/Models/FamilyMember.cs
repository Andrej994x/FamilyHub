using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.Models;

public class FamilyMember
{
    public Guid Id { get; set; }

    public Guid FamilyId { get; set; }

    /// <summary>Id of the <see cref="ApplicationUser"/> who is a member of the family.</summary>
    public string UserId { get; set; } = string.Empty;

    public FamilyRole Role { get; set; }

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Family? Family { get; set; }

    public ApplicationUser? User { get; set; }
}
