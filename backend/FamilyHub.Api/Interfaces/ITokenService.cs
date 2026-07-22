using FamilyHub.Api.Common;
using FamilyHub.Api.Models;

namespace FamilyHub.Api.Interfaces;

public interface ITokenService
{
    /// <summary>Creates a signed JWT access token for the given user.</summary>
    TokenResult CreateToken(ApplicationUser user);
}
