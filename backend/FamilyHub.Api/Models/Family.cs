namespace FamilyHub.Api.Models;

public class Family
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Id of the <see cref="ApplicationUser"/> who created the family.</summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public ApplicationUser? CreatedByUser { get; set; }

    public ICollection<FamilyMember> Members { get; set; } = new List<FamilyMember>();

    public ICollection<FamilyInvitation> Invitations { get; set; } = new List<FamilyInvitation>();

    public ICollection<ChildProfile> Children { get; set; } = new List<ChildProfile>();
}
