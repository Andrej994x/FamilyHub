using Microsoft.AspNetCore.Identity;

namespace FamilyHub.Api.Models;

/// <summary>
/// Application user. Inherits <see cref="IdentityUser.Id"/>, <see cref="IdentityUser.Email"/>,
/// the password hash, and other Identity fields.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
