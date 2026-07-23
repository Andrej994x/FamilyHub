namespace FamilyHub.Api.Interfaces;

/// <summary>
/// Minimal email abstraction. The development implementation logs the message; a real
/// SMTP implementation is used when SMTP settings are configured.
/// </summary>
public interface IEmailSender
{
    Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? textBody = null,
        CancellationToken cancellationToken = default);
}
