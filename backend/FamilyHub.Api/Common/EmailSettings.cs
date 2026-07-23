namespace FamilyHub.Api.Common;

/// <summary>
/// Email configuration, bound from the "Email" section of appsettings. When
/// <see cref="SmtpSettings.Host"/> is empty, invitations are logged instead of sent.
/// </summary>
public class EmailSettings
{
    public string FromName { get; set; } = "FamilyHub";

    public string FromEmail { get; set; } = "no-reply@familyhub.local";

    public SmtpSettings Smtp { get; set; } = new();
}

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool UseSsl { get; set; } = true;
}
