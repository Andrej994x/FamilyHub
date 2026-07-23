using FamilyHub.Api.Interfaces;

namespace FamilyHub.Api.Services;

/// <summary>
/// Development email sender. Instead of delivering mail it logs the message (including any
/// links), so the invitation flow can be exercised end-to-end without a mail server.
/// </summary>
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? textBody = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "EMAIL (dev, not delivered)\n  To: {To}\n  Subject: {Subject}\n  Body:\n{Body}",
            toEmail,
            subject,
            textBody ?? htmlBody);

        return Task.CompletedTask;
    }
}
