using FamilyHub.Api.DTOs.Auth;
using FamilyHub.Api.Validators;

namespace FamilyHub.Api.UnitTests.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    private static RegisterRequest Valid() => new("Ana", "Petrova", "ana@example.com", "Secret123");

    [Fact]
    public void Accepts_a_complete_request()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    [Theory]
    [InlineData("short1A", "Password must be at least 8 characters long.")]
    [InlineData("alllowercase1", "Password must contain an uppercase letter.")]
    [InlineData("ALLUPPERCASE1", "Password must contain a lowercase letter.")]
    [InlineData("NoDigitsHere", "Password must contain a digit.")]
    public void Rejects_passwords_that_break_a_rule(string password, string expectedMessage)
    {
        var result = _validator.Validate(Valid() with { Password = password });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(RegisterRequest.Password) && e.ErrorMessage == expectedMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Rejects_a_missing_or_malformed_email(string email)
    {
        var result = _validator.Validate(Valid() with { Email = email });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.Email));
    }

    [Fact]
    public void Rejects_missing_names()
    {
        var result = _validator.Validate(Valid() with { FirstName = "", LastName = "" });

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.FirstName));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.LastName));
    }
}
