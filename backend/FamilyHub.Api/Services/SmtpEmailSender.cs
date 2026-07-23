using System.Net;
using System.Net.Mail;
using FamilyHub.Api.Common;
using FamilyHub.Api.Interfaces;
using Microsoft.Extensions.Options;

namespace FamilyHub.Api.Services;

/// <summary>
/// Sends email over SMTP using the configured <see cref="EmailSettings"/>. Registered only
/// when an SMTP host is configured; otherwise <see cref="LoggingEmailSender"/> is used.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? textBody = null,
        CancellationToken cancellationToken = default)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_settings.Smtp.Host, _settings.Smtp.Port)
        {
            EnableSsl = _settings.Smtp.UseSsl,
        };

        if (!string.IsNullOrEmpty(_settings.Smtp.Username))
        {
            client.Credentials = new NetworkCredential(_settings.Smtp.Username, _settings.Smtp.Password);
        }

        await client.SendMailAsync(message, cancellationToken);
        _logger.LogInformation("Sent email to {To} via SMTP.", toEmail);
    }
}
